# ADR-001: Use a modular monolith

**Status:** Accepted

## Decision

Build ProductVault as one ASP.NET Core application with clear MVC, service, domain, and infrastructure boundaries.

## Rationale

The assignment requires a secure CRUD system, not independently deployed services. A modular monolith is simpler to run, debug, test, and explain while preserving separation of concerns.

## Consequence

The service boundaries can later be extracted if a concrete scale or team-ownership need emerges; no distributed-system overhead is paid today.
