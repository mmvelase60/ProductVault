# ProductVault interview preparation

## Your 60-second introduction

> “ProductVault is a private product catalogue application built with Angular, ASP.NET Core, EF Core, and MySQL. The API derives the caller from the bearer token and scopes every catalogue operation to that account. The project includes email verification, stock history, catalogue import, automated checks, and local monitoring because those concerns affect how a CRUD system behaves in practice.”

## Five-minute demonstration

1. Register a fresh account with first name, surname, email, and password. Mention the generated username and email verification code.
2. Sign in and show the empty, private dashboard.
3. Open **Import catalogue**. Load starter data or upload the provided CSV/Excel sample.
4. Show dashboard totals, then Categories and Products. Demonstrate filtering, paging, low-stock indicators, server-generated product codes, and a stock movement.
5. Open Swagger or the API documentation. Finish with GitHub Actions, tests, Prometheus/Grafana, and the architecture document.

## Key points

- “I chose a modular monolith because the business problem is CRUD-focused; microservices would add complexity without a scaling need.”
- “The UI never sends an owner ID. The API gets the user ID from the verified JWT and uses it in every query.”
- “I use database indexes and optimistic concurrency as safeguards in addition to application validation.”
- “The Import Centre supports a file contract now. A supplier, ERP, or POS adapter can later use the same authenticated contract without duplicating catalogue rules.”
- “I deliberately did not add Kafka or RabbitMQ because there is no asynchronous workload that justifies operating a broker.”
- “The stock value is quick to read from the product, while immutable movements make each quantity change explainable.”

## Before the interview

- Start MySQL, the API, and Angular; open the application and Swagger in separate tabs.
- Use a verified account and load the starter catalogue before screen sharing.
- Keep the CSV/Excel sample files available in `docs/sample-data/`.
- Check that `dotnet test ProductVault.sln` and `pnpm run build` pass.
- Keep GitHub Actions, the README, and `docs/architecture.md` ready in browser tabs.
