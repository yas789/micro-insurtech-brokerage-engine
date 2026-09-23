<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway;

final class BrokerService
{
    public function __construct(
        private readonly string $coreApiBaseUrl,
        private readonly int $timeoutSeconds = 15,
        private readonly ?HttpClient $httpClient = null,
    ) {
    }

    /**
     * @param array<string, mixed> $payload
     * @return array{statusCode:int, body:array<string, mixed>}
     */
    public function requestQuotes(array $payload): array
    {
        $quoteEndpointUrl = rtrim($this->coreApiBaseUrl, '/') . '/api/quotes';
        $encodedPayload = json_encode($payload);

        if ($encodedPayload === false) {
            return $this->gatewayError('Quote request could not be encoded.', 500);
        }

        $httpClient = $this->httpClient ?? new CurlHttpClient();
        $coreResponse = $httpClient->postJson($quoteEndpointUrl, $encodedPayload, [
            'Accept' => 'application/json',
            'Content-Type' => 'application/json',
        ], $this->timeoutSeconds);

        if ($coreResponse->error !== null) {
            return $this->gatewayError('Core engine is unavailable.', 502, $coreResponse->error);
        }

        $decodedResponse = json_decode($coreResponse->body, true);

        if (!is_array($decodedResponse)) {
            return $this->gatewayError('Core engine returned an invalid response.', 502);
        }

        if ($coreResponse->statusCode >= 500) {
            return $this->gatewayError('Core engine failed to generate quotes.', 502);
        }

        return [
            'statusCode' => $coreResponse->statusCode > 0 ? $coreResponse->statusCode : 502,
            'body' => $decodedResponse,
        ];
    }

    /**
     * @return array{statusCode:int, body:array<string, mixed>}
     */
    private function gatewayError(string $message, int $statusCode, ?string $detail = null): array
    {
        $body = ['error' => $message];

        if ($detail !== null && getenv('APP_ENV') === 'local') {
            $body['detail'] = $detail;
        }

        return [
            'statusCode' => $statusCode,
            'body' => $body,
        ];
    }
}
