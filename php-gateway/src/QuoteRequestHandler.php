<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway;

final class QuoteRequestHandler
{
    /**
     * @param array<string, mixed> $payload
     * @return list<string>
     */
    public function validate(array $payload): array
    {
        $errors = [];
        $clientPayload = $payload['client'] ?? null;
        $propertyPayload = $payload['property'] ?? null;

        if (!is_array($clientPayload)) {
            $errors[] = 'Client details are required.';
        } else {
            if (!$this->hasText($clientPayload['firstName'] ?? null)) {
                $errors[] = 'Client first name is required.';
            }

            if (!$this->hasText($clientPayload['lastName'] ?? null)) {
                $errors[] = 'Client last name is required.';
            }

            if (!$this->hasText($clientPayload['email'] ?? null) || filter_var($clientPayload['email'], FILTER_VALIDATE_EMAIL) === false) {
                $errors[] = 'A valid client email is required.';
            }
        }

        if (!is_array($propertyPayload)) {
            $errors[] = 'Property details are required.';
            return $errors;
        }

        if (!$this->hasText($propertyPayload['postcode'] ?? null)) {
            $errors[] = 'Property postcode is required.';
        }

        if (!isset($propertyPayload['yearBuilt']) || filter_var($propertyPayload['yearBuilt'], FILTER_VALIDATE_INT) === false) {
            $errors[] = 'Property year built must be an integer.';
        }

        if (!isset($propertyPayload['rebuildCost']) || !is_numeric($propertyPayload['rebuildCost']) || (float) $propertyPayload['rebuildCost'] <= 0) {
            $errors[] = 'Property rebuild cost must be greater than zero.';
        }

        if (!isset($propertyPayload['isUnoccupied']) || !is_bool($propertyPayload['isUnoccupied'])) {
            $errors[] = 'Property unoccupied status must be boolean.';
        }

        return $errors;
    }

    /**
     * @param array<string, mixed> $payload
     * @return array<string, mixed>
     */
    public function normalize(array $payload): array
    {
        /** @var array<string, mixed> $client */
        $clientPayload = $payload['client'];
        /** @var array<string, mixed> $property */
        $propertyPayload = $payload['property'];

        return [
            'client' => [
                'firstName' => trim((string) $clientPayload['firstName']),
                'lastName' => trim((string) $clientPayload['lastName']),
                'email' => trim((string) $clientPayload['email']),
            ],
            'property' => [
                'postcode' => trim((string) $propertyPayload['postcode']),
                'yearBuilt' => (int) $propertyPayload['yearBuilt'],
                'rebuildCost' => (float) $propertyPayload['rebuildCost'],
                'isUnoccupied' => (bool) $propertyPayload['isUnoccupied'],
            ],
        ];
    }

    private function hasText(mixed $value): bool
    {
        return is_string($value) && trim($value) !== '';
    }
}
