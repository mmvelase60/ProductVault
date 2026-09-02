# Testing and coverage

ProductVault uses three test levels: xUnit for business rules, ASP.NET Core's test host for HTTP-level integration, and Playwright for browser journeys. The .NET test project is included in `ProductVault.sln` and runs in the GitHub Actions pipeline.

## Current automated coverage

| Area | Tests | Purpose |
| --- | ---: | --- |
| Category validation | 5 | Accepts the required `AAA999` format and rejects invalid formats. |
| Product-code generation | 3 | Covers first code, monthly sequence continuation, and new-month reset. |
| Excel import reader | 2 | Verifies expected columns and preserves invalid rows for an import error report. |
| Inventory and audit | 5 | Verifies inventory validation and owner-scoped audit recording. |
| API integration | 7 | Exercises registration/roles, email-code confirmation, duplicate-email protection, owner isolation, stock movements, admin authorization, and rotating refresh-session revocation through the real HTTP pipeline. |
| Browser end-to-end | 6 × 2 viewports | Exercises anonymous route protection, keyboard skip navigation, registration navigation, clear email-verification success/failure dialogs, responsive account navigation, full-width catalogue data rendering, product/category/stock editor dialogs, and safe product-delete confirmation in Chromium and a Pixel-sized viewport. |
| **.NET total** | **22** | Unit and integration tests for high-value business rules and security boundaries. |

The Playwright suite runs the six scenarios above in Chromium and a mobile-sized viewport, for 12 browser checks. Counts should be treated as a quick regression signal, not as a substitute for the manual acceptance plan.

## Run tests

```powershell
dotnet test ProductVault.sln
```

## Run browser-level checks

The Playwright suite starts Angular on a dedicated local port and stubs the narrow API responses needed for navigation checks. This keeps browser tests deterministic while HTTP integration tests continue to cover the real API pipeline.

```powershell
cd frontend
pnpm install
pnpm exec playwright install chromium
pnpm run test:e2e
```

Use `PLAYWRIGHT_BASE_URL` to target an already-running frontend instead of starting a test server.

## Generate a coverage report

The solution includes Coverlet's data collector. Generate Cobertura coverage data with:

```powershell
dotnet test ProductVault.sln --collect:"XPlat Code Coverage" --results-directory TestResults
```

This creates a `coverage.cobertura.xml` file beneath `TestResults`. GitHub Actions retains the TRX test result and coverage output in its test-results artifact.

## Gaps worth closing

- Concurrency tests: verify stale `RowVersion` values show the retry message.
- Product image validation: unsupported files and oversize images are rejected.
- Excel import integration tests: inactive/missing category codes and invalid prices are rejected.
- API authorization tests: add direct anonymous `401` checks for every catalogue endpoint.
