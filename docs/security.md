# Security design

## Authentication and authorization

ProductVault uses ASP.NET Core Identity with secure application cookies. This is the appropriate authentication mechanism for the server-rendered Razor MVC application: the browser signs in once, then MVC screens and same-origin API requests use the authenticated session.

All catalogue controllers and API controllers use `[Authorize]`. The authenticated user ID is obtained from Identity and becomes the authoritative `OwnerId` for new records.

## Data isolation

Every category/product query includes an `OwnerId == currentUserId` filter. This is applied before read, edit, or delete actions. Consequently, a user who changes a numeric URL ID cannot access another user's data; the application returns `404 Not Found`.

## Input and upload safety

- Server-side data annotations validate required values, string lengths, price range, and category-code format.
- Category code format is constrained to `AAA999`; database indexes enforce uniqueness.
- Image uploads allow only JPG, JPEG, PNG, GIF, and WEBP extensions and reject files above 5 MB.
- Files are stored with generated GUID names rather than supplied filenames.
- Excel imports accept only `.xlsx`, limit rows to 500, validate price/name/category ownership, and run within a transaction.

## Request protection

- State-changing MVC form actions use antiforgery validation.
- Identity password hashing and cookie handling are provided by ASP.NET Core Identity.
- HTTPS redirection and HSTS are enabled outside Development.
- API endpoints require a signed-in session. If an external SPA/mobile client is introduced, add a dedicated token/OAuth design instead of mixing ad-hoc JWT handling into this MVC application.

## Data integrity and concurrency

- MySQL timestamp-backed concurrency tokens detect stale edit submissions and prevent lost updates.
- Product-code generation runs in a serializable transaction, with a unique database index as a final duplicate safeguard.
- Foreign-key restriction prevents category deletion from silently breaking existing products.

## Operational safety

- Prometheus metrics are mapped only in Development.
- Grafana's `admin/admin` credentials are local-development defaults only; they must be changed before any shared environment.
- Docker's local HTTPS scrape skips certificate verification solely because Visual Studio uses a self-signed development certificate.
