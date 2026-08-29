# Local operations runbook

## Start the application

1. Open `ProductVault.sln` in Visual Studio.
2. Configure your MySQL password with User Secrets, then run `dotnet ef database update` if the database has not yet been created.
3. Select the **https** launch profile and press F5.

Expected local endpoints:

| Service | Address |
| --- | --- |
| ProductVault UI | `https://localhost:7253` |
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
| Visual Studio has no startup item | Open `ProductVault.sln`, not the folder or only the `.csproj` file. |
| `dotnet ef database update` cannot connect | Start MySQL on port 3309 and verify the User Secrets connection string. |
| Prometheus target is DOWN | Restart ProductVault with **https** (not IIS Express), then check `https://localhost:7253/metrics` in the browser. |
| Docker cannot start containers | Start Docker Desktop, then rerun the compose command. First-time image pulls can take several minutes. |
| Grafana dashboard is missing | Restart the stack. Check that the `Monitoring/Grafana` folders remain mounted and unmodified. |
| Port already in use | Stop the conflicting local service or update the relevant port in `launchSettings.json`/`docker-compose.monitoring.yml`. |

## Routine developer checks

```powershell
dotnet build ProductVault.sln
dotnet test ProductVault.sln
docker compose -f docker-compose.monitoring.yml config
```
