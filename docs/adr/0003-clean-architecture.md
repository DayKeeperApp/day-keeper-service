<!-- markdownlint-disable MD013 -->

# ADR-0003: Clean Architecture

**Status:** Accepted
**Date:** 2026-02-15

---

## Context

Day Keeper is a multi-tenant backend service with complex domain logic (calendars, tasks, contacts, shopping lists), an offline-first sync protocol, and multiple API surfaces (REST + GraphQL). The architecture must support testability, clear boundaries, and independent evolution of each layer.

## Decision

Adopt a **four-layer Clean Architecture**: Domain, Application, Infrastructure, and Api.

- **Domain** — Zero external dependencies. Entities, enums, and interfaces only.
- **Application** — References Domain. Service interfaces, validation (FluentValidation), DTOs, and domain exceptions.
- **Infrastructure** — References Domain and Application. EF Core persistence, external service implementations (Firebase, Quartz, Ical.Net), interceptors.
- **Api** — References Application and Infrastructure. REST controllers, GraphQL (Hot Chocolate), middleware, DI wiring.

Dependency rule: each layer only depends on the layers below it.

## Consequences

- Domain logic is fully testable without infrastructure dependencies
- Third-party libraries are contained in Infrastructure behind abstractions
- API layer can be swapped or extended (e.g. adding gRPC) without touching business logic
- More projects to manage and occasional ceremony for simple changes
- New developers need to understand the layer boundaries

---

_Full architecture reference: [../ARCHITECTURE.md](../ARCHITECTURE.md)_
