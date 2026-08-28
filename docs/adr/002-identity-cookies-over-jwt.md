# ADR-002: Use Identity cookies over JWT

**Status:** Accepted

## Decision

Use ASP.NET Core Identity with application cookies for the Razor MVC application and its same-origin API.

## Rationale

Server-rendered MVC applications work naturally with secure HTTP-only session cookies. Adding JWT would duplicate authentication paths without a mobile app, separate SPA, or external API client.

## Consequence

The API remains protected for signed-in browser users. Add OAuth/token authentication only when an external client requirement exists.
