<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway;

final class CurlHttpClient implements HttpClient
{
    public function postJson(string $url, string $json, array $headers, int $timeoutSeconds): HttpResponse
    {
        $handle = curl_init($url);

        if ($handle === false) {
            return new HttpResponse(0, '', 'Gateway HTTP client could not be initialized.');
        }

        $formattedHeaders = [];

        foreach ($headers as $name => $value) {
            $formattedHeaders[] = $name . ': ' . $value;
        }

        curl_setopt_array($handle, [
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => $json,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => false,
            CURLOPT_TIMEOUT => $timeoutSeconds,
            CURLOPT_HTTPHEADER => $formattedHeaders,
        ]);

        $rawResponse = curl_exec($handle);
        $error = curl_error($handle);
        $statusCode = (int) curl_getinfo($handle, CURLINFO_RESPONSE_CODE);

        curl_close($handle);

        if ($rawResponse === false) {
            return new HttpResponse($statusCode, '', $error);
        }

        return new HttpResponse($statusCode, $rawResponse);
    }
}
