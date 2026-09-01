# ProductVault interview demo package

Use this script for a focused seven-minute demonstration. It is designed to show product thinking, secure engineering, and the ability to explain trade-offs without rushing through every screen.

## Pre-demo checklist

1. Start MySQL, the API, and Angular.
2. Sign in with a verified account and open the dashboard.
3. Keep these tabs ready: ProductVault, Swagger, the [architecture guide](architecture.md), and the GitHub Actions page.
4. Have one CSV/XLSX import sample available from `docs/sample-data/`.
5. Confirm the verification commands pass:

   ```powershell
   dotnet test ProductVault.sln
   cd frontend
   pnpm run build
   ```

6. If demonstrating administration, configure your account as described in the [API documentation](api.md#profile-and-administration), restart the API, then sign out and back in.

## Seven-minute demo flow

### 0:00–0:45 — Outcome and architecture

> “ProductVault is a secure private catalogue application. The browser is an Angular SPA; ASP.NET Core owns the API and security boundary; EF Core persists to MySQL. I kept it as a modular monolith because the business problem is CRUD and catalogue management, not independently scaling services.”

Open the dashboard and point out the four summary cards: products, active categories, catalogue value, and low-stock count.

### 0:45–1:45 — Secure onboarding

Briefly show the registration page or explain the flow:

> “A user registers with first name, surname, email, and password. The server generates a readable username such as `MMvelase`, sends a single-use six-digit verification code, and blocks sign-in until the email is verified.”

Mention that ASP.NET Identity handles password hashing, while ProductVault's verification-code service hashes the code, expires it after ten minutes, and limits failed attempts.

### 1:45–3:15 — Catalogue workflow

1. Open **Categories** and show the `AAA999` validation rule and active status.
2. Open **Products** and use search, category filtering, sorting, paging, and the low-stock filter.
3. Create or edit a product. Point out server-generated product codes and optimistic concurrency through row versions.
4. Open **Import catalogue**, download the template, then show that rejected rows produce a downloadable error CSV while valid rows still import.

Suggested sentence:

> “The Import Centre is a real integration boundary: files work today, while an ERP or supplier adapter can later submit the same authenticated contract without changing catalogue rules.”

### 3:15–4:15 — Inventory movement history

1. In **Products**, click **Stock** on a low-stock item.
2. Receive stock with a delivery reference, then show the before/after movement entry.
3. Show the refreshed low-stock metric and dashboard activity.

Say:

> “Quantity changes are not just overwritten. Every receive or adjustment stores the prior quantity, resulting quantity, operation, note, timestamp, and actor. The API also writes a workspace audit event, so the dashboard shows the business activity.”

### 4:15–5:00 — Profile, roles, and ownership

Open **Profile** and show editable names, generated username, roles, and password change. If configured, open **Admin**.

> “All catalogue queries derive the user ID from the JWT and filter by `OwnerId`; the client never submits an owner ID. The Admin endpoint is protected by an API role policy, not only hidden in the navigation.”

### 5:00–6:15 — Engineering quality

Open Swagger and the documentation index.

> “I added xUnit unit tests for validation and file parsing, then HTTP-level integration tests for registration role assignment, owner isolation, stock movement, and admin protection. This exercises the real middleware, JWT, controllers, Identity, and EF Core test host.”

Point out the health endpoint, local Prometheus/Grafana setup, migrations, and the [codebase guide](codebase-guide.md).

### 6:15–7:00 — Finish with trade-offs

> “I deliberately did not introduce microservices, a message broker, or a generic repository layer. For this scope, those would add operational and abstraction cost without solving a demonstrated need. The boundaries are already explicit, so the application can evolve when a real requirement justifies it.”

## Fast recovery plan

If a live feature is slow or unavailable during the demo:

- Use the dashboard and existing starter data instead of registering live.
- Open Swagger to demonstrate the API contract.
- Use the sample CSV/XLSX and the import documentation to explain the integration flow.
- Show the latest successful build/test output and the migration files.

## Likely interviewer questions

| Question | Strong short answer |
| --- | --- |
| How do you prevent users from seeing each other's catalogue? | The API gets the user ID from the validated JWT and adds an `OwnerId` filter to every catalogue query and mutation. |
| Why store stock movements separately? | The current quantity is fast to display, while immutable movements give explainability and a reliable audit history. |
| Why are file imports partial-success? | One bad row should not prevent valid data from loading. The response identifies rejected rows, and the UI downloads a correction report. |
| How are roles secured? | Roles are added to the signed JWT and enforced with `[Authorize(Roles = "Admin")]` at the API endpoint. The frontend visibility is only a convenience. |
| What do your integration tests prove? | They prove that the deployed HTTP pipeline, Identity, authorization, controllers, and EF Core work together for core security workflows. |
