# Interview walkthrough

Use this as a concise three-to-five-minute demonstration. Start with a fresh account if you want to use the sample catalogue.

## 1. Start with the outcome

> “ProductVault is a private catalogue application. I built it as an Angular SPA backed by an ASP.NET Core Web API, with EF Core and MySQL for persistence.”

Register with first name, surname, email, and password. Explain that the server generates a username such as `MMvelase`, emails a single-use verification code, and only permits sign-in after verification. From the dashboard, open **Import catalogue** and select **Load starter data** to populate three categories and five products.

## 2. Demonstrate the user journey

1. Open **Categories** and point out the `AAA999` category-code rule and active/inactive state.
2. Open **Products** and demonstrate search, filtering, sorting, and the ten-item page size.
3. Add or edit a product to show server-generated `yyyyMM-###` product codes.
4. Open **Import catalogue** and show the CSV/Excel template, combined category/product contract, and starter-provider option.
5. Show image upload and Excel export if time allows.

## 3. Explain the important technical decisions

- Angular keeps the UI separate from the API and sends the JWT through one HTTP interceptor.
- ASP.NET Core Identity hashes passwords and issues time-limited password-reset tokens; registration codes are cryptographically generated, stored as hashes, single-use, and rate-limited to five attempts. The API validates JWT issuer, audience, signing key, and expiry.
- The server derives the current user from the token and applies `OwnerId` filtering to every category/product operation. This prevents one user from reading or changing another user's data.
- EF Core migrations manage the MySQL schema. Database indexes back up the unique code rules, while timestamp row versions detect conflicting edits.
- Product creation and catalogue imports use serializable, retry-safe transactions to protect product-code generation.

## 4. Finish with engineering quality

Show the GitHub Actions workflow, then briefly mention:

- Focused xUnit tests for category validation, product-code generation, and Excel reading.
- Swagger and `/health` for local API verification.
- Prometheus and Grafana for local request and business metrics.
- The documentation index, which includes architecture, ERD, API reference, security notes, acceptance tests, and decision records.

## Likely questions

| Question | Short answer |
| --- | --- |
| Why Angular and a separate API? | It matches my strengths and keeps the presentation layer independently deployable from the business/API layer. |
| Why JWT? | The UI and API are separate applications. JWT provides a clear stateless boundary, and the API still owns authorization decisions. |
| Why MySQL? | It is a production-grade relational database I use regularly; EF Core migrations make the schema repeatable. |
| Why no Kafka or RabbitMQ? | The workload is synchronous CRUD. A broker would add operational complexity without a real asynchronous or cross-service need. |
| How would you integrate a supplier or ERP later? | The Import Centre already defines one authenticated CSV/Excel contract. I would add a source-specific adapter and service credential at that boundary, without changing the catalogue rules. |
| What would you improve next? | Integration tests for authorization/concurrency, per-row import error reports, and structured logs with correlation IDs. |
