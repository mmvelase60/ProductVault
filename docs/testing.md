# Testing and coverage

ProductVault uses xUnit for fast business-rule tests. The test project is included in `ProductVault.sln` and runs in the GitHub Actions pipeline.

## Current automated coverage

| Area | Tests | Purpose |
| --- | ---: | --- |
| Category validation | 5 | Accepts the required `AAA999` format and rejects invalid formats. |
| Product-code generation | 3 | Covers first code, monthly sequence continuation, and new-month reset. |
| Excel import reader | 1 | Verifies the expected import columns are read correctly. |
| **Total** | **9** | Focused unit tests of high-value business rules. |

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

- Controller-level ownership tests: verify another user's IDs always return `404`.
- Concurrency tests: verify stale `RowVersion` values show the retry message.
- Product image validation: unsupported files and oversize images are rejected.
- Excel import integration tests: inactive/missing category codes and invalid prices are rejected.
- API authorization tests: anonymous requests return `401`.
