# Testing and coverage

ProductVault uses xUnit for fast business-rule tests, ASP.NET Core's test host for HTTP-level integration tests, and Playwright for browser-level UI checks. The .NET test project is included in `ProductVault.sln` and runs in the GitHub Actions pipeline.

## Current automated coverage

| Area | Tests | Purpose |
| --- | ---: | --- |
| Category validation | 5 | Accepts the required `AAA999` format and rejects invalid formats. |
| Product-code generation | 3 | Covers first code, monthly sequence continuation, and new-month reset. |
| Excel import reader | 2 | Verifies expected columns and preserves invalid rows for an import error report. |
| Inventory and audit | 5 | Verifies inventory validation and owner-scoped audit recording. |
| API integration | 6 | Exercises registration/roles, email-code confirmation, owner isolation, stock movements, admin authorization, and rotating refresh-session revocation through the real HTTP pipeline. |
| Browser end-to-end | 6 × 2 viewports | Exercises anonymous route protection, keyboard skip navigation, registration navigation, clear email-verification success/failure dialogs, responsive account navigation, and initial catalogue-data rendering in Chromium and a Pixel-sized viewport. |
| **Total** | **21** | Focused unit and integration tests of high-value business rules and security boundaries. |

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

## Recommended next test cases

- Concurrency tests: verify stale `RowVersion` values show the retry message.
- Product image validation: unsupported files and oversize images are rejected.
- Excel import integration tests: inactive/missing category codes and invalid prices are rejected.
- API authorization tests: anonymous requests return `401`.
