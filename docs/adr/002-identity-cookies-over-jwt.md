# ADR-002: Use short-lived JWTs with rotating secure browser sessions

**Status:** Accepted

## Decision

Use ASP.NET Core Identity for account management. Issue short-lived signed JWT access tokens to the separate Angular SPA and restore browser sessions with rotating hashed refresh tokens.

## Rationale

The frontend is a separately served Angular client. Bearer access tokens give the API a clear authentication boundary, make the API independently consumable, and let every controller derive the owner from trusted token claims. Keeping the access token only in memory reduces the impact of browser-storage XSS. A rotating, server-revocable refresh token provides a usable session across a page refresh without exposing that credential to JavaScript. The signing key stays in local User Secrets rather than source control.

## Consequence

The SPA holds a 15-minute access token in memory and attaches it through one HTTP interceptor. A seven-day `HttpOnly`, `Secure`, `SameSite=None` refresh cookie is rotated on use, stored as a SHA-256 hash in MySQL, and protected by a rotating CSRF header/cookie pair. API endpoints use `[Authorize]`; the server validates issuer, audience, signing key, and expiry. Sign-out, password reset, and password change revoke refresh sessions server-side.
