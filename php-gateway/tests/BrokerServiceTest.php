<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway\Tests;

use MicroInsurTech\Gateway\BrokerService;
use MicroInsurTech\Gateway\HttpClient;
use MicroInsurTech\Gateway\HttpResponse;
use PHPUnit\Framework\TestCase;

final class BrokerServiceTest extends TestCase
{
    private const EmptyQuoteResponseJson = '{"quotes":[]}';
    private const InvalidRiskResponseJson = '{"errors":["Invalid risk"]}';
    private const LeakyServerErrorJson = '{"error":"database password leaked"}';

    public function testRequestQuotesPostsJsonToCoreApi(): void
    {
        $httpClient = new FakeHttpClient(new HttpResponse(200, self::EmptyQuoteResponseJson));
        $service = new BrokerService('http://core-engine.local/', 7, $httpClient);
        $payload = QuotePayloadFactory::validPayload();

        $result = $service->requestQuotes($payload);

        self::assertSame(200, $result['statusCode']);
        self::assertSame(['quotes' => []], $result['body']);
        self::assertSame('http://core-engine.local/api/quotes', $httpClient->url);
        self::assertSame(7, $httpClient->timeoutSeconds);
        self::assertSame('application/json', $httpClient->headers['Accept']);
        self::assertSame('application/json', $httpClient->headers['Content-Type']);
        self::assertSame($payload, json_decode($httpClient->json, true));
    }

    public function testRequestQuotesReturnsUpstreamValidationResponse(): void
    {
        $httpClient = new FakeHttpClient(new HttpResponse(400, self::InvalidRiskResponseJson));
        $service = new BrokerService('http://core-engine.local', 15, $httpClient);

        $result = $service->requestQuotes(QuotePayloadFactory::validPayload());

        self::assertSame(400, $result['statusCode']);
        self::assertSame(['errors' => ['Invalid risk']], $result['body']);
    }

    public function testRequestQuotesConvertsUpstreamServerErrorToSafeGatewayError(): void
    {
        $httpClient = new FakeHttpClient(new HttpResponse(500, self::LeakyServerErrorJson));
        $service = new BrokerService('http://core-engine.local', 15, $httpClient);

        $result = $service->requestQuotes(QuotePayloadFactory::validPayload());

        self::assertSame(502, $result['statusCode']);
        self::assertSame(['error' => 'Core engine failed to generate quotes.'], $result['body']);
    }

    public function testRequestQuotesConvertsNetworkFailureToSafeGatewayError(): void
    {
        putenv('APP_ENV=production');
        $httpClient = new FakeHttpClient(new HttpResponse(0, '', 'Connection refused'));
        $service = new BrokerService('http://core-engine.local', 15, $httpClient);

        $result = $service->requestQuotes(QuotePayloadFactory::validPayload());

        self::assertSame(502, $result['statusCode']);
        self::assertSame(['error' => 'Core engine is unavailable.'], $result['body']);
    }

    public function testRequestQuotesIncludesNetworkFailureDetailInLocalEnvironment(): void
    {
        putenv('APP_ENV=local');
        $httpClient = new FakeHttpClient(new HttpResponse(0, '', 'Connection refused'));
        $service = new BrokerService('http://core-engine.local', 15, $httpClient);

        $result = $service->requestQuotes(QuotePayloadFactory::validPayload());

        self::assertSame(502, $result['statusCode']);
        self::assertSame([
            'error' => 'Core engine is unavailable.',
            'detail' => 'Connection refused',
        ], $result['body']);
    }

    public function testRequestQuotesConvertsInvalidJsonToGatewayError(): void
    {
        $httpClient = new FakeHttpClient(new HttpResponse(200, 'not-json'));
        $service = new BrokerService('http://core-engine.local', 15, $httpClient);

        $result = $service->requestQuotes(QuotePayloadFactory::validPayload());

        self::assertSame(502, $result['statusCode']);
        self::assertSame(['error' => 'Core engine returned an invalid response.'], $result['body']);
    }

}

final class FakeHttpClient implements HttpClient
{
    public string $url = '';

    public string $json = '';

    /** @var array<string, string> */
    public array $headers = [];

    public int $timeoutSeconds = 0;

    public function __construct(private readonly HttpResponse $response)
    {
    }

    public function postJson(string $url, string $json, array $headers, int $timeoutSeconds): HttpResponse
    {
        $this->url = $url;
        $this->json = $json;
        $this->headers = $headers;
        $this->timeoutSeconds = $timeoutSeconds;

        return $this->response;
    }
}
