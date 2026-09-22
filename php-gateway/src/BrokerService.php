<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway;

final class BrokerService
{
    public function __construct(
        private readonly string $coreApiBaseUrl,
        private readonly int $timeoutSeconds = 15,
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

        $handle = curl_init($url);

        if ($handle === false) {
            return $this->gatewayError('Gateway HTTP client could not be initialized.', 500);
        }

        curl_setopt_array($handle, [
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => $encodedPayload,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => false,
            CURLOPT_TIMEOUT => $this->timeoutSeconds,
            CURLOPT_HTTPHEADER => [
                'Accept: application/json',
                'Content-Type: application/json',
            ],
        ]);

        $rawResponse = curl_exec($handle);
        $curlError = curl_error($handle);
        $statusCode = (int) curl_getinfo($handle, CURLINFO_RESPONSE_CODE);

        curl_close($handle);

        if ($rawResponse === false) {
            return $this->gatewayError('Core engine is unavailable.', 502, $curlError);
        }

        $decodedResponse = json_decode($rawResponse, true);

        if (!is_array($decodedResponse)) {
            return $this->gatewayError('Core engine returned an invalid response.', 502);
        }

        if ($statusCode >= 500) {
            return $this->gatewayError('Core engine failed to generate quotes.', 502);
        }

        return [
            'statusCode' => $statusCode > 0 ? $statusCode : 502,
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
