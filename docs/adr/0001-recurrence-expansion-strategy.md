<!-- markdownlint-disable MD013 -->

# ADR-0001: Recurrence Expansion Strategy

**Status:** Accepted
**Date:** 2026-02-25

---

## Context

Day Keeper needs to support recurring calendar events (daily, weekly, monthly, etc.) with the ability to modify or cancel individual occurrences. The implementation must integrate with the existing sync protocol and support offline-first clients.

## Decision

Use **server-side expansion at query time** with **Ical.Net 5.x** for RFC 5545 RRULE processing. Recurring events are stored as single master rows. When a client requests a date range, the server expands occurrences in memory, applies exceptions, and returns the merged result.

Key choices:

- Ical.Net for DST-aware RRULE expansion (contained in Infrastructure layer behind `IRecurrenceExpander`)
- `RecurrenceException` entity for per-occurrence modifications and cancellations
- `ExpandedOccurrence` read-only projection (never persisted)
- Series splitting for "edit this and all future" operations

## Consequences

- Sync stays efficient: one ChangeLog row per series operation instead of N rows per occurrence
- Server can schedule reminder notifications (has access to expanded occurrences)
- Clients can expand locally for offline use
- Query performance is dominated by DB round-trip, not expansion (sub-ms in memory)
- Third-party dependency (Ical.Net) is isolated behind an abstraction

---

_Full design document: [../RECURRENCE.md](../RECURRENCE.md)_
