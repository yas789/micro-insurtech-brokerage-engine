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
        $client = $payload['client'] ?? null;
        $property = $payload['property'] ?? null;

        if (!is_array($client)) {
            $errors[] = 'Client details are required.';
        } else {
            if (!$this->hasText($client['firstName'] ?? null)) {
                $errors[] = 'Client first name is required.';
            }

            if (!$this->hasText($client['lastName'] ?? null)) {
                $errors[] = 'Client last name is required.';
            }

            if (!$this->hasText($client['email'] ?? null) || filter_var($client['email'], FILTER_VALIDATE_EMAIL) === false) {
                $errors[] = 'A valid client email is required.';
            }
        }

        if (!is_array($property)) {
            $errors[] = 'Property details are required.';
            return $errors;
        }

        if (!$this->hasText($property['postcode'] ?? null)) {
            $errors[] = 'Property postcode is required.';
        }

        if (!isset($property['yearBuilt']) || filter_var($property['yearBuilt'], FILTER_VALIDATE_INT) === false) {
            $errors[] = 'Property year built must be an integer.';
        }

        if (!isset($property['rebuildCost']) || !is_numeric($property['rebuildCost']) || (float) $property['rebuildCost'] <= 0) {
            $errors[] = 'Property rebuild cost must be greater than zero.';
        }

        if (!isset($property['isUnoccupied']) || !is_bool($property['isUnoccupied'])) {
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
        $client = $payload['client'];
        /** @var array<string, mixed> $property */
        $property = $payload['property'];

        return [
            'client' => [
                'firstName' => trim((string) $client['firstName']),
                'lastName' => trim((string) $client['lastName']),
                'email' => trim((string) $client['email']),
            ],
            'property' => [
                'postcode' => trim((string) $property['postcode']),
                'yearBuilt' => (int) $property['yearBuilt'],
                'rebuildCost' => (float) $property['rebuildCost'],
                'isUnoccupied' => (bool) $property['isUnoccupied'],
            ],
        ];
    }

    private function hasText(mixed $value): bool
    {
        return is_string($value) && trim($value) !== '';
    }
}
