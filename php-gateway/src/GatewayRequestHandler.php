<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway;

final class GatewayRequestHandler
{
    public function __construct(
        private readonly QuoteRequestHandler $quoteRequestHandler,
        private readonly BrokerService $brokerService,
    ) {
    }

    /**
     * @return array{statusCode:int, body:array<string, mixed>}
     */
    public function handle(string $method, string $path, string $rawBody): array
    {
        if ($method === 'GET' && $path === '/health') {
            return ['statusCode' => 200, 'body' => ['status' => 'PHP gateway running']];
        }

        if ($path !== '/api/quotes') {
            return ['statusCode' => 404, 'body' => ['error' => 'Route not found.']];
        }

        if ($method !== 'POST') {
            return ['statusCode' => 405, 'body' => ['error' => 'Method not allowed.']];
        }

        $decodeResult = $this->decodeJsonRequest($rawBody);

        if ($decodeResult['error'] !== null) {
            return ['statusCode' => 400, 'body' => ['errors' => [$decodeResult['error']]]];
        }

        $decodedBody = $decodeResult['payload'];

        $validationErrors = $this->quoteRequestHandler->validate($decodedBody);

        if ($validationErrors !== []) {
            return ['statusCode' => 400, 'body' => ['errors' => $validationErrors]];
        }

        return $this->brokerService->requestQuotes($this->quoteRequestHandler->normalize($decodedBody));
    }

    /**
     * @return array{payload:array<string, mixed>, error:?string}
     */
    private function decodeJsonRequest(string $rawBody): array
    {
        if (trim($rawBody) === '') {
            return ['payload' => [], 'error' => 'Request body is required.'];
        }

        $decodedBody = json_decode($rawBody, true);

        if (!is_array($decodedBody)) {
            return ['payload' => [], 'error' => 'Request body must be valid JSON.'];
        }

        return ['payload' => $decodedBody, 'error' => null];
    }
}
