# ADR-002: Use JWT bearer tokens for the Angular SPA

**Status:** Accepted

## Decision

Use ASP.NET Core Identity for account management and issue signed JWT bearer tokens to the separate Angular SPA.

## Rationale

The frontend is now a separately served Angular client. Bearer tokens give the API a clear authentication boundary, make the API independently consumable, and let every controller derive the owner from trusted token claims. The signing key stays in local User Secrets rather than source control.

## Consequence

The SPA stores the active local development session and attaches the bearer token through one HTTP interceptor. API endpoints use `[Authorize]`; the server validates issuer, audience, signing key, and expiry. For a production internet-facing system, prefer a hardened token-storage and refresh-token strategy.
