#!/usr/bin/env python3
"""Deterministic postcodes.io substitute for integration tests only."""

import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote_plus, urlsplit


class PostcodeHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        path = unquote_plus(urlsplit(self.path).path)
        if path == "/health":
            status, body = 200, {"status": "Postcode fixture running"}
        elif path == "/postcodes/SW1A 1AA":
            status, body = 200, {"result": {"region": "London"}}
        elif path == "/postcodes/ZZ99 9ZZ":
            status, body = 503, {"error": "Simulated postcode outage"}
        else:
            status, body = 404, {"error": "Postcode not found"}
        encoded = json.dumps(body).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(encoded)))
        self.end_headers()
        self.wfile.write(encoded)


if __name__ == "__main__":
    with ThreadingHTTPServer(("127.0.0.1", int(os.getenv("POSTCODE_FIXTURE_PORT", "5099"))), PostcodeHandler) as server:
        server.serve_forever()
