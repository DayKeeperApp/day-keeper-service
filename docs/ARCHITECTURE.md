# Architecture

Living architecture reference for the Day Keeper service.
For the original design plan and rationale, see [day-keeper-plan.md](day-keeper-plan.md).

## Clean Architecture Layers

```mermaid
flowchart BT
  Domain["DayKeeper.Domain\n(Entities, Enums, Interfaces)"]
  App["DayKeeper.Application\n(Service Interfaces, Validation, DTOs)"]
  Infra["DayKeeper.Infrastructure\n(EF Core, Services, Jobs)"]
  Api["DayKeeper.Api\n(Controllers, GraphQL, Middleware)"]

  App --> Domain
  Infra --> Domain
  Infra --> App
  Api --> App
  Api --> Infra
```

**Dependency rule:** each layer only depends on the layers below it.

### Domain

Zero external dependencies. Contains:

- **Entities** &mdash; All inherit from `BaseEntity` (Id, CreatedAt, UpdatedAt,
  DeletedAt soft-delete). Core models: `Tenant`, `User`, `Space`,
  `SpaceMembership`, `Calendar`, `CalendarEvent`, `EventReminder`,
  `RecurrenceException`, `TaskItem`, `Project`, `Person`, `ContactMethod`,
  `Address`, `ImportantDate`, `ShoppingList`, `ListItem`, `Attachment`,
  `Device`, `ChangeLog`.
- **Enums** &mdash; `SpaceRole`, `SpaceType`, `TaskItemStatus`,
  `TaskItemPriority`, `ReminderMethod`, `ContactMethodType`,
  `DevicePlatform`, `WeekStart`, `ChangeLogEntityType`, `ChangeOperation`.
- **Interfaces** &mdash; `ITenantScoped` (required TenantId),
  `IOptionalTenantScoped` (nullable TenantId for system spaces).

### Application

References Domain only. Contains:

- **Service interfaces** &mdash; `ITenantService`, `IUserService`,
  `ISpaceService`, `ICalendarService`, `IEventService`, `ITaskItemService`,
  `IPersonService`, `IShoppingListService`, `IAttachmentService`,
  `IDeviceService`, `ISyncService`, `IRecurrenceExpander`,
  `INotificationSender`, `IReminderSchedulerService`, `IDateTimeProvider`,
  `IAttachmentStorageService`, `IRepository<T>`.
- **Validation** &mdash; FluentValidation command records
  (`CreateTenantCommand`, `UpdateCalendarEventCommand`, etc.) and their
  validators.
- **DTOs** &mdash; Sync protocol payloads (`SyncPullRequest`,
  `SyncPullResponse`, `SyncPushRequest`, `SyncPushResponse`,
  `SyncChangeEntry`, `SyncConflict`).
- **Exceptions** &mdash; `EntityNotFoundException`,
  `BusinessRuleViolationException`, `InputValidationException`,
  `DuplicateSlugException`, and other domain-specific errors.

### Infrastructure

References Domain and Application. Contains:

- **Persistence** &mdash; `DayKeeperDbContext` with Npgsql, tenant-scoped
  query filters, and soft-delete filters. Entity configurations follow the
  `BaseEntityConfiguration<T>` pattern (auto-discovered via
  `ApplyConfigurationsFromAssembly`). Interceptors: `AuditFieldsInterceptor`
  (timestamps), `ChangeLogInterceptor` (append-only mutation journal).
  Generic `Repository<T>`.
- **Services** &mdash; Implementations of all Application interfaces
  including `SyncService` (cursor-based sync with LWW conflict resolution),
  `FcmNotificationSender` (Firebase Cloud Messaging),
  `IcalNetRecurrenceExpander` (iCalendar RRULE expansion),
  `ReminderSchedulerService` (Quartz job scheduling).
- **Jobs** &mdash; `ReminderNotificationJob` (Quartz job that dispatches
  push notifications at reminder time).

### Api

References Application and Infrastructure. Contains:

- **REST controllers** &mdash; `SyncController`
  (`/api/v1/sync/pull`, `/api/v1/sync/push`), `AttachmentsController`
  (`/api/v1/attachments`).
- **GraphQL** &mdash; Hot Chocolate server at `/graphql` with query and
  mutation type extensions for every domain entity. Cursor pagination
  (default 25, max 100), filtering, sorting, projections.
- **Validation pipeline** &mdash; `ValidationTypeInterceptor` auto-wires
  `ValidationMiddleware` into mutations; `InputFactory` maps GraphQL
  arguments to FluentValidation commands.
- **Error handling** &mdash; `ExceptionHandlingMiddleware` (REST),
  `DomainErrorFilter` (GraphQL).
- **Tenant context** &mdash; `HttpTenantContext` resolves tenant from
  `X-Tenant-Id` header (JWT claims planned).

## Request Lifecycle

```mermaid
flowchart TD
  Client([HTTP Request])
  EH[ExceptionHandlingMiddleware]
  SL[Serilog Request Logging]
  CORS[CORS]
  REST{Route?}
  Controllers[REST Controllers]
  GQL[GraphQL /graphql]
  VM[ValidationMiddleware]
  Svc[Application Services]
  Repo[Repository]
  DbCtx[DayKeeperDbContext]
  AF[AuditFieldsInterceptor]
  CL[ChangeLogInterceptor]
  PG[(PostgreSQL)]

  Client --> EH --> SL --> CORS --> REST
  REST -->|/api/v1/*| Controllers
  REST -->|/graphql| GQL
  GQL --> VM --> Svc
  Controllers --> Svc
  Svc --> Repo --> DbCtx
  DbCtx --> AF --> CL --> PG
```

## Sync Flow

The sync protocol uses a monotonic cursor (auto-increment `ChangeLog.Id`)
for efficient incremental synchronization. See
[SYNC-PROTOCOL.md](SYNC-PROTOCOL.md) for the full specification.

```mermaid
sequenceDiagram
  participant App as Android Client
  participant SC as SyncController
  participant SS as SyncService
  participant DB as DbContext
  participant CL as ChangeLog Table

  Note over App,CL: Push Phase (upload local changes)
  App->>SC: POST /api/v1/sync/push<br/>{changes: [...]}
  SC->>SS: PushAsync(changes)
  loop Each change
    SS->>DB: Load entity by ID
    alt Client timestamp >= Server timestamp
      SS->>DB: Apply change (create/update/delete)
      DB-->>CL: ChangeLogInterceptor writes entry
    else Client timestamp < Server timestamp
      SS-->>SS: Record conflict (LWW rejection)
    end
  end
  SS-->>SC: {appliedCount, rejectedCount, conflicts}
  SC-->>App: 200 OK

  Note over App,CL: Pull Phase (download server changes)
  App->>SC: POST /api/v1/sync/pull<br/>{cursor, spaceId?, limit?}
  SC->>SS: PullAsync(cursor, spaceId, limit)
  SS->>CL: SELECT WHERE Id > cursor<br/>ORDER BY Id LIMIT 1000
  CL-->>SS: Change entries
  SS->>DB: Load full entities for each entry
  SS-->>SC: {changes, cursor, hasMore}
  SC-->>App: 200 OK
```

## Notification Flow

Event reminders are scheduled as Quartz jobs and delivered via Firebase
Cloud Messaging.

```mermaid
sequenceDiagram
  participant GQL as GraphQL Mutation
  participant ES as EventService
  participant RS as ReminderSchedulerService
  participant Q as Quartz Scheduler
  participant RJ as ReminderNotificationJob
  participant FCM as FcmNotificationSender
  participant FB as Firebase Cloud Messaging
  participant App as Android Client

  GQL->>ES: Create EventReminder
  ES->>RS: ScheduleReminderAsync(reminder, event)
  RS->>Q: Schedule job at<br/>eventStart - minutesBefore

  Note over Q,App: At scheduled time
  Q->>RJ: Execute job
  RJ->>RJ: Load event, calendar,<br/>space members, devices
  RJ->>FCM: SendAsync(tokens, title, body, data)
  FCM->>FB: MulticastMessage
  FB-->>App: Push notification

  alt Stale FCM token detected
    FCM->>FCM: Soft-delete stale device
  end
```

## Database Schema Overview

All tables share a common pattern: client-generated UUID primary keys,
`created_at`/`updated_at` audit timestamps, and `deleted_at` for
soft-deletes.

```mermaid
erDiagram
  Tenant ||--o{ User : has
  Tenant ||--o{ Space : owns
  Space ||--o{ SpaceMembership : has
  User ||--o{ SpaceMembership : joins
  User ||--o{ Device : registers

  Space ||--o{ Calendar : contains
  Calendar ||--o{ CalendarEvent : contains
  CalendarEvent ||--o{ EventReminder : has
  CalendarEvent ||--o{ RecurrenceException : has
  CalendarEvent ||--o{ Attachment : has

  Space ||--o{ Project : contains
  Space ||--o{ TaskItem : contains
  Project ||--o{ TaskItem : groups
  TaskItem ||--o{ TaskCategory : tagged
  TaskItem ||--o{ Attachment : has

  Space ||--o{ Person : contains
  Person ||--o{ ContactMethod : has
  Person ||--o{ Address : has
  Person ||--o{ ImportantDate : has
  Person ||--o{ Attachment : has

  Space ||--o{ ShoppingList : contains
  ShoppingList ||--o{ ListItem : contains
```

The `ChangeLog` table is an append-only journal with an auto-increment
`Id` (used as the sync cursor), `EntityType`, `EntityId`, `Operation`,
`Timestamp`, `TenantId`, and `SpaceId`.

## Deployment Topology

```mermaid
flowchart LR
  CF[Cloudflare Tunnel]
  Kong[Kong Gateway]

  subgraph k3d["k3d Cluster (namespace: daykeeper)"]
    direction TB
    Init["Init Container\n(efbundle migrations)"]
    API["DayKeeper API\nASP.NET Core :8080"]
    PG[(PostgreSQL\nStatefulSet)]
    PVC_A[("PVC\nattachments")]
    PVC_DB[("PVC\ndb data")]
    FB["Firebase Credentials\n(K8s Secret)"]

    Init -->|runs before| API
    API --> PG
    API --- PVC_A
    API --- FB
    PG --- PVC_DB
  end

  CF --> Kong --> API
```

The API container runs as non-root (UID 10654) with a read-only root
filesystem. Liveness (`/health/live`), readiness (`/health/ready`), and
startup probes ensure rolling updates are safe.
