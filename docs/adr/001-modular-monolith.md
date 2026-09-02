# ADR-001: Use a modular monolith

**Status:** Accepted

## Decision

Build ProductVault as one ASP.NET Core API with clear controller, service, domain, and infrastructure boundaries, consumed by a separate Angular SPA.

## Context

ProductVault has one core business workflow: users manage a private catalogue. There is no independent scaling requirement, separate deployment cadence, or team boundary that requires distributed services. A modular monolith is simpler to run, debug, and test while retaining separation of concerns.

## Consequence

The service boundaries can later be extracted if a concrete scale or team-ownership need emerges; no distributed-system overhead is paid today.
