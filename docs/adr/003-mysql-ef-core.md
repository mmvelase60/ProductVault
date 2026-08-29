# ADR-003: Use MySQL with EF Core

**Status:** Accepted

## Decision

Use MySQL 8.0 for development with Entity Framework Core migrations through the Pomelo provider.

## Rationale

MySQL is one of the assignment's approved databases and is the development database the project owner uses daily. The Pomelo provider supports ASP.NET Core Identity, decimal money fields, MySQL timestamp-backed concurrency tokens, and EF Core migrations.

## Consequence

Developers create the schema with `dotnet ef database update`. Local credentials are configured through User Secrets rather than committed connection strings.
