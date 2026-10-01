#!/usr/bin/env python3
"""Run the PHP gateway and frontend proxy together; stop both on Ctrl-C."""

import json
import os
from pathlib import Path
import shutil
import signal
import socket
import subprocess
import sys
import time
from urllib.error import URLError
from urllib.request import urlopen


ROOT = Path(__file__).resolve().parents[1]


def wait_for_gateway(process, port):
    for _ in range(50):
        if process.poll() is not None:
            raise RuntimeError("PHP gateway exited during startup")
        try:
            with urlopen(f"http://127.0.0.1:{port}/health", timeout=1) as response:
                if json.load(response) == {"status": "PHP gateway running"}:
                    return
        except (OSError, URLError, ValueError):
            pass
        time.sleep(0.1)
    raise RuntimeError("PHP gateway did not become healthy")


def main():
    if not shutil.which("php"):
        raise RuntimeError("PHP 8.1+ with the curl extension is required on PATH")
    subprocess.run([
        "php", "-r", "exit(PHP_VERSION_ID >= 80100 && extension_loaded('curl') ? 0 : 1);",
    ], check=True)
    gateway_port = int(os.getenv("GATEWAY_PORT", "8080"))
    frontend_port = int(os.getenv("FRONTEND_PORT", "3000"))
    if gateway_port == frontend_port:
        raise RuntimeError("GATEWAY_PORT and FRONTEND_PORT must differ")
    for port in (gateway_port, frontend_port):
        with socket.socket() as probe:
            probe.bind(("127.0.0.1", port))

    children = []
    try:
        gateway = subprocess.Popen([
            "php", "-S", f"127.0.0.1:{gateway_port}",
            "-t", str(ROOT / "php-gateway/public"),
            str(ROOT / "php-gateway/public/index.php"),
        ], cwd=ROOT)
        children.append(gateway)
        wait_for_gateway(gateway, gateway_port)
        environment = dict(os.environ, GATEWAY_BASE_URL=f"http://127.0.0.1:{gateway_port}")
        children.append(subprocess.Popen([
            sys.executable, str(ROOT / "scripts/serve-frontend.py"),
        ], cwd=ROOT, env=environment))
        print(f"Open http://localhost:{frontend_port}; Ctrl-C stops both web processes.", flush=True)
        while all(child.poll() is None for child in children):
            time.sleep(0.2)
        raise RuntimeError("A web process exited; stopping the local web stack")
    finally:
        for child in reversed(children):
            if child.poll() is None:
                child.terminate()
            try:
                child.wait(timeout=5)
            except subprocess.TimeoutExpired:
                child.kill()
                child.wait()


def stop(*_):
    raise KeyboardInterrupt


if __name__ == "__main__":
    signal.signal(signal.SIGTERM, stop)
    try:
        main()
    except KeyboardInterrupt:
        pass
    except (RuntimeError, OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"Local web startup failed: {error}", file=sys.stderr)
        sys.exit(1)
