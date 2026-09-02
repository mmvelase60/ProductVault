# Security design

## Authentication and authorization

ProductVault uses ASP.NET Core Identity for password hashing, account storage, and password-reset tokens. Registration requires a six-digit email verification code before the API issues a signed, time-limited JWT bearer token to the Angular SPA. Angular attaches the token only to API requests through a centralized HTTP interceptor.

All catalogue controllers and API controllers use `[Authorize]`. The authenticated user ID is obtained from Identity and becomes the authoritative `OwnerId` for new records.

## Data isolation

Every category/product query includes an `OwnerId == currentUserId` filter. This is applied before read, edit, or delete actions. Consequently, a user who changes a numeric URL ID cannot access another user's data; the application returns `404 Not Found`.

## Input and upload safety

- Server-side validation validates required values, price range, and category-code format even when a request bypasses Angular.
- Category code format is constrained to `AAA999`; database indexes enforce uniqueness.
- Image uploads allow JPG/JPEG/JFIF, PNG, GIF, and WEBP files and reject files above 5 MB. JFIF uploads are stored with a standard `.jpg` extension so browsers serve them consistently.
- Files are stored with generated GUID names rather than supplied filenames.
- Excel imports accept only `.xlsx`, limit rows to 500, validate price/name/category ownership, and run in a retry-safe serializable transaction.

## Request protection

- The API validates JWT issuer, audience, signature, and expiry before protected endpoints run.
- In Development, CORS permits HTTP origins on `localhost` so Angular can use a fallback port when `4200` is busy. Production uses the explicit `Cors:AllowedOrigins` configuration.
- Verification codes are created with a cryptographic random-number generator, stored only as salted Identity password hashes, expire after 10 minutes, are single-use, and are invalidated after five incorrect attempts. Password-reset tokens are issued by ASP.NET Core Identity; the JWT signing key and Gmail SMTP App Password are stored in User Secrets, never source control.
- Gmail SMTP uses TLS and an App Password. A dedicated sender account should be used and its credentials must never be committed or shared.
- HTTPS redirection and HSTS are enabled outside Development.
- API endpoints require a valid bearer token. A production deployment should replace the local token issuer with a managed identity/OAuth provider and secure token storage strategy.

## Data integrity and concurrency

- MySQL timestamp-backed concurrency tokens detect stale edit submissions and prevent lost updates.
- Product-code generation runs in a serializable transaction, with a unique database index as a final duplicate safeguard.
- Foreign-key restriction prevents category deletion from silently breaking existing products.

## Operational safety

- Prometheus metrics are mapped only in Development.
- Grafana's `admin/admin` credentials are local-development defaults only; they must be changed before any shared environment.
- Docker's local HTTPS scrape skips certificate verification solely because Visual Studio uses a self-signed development certificate.
