# ProductVault interview rehearsal guide

Use this guide to practise explaining ProductVault clearly in an intermediate software-engineer interview. It focuses on decisions you can prove in the code and demonstrate live.

## The project in 30 seconds

> “ProductVault is a private catalogue-management application for products and categories. The user interface is Angular and the API is ASP.NET Core backed by MySQL. Security and ownership are enforced by the API: Angular holds a short-lived JWT in memory, restores a session through a rotating HttpOnly cookie, and each catalogue operation derives the owner from the validated token. The application also includes email verification, imports, inventory history, role-based administration, tests, monitoring, and documentation.”

## The project in 90 seconds

> “I chose a modular monolith because this is a focused catalogue workflow, not a case where microservices would solve a real scaling or team-boundary problem. Angular handles the browser experience, including route protection and short-lived bearer-token attachment. ASP.NET Core Identity owns password hashing and users; the API verifies the JWT and applies owner filtering before data reaches EF Core or MySQL.
>
> Users must verify a single-use, six-digit email code before signing in. Authentication endpoints are rate limited, and the frontend gives useful feedback for expired sessions, rate limits, and server failures. Product and category edits use row versions to prevent silent overwrites. Inventory is stored both as a fast current quantity and immutable movement history, so changes are explainable. Finally, I use unit tests, HTTP integration tests, and browser-level Playwright tests because a good UI still needs repeatable evidence that the key journeys work.”

## Facts to remember

| Topic | Truthful point to make |
| --- | --- |
| Architecture | Angular SPA → ASP.NET Core 8 Web API → EF Core → MySQL. |
| Identity | ASP.NET Core Identity hashes passwords; ProductVault adds email-code verification, role assignment, short-lived in-memory access tokens, and rotating revocable refresh sessions. |
| Authorization | The API derives the caller from the JWT and applies `OwnerId` filtering; the client never sends an owner ID. |
| Username | The server generates `first initial + surname`, for example `MMvelase`, and adds a suffix if needed. |
| Imports | CSV/XLSX imports accept up to 500 rows, create categories first, skip duplicates, and return row-level errors. |
| Inventory | Product quantity is quick to display; immutable stock movements explain every receive or adjustment. |
| Concurrency | Product/category updates carry a MySQL row version. Stale writes return a conflict instead of overwriting someone else’s change. |
| Resilience | Authentication is limited to five requests per minute per client and endpoint; errors use problem-details responses with a trace ID. |
| Quality | 22 .NET unit/integration tests and 12 Playwright checks currently pass. |
| Scope | No live supplier/ERP yet. The Import Centre is the prepared integration boundary, not a claim of a finished external integration. |

## Seven-minute rehearsal

Use the detailed [demo package](interview-demo.md) as your script. Rehearse this shorter sequence until you can explain it without reading:

1. **0:00–0:45 — Outcome.** Open the dashboard and state the project purpose and architecture.
2. **0:45–1:45 — Secure onboarding.** Explain registration, generated username, emailed verification code, and sign-in lockout until verification.
3. **1:45–3:15 — Catalogue workflow.** Show categories, products, search/filtering, low stock, and server-generated product codes.
4. **3:15–4:15 — Inventory accountability.** Use the Stock panel to receive a quantity and show the movement history.
5. **4:15–5:00 — Integration boundary.** Show Import catalogue and explain CSV/XLSX, partial success, and the future adapter boundary.
6. **5:00–6:10 — Security and quality.** Show Profile/Admin if configured, then point to API ownership rules, rate limiting, tests, and documentation.
7. **6:10–7:00 — Trade-off.** Explain why a modular monolith was the right starting point and name one responsible next step.

## Questions you are likely to get

### Why did you separate Angular and ASP.NET Core?

“It gives the presentation layer and API a clear boundary. Angular is responsible for the user experience; the API owns authentication, authorization, business validation, and persistence. That makes the API usable by another client later without duplicating business rules.”

### How do you stop one user accessing another user’s products?

“I do not trust an owner ID from the browser. The API validates the bearer token, reads the user ID from its claims, then scopes every category and product query and mutation to that owner. The integration tests exercise owner isolation through the real HTTP pipeline.”

### Why use JWTs and refresh cookies together?

“The short-lived JWT gives the separately hosted Angular app a clean bearer-token boundary for API requests. I keep it in memory instead of local storage. A rotating HttpOnly refresh cookie restores the session after a page reload, can be revoked on the server, and is protected by a CSRF header. That gives me usability without treating browser storage as a safe secret vault.”

### How is authentication protected from abuse?

“Passwords are hashed by ASP.NET Core Identity. Registration requires a single-use email code with expiry and incorrect-attempt limits. The authentication endpoints also have a per-client, per-endpoint fixed-window request limit. Rejections return a standard 429 problem-details response, and the UI tells the user when to retry.”

### Why keep current stock and a movement table?

“The current quantity makes reads and dashboard calculations simple. The movement table preserves why and how a change happened: before quantity, after quantity, operation, note, timestamp, and actor. That is much more useful when investigating a stock discrepancy.”

### Why no generic repository layer?

“EF Core’s `DbContext` already provides a unit-of-work and repository-style abstraction. Adding a generic wrapper here would mostly hide useful EF Core features without simplifying a real business rule. I extracted services where there is actual reusable workflow, such as verification codes, email delivery, Excel reading, product codes, and audit logging.”

### What would you improve next?

“For the next production increment I would add a background email queue and a source-specific ERP adapter once the external system’s contract is known. I would not build those speculatively because they introduce operational decisions that need real requirements.”

## Plain-language explanation

If the interviewer is less technical, say:

> “I built a secure personal catalogue workspace. People verify their email before using it, manage products and categories, import their existing catalogue from Excel or CSV, and can trace stock changes. I also made sure the system gives clear feedback, works well on mobile, and has automated checks behind it.”

## Final rehearsal checklist

- Use a verified account and pre-load starter data before sharing your screen.
- Keep ProductVault, Swagger, the documentation index, and GitHub Actions open in separate tabs.
- Run `dotnet test ProductVault.sln`, `pnpm run build`, and `pnpm run test:e2e` before the interview.
- Do not expose Gmail app passwords, JWT secrets, or database credentials on screen.
- Describe trade-offs confidently. Do not apologise for not using microservices, Kafka, or a real ERP when the requirements did not justify them.
- End with the outcome: secure ownership, explainable stock, import-ready data flow, and repeatable verification.
