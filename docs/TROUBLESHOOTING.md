# Troubleshooting

Quick reference for diagnosing and resolving common issues.

## Build & Compilation

### bin/ directory corruption (nested directories)

**Symptom:** Build fails with cryptic MSBuild errors or missing references.
The `bin/` or `obj/` directories contain unexpected nested subdirectories.

**Fix:**

```bash
rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj
dotnet restore --force-evaluate
```

### Release configuration missing staticwebassets.build.json

**Symptom:** `dotnet publish -c Release` fails on first run or after
cleaning build outputs.

**Fix:** Build Release before publishing:

```bash
dotnet build -c Release
dotnet publish src/DayKeeper.Api/DayKeeper.Api.csproj -c Release
```

Or use the Taskfile shortcut:

```bash
task build:release && task publish
```

### TreatWarningsAsErrors build failures

**Symptom:** Build fails with warnings treated as errors (nullable
reference types, unused variables, etc.).

**Cause:** `Directory.Build.props` sets `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
and `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.

**Fix:** Address the warning in source code. Do not add `#pragma warning disable`
unless there is a documented justification. Check the specific warning code
(e.g. CS8618, IDE0060) and fix accordingly.

### packages.lock.json conflicts

**Symptom:** `dotnet restore` fails with lock file mismatch after
pulling changes from another branch.

**Fix:**

```bash
dotnet restore --force-evaluate
```

This regenerates the lock file. Commit the updated `packages.lock.json` files.

## EF Core Migrations

### Conflicting migrations from parallel branches

**Symptom:** Build error in migration snapshot, or `dotnet ef` reports
conflicting model snapshots.

**Fix:** Follow the procedure in [MIGRATIONS.md](MIGRATIONS.md#resolving-conflicts):

1. Merge the target branch into your branch.
2. Delete your migration files (keep entity/configuration changes).
3. Recreate: `task db:migrate:add -- YourMigrationName`
4. Verify the regenerated migration is correct.

### Migration charset mismatch (BOM vs ASCII)

**Symptom:** Editorconfig or encoding warnings on migration files.

**Cause:** EF Core generates migrations with UTF-8 BOM. Older migrations
may be ASCII. The `.editorconfig` sets `charset = unset` for
`**/Migrations/*.cs` to accommodate both.

**Fix:** No action needed if `.editorconfig` is in place. If you see
charset warnings, verify the editorconfig rule exists:

```ini
[**/Migrations/*.cs]
charset = unset
```

### efbundle failure in init container

**Symptom:** The `migrate` init container crashes in k3d. API pod stays
in `Init:CrashLoopBackOff`.

**Diagnose:**

```bash
kubectl -n daykeeper logs daykeeper-api-<pod-id> -c migrate
```

**Common causes:**

- **Connection string wrong:** Check the `daykeeper-api-secrets` Secret
  contains a valid `ConnectionStrings__DefaultConnection`.
- **Postgres not ready:** The deployment waits for the postgres
  StatefulSet rollout, but if postgres is unhealthy the init container
  will fail. Check postgres pod status first.
- **Bundle extraction fails:** The init container uses a read-only root
  filesystem. Ensure the `/tmp` emptyDir volume is mounted (it is by
  default in the base manifests).

## Pre-commit / Pre-push Hooks

### Identifying which hook failed

**Symptom:** `git commit` or `git push` is rejected by lefthook.

**Diagnose:** Lefthook prints the failing job and command name. The
hooks run in this order:

| Hook       | Jobs (sequential) | Commands (parallel within job)                                                                                  |
| ---------- | ----------------- | --------------------------------------------------------------------------------------------------------------- |
| pre-commit | format            | dotnet-format, prettier                                                                                         |
| pre-commit | lint              | dotnet-build, actionlint, markdownlint, shellcheck, hadolint, jsonlint, yamllint, kubescore, editorconfig-check |
| pre-commit | security          | gitleaks, checkov                                                                                               |
| pre-push   | (all parallel)    | dotnet-verify-format, dotnet-build-release, dotnet-test, dotnet-publish, dotnet-vuln, dotnet-outdated           |
| commit-msg |                   | commitlint                                                                                                      |

### dotnet-format hook fails

**Fix:** Run formatting and restage:

```bash
task format
git add -u
```

### commitlint rejects commit message

**Cause:** Commit messages must follow
[Conventional Commits](https://www.conventionalcommits.org/). Format:
`type(scope): description` (e.g. `feat: add calendar sharing`).

**Valid types:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`,
`chore`, `ci`, `build`, `perf`, `revert`.

### gitleaks detects secrets

**Fix:** Remove the secret from tracked files. If it is a false
positive, add an inline `# gitleaks:allow` comment or update
`.gitleaksignore`.

### Running hooks manually

```bash
task lint          # Run pre-commit hooks on staged files
task lint:all      # Run pre-commit hooks on all files
lefthook run pre-push   # Run pre-push hooks manually
```

## Database

### PostgreSQL connection failure (local)

**Symptom:** `task run` or `task db:migrate:apply` fails with
"connection refused" or "authentication failed".

**Checks:**

1. Is PostgreSQL running? `pg_isready -h localhost -p 5432`
2. Is the connection string set? Check user secrets or environment:
   `ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=daykeeper;Username=...;Password=...`
3. Does the database exist? `psql -h localhost -l | grep daykeeper`

### PostgreSQL connection failure (k3d)

**Diagnose:**

```bash
kubectl -n daykeeper get pods -l app.kubernetes.io/name=postgres
kubectl -n daykeeper logs statefulset/postgres
kubectl -n daykeeper exec -it postgres-0 -- pg_isready -U daykeeper
```

**Common causes:**

- PVC not bound (check `kubectl -n daykeeper get pvc`).
- Secret `postgres-credentials` missing or incorrect.

## Docker & k3d

### Docker build fails on MSBuild glob

**Symptom:** Build stage fails with glob-related error on `*.resx` files.

**Cause:** MSBuild glob expansion fails on overlay2 filesystem when
`bin/Debug` doesn't exist.

**Fix:** Already handled in the Dockerfile with:

```dockerfile
RUN mkdir -p src/DayKeeper.Api/bin/Debug
```

If building outside Docker, ensure this directory exists or clean and
rebuild.

### k3d image import fails

**Symptom:** `k3d image import` hangs or fails.

**Checks:**

1. Is the k3d cluster running? `k3d cluster list`
2. Does the image exist locally? `docker images | grep daykeeper-api`
3. Try recreating: `task deploy:teardown && task deploy:dev`

### Deployment rollout timeout

**Symptom:** `kubectl rollout status` times out (120s for postgres,
180s for API).

**Diagnose:**

```bash
kubectl -n daykeeper describe deployment daykeeper-api
kubectl -n daykeeper describe pod <pod-name>
kubectl -n daykeeper get events --sort-by='.lastTimestamp'
```

**Common causes:**

- Image not imported into k3d (pod stuck in `ErrImageNeverPull`).
- Init container failing (see [efbundle failure](#efbundle-failure-in-init-container)).
- Resource limits too low (OOMKilled).

## Backup & Restore

### Backup job Permission denied

**Symptom:** Backup job logs show `can't create /backups/...: Permission denied`.

**Cause:** The `hostPath` directory was created by the kubelet as root. The
init container that fixes ownership may not have run (e.g. the CronJob spec
was applied without the init container).

**Fix:** Redeploy the CronJob and trigger a new backup:

```bash
kubectl apply -k deploy/k8s/overlays/dev/
task deploy:db-backup
```

### Backup completes instantly with tiny file

**Symptom:** Backup job succeeds but the dump file is unexpectedly small.

**Cause:** The database may be empty (e.g. after a fresh deployment before
any data is created).

**Fix:** Verify the database has data:

```bash
kubectl -n daykeeper exec -it postgres-0 -- \
  psql -U daykeeper -d daykeeper -c "SELECT count(*) FROM \"__EFMigrationsHistory\";"
```

### No backups found on host

**Symptom:** `ls /var/backups/daykeeper/` shows nothing or the directory
doesn't exist.

**Cause:** With k3d, `hostPath` volumes only map to the host if the cluster
was created with `--volume "/var/backups/daykeeper:/var/backups/daykeeper"`.
The `deploy:cluster` task includes this flag, but clusters created before this
change need to be recreated.

**Fix:**

```bash
task deploy:teardown && task deploy:dev
```

### Restore job fails with "No backups found"

**Symptom:** `task deploy:db-restore:latest` fails with no backups found.

**Fix:** Run a manual backup first, then retry:

```bash
task deploy:db-backup
task deploy:db-restore:latest
```

## Health Checks

### Readiness probe failing

**Symptom:** Pod is Running but not Ready. Traffic is not routed.

**Cause:** The `/health/ready` endpoint checks database connectivity.
If postgres is down or the connection string is wrong, readiness fails.

**Diagnose:**

```bash
kubectl -n daykeeper exec -it deployment/daykeeper-api -- \
  wget -qO- http://localhost:8080/health/ready
```

### Startup probe exhaustion

**Symptom:** Pod restarts repeatedly during startup. Events show
"Startup probe failed".

**Cause:** The startup probe allows 60 seconds (12 attempts x 5s).
If the app takes longer to start (e.g. waiting for migrations), the
pod is killed.

**Fix:** Check init container logs. The `migrate` init container should
complete before the API container starts.

## Firebase / Notifications

### Missing Firebase credentials

**Symptom:** Notification sending fails with "Could not load the
default credentials" or similar Firebase error.

**Checks:**

- **Local:** Ensure `GOOGLE_APPLICATION_CREDENTIALS` points to a valid
  service account JSON file (see `launchSettings.json`).
- **k3d:** Ensure the `daykeeper-api-secrets` Secret contains the
  `firebase-credentials.json` key and the file is valid JSON.

### Stale FCM tokens

**Symptom:** Notifications fail for specific users. Logs show
"Requested entity was not found" or "registration-token-not-registered".

**Cause:** The `FcmNotificationSender` automatically soft-deletes
devices with stale tokens. No manual action needed. If the problem
persists, the user needs to re-register their device.

## CI/CD

### Migration validation fails in CI

**Symptom:** The "Validate migrations" step fails in GitHub Actions.

**Cause:** The CI pipeline generates an idempotent SQL script and
applies migrations to a test database. This catches broken snapshots
and conflicting migrations.

**Fix:** Run locally to reproduce:

```bash
task db:migrate:script
task db:migrate:apply
```

### Trivy scan fails

**Symptom:** Docker build job fails at "Scan with Trivy" step.

**Cause:** Trivy reports CRITICAL or HIGH CVEs in the container image.

**Fix:** Update the affected base image or NuGet packages. Check:

```bash
task vuln
task outdated
```

### Format check fails in CI

**Symptom:** The "Verify formatting" step fails.

**Fix:** Run locally and commit:

```bash
task format
git add -u && git commit -m "style: fix formatting"
```
