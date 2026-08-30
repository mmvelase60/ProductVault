# Local operations runbook

## Start the application

1. Configure your MySQL password with User Secrets using `backend/ProductVault.csproj`, then run `dotnet ef database update` from the `backend` folder if the database has not yet been created.
2. Start the API in one terminal:

   ```powershell
   cd backend
   dotnet run --launch-profile https
   ```

3. Start the Angular SPA in a second terminal:

   ```powershell
   cd frontend
   pnpm install
   pnpm start
   ```

Expected local endpoints:

| Service | Address |
| --- | --- |
| Angular UI | `http://localhost:4200` |
| ProductVault API | `https://localhost:7253` |
| Swagger (Development only) | `https://localhost:7253/swagger` |
| Health check | `https://localhost:7253/health` |
| Metrics (Development only) | `https://localhost:7253/metrics` |

## Start monitoring

With ProductVault running on the https profile and Docker Desktop started:

```powershell
docker compose -f docker-compose.monitoring.yml up -d
```

| Service | Address | Expected check |
| --- | --- | --- |
| Prometheus | `http://localhost:9090/targets` | `productvault-webapp` is **UP**. |
| Grafana | `http://localhost:3000` | Sign in as `admin` / `admin`; dashboard is provisioned. |

Stop the local containers without deleting their data:

```powershell
docker compose -f docker-compose.monitoring.yml down
```

To remove local monitoring data as well:

```powershell
docker compose -f docker-compose.monitoring.yml down --volumes
```

## Common troubleshooting

| Symptom | Check / resolution |
| --- | --- |
| Angular page cannot call the API | Ensure the API is running on `https://localhost:7253`, then accept its local development certificate if the browser asks. |
| `dotnet ef database update` cannot connect | Start MySQL on port 3306 and verify the User Secrets connection string. |
| Prometheus target is DOWN | Restart ProductVault with **https** (not IIS Express), then check `https://localhost:7253/metrics` in the browser. |
| Docker cannot start containers | Start Docker Desktop, then rerun the compose command. First-time image pulls can take several minutes. |
| Grafana dashboard is missing | Restart the stack. Check that the `Monitoring/Grafana` folders remain mounted and unmodified. |
| Port already in use | Stop the conflicting local service or update the relevant port in `launchSettings.json`/`docker-compose.monitoring.yml`. |

## Routine developer checks

```powershell
dotnet build ProductVault.sln
dotnet test ProductVault.sln
cd frontend; pnpm run build
docker compose -f docker-compose.monitoring.yml config
```
