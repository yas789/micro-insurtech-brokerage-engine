<?php

declare(strict_types=1);

namespace MicroInsurTech\Gateway\Tests;

use PHPUnit\Framework\TestCase;

final class EntrypointTest extends TestCase
{
    public function testHealthEntrypointBootsWithoutComposerAutoloading(): void
    {
        // A separate interpreter ensures PHPUnit's autoloader cannot hide
        // dependency-order failures in the deployed entrypoint.
        $process = proc_open([
            PHP_BINARY,
            '-r',
            '$_SERVER["REQUEST_METHOD"] = "GET"; $_SERVER["REQUEST_URI"] = "/health"; require $argv[1];',
            __DIR__ . '/../public/index.php',
        ], [1 => ['pipe', 'w'], 2 => ['pipe', 'w']], $pipes);

        self::assertIsResource($process);
        $output = stream_get_contents($pipes[1]);
        $errors = stream_get_contents($pipes[2]);
        fclose($pipes[1]);
        fclose($pipes[2]);

        self::assertSame(0, proc_close($process), $errors);
        self::assertSame(['status' => 'PHP gateway running'], json_decode($output, true));
    }
}
