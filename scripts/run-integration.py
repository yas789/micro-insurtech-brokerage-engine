#!/usr/bin/env python3
"""Start the real quote stack against SQL Server and run integration checks."""

from contextlib import ExitStack
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


def stop_process(process):
    if process.poll() is None:
        process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait()


def wait_for_health(url, expected, processes):
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        if any(process.poll() is not None for process in processes):
            raise RuntimeError("An integration service exited; see logs/integration")
        try:
            with urlopen(url, timeout=1) as response:
                if json.load(response) == {"status": expected}:
                    return
        except (OSError, URLError, ValueError):
            pass
        time.sleep(0.2)
    raise RuntimeError(f"Service did not become ready: {url}; see logs/integration")


def main():
    for executable in ("dotnet", "php", "docker"):
        if not shutil.which(executable):
            raise RuntimeError(f"{executable} is required on PATH")
    # Fixed, documented integration ports prevent connecting to an unrelated service.
    for port in (3000, 5000, 8080, 8081, 5099):
        with socket.socket() as probe:
            probe.bind(("127.0.0.1", port))
    environment = dict(os.environ)
    environment.setdefault("SQLSERVER_DATABASE_NAME", "MicroInsurTechVerification")
    environment.setdefault("SQLSERVER_SA_PASSWORD", "Change_this_password_123!")
    environment.update({
        "ASPNETCORE_ENVIRONMENT": "Production",
        "ASPNETCORE_URLS": "http://127.0.0.1:5000",
        "POSTCODES_API_BASE_URL": "http://127.0.0.1:5099",
        "POSTCODE_FIXTURE_PORT": "5099",
        "BROKER_CORE_API_BASE_URL": "http://127.0.0.1:5000",
        "APP_ENV": "test",
        "GATEWAY_PORT": "8080",
        "FRONTEND_PORT": "3000",
        "FRONTEND_BASE_URL": "http://127.0.0.1:3000",
    })
    password = environment["SQLSERVER_SA_PASSWORD"].replace('"', '""')
    environment["ConnectionStrings__BrokerDatabase"] = (
        f"Server=127.0.0.1,{environment.get('SQLSERVER_HOST_PORT', '1433')};"
        f"Database={environment['SQLSERVER_DATABASE_NAME']};User Id=sa;"
        f'Password="{password}";TrustServerCertificate=True;'
    )

    def run(*command, cwd=ROOT):
        subprocess.run(command, cwd=cwd, env=environment, check=True)

    run("sh", "scripts/start-local-sqlserver.sh")
    run("sh", "scripts/apply-local-schema.sh")
    run("dotnet", "build", "core-engine/MicroInsurTech.CoreEngine.csproj", "--configuration", "Release")

    logs = ROOT / "logs/integration"
    logs.mkdir(parents=True, exist_ok=True)
    with ExitStack() as stack:
        processes = []

        def start(name, command, overrides=None):
            output = stack.enter_context((logs / f"{name}.log").open("w"))
            process = subprocess.Popen(command, cwd=ROOT, env={**environment, **(overrides or {})},
                                       stdout=output, stderr=subprocess.STDOUT)
            stack.callback(stop_process, process)
            processes.append(process)

        start("postcodes", [sys.executable, "scripts/fixtures/postcodes.py"])
        start("core", ["dotnet", "core-engine/bin/Release/net8.0/MicroInsurTech.CoreEngine.dll"])
        start("web", [sys.executable, "scripts/start-local-web.py"])
        # Port 1 is deliberately unavailable; no mock core response is used.
        start("unavailable-gateway", ["php", "-S", "127.0.0.1:8081", "-t", "php-gateway/public", "php-gateway/public/index.php"],
              {"BROKER_CORE_API_BASE_URL": "http://127.0.0.1:1", "BROKER_CORE_API_TIMEOUT_SECONDS": "1"})
        for url, expected in (
            ("http://127.0.0.1:5099/health", "Postcode fixture running"),
            ("http://127.0.0.1:5000/health", "Core engine running"),
            ("http://127.0.0.1:3000/health", "PHP gateway running"),
            ("http://127.0.0.1:8081/health", "PHP gateway running"),
        ):
            wait_for_health(url, expected, processes)
        run(sys.executable, "scripts/verify-quote-journey.py", "--postcode-fixture",
            "--unavailable-gateway-url", "http://127.0.0.1:8081")
    print("Integration checks passed. Services stopped; SQL container and sample records are preserved.")


def interrupt(*_):
    raise KeyboardInterrupt


if __name__ == "__main__":
    signal.signal(signal.SIGTERM, interrupt)
    try:
        main()
    except KeyboardInterrupt:
        sys.exit(130)
    except (RuntimeError, OSError, ValueError, subprocess.SubprocessError) as error:
        print(f"Integration verification failed: {error}", file=sys.stderr)
        sys.exit(1)
