# Quote Journey Verification

## Unit checks

From the repository root, with the .NET 8 SDK/runtime installed:

```sh
dotnet test core-engine-tests/MicroInsurTech.CoreEngine.Tests.csproj
```

Underwriting boundary tests cover 1919/1920/1921 for Aviva and rebuild costs
immediately below, at, and above £500,000 for Niche Property Cover. AXA tests
cover occupied and unoccupied properties. These are local rating rules, not
live insurer integrations.

Postcode HTTP-client tests use an in-process message handler, without external
network access. They check postcode encoding, region parsing, blank input,
404/503 responses, malformed JSON, transport failures, and timeouts. These
failures return an unavailable region; caller cancellation must still propagate.

Orchestration tests hold scheme tasks open to verify that all schemes start
before waiting for their results. A failed scheme fails the request rather than
returning a partial quote list. No timing sleeps or live insurers are required.
