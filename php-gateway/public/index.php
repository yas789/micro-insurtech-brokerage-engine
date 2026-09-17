<?php

declare(strict_types=1);

// Placeholder controller for the PHP gateway.
// Request parsing, validation, and BrokerService orchestration will be implemented later.

http_response_code(501);
header('Content-Type: application/json');

echo json_encode([
    'error' => 'PHP gateway scaffold only. Implementation pending.',
]);
