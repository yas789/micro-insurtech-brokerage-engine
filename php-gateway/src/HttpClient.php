<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway;

interface HttpClient
{
    /** @param array<string, string> $headers */
    public function get(string $url, array $headers, int $timeoutSeconds): HttpResponse;

    /**
     * @param array<string, string> $headers
     */
    public function postJson(string $url, string $json, array $headers, int $timeoutSeconds): HttpResponse;
}
