# Requirements traceability matrix

This matrix maps the assignment requirements to implementation evidence and verification steps.

| Requirement | Implementation evidence | Verification |
| --- | --- | --- |
| C#, .NET 8, ASP.NET Core | `backend/ProductVault.csproj`, `backend/Program.cs` | Build the solution. |
| Angular/TypeScript UI | `frontend/` standalone Angular application | Run `pnpm start` and navigate the SPA. |
| EF Core + MySQL | `ApplicationDbContext`, migrations, `appsettings.json` | Run `dotnet ef database update`. |
| Registration and login | `AuthController`, Identity, email confirmation, password reset, JWT bearer configuration | Register, confirm by email, sign in, and recover a password through Angular. |
| Demonstrable starting catalogue | `DashboardApiController` demo-data endpoint and Angular dashboard action | With a fresh account, load 3 categories and 5 products, then verify the dashboard totals. |
| Each user manages only own data | `OwnerId` on entities; ownership filters in controllers/API | Sign in as two accounts and try another record ID. |
| Category view/add/edit | Angular categories screen and `CategoriesApiController` | Complete the category acceptance tests. |
| `AAA999` category code | Data annotations and controller checks | Try valid `ACC001` and invalid `AC1001`. |
| Unique category code | Unique `(OwnerId, CategoryCode)` database index | Try duplicate code as same user. |
| Product view/add/edit/delete | Angular products screen and `ProductsApiController` | Complete the product acceptance tests. |
| Product paging (10 per page) | `ProductsApiController` page-size limit | Create/import 11+ products. |
| Category required for product | Angular form plus server-side category ownership check | Submit without/with inactive category. |
| Auto product code | `ProductCodeGenerator` + serializable transaction | Create products and inspect `yyyyMM-###` values. |
| Product image upload | `ProductsApiController` image endpoint | Upload a supported image below 5 MB. |
| Excel import/export | `ExcelProductService` and product actions | Import a valid workbook, then export. |
| Clean layers / SOLID | Controllers, services, domain models, EF infrastructure | Review [architecture](architecture.md). |
| Concurrency | `RowVersion` fields and edit handling | Submit a stale edit after a second update. |
| Validation and exception handling | Data annotations, model state, guarded file/import workflows | Test invalid values and malformed imports. |
| API protection | JWT bearer authentication and `[Authorize]` on API controllers | Call API without/with a bearer token. |
| Auditing | `AuditableEntity` fields set in controllers | Inspect created/updated records in MySQL. |
| Unit tests | `backend/tests/ProductVault.Tests` | Run `dotnet test ProductVault.sln`. |
| ERD, setup, technical docs | `README.md`, `docs/` | Review [documentation index](index.md). |
| GitHub source control | Commit history and Actions workflow | Review the public repository and CI run. |
