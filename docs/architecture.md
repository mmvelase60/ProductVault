# Architecture

ProductVault is a modular monolith with a separate Angular client and ASP.NET Core API. It is one deployable business application with explicit presentation, HTTP, application, and persistence boundaries. That keeps the solution straightforward to run while avoiding the coupling that would come from putting browser, security, and database concerns in one layer.

![ProductVault component architecture](diagrams/productvault-architecture.svg)

```mermaid
flowchart TB
    Browser[Angular SPA] --> API[ASP.NET Core Web API]
    API --> Identity[ASP.NET Core Identity + short-lived JWT]
    API --> Services
    Services --> EF[EF Core ApplicationDbContext]
    API --> EF
    EF --> SQL[(MySQL)]
    API --> Files[Local product image storage]
    App[ASP.NET Core application] --> Metrics[/metrics endpoint/]
    Prometheus --> Metrics
    Grafana --> Prometheus
```

## Layers and responsibilities

| Layer | Main components | Responsibility |
| --- | --- | --- |
| Frontend | Angular standalone components, route guard, JWT interceptor | Render the SPA, validate forms, retain short-lived access tokens only in memory, and call the API with bearer tokens. |
| API | ASP.NET Core REST controllers, JWT authentication, rotating refresh sessions, CORS | Authorize requests, validate input, rotate/revoke browser sessions, and return JSON/file responses. |
| Application | `ProductCodeGenerator`, `ExcelProductService`, `EmailVerificationCodeService`, `SmtpEmailSender` | Hold reusable workflows such as product-code creation, CSV/XLSX conversion, email-code protection, and email delivery. |
| Domain | `Product`, `Category`, `AuditableEntity` | Represent catalogue rules, audit state, ownership, and concurrency data. |
| Infrastructure | EF Core, MySQL, Identity, local image storage | Persist data, authenticate users, store images, and apply migrations. |
| Observability | prometheus-net, Prometheus, Grafana | Collect request/business metrics and visualize them locally. |

## Key design choices

- **Modular monolith:** avoids distributed-system complexity while leaving clear boundaries for future extraction.
- **JWT boundary:** Angular owns the browser UI; the API verifies bearer tokens and derives the user ID server-side. The access token expires after 15 minutes and stays in memory; an `HttpOnly` refresh cookie restores it safely after a reload.
- **Ownership at query level:** every data read and mutation filters by the authenticated `OwnerId`.
- **Optimistic concurrency:** MySQL timestamp-backed concurrency tokens detect conflicting product/category edits.
- **Database constraints as safeguards:** indexes protect category-code and product-code uniqueness even if application logic is bypassed.
- **Retry-safe writes:** serializable product and import transactions execute through EF Core's MySQL retry strategy, so transient database failures do not bypass the write workflow.
- **Local-first monitoring:** `/metrics` is only mapped in Development; Grafana and Prometheus run through Docker Compose.

## Request lifecycle

1. An Angular component calls `AuthService` or `ApiService`; the interceptor attaches the current short-lived bearer token when one is available.
2. ASP.NET Core validates the token and the controller derives the caller from its claims. Client input never decides an `OwnerId`.
3. The controller either delegates reusable work to a focused service or performs a small owner-scoped EF Core query.
4. EF Core persists the change to MySQL. Category and product writes include a row version so stale edits fail with `409 Conflict` rather than overwriting a later change.
5. The API returns a typed JSON response; the component refreshes or updates its local view state.

`ApplicationDbContext` is deliberately used as the data-access boundary. The reasoning for not adding a generic repository wrapper is documented in the [codebase guide](codebase-guide.md#4-repository-and-database-access).
