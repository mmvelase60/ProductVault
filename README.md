# ProductVault

ProductVault is a separated full-stack product catalogue application built for the assessment brief.

- **Frontend:** Angular 22, TypeScript, standalone components, route guards, JWT interceptor.
- **Backend:** ASP.NET Core 8 Web API, EF Core, ASP.NET Core Identity, JWT bearer authentication.
- **Database:** MySQL 8 with EF Core migrations.

Every product and category belongs to its authenticated owner. The API enforces this ownership for every read and mutation.

> Start with the [technical documentation index](docs/index.md) for diagrams, API details, database schema, security decisions, testing, and operations notes.

## Features

- JWT registration and sign-in for the Angular SPA.
- Category create/edit with per-user `ABC123` code validation.
- Responsive Angular UI with an accessible mobile navigation, touch-friendly controls, and scroll-safe catalogue tables.
- Product CRUD, 10-item paging, searching, category filtering, sorting, and optimistic concurrency.
- Product image upload plus Excel import (up to 500 records) and export.
- MySQL-backed auditing, health checks, Prometheus metrics, Grafana dashboard, xUnit tests, and GitHub Actions CI.

## Repository layout

```text
ProductVault/
├── backend/                  # ASP.NET Core Web API
│   ├── Controllers/Api/       # JWT, dashboard, category, and product endpoints
│   ├── Data/                  # EF Core context and MySQL migrations
│   ├── Services/              # Product-code and Excel business services
│   └── tests/                 # xUnit business-rule tests
├── frontend/                 # Angular SPA
├── docs/                     # Architecture, API, testing, and operations notes
└── Monitoring/               # Local Prometheus and Grafana configuration
```

## Run locally

### 1. Prerequisites

- .NET 8 SDK
- MySQL 8 running at `localhost:3306`
- Node.js 24+ and pnpm (`corepack enable` if pnpm is not available)

### 2. Configure local secrets

From the repository root, keep your MySQL password and JWT signing key outside Git:

```powershell
dotnet user-secrets set --project backend/ProductVault.csproj "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=productvault;User ID=root;Password=YOUR_MYSQL_PASSWORD;"
dotnet user-secrets set --project backend/ProductVault.csproj "Jwt:Key" "a-long-random-secret-of-at-least-32-characters"
```

### 3. Create the MySQL schema and start the API

```powershell
cd backend
dotnet ef database update
dotnet run --launch-profile https
```

The API runs at `https://localhost:7253`. In Development, browse [Swagger](https://localhost:7253/swagger) for endpoint exploration and use `https://localhost:7253/health` for the health check.

### 4. Start Angular

In a second terminal:

```powershell
cd frontend
pnpm install
pnpm start
```

Open the localhost URL printed by Angular (normally `http://localhost:4200`), register an account, then create a category and product. An empty workspace also offers a **Load demo data** button for a ready-to-present catalogue.

## API authentication

`POST /api/auth/register` and `POST /api/auth/login` return an eight-hour JWT access token. Angular stores the active session locally and its interceptor sends:

```text
Authorization: Bearer <access-token>
```

Protected API routes include `/api/dashboard`, `/api/categories`, and `/api/products`.

## Verification

```powershell
dotnet build ProductVault.sln
dotnet test ProductVault.sln
cd frontend; pnpm run build
```

## Local monitoring

With the API running on the HTTPS profile and Docker Desktop started:

```powershell
docker compose -f docker-compose.monitoring.yml up -d
```

Open [Prometheus](http://localhost:9090/targets) and [Grafana](http://localhost:3000/). See the [operations runbook](docs/operations-runbook.md) for more detail.

## Interview summary

> “I separated ProductVault into an Angular SPA and ASP.NET Core Web API. Angular uses a JWT interceptor and route guard, while the API derives the authenticated user ID from the bearer token and applies `OwnerId` filtering to every catalogue query. EF Core persists the MySQL schema and MySQL timestamp-backed concurrency tokens prevent silent lost updates.”
