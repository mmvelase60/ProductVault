# Requirements traceability matrix

This matrix maps the assignment requirements to implementation evidence and verification steps.

| Requirement | Implementation evidence | Verification |
| --- | --- | --- |
| C#, .NET 8, ASP.NET Core | `ProductVault.csproj`, `Program.cs` | Build the solution. |
| MVC/Razor UI | `Controllers/`, `Views/` | Run the application and navigate the UI. |
| EF Core + MySQL | `ApplicationDbContext`, migrations, `appsettings.json` | Run `dotnet ef database update`. |
| Registration and login | ASP.NET Core Identity in `Areas/Identity`, `Program.cs` | Register and sign in. |
| Each user manages only own data | `OwnerId` on entities; ownership filters in controllers/API | Sign in as two accounts and try another record ID. |
| Category view/add/edit | `CategoriesController`, `Views/Categories/` | Complete the category acceptance tests. |
| `AAA999` category code | Data annotations and controller checks | Try valid `ACC001` and invalid `AC1001`. |
| Unique category code | Unique `(OwnerId, CategoryCode)` database index | Try duplicate code as same user. |
| Product view/add/edit/delete | `ProductsController`, `Views/Products/` | Complete the product acceptance tests. |
| Product paging (10 per page) | `ProductsController.PageSize = 10` | Create/import 11+ products. |
| Category required for product | `ProductInputViewModel`, server-side category ownership check | Submit without/with inactive category. |
| Auto product code | `ProductCodeGenerator` + serializable transaction | Create products and inspect `yyyyMM-###` values. |
| Product image upload | `SaveImageAsync` in `ProductsController` | Upload a supported image below 5 MB. |
| Excel import/export | `ExcelProductService` and product actions | Import a valid workbook, then export. |
| Clean layers / SOLID | Controllers, services, domain models, EF infrastructure | Review [architecture](architecture.md). |
| Concurrency | `RowVersion` fields and edit handling | Submit a stale edit after a second update. |
| Validation and exception handling | Data annotations, model state, guarded file/import workflows | Test invalid values and malformed imports. |
| API protection | `[Authorize]` on API controllers | Call API unauthenticated and authenticated. |
| Auditing | `AuditableEntity` fields set in controllers | Inspect created/updated records in MySQL. |
| Unit tests | `tests/ProductVault.Tests` | Run `dotnet test ProductVault.sln`. |
| ERD, setup, technical docs | `README.md`, `docs/` | Review [documentation index](index.md). |
| GitHub source control | Commit history and Actions workflow | Review the public repository and CI run. |
