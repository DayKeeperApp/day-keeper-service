<!-- markdownlint-disable MD013 -->

# ADR-0002: API Versioning

**Status:** Accepted
**Date:** 2026-03-06

---

## Context

Day Keeper exposes both REST and GraphQL APIs to the Android client. A clear versioning strategy is needed to evolve APIs without breaking existing clients, especially given the offline-first sync protocol that relies on stable contracts.

## Decision

Use **URL path segment versioning** for REST endpoints (`/api/v{major}/...`) and **schema evolution** for GraphQL (no URL versioning, additive changes with `@deprecated` annotations).

Key choices:

- REST: major version bump only for breaking changes; additive changes are non-breaking
- GraphQL: evolve additively, deprecate before removing, no URL versioning
- Deprecation window: at least two release cycles before removal
- `Sunset` HTTP header for deprecated REST versions (RFC 8594)

## Consequences

- REST clients target a specific version and upgrade via app updates
- GraphQL clients get new fields immediately without version negotiation
- Both APIs share the same Application/Infrastructure layers (no logic duplication)
- Requires discipline to distinguish breaking vs non-breaking changes

---

_Full versioning policy: [../API-VERSIONING.md](../API-VERSIONING.md)_
