<!-- markdownlint-disable MD013 -->

# Recurrence Expansion Strategy

**Status:** Accepted
**Date:** 2026-02-25
**Task:** DKS-omk

---

## Table of Contents

1. [RRULE Library Choice](#1-rrule-library-choice)
2. [Expansion Strategy](#2-expansion-strategy)
3. [New Entity: RecurrenceException](#3-new-entity-recurrenceexception)
4. [Read-Only Projection: ExpandedOccurrence](#4-read-only-projection-expandedoccurrence)
5. [Application Layer Interfaces](#5-application-layer-interfaces)
6. [CalendarEvent Changes](#6-calendarevent-changes)
7. [Exception Handling Patterns](#7-exception-handling-patterns)
8. [Sync Integration](#8-sync-integration)
9. [TaskItem Recurrence](#9-taskitem-recurrence)
10. [Implementation Sequence](#10-implementation-sequence)

---

## 1. RRULE Library Choice

### Decision: Ical.Net 5.x

| Attribute         | Value                                                |
| ----------------- | ---------------------------------------------------- |
| Package           | [Ical.Net](https://www.nuget.org/packages/Ical.net/) |
| Version           | 5.2.1 (February 2026)                                |
| License           | MIT                                                  |
| NuGet downloads   | 26.7M total                                          |
| Target framework  | .NET Standard 2.0 (compatible with .NET 10)          |
| Timezone handling | NodaTime (bundled dependency)                        |
| RFC 5545 coverage | RRULE, EXDATE, RDATE, EXRULE, RECURRENCE-ID          |

### Why Ical.Net

- Only production-grade .NET library for RFC 5545 RRULE expansion
- Actively maintained with organization-based governance (since Sept 2024)
- v5 rewrite: 50% memory reduction over v4, ANTLR parser replaced with faster SimpleDeserializer
- Proper DST-aware timezone handling via NodaTime
- Well-documented API for date-range-bounded expansion

### Alternatives Rejected

| Library                     | Reason                                                                                                                                                                                                     |
| --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Custom RRULE parser         | RFC 5545 has 14 parameters with non-obvious interaction rules. DST transitions, negative BYDAY offsets (`-1SU`), leap years, and BYSETPOS make a correct implementation a multi-month project. Not viable. |
| EWSoftware.PDI              | Smaller community, less battle-tested. Recurrence engine works standalone but lacks the ecosystem and test coverage of Ical.Net.                                                                           |
| RRule-Parser.NET            | Converts RRULE to human-readable text only. No date expansion capability.                                                                                                                                  |
| Syncfusion RecurrenceHelper | Commercial license. UI-focused. Overkill dependency for backend expansion.                                                                                                                                 |

### Integration Point

Ical.Net is added to `DayKeeper.Infrastructure` only. The Application layer depends on an `IRecurrenceExpander` abstraction, keeping the third-party library contained.

```text
Directory.Packages.props  →  <PackageVersion Include="Ical.Net" Version="5.2.1" />
DayKeeper.Infrastructure   →  <PackageReference Include="Ical.Net" />
DayKeeper.Application      →  IRecurrenceExpander (interface only)
```

---

## 2. Expansion Strategy

### Decision: Server-Side Expansion at Query Time

Recurring events are stored as **single master rows**. When a client requests a date range, the server fetches applicable masters, expands occurrences in memory using `IRecurrenceExpander`, applies exceptions, and returns the merged result.

### Approaches Evaluated

| Criterion                    | Server-side expansion             | Pre-materialized instances     | Client-side only             |
| ---------------------------- | --------------------------------- | ------------------------------ | ---------------------------- |
| Sync efficiency              | 1 row per series                  | N rows per series per year     | 1 row per series             |
| Storage cost                 | Minimal                           | Unbounded growth               | Minimal                      |
| Query performance            | Good (in-memory expansion sub-ms) | Best (simple SQL range query)  | N/A server-side              |
| "Edit all future" complexity | Low (split series)                | High (update hundreds of rows) | Low                          |
| Notification scheduling      | Works (server has occurrences)    | Works                          | Broken (server can't expand) |
| Offline client support       | Works (client expands locally)    | Works but sync-heavy           | Works                        |

**Server-side expansion wins** because:

1. **Sync efficiency.** The ChangeLog system uses a monotonic cursor. With pre-materialized instances, editing "this and all future" on a weekly series generates 52+ ChangeLog rows per year. With server-side expansion, it generates exactly 2 (original master updated + new master created).

2. **Notification scheduling.** The server needs expanded occurrences to schedule Quartz/Hangfire reminders. Client-side-only expansion breaks this.

3. **Storage alignment.** `CalendarEvent` already stores `RecurrenceRule` as a string. The existing `(CalendarId, StartAt)` composite index works for master queries.

### Query Algorithm

```sql
-- GetOccurrencesAsync(calendarIds, rangeStart, rangeEnd):

-- 1. Single events:
SELECT * FROM CalendarEvents
WHERE CalendarId IN (@calendarIds)
  AND RecurrenceRule IS NULL
  AND StartAt < @rangeEnd
  AND EndAt > @rangeStart

-- 2. Recurring masters (with exceptions eagerly loaded):
SELECT * FROM CalendarEvents
  LEFT JOIN RecurrenceExceptions ON ...
WHERE CalendarId IN (@calendarIds)
  AND RecurrenceRule IS NOT NULL
  AND StartAt < @rangeEnd
  AND (RecurrenceEndAt IS NULL
    OR RecurrenceEndAt >= @rangeStart)
```

Step 3 (in-memory, per recurring master):

- Call `IRecurrenceExpander.GetOccurrences(...)`
- Build dictionary of exceptions keyed by `OriginalStartAt`
- For each computed occurrence:
  - Cancelled exception exists: skip
  - Modified exception exists: merge override fields
  - Otherwise: project from master
- Emit `ExpandedOccurrence` records

Step 4: Merge single-event + recurring occurrences, sort by `StartAt`, return.

### Performance Characteristics (typical month view)

- 2 SQL queries (parallelizable)
- ~50 single events + ~10 recurring masters loaded from DB
- ~40 occurrences expanded in-memory (sub-millisecond via Ical.Net)
- Total latency dominated by DB round-trip, not expansion

---

## 3. New Entity: RecurrenceException

Records a modification or cancellation to a single occurrence within a recurring series. The `OriginalStartAt` identifies which occurrence is overridden (standard iCalendar RECURRENCE-ID pattern).

### RecurrenceException Fields

| Field           | Type      | Required | Description                            |
| --------------- | --------- | -------- | -------------------------------------- |
| Id              | Guid      | Yes      | From BaseEntity (client-generated)     |
| CalendarEventId | Guid      | Yes      | FK to the recurring master             |
| OriginalStartAt | DateTime  | Yes      | UTC start of the overridden occurrence |
| IsCancelled     | bool      | Yes      | If true, occurrence is deleted         |
| Title           | string?   | No       | Overridden title (null = inherit)      |
| Description     | string?   | No       | Overridden description                 |
| StartAt         | DateTime? | No       | Overridden start time (reschedule)     |
| EndAt           | DateTime? | No       | Overridden end time                    |
| Location        | string?   | No       | Overridden location                    |
| CreatedAt       | DateTime  | Yes      | From BaseEntity                        |
| UpdatedAt       | DateTime  | Yes      | From BaseEntity                        |
| DeletedAt       | DateTime? | No       | From BaseEntity (soft delete)          |

### Constraints

- **Unique index:** `(CalendarEventId, OriginalStartAt)`
- **FK:** `CalendarEventId` -> `CalendarEvent.Id` with `ON DELETE CASCADE`
- **String limits:** Title max 512, Location max 512

### ChangeLog Integration

Add `RecurrenceException = 19` to `ChangeLogEntityType` enum.

---

## 4. Read-Only Projection: ExpandedOccurrence

A computed projection returned by the query service. **Never persisted.** Not registered in DbContext.

### ExpandedOccurrence Fields

| Field                 | Type      | Description                              |
| --------------------- | --------- | ---------------------------------------- |
| CalendarEventId       | Guid      | Master event ID                          |
| RecurrenceExceptionId | Guid?     | Exception ID if occurrence was modified  |
| OriginalStartAt       | DateTime  | Computed start from RRULE (pre-override) |
| Title                 | string    | Effective title                          |
| Description           | string?   | Effective description                    |
| StartAt               | DateTime  | Effective start time                     |
| EndAt                 | DateTime  | Effective end time                       |
| IsAllDay              | bool      | From master                              |
| StartDate             | DateOnly? | From master (all-day events)             |
| EndDate               | DateOnly? | From master (all-day events)             |
| Timezone              | string    | From master                              |
| Location              | string?   | Effective location                       |
| CalendarId            | Guid      | From master                              |
| EventTypeId           | Guid?     | From master                              |
| IsRecurring           | bool      | True if occurrence of a recurring series |
| IsException           | bool      | True if modified from the master         |

Implemented as a C# `record` in the Domain layer.

---

## 5. Application Layer Interfaces

### IRecurrenceExpander

Abstracts the RRULE library. Lives in `DayKeeper.Application.Interfaces`.

```csharp
IReadOnlyList<DateTime> GetOccurrences(
    string rrule,
    DateTime seriesStart,
    string timezone,
    DateTime rangeStart,
    DateTime rangeEnd);
```

Parameters:

- `rrule`: RFC 5545 RRULE string
- `seriesStart`: DTSTART of the series (UTC)
- `timezone`: IANA timezone for DST-aware expansion
- `rangeStart`: Inclusive start of query window (UTC)
- `rangeEnd`: Exclusive end of query window (UTC)

The `timezone` parameter is critical: a weekly Monday event in "America/Chicago" must expand using local time to handle DST transitions, then convert back to UTC.

### ICalendarEventRepository

Extends `IRepository<CalendarEvent>` with specialized query methods:

```csharp
Task<IReadOnlyList<CalendarEvent>> GetSingleEventsInRangeAsync(
    IReadOnlyList<Guid> calendarIds,
    DateTime rangeStart,
    DateTime rangeEnd,
    CancellationToken ct);

Task<IReadOnlyList<CalendarEvent>> GetRecurringMastersForRangeAsync(
    IReadOnlyList<Guid> calendarIds,
    DateTime rangeStart,
    DateTime rangeEnd,
    CancellationToken ct);
```

### ICalendarEventQueryService

Orchestrates the expansion algorithm from Section 2:

```csharp
Task<IReadOnlyList<ExpandedOccurrence>> GetOccurrencesAsync(
    IReadOnlyList<Guid> calendarIds,
    DateTime rangeStart,
    DateTime rangeEnd,
    CancellationToken ct);
```

---

## 6. CalendarEvent Changes

### New Property: RecurrenceEndAt

```csharp
public DateTime? RecurrenceEndAt { get; set; }
```

Denormalized end boundary for the recurrence series (UTC). Computed from RRULE UNTIL or COUNT when saving. Null for infinite recurrence. Used to optimize range queries by filtering out expired series in SQL. The RRULE string remains the source of truth.

### New Navigation Property

```csharp
public ICollection<RecurrenceException> RecurrenceExceptions { get; set; } = [];
```

### New Index

PostgreSQL partial index for efficiently finding recurring masters:

```csharp
builder.HasIndex(e => e.CalendarId)
    .HasFilter("\"RecurrenceRule\" IS NOT NULL")
    .HasDatabaseName("IX_CalendarEvent_CalendarId_Recurring");
```

---

## 7. Exception Handling Patterns

### Delete one occurrence

Create a `RecurrenceException` with `IsCancelled = true`. The expansion loop skips it.

```json
{
  "originalStartAt": "2026-03-23T14:00:00Z",
  "isCancelled": true
}
```

### Edit one occurrence

Create a `RecurrenceException` with override fields. Null fields inherit from master.

```json
{
  "originalStartAt": "2026-03-16T14:00:00Z",
  "title": "Standup (offsite)",
  "location": "Building B"
}
```

### Edit this and all future

Split the series:

1. Modify original master's RRULE to add UNTIL (split date minus one interval)
2. Create a new CalendarEvent master with updated fields and a new RRULE starting from the split date
3. ChangeLog: 1 update + 1 create

This is the standard iCalendar approach. It avoids rewriting exception history for the original series.

### Edit all occurrences

Update the master CalendarEvent directly. Optionally clean up redundant exceptions.

---

## 8. Sync Integration

### What Gets Synced

| Entity                 | ChangeLog EntityType     | When                        |
| ---------------------- | ------------------------ | --------------------------- |
| CalendarEvent (master) | CalendarEvent (5)        | Created / Updated / Deleted |
| RecurrenceException    | RecurrenceException (19) | Created / Updated / Deleted |

### Client Sync Flow

1. Client calls `GET /api/sync?cursor=N&spaceId=...`
2. Server returns ChangeLog entries with `Id > N`
3. For CalendarEvent changes: client fetches the full entity
4. For RecurrenceException changes: client fetches the exception
5. Client stores locally and re-expands visible occurrences

### Efficiency Comparison

| Scenario             | Server-side expansion | Pre-materialized   |
| -------------------- | --------------------- | ------------------ |
| Create weekly event  | 1 sync row            | 52+ sync rows/year |
| Edit one occurrence  | 1 sync row            | 1 sync row         |
| Edit recurring title | 1 sync row            | 52+ sync rows      |
| Delete recurring     | 1 sync row            | 52+ sync rows      |

### Offline-First Support

The client has the RRULE string and all exception records locally. It can expand occurrences for any date range without server contact. When back online, it syncs only master + exception changes.

---

## 9. TaskItem Recurrence

**Out of scope for this design.** Noted here for awareness.

Tasks use a **rolling next occurrence** model rather than full expansion:

- When a recurring TaskItem is completed, compute the next due date
- Shares `IRecurrenceExpander` infrastructure
- Will be designed in a separate task

---

## 10. Implementation Sequence

Each step below is a candidate for its own beads task:

1. **Domain entities** -- RecurrenceException, ExpandedOccurrence record, CalendarEvent additions
2. **EF Configuration + Migration** -- RecurrenceExceptionConfiguration, CalendarEventConfiguration updates
3. **NuGet package + IcalNetRecurrenceExpander** -- Add Ical.Net, implement IRecurrenceExpander
4. **Application interfaces + CalendarEventQueryService** -- expansion orchestration
5. **DI registration** -- Wire up new services
6. **GraphQL/REST integration** -- ExpandedOccurrence query type, exception CRUD
7. **Unit tests** -- Expansion logic, exception merging, edge cases
