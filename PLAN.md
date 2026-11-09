# Saved comparison delivery plan

## Current feature

Goal: reopen original saved comparisons by private link and keep the newest 20
references in this browser. Branch: `feature/quote-history`, based on `dev`
at `4a6cb31`. Feature PR targets `dev`; production promotion is separate.
Contracts and acceptance criteria: `docs/quote-history.md`.

Baseline: submissions already persist a new client/property and quote set before
success. There is no retrieval endpoint, public reference, or browser history.
The SQL initializer preserves existing tables but does not upgrade them.

Execute one item at a time; record actual verification before committing each
item. No implementation is included in step 1. Prior completed work is retained
below for historical context.

- [x] 1. Document saved comparison contracts and delivery plan.
  Verification: documentation checker and review against existing consumers.
  Result: docs and whitespace checks pass; additive POST fields preserve current
  quotes consumers. Contract excludes identity data and defines legacy upgrades.
- [x] 2. Add comparison references and timestamps to SQL schema.
  Verification: fresh SQL, populated legacy upgrade, repeated upgrade, unique references.
  Result: all five checks passed in CI run 42537192314, including real SQL upgrade verifier.
- [x] 3. Support comparison schema upgrades in local tooling.
  Verification: fresh/legacy/current/partial schema integration checks.
  Result: all five CI checks passed in run 42537357001, including initializer cases.
- [x] 4. Map saved comparison metadata in EF Core.
  Verification: build and EF mapping tests.
  Result: build and focused metadata test pass with .NET 9 roll-forward.
- [x] 5. Persist and return saved comparison metadata.
  Verification: persistence tests, including distinct references and empty results.
  Result: all 6 persistence tests pass with .NET 9 roll-forward.
- [ ] 6. Include comparison references in quote responses.
  Verification: controller success and failure-side-effect tests.
- [ ] 7. Read saved comparisons without recalculating quotes.
  Verification: retrieval service tests for stored values, missing/empty results, identity exclusion.
- [ ] 8. Expose saved comparison retrieval in the core API.
  Verification: endpoint tests for valid, malformed, missing references and read-only behavior.
- [ ] 9. Add saved comparison retrieval to the gateway client.
  Verification: PHP HTTP-client and upstream-failure tests.
- [ ] 10. Route saved comparison requests through PHP.
  Verification: route/method/reference tests and no-forwarding checks.
- [ ] 11. Preserve retrieval response headers through the local proxy.
  Verification: no-store headers and proxy regression tests.
- [ ] 12. Display saved comparison references and private links.
  Verification: DOM metadata/link tests and existing submission regressions.
- [ ] 13. Open saved comparisons from private links.
  Verification: direct opening, malformed/missing links, network errors, no POST.
- [ ] 14. Prevent stale comparison loads from replacing current results.
  Verification: deferred retrieval/generation race tests.
- [ ] 15. Store bounded browser comparison history.
  Verification: storage ordering, bounds, deduplication, corrupt/blocked storage tests.
- [ ] 16. Render recent comparisons with reopen actions.
  Verification: DOM history rendering, updates, reopening, no client information.
- [ ] 17. Add local history removal and accessible navigation.
  Verification: DOM and browser removal/clear/focus checks with no server deletion.
- [ ] 18. Verify saved comparison retrieval against SQL Server.
  Verification: full-stack saved metadata/value comparison, no writes, missing reference.
- [ ] 19. Cover comparison links and browser history in Playwright.
  Verification: real-stack history/link/failure browser journeys.
- [ ] 20. Finalize saved comparison documentation and verification.
  Verification: full CI, whitespace checks, all-commit and full-diff review, feature PR.

## Blockers and verification notes

Local environment has .NET 9 only; Docker, PHP, and Composer are absent from PATH.
Use .NET roll-forward locally and native .NET 8/SQL/PHP in CI. User requested
continuing without waiting for slow verification; quick checks run per step,
with pending remote checks explicitly recorded rather than claimed as passed.

---

# Prior codebase improvement plan

## Goal and baseline

Deliver fifteen small, individually verified commits improving validation, quote
UI reliability, developer guidance, and CI. Started from `origin/dev` at
`7a5623a`, which includes rebuild-cost validation and deferred cover state.
Implementation branch: `feature/codebase-improvements`; PR targets `dev`.
Each behavior/workflow change includes documentation in `docs/` or `README.md`.

## Checklist

- [x] 1. Align gateway year-built range with core (1500–2100).
  Verification: PHPUnit gateway boundary and no-forwarding tests.
  Result: PHPUnit passed (40 tests, 80 assertions).
- [x] 2. Validate trimmed gateway text lengths using UTF-16 storage units.
  Verification: PHPUnit ASCII/Unicode boundary and no-forwarding tests.
  Result: PHPUnit passed (52 tests, 110 assertions).
- [x] 3. Validate trimmed gateway email addresses.
  Verification: PHPUnit whitespace and invalid-email cases.
  Result: PHPUnit passed (58 tests, 126 assertions).
- [x] 4. Strengthen direct core email validation.
  Verification: .NET controller email acceptance/rejection tests.
  Result: Build passed; 34 controller tests passed with DOTNET_ROLL_FORWARD=Major
  on .NET 9. Native .NET 8 verification is deferred to CI.
- [x] 5. Normalize upstream field-keyed validation errors at gateway.
  Verification: PHPUnit realistic ASP.NET binding-error responses.
  Result: PHPUnit passed (60 tests, 129 assertions).
- [x] 6. Display useful errors for non-JSON quote responses.
  Verification: Vitest HTML, empty, malformed response cases.
  Result: npm test passed (19 tests).
- [x] 7. Prevent overlapping submissions per form.
  Verification: Vitest deferred requests and subsequent submission.
  Result: npm test passed (22 tests), including independent forms.
- [x] 8. Distinguish empty quote responses from successful comparisons.
  Verification: Vitest empty/nonempty status assertions.
  Result: npm test passed (23 tests).
- [x] 9. Display unavailable premiums honestly, preserving zero.
  Verification: Vitest missing/invalid/zero/normal premium cases.
  Result: npm test passed (39 tests).
- [x] 10. Expose results loading through aria-busy.
  Verification: Vitest pending/success/failure DOM assertions.
  Result: npm test passed (39 tests), with busy-state assertions on all outcomes.
- [x] 11. Refresh repository tooling guidance and verification entry points.
  Verification: Cross-check package/scripts; documentation checker.
  Result: Docs checker and Composer validation passed; npm scripts confirmed;
  all 3 Python frontend proxy tests passed.
- [x] 12. Require docs/verification.md in documentation check.
  Verification: Real repo pass and isolated missing-file failure.
  Result: Repo passed; isolated archived fixture without verification.md failed
  with the expected missing-file message (exit 1).
- [x] 13. Cache npm downloads in frontend and integration CI jobs.
  Verification: Workflow validation and remote CI run.
  Result: actionlint and docs check passed; cache execution will be checked in PR CI.
- [x] 14. Upload browser diagnostics on integration failures.
  Verification: Workflow validation and failure-artifact exercise.
  Result: actionlint and docs check passed; paths match Playwright and integration
  runner outputs. Actual failure-only upload remains unexercised unless CI fails.
- [x] 15. Cancel superseded PR CI runs without cancelling manual runs.
  Verification: Workflow validation and two-update PR exercise.
  Result: actionlint passed; PR number, event name, and manual run-ID grouping
  reviewed. Live cancellation verification is pending PR CI.

## Environment and final verification

- Local .NET runtime is 9 only. Isolated runtime download succeeded but the test
  runner still selected the system host; isolated SDK download was interrupted.
  User requested moving on. Local tests use DOTNET_ROLL_FORWARD=Major; CI uses 8.
- Homebrew PHP installation failed before installation; standalone PHP 8.2.32
  downloaded to the harness temporary directory runs local PHPUnit checks.
  Composer dependencies are installed in ignored `php-gateway/vendor/`.
- Full SQL/browser integration requires Docker/amd64 Linux; run in GitHub CI
  if Docker is unavailable locally.
- Review all fifteen commits and complete PR checks before delivery.

### Local verification summary

- PHP 8.2.32: 60 PHPUnit tests, 129 assertions passed; strict Composer validation
  passed. Generated dependencies remain ignored.
- Frontend: 39 Vitest tests passed, including all new loading/error-state checks.
- Core: Release build and all 67 xUnit tests passed with .NET 9.0.10 roll-forward.
- Python frontend proxy: 3 tests passed.
- Documentation: repo passes; isolated missing-guide fixture fails as expected.
- Workflows: actionlint 1.7.12 passed after each CI change.
- Native .NET 8, SQL Server, and Chromium integration will be verified in PR CI.
  Remote cache behavior, failure-only artifacts, and cancellation outcomes will
  be reported in the PR verification notes rather than claimed from static checks.
