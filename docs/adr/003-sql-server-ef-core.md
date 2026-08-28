# ADR-003: Use SQL Server with EF Core

**Status:** Accepted

## Decision

Use SQL Server LocalDB for development with Entity Framework Core migrations.

## Rationale

SQL Server is one of the assignment's approved databases and works well with ASP.NET Core Identity, `rowversion`, decimal money fields, and EF Core migrations.

## Consequence

Developers can reproduce the schema with `Update-Database`; the generated migration history provides an auditable database evolution path.
