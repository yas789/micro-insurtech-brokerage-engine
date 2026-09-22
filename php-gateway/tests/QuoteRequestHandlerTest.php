<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway\Tests;

use MicroInsurTech\Gateway\QuoteRequestHandler;
use PHPUnit\Framework\TestCase;

final class QuoteRequestHandlerTest extends TestCase
{
    public function testValidateAcceptsValidQuoteRequest(): void
    {
        $handler = new QuoteRequestHandler();

        self::assertSame([], $handler->validate($this->validPayload()));
    }

    public function testNormalizeTrimsTextAndCastsNumbers(): void
    {
        $handler = new QuoteRequestHandler();
        $payload = [
            'client' => [
                'firstName' => ' Jane ',
                'lastName' => ' Broker ',
                'email' => ' jane@example.com ',
            ],
            'property' => [
                'postcode' => ' SW1A 1AA ',
                'yearBuilt' => '1910',
                'rebuildCost' => '750000.50',
                'isUnoccupied' => true,
            ],
        ];

        self::assertSame([
            'client' => [
                'firstName' => 'Jane',
                'lastName' => 'Broker',
                'email' => 'jane@example.com',
            ],
            'property' => [
                'postcode' => 'SW1A 1AA',
                'yearBuilt' => 1910,
                'rebuildCost' => 750000.50,
                'isUnoccupied' => true,
            ],
        ], $handler->normalize($payload));
    }

    public function testValidateRejectsMissingClient(): void
    {
        $handler = new QuoteRequestHandler();
        $payload = $this->validPayload();
        unset($payload['client']);

        self::assertContains('Client details are required.', $handler->validate($payload));
    }

    public function testValidateRejectsMissingProperty(): void
    {
        $handler = new QuoteRequestHandler();
        $payload = $this->validPayload();
        unset($payload['property']);

        self::assertContains('Property details are required.', $handler->validate($payload));
    }

    /**
     * @dataProvider invalidFieldProvider
     * @param array<string, mixed> $override
     */
    public function testValidateRejectsInvalidFields(array $override, string $expectedError): void
    {
        $handler = new QuoteRequestHandler();
        $payload = array_replace_recursive($this->validPayload(), $override);

        self::assertContains($expectedError, $handler->validate($payload));
    }

    /**
     * @return iterable<string, array{array<string, mixed>, string}>
     */
    public static function invalidFieldProvider(): iterable
    {
        yield 'first name' => [['client' => ['firstName' => '']], 'Client first name is required.'];
        yield 'last name' => [['client' => ['lastName' => '']], 'Client last name is required.'];
        yield 'email' => [['client' => ['email' => 'not-an-email']], 'A valid client email is required.'];
        yield 'postcode' => [['property' => ['postcode' => '']], 'Property postcode is required.'];
        yield 'year built' => [['property' => ['yearBuilt' => 'old']], 'Property year built must be an integer.'];
        yield 'rebuild cost' => [['property' => ['rebuildCost' => 0]], 'Property rebuild cost must be greater than zero.'];
        yield 'unoccupied' => [['property' => ['isUnoccupied' => 'false']], 'Property unoccupied status must be boolean.'];
    }

    /**
     * @return array<string, mixed>
     */
    private function validPayload(): array
    {
        return [
            'client' => [
                'firstName' => 'Jane',
                'lastName' => 'Broker',
                'email' => 'jane@example.com',
            ],
            'property' => [
                'postcode' => 'SW1A 1AA',
                'yearBuilt' => 1910,
                'rebuildCost' => 750000.00,
                'isUnoccupied' => false,
            ],
        ];
    }
}
