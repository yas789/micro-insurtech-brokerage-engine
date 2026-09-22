<?php

declare(strict_types=1);

require_once __DIR__ . '/../src/BrokerService.php';
require_once __DIR__ . '/../src/CurlHttpClient.php';
require_once __DIR__ . '/../src/HttpClient.php';
require_once __DIR__ . '/../src/HttpResponse.php';
require_once __DIR__ . '/../src/QuoteRequestHandler.php';

use MicroInsurTech\Gateway\BrokerService;
use MicroInsurTech\Gateway\QuoteRequestHandler;

header('Content-Type: application/json');

$method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
$path = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH) ?: '/';

if ($method === 'GET' && $path === '/health') {
    respond(200, ['status' => 'PHP gateway running']);
}

if ($path !== '/api/quotes') {
    respond(404, ['error' => 'Route not found.']);
}

if ($method !== 'POST') {
    respond(405, ['error' => 'Method not allowed.']);
}

$payload = decodeJsonRequest();
$handler = new QuoteRequestHandler();
$validationErrors = $handler->validate($payload);

if ($validationErrors !== []) {
    respond(400, ['errors' => $validationErrors]);
}

$service = new BrokerService(
    getenv('BROKER_CORE_API_BASE_URL') ?: 'http://localhost:5000',
    max(1, (int) (getenv('BROKER_CORE_API_TIMEOUT_SECONDS') ?: 15)),
);

$result = $service->requestQuotes($handler->normalize($payload));
respond($result['statusCode'], $result['body']);

/**
 * @return array<string, mixed>
 */
function decodeJsonRequest(): array
{
    $rawBody = file_get_contents('php://input');

    if ($rawBody === false || trim($rawBody) === '') {
        respond(400, ['errors' => ['Request body is required.']]);
    }

    $decodedBody = json_decode($rawBody, true);

    if (!is_array($decodedBody)) {
        respond(400, ['errors' => ['Request body must be valid JSON.']]);
    }

    return $decodedBody;
}

/**
 * @param array<string, mixed> $body
 */
function respond(int $statusCode, array $body): never
{
    http_response_code($statusCode);
    echo json_encode($body, JSON_UNESCAPED_SLASHES) ?: '{"error":"Response could not be encoded."}';
    exit;
}
