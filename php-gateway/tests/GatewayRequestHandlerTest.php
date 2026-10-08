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

    /** @dataProvider textLengthProvider */
    public function testHandleChecksTextLengthsBeforeForwarding(string $section, string $field, string $value, ?string $error): void
    {
        $client = new GatewayFakeHttpClient(new HttpResponse(200, '{"quotes":[]}'));
        $handler = $this->createHandler($client);
        $payload = QuotePayloadFactory::validPayload();
        $payload[$section][$field] = $value;

        $response = $handler->handle('POST', '/api/quotes', json_encode($payload, JSON_THROW_ON_ERROR));

        self::assertSame($error === null ? 200 : 400, $response['statusCode']);
        self::assertSame($error === null ? 'http://core-engine.local/api/quotes' : '', $client->url);
        if ($error !== null) {
            self::assertSame(['errors' => [$error]], $response['body']);
        }
    }

    public static function textLengthProvider(): iterable
    {
        foreach (['firstName' => 'first name', 'lastName' => 'last name'] as $field => $label) {
            yield "$field boundary" => ['client', $field, ' ' . str_repeat('A', 100) . ' ', null];
            yield "$field overflow" => ['client', $field, str_repeat('A', 101), "Client $label must be 100 characters or fewer."];
        }
        yield 'accented boundary' => ['client', 'firstName', str_repeat('é', 100), null];
        yield 'accented overflow' => ['client', 'firstName', str_repeat('é', 101), 'Client first name must be 100 characters or fewer.'];
        yield 'supplementary boundary' => ['client', 'lastName', str_repeat('😀', 50), null];
        yield 'supplementary overflow' => ['client', 'lastName', str_repeat('😀', 50) . 'A', 'Client last name must be 100 characters or fewer.'];
        $email = str_repeat('a', 64) . '@' . str_repeat('b', 63) . '.' . str_repeat('c', 63) . '.' . str_repeat('d', 61);
        yield 'email boundary' => ['client', 'email', $email, null];
        yield 'email overflow' => ['client', 'email', $email . 'd', 'Client email must be 254 characters or fewer.'];
        yield 'postcode boundary' => ['property', 'postcode', ' ' . str_repeat('A', 16) . ' ', null];
        yield 'postcode overflow' => ['property', 'postcode', str_repeat('A', 17), 'Property postcode must be 16 characters or fewer.'];
    }

    /** @dataProvider yearBuiltProvider */
    public function testHandleChecksYearBuiltBeforeForwarding(int $year, bool $valid): void
    {
        $client = new GatewayFakeHttpClient(new HttpResponse(200, '{"quotes":[]}'));
        $handler = $this->createHandler($client);
        $payload = QuotePayloadFactory::validPayload();
        $payload['property']['yearBuilt'] = $year;

        $response = $handler->handle('POST', '/api/quotes', json_encode($payload, JSON_THROW_ON_ERROR));

        self::assertSame($valid ? 200 : 400, $response['statusCode']);
        self::assertSame($valid ? 'http://core-engine.local/api/quotes' : '', $client->url);
        if (!$valid) {
            self::assertSame(['errors' => ['Property year built must be between 1500 and 2100.']], $response['body']);
        }
    }

    public static function yearBuiltProvider(): iterable
    {
        yield 'lower boundary' => [1500, true];
        yield 'upper boundary' => [2100, true];
        yield 'too old' => [1499, false];
        yield 'too new' => [2101, false];
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
