#!/usr/bin/env python3
"""Local static frontend server with a same-origin PHP gateway proxy."""

import http.client
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit


FRONTEND = Path(__file__).resolve().parents[1] / "frontend"
ASSETS = {
    "/": ("index.html", "text/html; charset=utf-8"),
    "/index.html": ("index.html", "text/html; charset=utf-8"),
    "/app.js": ("app.js", "text/javascript; charset=utf-8"),
    "/quote-ui.js": ("quote-ui.js", "text/javascript; charset=utf-8"),
    "/styles.css": ("styles.css", "text/css; charset=utf-8"),
}


def create_server(host="127.0.0.1", port=3000, gateway_url="http://127.0.0.1:8080"):
    gateway = urlsplit(gateway_url)
    if gateway.scheme != "http" or not gateway.hostname or gateway.path not in ("", "/"):
        raise ValueError("GATEWAY_BASE_URL must be an HTTP origin without a path")

    class Handler(BaseHTTPRequestHandler):
        def respond(self, status, body, content_type):
            self.send_response(status)
            self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            if self.command != "HEAD":
                self.wfile.write(body)

        def handle_request(self):
            path = urlsplit(self.path).path
            if path == "/health" or path.startswith("/api/"):
                self.proxy_request()
            elif self.command in ("GET", "HEAD") and path in ASSETS:
                filename, content_type = ASSETS[path]
                self.respond(200, (FRONTEND / filename).read_bytes(), content_type)
            else:
                self.respond(404, b'{"error":"Route not found."}', "application/json")

        def proxy_request(self):
            try:
                length = int(self.headers.get("Content-Length", "0"))
            except ValueError:
                length = -1
            if not 0 <= length <= 1_048_576 or self.headers.get("Transfer-Encoding"):
                self.respond(400, b'{"error":"Invalid request length."}', "application/json")
                return
            body = self.rfile.read(length)
            upstream = http.client.HTTPConnection(gateway.hostname, gateway.port or 80, timeout=20)
            try:
                upstream.request(self.command, self.path, body=body, headers={
                    "Content-Type": self.headers.get("Content-Type", "application/json"),
                    "Accept": "application/json",
                })
                response = upstream.getresponse()
                payload = response.read()
                content_type = response.getheader("Content-Type", "application/json")
                self.respond(response.status, payload, content_type)
            except (OSError, http.client.HTTPException):
                self.respond(502, b'{"error":"PHP gateway is unavailable."}', "application/json")
            finally:
                upstream.close()

        do_GET = handle_request
        do_HEAD = handle_request
        do_POST = handle_request
        do_PUT = handle_request
        do_DELETE = handle_request
        do_OPTIONS = handle_request

    return ThreadingHTTPServer((host, port), Handler)


if __name__ == "__main__":
    port = int(os.getenv("FRONTEND_PORT", "3000"))
    with create_server(port=port, gateway_url=os.getenv("GATEWAY_BASE_URL", "http://127.0.0.1:8080")) as server:
        print(f"Frontend: http://localhost:{port} (API requests go through PHP)", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass
