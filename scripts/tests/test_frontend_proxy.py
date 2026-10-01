import http.client
import importlib.util
import threading
import unittest
from contextlib import contextmanager
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


spec = importlib.util.spec_from_file_location("frontend_server", Path(__file__).parents[1] / "serve-frontend.py")
frontend_server = importlib.util.module_from_spec(spec)
spec.loader.exec_module(frontend_server)


@contextmanager
def running(server):
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        yield server.server_address[1]
    finally:
        server.shutdown()
        server.server_close()
        thread.join()


def request(port, method, path, body=None):
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=5)
    try:
        connection.request(method, path, body, {"Content-Type": "application/json"})
        response = connection.getresponse()
        return response.status, response.getheader("Content-Type"), response.read()
    finally:
        connection.close()


class FrontendProxyTests(unittest.TestCase):
    def test_serves_only_browser_assets(self):
        with running(frontend_server.create_server(port=0)) as port:
            status, content_type, body = request(port, "GET", "/")
            self.assertEqual(200, status)
            self.assertIn("text/html", content_type)
            self.assertIn(b'id="quote-form"', body)
            self.assertIn("javascript", request(port, "GET", "/quote-ui.js?v=1")[1])
            for path in ("/../.env", "/package.json", "/tests/app.test.js"):
                self.assertEqual(404, request(port, "GET", path)[0])

    def test_preserves_gateway_request_and_validation_response(self):
        received = []

        class Gateway(BaseHTTPRequestHandler):
            def do_POST(self):
                received.append((self.path, self.rfile.read(int(self.headers["Content-Length"]))))
                self.send_response(400)
                self.send_header("Content-Type", "application/json")
                self.end_headers()
                self.wfile.write(b'{"errors":["Client details are required."]}')

        with running(ThreadingHTTPServer(("127.0.0.1", 0), Gateway)) as gateway_port:
            with running(frontend_server.create_server(port=0, gateway_url=f"http://127.0.0.1:{gateway_port}")) as port:
                status, content_type, body = request(port, "POST", "/api/quotes", b"{}")
                self.assertEqual(400, status)
                self.assertEqual("application/json", content_type)
                self.assertIn(b"Client details are required", body)
        self.assertEqual([("/api/quotes", b"{}")], received)

    def test_returns_json_when_gateway_is_unavailable(self):
        class DisconnectedGateway(BaseHTTPRequestHandler):
            def do_POST(self):
                self.close_connection = True

        with running(ThreadingHTTPServer(("127.0.0.1", 0), DisconnectedGateway)) as gateway_port:
            url = f"http://127.0.0.1:{gateway_port}"
            with running(frontend_server.create_server(port=0, gateway_url=url)) as port:
                status, _, body = request(port, "POST", "/api/quotes", b"{}")
                self.assertEqual(502, status)
                self.assertEqual(b'{"error":"PHP gateway is unavailable."}', body)


if __name__ == "__main__":
    unittest.main()
