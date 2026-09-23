<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway\Tests;

final class QuotePayloadFactory
{
    /**
     * @return array<string, mixed>
     */
    public static function validPayload(): array
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
