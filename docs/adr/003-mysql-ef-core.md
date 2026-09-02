# ADR-003: Use MySQL with EF Core

**Status:** Accepted

## Decision

Use MySQL 8.0 for development with Entity Framework Core migrations through the Pomelo provider.

## Context

The local development environment uses MySQL 8. The Pomelo provider supports ASP.NET Core Identity, decimal money fields, MySQL timestamp-backed concurrency tokens, and EF Core migrations, so it fits the application without a custom persistence layer.

## Consequence

Developers create the schema with `dotnet ef database update`. Local credentials are configured through User Secrets rather than committed connection strings.
