<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway\Tests;

use MicroInsurTech\Gateway\BrokerService;
use MicroInsurTech\Gateway\GatewayRequestHandler;
use MicroInsurTech\Gateway\HttpClient;
use MicroInsurTech\Gateway\HttpResponse;
use MicroInsurTech\Gateway\QuoteRequestHandler;
use PHPUnit\Framework\TestCase;

final class GatewayRequestHandlerTest extends TestCase
{
    public function testHandleReturnsHealthResponse(): void
    {
        $handler = $this->createHandler();

        $response = $handler->handle('GET', '/health', '');

        self::assertSame(200, $response['statusCode']);
        self::assertSame(['status' => 'PHP gateway running'], $response['body']);
    }

    public function testHandleReturnsNotFoundForUnknownRoute(): void
    {
        $handler = $this->createHandler();

        $response = $handler->handle('GET', '/missing', '');

        self::assertSame(404, $response['statusCode']);
        self::assertSame(['error' => 'Route not found.'], $response['body']);
    }

    public function testHandleReturnsMethodNotAllowedForQuoteGet(): void
    {
        $handler = $this->createHandler();

        $response = $handler->handle('GET', '/api/quotes', '');

        self::assertSame(405, $response['statusCode']);
        self::assertSame(['error' => 'Method not allowed.'], $response['body']);
    }

    public function testHandleRejectsEmptyQuoteRequestBody(): void
    {
        $handler = $this->createHandler();

        $response = $handler->handle('POST', '/api/quotes', '');

        self::assertSame(400, $response['statusCode']);
        self::assertSame(['errors' => ['Request body is required.']], $response['body']);
    }

    public function testHandleRejectsInvalidJsonQuoteRequestBody(): void
    {
        $handler = $this->createHandler();

        $response = $handler->handle('POST', '/api/quotes', 'not-json');

        self::assertSame(400, $response['statusCode']);
        self::assertSame(['errors' => ['Request body must be valid JSON.']], $response['body']);
    }

    public function testHandleReturnsValidationErrorsForInvalidQuotePayload(): void
    {
        $handler = $this->createHandler();
        $payload = QuotePayloadFactory::validPayload();
        unset($payload['client']);

        $response = $handler->handle('POST', '/api/quotes', json_encode($payload) ?: '');

        self::assertSame(400, $response['statusCode']);
        self::assertSame(['errors' => ['Client details are required.']], $response['body']);
    }

    public function testHandleForwardsValidQuoteRequest(): void
    {
        $httpClient = new GatewayFakeHttpClient(new HttpResponse(200, '{"quotes":[]}'));
        $handler = $this->createHandler($httpClient);

        $response = $handler->handle('POST', '/api/quotes', json_encode(QuotePayloadFactory::validPayload()) ?: '');

        self::assertSame(200, $response['statusCode']);
        self::assertSame(['quotes' => []], $response['body']);
        self::assertSame('http://core-engine.local/api/quotes', $httpClient->url);
    }

    /** @dataProvider rebuildCostProvider */
    public function testHandleChecksRebuildCostBeforeForwarding(float|string $cost, ?string $error): void
    {
        $client = new GatewayFakeHttpClient(new HttpResponse(200, '{"quotes":[]}'));
        $handler = $this->createHandler($client);
        $payload = QuotePayloadFactory::validPayload();
        $payload['property']['rebuildCost'] = $cost;

        $response = $handler->handle('POST', '/api/quotes', json_encode($payload, JSON_THROW_ON_ERROR));

        if ($error === null) {
            self::assertSame(200, $response['statusCode']);
            self::assertSame('http://core-engine.local/api/quotes', $client->url);
        } else {
            self::assertSame(400, $response['statusCode']);
            self::assertSame(['errors' => [$error]], $response['body']);
            self::assertSame('', $client->url);
        }
    }

    public static function rebuildCostProvider(): iterable
    {
        yield 'minimum' => [0.01, null];
        yield 'maximum' => [99999999.99, null];
        yield 'first overflowing penny' => [100000000.00, 'Property rebuild cost must be 99999999.99 or less.'];
        yield 'rounding overflow' => [99999999.995, 'Property rebuild cost must be 99999999.99 or less.'];
        yield 'fractional penny' => [750000.001, 'Property rebuild cost must have at most two decimal places.'];
        yield 'rounds to zero' => [0.001, 'Property rebuild cost must have at most two decimal places.'];
        yield 'non-finite numeric string' => ['1e309', 'Property rebuild cost must be 99999999.99 or less.'];
    }

    private function createHandler(?GatewayFakeHttpClient $httpClient = null): GatewayRequestHandler
    {
        return new GatewayRequestHandler(
            new QuoteRequestHandler(),
            new BrokerService('http://core-engine.local', 15, $httpClient ?? new GatewayFakeHttpClient(new HttpResponse(200, '{"quotes":[]}'))),
        );
    }
}

final class GatewayFakeHttpClient implements HttpClient
{
    public string $url = '';

    public function __construct(private readonly HttpResponse $response)
    {
    }

    public function postJson(string $url, string $json, array $headers, int $timeoutSeconds): HttpResponse
    {
        $this->url = $url;

        return $this->response;
    }
}
