<?php

declare(strict_types=1);

require_once __DIR__ . '/../src/BrokerService.php';
require_once __DIR__ . '/../src/HttpClient.php';
require_once __DIR__ . '/../src/CurlHttpClient.php';
require_once __DIR__ . '/../src/GatewayRequestHandler.php';
require_once __DIR__ . '/../src/HttpResponse.php';
require_once __DIR__ . '/../src/QuoteRequestHandler.php';

use MicroInsurTech\Gateway\BrokerService;
use MicroInsurTech\Gateway\GatewayRequestHandler;
use MicroInsurTech\Gateway\QuoteRequestHandler;

header('Content-Type: application/json');

$method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
$path = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH) ?: '/';

$service = new BrokerService(
    getenv('BROKER_CORE_API_BASE_URL') ?: 'http://localhost:5000',
    max(1, (int) (getenv('BROKER_CORE_API_TIMEOUT_SECONDS') ?: 15)),
);
$handler = new GatewayRequestHandler(new QuoteRequestHandler(), $service);
$rawBody = file_get_contents('php://input') ?: '';

$quoteGatewayResponse = $handler->handle($method, $path, $rawBody);
respond($quoteGatewayResponse['statusCode'], $quoteGatewayResponse['body']);

/**
 * @param array<string, mixed> $body
 */
function respond(int $statusCode, array $body): never
{
    http_response_code($statusCode);
    echo json_encode($body, JSON_UNESCAPED_SLASHES) ?: '{"error":"Response could not be encoded."}';
    exit;
}
