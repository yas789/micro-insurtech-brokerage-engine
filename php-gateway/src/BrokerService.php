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
        $url = rtrim($this->coreApiBaseUrl, '/') . '/api/quotes';
        $encodedPayload = json_encode($payload);

        if ($encodedPayload === false) {
            return $this->gatewayError('Quote request could not be encoded.', 500);
        }

        $client = $this->httpClient ?? new CurlHttpClient();
        $response = $client->postJson($url, $encodedPayload, [
            'Accept' => 'application/json',
            'Content-Type' => 'application/json',
        ], $this->timeoutSeconds);

        if ($response->error !== null) {
            return $this->gatewayError('Core engine is unavailable.', 502, $response->error);
        }

        $decodedResponse = json_decode($response->body, true);

        if (!is_array($decodedResponse)) {
            return $this->gatewayError('Core engine returned an invalid response.', 502);
        }

        if ($response->statusCode >= 500) {
            return $this->gatewayError('Core engine failed to generate quotes.', 502);
        }

        return [
            'statusCode' => $response->statusCode > 0 ? $response->statusCode : 502,
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
