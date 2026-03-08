# Runbook

Operational procedures for the Day Keeper service.

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- [Task](https://taskfile.dev/) (task runner)
- [lefthook](https://github.com/evilmartians/lefthook) (git hooks)
- [Node.js](https://nodejs.org/) (for prettier, markdownlint, commitlint)
- PostgreSQL (local instance or container)
- [k3d](https://k3d.io/) (for local Kubernetes deployment, optional)

### First-Time Setup

```bash
task setup                     # Restore packages, install tools, set up hooks
```

Configure the database connection string via user secrets or environment
variable:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=daykeeper;Username=<user>;Password=<pass>" \
  --project src/DayKeeper.Api/DayKeeper.Api.csproj
```

Apply migrations to create the database schema:

```bash
task db:migrate:apply
```

### Running the Service

```bash
task run      # Start API on https://localhost:5101 / http://localhost:5100
task watch    # Start with hot reload
```

## Deploy to k3d

### Dev Environment

```bash
task deploy:dev
```

This chains: cluster create, docker build, image import, kubectl apply
(dev overlay). Waits for postgres (120s) and API (180s) rollout.

### Prod Environment

```bash
task deploy:prod
```

Same flow with production overlay (different ConfigMap values).

### Verify Deployment

```bash
task deploy:status                                              # All resources
kubectl -n daykeeper get pods                                   # Pod status
kubectl -n daykeeper exec -it deployment/daykeeper-api -- \
  wget -qO- http://localhost:8080/health/ready                  # Health check
```

### Teardown

```bash
task deploy:teardown    # Deletes the k3d cluster
```

## Restart Services

### Restart API Pod

```bash
kubectl -n daykeeper rollout restart deployment/daykeeper-api
kubectl -n daykeeper rollout status deployment/daykeeper-api --timeout=180s
```

### Restart PostgreSQL

```bash
kubectl -n daykeeper delete pod postgres-0
# StatefulSet controller recreates the pod automatically.
kubectl -n daykeeper rollout status statefulset/postgres --timeout=120s
```

### Full Cluster Rebuild

When the cluster is in a bad state, tear down and redeploy:

```bash
task deploy:teardown
task deploy:dev          # or task deploy:prod
```

## Check Logs

### API Logs (k3d)

```bash
task deploy:logs                                                # Tail logs
kubectl -n daykeeper logs deployment/daykeeper-api --tail=100   # Last 100 lines
kubectl -n daykeeper logs deployment/daykeeper-api --previous   # Crashed pod
```

### Init Container Logs (Migration)

```bash
kubectl -n daykeeper logs <pod-name> -c migrate
```

### PostgreSQL Logs

```bash
kubectl -n daykeeper logs statefulset/postgres --tail=100
```

### Serilog File Logs

Inside the container, logs are written to `/app/logs/daykeeper-*.log`
(daily rolling, 14-day retention). Access them with:

```bash
kubectl -n daykeeper exec -it deployment/daykeeper-api -- \
  ls /app/logs/
kubectl -n daykeeper exec -it deployment/daykeeper-api -- \
  cat /app/logs/daykeeper-$(date +%Y%m%d).log
```

### Log Levels

Default log levels (configured in `appsettings.json`):

| Source                        | Level       |
| ----------------------------- | ----------- |
| Application code              | Information |
| Microsoft.AspNetCore          | Warning     |
| Microsoft.EntityFrameworkCore | Warning     |
| System                        | Warning     |
| Quartz                        | Warning     |

To temporarily increase verbosity in development, edit
`appsettings.Development.json` and restart.

## Database Operations

### Connect to Pod Database

```bash
kubectl -n daykeeper exec -it postgres-0 -- \
  psql -U daykeeper -d daykeeper
```

### Backup

```bash
kubectl -n daykeeper exec -it postgres-0 -- \
  pg_dump -U daykeeper -d daykeeper --format=custom \
  > daykeeper-backup-$(date +%Y%m%d).dump
```

### Restore

```bash
kubectl -n daykeeper exec -i postgres-0 -- \
  pg_restore -U daykeeper -d daykeeper --clean --if-exists \
  < daykeeper-backup-20260308.dump
```

### Apply Migrations Manually

**Local:**

```bash
task db:migrate:apply
```

**k3d (via efbundle):** Migrations run automatically via the init
container on pod startup. To force a re-run, restart the pod:

```bash
kubectl -n daykeeper rollout restart deployment/daykeeper-api
```

**SQL script (for manual review):**

```bash
task db:migrate:script
# Review migrations.sql, then apply:
psql -h <host> -U <user> -d daykeeper -f migrations.sql
```

### Check Migration History

```bash
# Local
dotnet ef migrations list \
  --project src/DayKeeper.Infrastructure/DayKeeper.Infrastructure.csproj \
  --startup-project src/DayKeeper.Api/DayKeeper.Api.csproj

# In-database
kubectl -n daykeeper exec -it postgres-0 -- \
  psql -U daykeeper -d daykeeper \
  -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";"
```

## Health Check Verification

### Endpoints

| Endpoint            | Purpose         | Checks                               |
| ------------------- | --------------- | ------------------------------------ |
| `GET /health/live`  | Liveness probe  | Process is running                   |
| `GET /health/ready` | Readiness probe | Process running + database connected |

### Local

```bash
curl -s http://localhost:5100/health/live
curl -s http://localhost:5100/health/ready
```

### k3d

```bash
kubectl -n daykeeper exec -it deployment/daykeeper-api -- \
  wget -qO- http://localhost:8080/health/live
kubectl -n daykeeper exec -it deployment/daykeeper-api -- \
  wget -qO- http://localhost:8080/health/ready
```

### Probe Configuration

| Probe     | Path          | Initial Delay | Period | Timeout | Failures     |
| --------- | ------------- | ------------- | ------ | ------- | ------------ |
| Startup   | /health/live  | 5s            | 5s     | —       | 12 (60s max) |
| Liveness  | /health/live  | 10s           | 15s    | 3s      | 3            |
| Readiness | /health/ready | 15s           | 10s    | 5s      | 3            |

## Incident Response

### Quick Triage Checklist

1. **Check pod status:**

   ```bash
   kubectl -n daykeeper get pods
   ```

   Look for: `CrashLoopBackOff`, `Init:Error`, `OOMKilled`,
   `ErrImageNeverPull`, not Ready.

2. **Check recent events:**

   ```bash
   kubectl -n daykeeper get events --sort-by='.lastTimestamp' | tail -20
   ```

3. **Check API logs:**

   ```bash
   kubectl -n daykeeper logs deployment/daykeeper-api --tail=50
   ```

4. **Check database:**

   ```bash
   kubectl -n daykeeper exec -it postgres-0 -- pg_isready -U daykeeper
   ```

5. **Check network policies:**

   ```bash
   kubectl -n daykeeper get networkpolicy
   kubectl -n daykeeper describe networkpolicy
   ```

6. **Check resource usage:**

   ```bash
   kubectl -n daykeeper top pods
   ```

### Common Incident Patterns

| Symptom                 | Likely Cause                       | Action                                                                               |
| ----------------------- | ---------------------------------- | ------------------------------------------------------------------------------------ |
| Pod `CrashLoopBackOff`  | Init container (migration) failing | Check migrate container logs                                                         |
| Pod `ErrImageNeverPull` | Image not imported to k3d          | `task docker:build` then `k3d image import daykeeper-api:latest --cluster daykeeper` |
| Pod `OOMKilled`         | Memory limit exceeded              | Check for memory leaks in logs, consider increasing limits                           |
| Ready 0/1               | Database unreachable               | Check postgres pod, secrets, network policy                                          |
| 502/503 from Cloudflare | API not ready or tunnel down       | Check readiness probe, Cloudflare tunnel status                                      |

## Useful Commands

### Taskfile Quick Reference

```bash
task                    # List all available tasks
task build              # Build (Debug)
task build:release      # Build (Release)
task test               # Run all tests
task test:unit          # Unit tests only
task test:integration   # Integration tests only
task format             # Auto-format C# code
task format:check       # Verify formatting
task lint               # Run pre-commit hooks
task vuln               # Check vulnerable packages
task outdated           # Check outdated packages
task schema             # Export OpenAPI + GraphQL schemas
```

### kubectl Quick Reference

```bash
kubectl -n daykeeper get all                          # All resources
kubectl -n daykeeper get pods -o wide                 # Pods with node info
kubectl -n daykeeper describe pod <name>              # Pod details
kubectl -n daykeeper logs -f deployment/daykeeper-api # Follow logs
kubectl -n daykeeper port-forward svc/daykeeper-api 8080:8080  # Port forward
kubectl -n daykeeper exec -it postgres-0 -- bash      # Shell into postgres
```
