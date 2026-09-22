<?php

declare(strict_types=1);

require_once __DIR__ . '/../src/BrokerService.php';

use MicroInsurTech\Gateway\BrokerService;

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
$validationErrors = validateQuoteRequest($payload);

if ($validationErrors !== []) {
    respond(400, ['errors' => $validationErrors]);
}

$service = new BrokerService(
    getenv('BROKER_CORE_API_BASE_URL') ?: 'http://localhost:5000',
    max(1, (int) (getenv('BROKER_CORE_API_TIMEOUT_SECONDS') ?: 15)),
);

$result = $service->requestQuotes(normalizeQuoteRequest($payload));
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
 * @param array<string, mixed> $payload
 * @return list<string>
 */
function validateQuoteRequest(array $payload): array
{
    $errors = [];
    $client = $payload['client'] ?? null;
    $property = $payload['property'] ?? null;

    if (!is_array($client)) {
        $errors[] = 'Client details are required.';
    } else {
        if (!hasText($client['firstName'] ?? null)) {
            $errors[] = 'Client first name is required.';
        }

        if (!hasText($client['lastName'] ?? null)) {
            $errors[] = 'Client last name is required.';
        }

        if (!hasText($client['email'] ?? null) || filter_var($client['email'], FILTER_VALIDATE_EMAIL) === false) {
            $errors[] = 'A valid client email is required.';
        }
    }

    if (!is_array($property)) {
        $errors[] = 'Property details are required.';
        return $errors;
    }

    if (!hasText($property['postcode'] ?? null)) {
        $errors[] = 'Property postcode is required.';
    }

    if (!isset($property['yearBuilt']) || filter_var($property['yearBuilt'], FILTER_VALIDATE_INT) === false) {
        $errors[] = 'Property year built must be an integer.';
    }

    if (!isset($property['rebuildCost']) || !is_numeric($property['rebuildCost']) || (float) $property['rebuildCost'] <= 0) {
        $errors[] = 'Property rebuild cost must be greater than zero.';
    }

    if (!isset($property['isUnoccupied']) || !is_bool($property['isUnoccupied'])) {
        $errors[] = 'Property unoccupied status must be boolean.';
    }

    return $errors;
}

function hasText(mixed $value): bool
{
    return is_string($value) && trim($value) !== '';
}

/**
 * @param array<string, mixed> $payload
 * @return array<string, mixed>
 */
function normalizeQuoteRequest(array $payload): array
{
    /** @var array<string, mixed> $client */
    $client = $payload['client'];
    /** @var array<string, mixed> $property */
    $property = $payload['property'];

    return [
        'client' => [
            'firstName' => trim((string) $client['firstName']),
            'lastName' => trim((string) $client['lastName']),
            'email' => trim((string) $client['email']),
        ],
        'property' => [
            'postcode' => trim((string) $property['postcode']),
            'yearBuilt' => (int) $property['yearBuilt'],
            'rebuildCost' => (float) $property['rebuildCost'],
            'isUnoccupied' => (bool) $property['isUnoccupied'],
        ],
    ];
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
