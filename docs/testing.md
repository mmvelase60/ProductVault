# Testing and coverage

ProductVault uses xUnit for fast business-rule tests and ASP.NET Core's test host for HTTP-level integration tests. The test project is included in `ProductVault.sln` and runs in the GitHub Actions pipeline.

## Current automated coverage

| Area | Tests | Purpose |
| --- | ---: | --- |
| Category validation | 5 | Accepts the required `AAA999` format and rejects invalid formats. |
| Product-code generation | 3 | Covers first code, monthly sequence continuation, and new-month reset. |
| Excel import reader | 2 | Verifies expected columns and preserves invalid rows for an import error report. |
| Inventory and audit | 5 | Verifies inventory validation and owner-scoped audit recording. |
| API integration | 4 | Exercises registration/roles, owner isolation, stock movements, and admin authorization through the real HTTP pipeline. |
| **Total** | **19** | Focused unit and integration tests of high-value business rules and security boundaries. |

## Run tests

```powershell
dotnet test ProductVault.sln
```

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
