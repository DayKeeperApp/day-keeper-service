# Day Keeper Service

[![CI](https://github.com/DayKeeperApp/day-keeper-service/actions/workflows/ci.yml/badge.svg)](https://github.com/DayKeeperApp/day-keeper-service/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)

Backend API for Day Keeper, a personal life management app
for calendar events, tasks, contacts, and shared lists.
Built with ASP.NET Core using Clean Architecture.

## Architecture

```text
┌─────────────────────────────────────────────────────┐
│                  DayKeeper.Api                      │
│         (Controllers, Middleware, DI)               │
├──────────────────────┬──────────────────────────────┤
│  DayKeeper.Application  │  DayKeeper.Infrastructure │
│  (Interfaces, Services) │  (Implementations, Data)  │
├──────────────────────┴──────────────────────────────┤
│                DayKeeper.Domain                     │
│            (Entities, Value Objects)                │
└─────────────────────────────────────────────────────┘
```

**Dependency rule**: each layer only depends on the layers
below it. Domain has zero dependencies.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`)
- [Task](https://taskfile.dev/) (task runner)
- [Docker](https://docs.docker.com/get-docker/) (optional, for container builds)
- [Lefthook](https://github.com/evilmartians/lefthook) (git hooks)
- EF Core CLI tools (installed via `dotnet tool restore` / `task setup`)

## Getting Started

```bash
# Clone & setup
git clone git@github.com:DayKeeperApp/day-keeper-service.git
cd day-keeper-service
task setup

# Build & run
task build
task run

# Run tests
task test
```

The API starts at `http://localhost:5000` (or `https://localhost:5001`).
Run `task` to see all available commands.

## API Endpoints

| Method | Path              | Description                     |
| ------ | ----------------- | ------------------------------- |
| GET    | `/api/helloworld` | Smoke test endpoint             |
| GET    | `/health/live`    | Liveness probe (Kubernetes)     |
| GET    | `/health/ready`   | Readiness probe (Kubernetes)    |
| GET    | `/scalar/v1`      | Interactive API docs (dev only) |

## Development

### Setup

```bash
brew install go-task lefthook
task setup
```

### What the hooks run

| Hook       | Job          | Tools                                                                  | Runs on                           |
| ---------- | ------------ | ---------------------------------------------------------------------- | --------------------------------- |
| pre-commit | Format       | `dotnet format`, `prettier`                                            | `*.cs`, `*.yml`, `*.json`, `*.md` |
| pre-commit | Lint         | `dotnet build`, `actionlint`, `markdownlint`, `shellcheck`, `hadolint` | Various                           |
| pre-commit | Security     | `gitleaks`                                                             | All staged files                  |
| commit-msg | Commitlint   | `commitlint`                                                           | Commit message                    |
| pre-push   | Test & Build | `dotnet test`, `dotnet build -c Release`                               | Full solution                     |

### Run tests with coverage

```bash
task test:coverage
```

Coverage reports (Cobertura XML) are written to `TestResults/`.

### Code style

Code style is enforced by `.editorconfig` and `task format`.
The build treats all warnings as errors (`TreatWarningsAsErrors`),
so code analysis violations fail the build.
Run `task format:check` to verify without modifying files.

### Database Migrations

Migrations are managed via EF Core CLI, wrapped in Taskfile commands:

```bash
task db:migrate:add -- AddSpacesTable   # Create migration
task db:migrate:apply                    # Apply to local DB
task db:migrate:script                   # Generate SQL script
```

See [docs/MIGRATIONS.md](docs/MIGRATIONS.md) for the full workflow.

## Docker

```bash
# Build & run
task docker:build
task docker:run

# Verify
curl http://localhost:8080/api/helloworld
curl http://localhost:8080/health/live
```

## CI/CD

GitHub Actions runs on every push to `main` and on all pull requests:

1. **Build & Test** — restore, build, test with code coverage, octocov PR comments
2. **Docker Build** — verifies the Dockerfile compiles (main branch only)

## Project Structure

```text
day-keeper-service/
├── src/
│   ├── DayKeeper.Api/              # ASP.NET Core entry point
│   │   ├── Controllers/            # REST controllers
│   │   └── Middleware/             # Exception handling
│   ├── DayKeeper.Application/      # Use cases, service interfaces
│   │   └── Interfaces/
│   ├── DayKeeper.Domain/           # Entities, domain logic
│   │   └── Entities/
│   └── DayKeeper.Infrastructure/   # Implementations, data access
│       ├── Persistence/Migrations/ # EF Core migrations
│       └── Services/
├── tests/
│   └── DayKeeper.Api.Tests/        # Unit + integration tests
│       ├── Unit/
│       └── Integration/
├── .github/
│   ├── workflows/ci.yml             # GitHub Actions pipeline
│   └── renovate.json5               # Automated dependency updates
├── Taskfile.yml                     # Task runner commands
├── lefthook.yml                     # Git hooks configuration
├── Directory.Build.props            # Shared build properties
├── Directory.Packages.props         # Central NuGet version management
├── Dockerfile                       # Multi-stage production build
└── DayKeeper.slnx                   # .NET solution file
```

## Dependency Management

[Renovate](https://docs.renovatebot.com/) keeps NuGet packages,
Docker base images, and GitHub Actions versions up to date.
Patch updates are auto-merged; minor and major updates create
PRs for review.

NuGet package versions are managed centrally in
`Directory.Packages.props` — individual `.csproj` files
reference packages without specifying versions.
