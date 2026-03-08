# Day Keeper Service

[![CI](https://github.com/DayKeeperApp/day-keeper-service/actions/workflows/ci.yml/badge.svg)](https://github.com/DayKeeperApp/day-keeper-service/actions/workflows/ci.yml)
![Coverage](https://raw.githubusercontent.com/DayKeeperApp/day-keeper-service/badges/badges/coverage.svg)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
[![Renovate](https://img.shields.io/badge/renovate-enabled-brightgreen?logo=renovatebot)](https://github.com/DayKeeperApp/day-keeper-service/pulls?q=is%3Apr+author%3Arenovate)
[![Link checker](https://github.com/DayKeeperApp/day-keeper-service/actions/workflows/links.yml/badge.svg)](https://github.com/DayKeeperApp/day-keeper-service/actions/workflows/links.yml)

Backend API for Day Keeper, a personal life management app for calendar
events, tasks, contacts, and shared lists. Built with ASP.NET Core (.NET 10)
and PostgreSQL using Clean Architecture. Designed for offline-first sync with
mobile clients.

## Quick Start

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download),
[Task](https://taskfile.dev/), and
[Lefthook](https://github.com/evilmartians/lefthook).
See the [Runbook](docs/RUNBOOK.md) for full setup including database,
Docker, and Firebase.

```bash
git clone git@github.com:DayKeeperApp/day-keeper-service.git
cd day-keeper-service
task setup   # restores packages, installs tools & git hooks
task run     # starts API at http://localhost:5100
```

Run `task test` for tests, or `task` to list all commands.
API docs are available at `/scalar/v1` in development.

## Tech Stack

- .NET 10 / ASP.NET Core
- Entity Framework Core + PostgreSQL
- Clean Architecture (Domain / Application / Infrastructure / Api)
- Firebase Cloud Messaging (push notifications)
- Docker multi-stage builds
- GitHub Actions CI/CD
- Renovate (automated dependency updates)

## Documentation

| Document                                                    | Description                                                 |
| ----------------------------------------------------------- | ----------------------------------------------------------- |
| [Architecture](docs/ARCHITECTURE.md)                        | Clean Architecture layers, dependency rules, project layout |
| [Runbook](docs/RUNBOOK.md)                                  | Local dev setup, Docker, Firebase, deployment, maintenance  |
| [Migrations](docs/MIGRATIONS.md)                            | EF Core migration workflow and conventions                  |
| [Troubleshooting](docs/TROUBLESHOOTING.md)                  | Common build, test, and runtime issues                      |
| [Sync Protocol](docs/SYNC-PROTOCOL.md)                      | Offline-first sync protocol specification                   |
| [Recurrence](docs/RECURRENCE.md)                            | Recurring events expansion strategy                         |
| [API Versioning](docs/API-VERSIONING.md)                    | REST and GraphQL versioning policy                          |
| [API Schemas](docs/api/)                                    | OpenAPI and GraphQL schema files                            |
| [ADRs](docs/adr/index.md)                                   | Architecture Decision Records                               |
| [Kong Gateway](docs/kong-requirements.md)                   | Kong gateway integration requirements                       |
| [Cloudflare Tunnel](docs/cloudflare-tunnel-requirements.md) | Cloudflare tunnel setup for self-hosted access              |
| [Design Plan](docs/day-keeper-plan.md)                      | Original architecture and data model plan                   |

## Project Structure

```text
day-keeper-service/
├── src/
│   ├── DayKeeper.Api/              # ASP.NET Core entry point
│   ├── DayKeeper.Application/      # Use cases, service interfaces
│   ├── DayKeeper.Domain/           # Entities, domain logic
│   └── DayKeeper.Infrastructure/   # EF Core, external services
├── tests/
│   └── DayKeeper.Api.Tests/        # Unit + integration tests
├── docs/                           # Architecture & operations docs
├── .github/workflows/              # CI/CD pipelines
├── Taskfile.yml                    # Task runner commands
├── lefthook.yml                    # Git hooks configuration
├── Dockerfile                      # Multi-stage production build
└── DayKeeper.slnx                  # .NET solution file
```

## Contributing

See the [Runbook](docs/RUNBOOK.md) for development environment setup.
Git hooks (via Lefthook) enforce formatting, linting, and tests
automatically on commit and push.
