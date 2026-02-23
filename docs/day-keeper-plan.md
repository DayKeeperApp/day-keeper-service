# Day Keeper App — Architecture & Data Model Plan (v1)

**Status:** Draft Baseline
**Audience:** Personal project (single operator, multi-user capable)
**Goal:** Scalable, offline-first, self-hosted life management
platform with Android client

---

## Table of Contents

1. Vision & Scope
2. High-Level Architecture
3. Technology Stack
4. Security Model
5. Sync Strategy
6. Notifications Strategy
7. Storage Strategy (Attachments)
8. Sharing & Authorization Model
9. Feature Specification (Locked v1)
10. Database Design Principles
11. Database Schema (Detailed)
12. Indexing & Performance
13. Future-Proofing Notes
14. Open Questions / Future Enhancements

---

## 1. Vision & Scope

## Primary Goals

- Personal life management system
- Offline-first mobile experience
- Multi-user capable from day one
- Fully self-hosted and free to operate
- Scalable and maintainable schema
- Support sharing of key resources

## Core Domains

- Calendar & events
- People & important dates
- Tasks & projects
- Lists (shopping)
- Attachments
- Notifications & reminders
- Background sync

---

## 2. High-Level Architecture

Android App (Kotlin, Compose)
│
│ GraphQL (normal operations)
│ REST (sync)
▼
Kong Gateway
▼
Backend Service (C# / ASP.NET Core)
├── Quartz/Hangfire Scheduler
├── Attachment File Store (PVC)
└── PostgreSQL

## Deployment Environment

- Local Kubernetes via **k3d**
- Public access via **Cloudflare Tunnel**
- Single gateway (Kong) for future services

---

## 3. Technology Stack

## Backend

- Language: **C# (.NET LTS)**
- Framework: **ASP.NET Core**
- ORM: **Entity Framework Core**
- Scheduler: **Quartz.NET** (or Hangfire)
- API Style:
  - GraphQL → interactive operations
  - REST → sync endpoints

## Mobile

- Language: **Kotlin**
- UI: **Jetpack Compose**
- Local DB: **Room (SQLite)**
- Networking: Retrofit/OkHttp or Ktor
- Background work: WorkManager
- Push: Firebase Cloud Messaging (FCM)

## Infrastructure

- Kubernetes: **k3d**
- Gateway: **Kong**
- Tunnel: **Cloudflare Tunnel**
- Database: **PostgreSQL**
- Attachment storage: **PVC-backed filesystem**

---

## 4. Security Model (Balanced)

## Included

- TLS in transit
- Auth + refresh tokens
- Device registration
- Encrypted host storage
- Kong as single exposed entry

## Not Included (by design)

- End-to-end encryption (deferred)
- Zero-knowledge server model

**Rationale:** trusted self-hosted environment.

---

## 5. Sync Strategy (Offline-First)

## Protocol Split

- **GraphQL**
  - CRUD operations
  - rich queries
- **REST**
  - `/sync/push`
  - `/sync/pull`

## Sync Requirements

All syncable tables include:

- `created_at`
- `updated_at`
- `deleted_at` (tombstone)

## Change Feed

- Global monotonic cursor
- Append-only `change_log`
- Client pulls by cursor

## Conflict Strategy (v1)

- Last-write-wins
- Room for future refinement

---

## 6. Notifications Strategy

## Model

### Server-driven push (FCM)

### Flow

1. Backend schedules reminders
2. Quartz/Hangfire triggers
3. Backend sends FCM
4. App displays notification
5. App performs background sync

## Optional (Client)

- Local custom reminders allowed
- Server remains source of truth

---

## 7. Storage Strategy — Attachments

## Approach: PVC-backed filesystem

### Why

- Fully free
- Simple operations
- Keeps Postgres lean
- Future migration to S3-compatible possible

## Supported Media Types (v1)

### Images

- image/jpeg
- image/png
- image/webp
- image/heic (optional)

### Documents

- application/pdf

### Not Supported (v1)

- video
- audio

## Phone Storage Policy

- Sync **metadata only**
- Download bytes on demand
- Local LRU cache
- Configurable size cap

---

## 8. Sharing & Authorization Model

## Core Concept: Spaces

A **space** is the fundamental sharing boundary.

Types:

- personal
- shared
- system

## Why Spaces

- Clean permission model
- Works across calendars, lists, projects
- Avoids per-entity ACL sprawl
- Future collaboration ready

## Roles

- owner
- editor
- viewer

Permissions enforced at space level (v1).

---

## 9. Feature Specification (Locked v1)

## Accounts & Devices

- Multi-user support
- Device registration
- Timezone preferences
- Week start preference

## Calendar

- Multiple calendars per space
- Shared calendars supported
- Event types (system-defined)
- Holidays via system calendars
- Single events
- Recurring events
- Event reminders (multiple)

## People

- Contacts
- Contact methods (phone/email)
- Addresses
- Important dates (birthday, etc.)
- Attachments on people

## Tasks & Projects

- Shared projects
- Tasks inside or outside projects
- Recurring tasks
- Priority
- Categories (system + user)

## Lists (Shopping)

- Shared lists
- Items with:
  - decimal quantity
  - recommended unit OR freeform unit
  - checked state
  - ordering

## Attachments

Supported on:

- events
- tasks
- people

## Sync (v1)

- REST push/pull
- Tombstones
- Server cursor

## Notifications

- Server-driven via FCM
- Multiple reminders per item
- Future digest mode possible

---

## 10. Database Design Principles

## IDs

- UUID primary keys

## Time Handling

- UTC timestamps
- Explicit timezone fields

## Naming Uniqueness

Use normalized column:

```sql
lower(regexp_replace(trim(name), '\s+', ' ', 'g'))
```

Prevents:

- Appointment vs appointment
- whitespace variants

## Ownership

All major entities include:

- `tenant_id`
- `space_id`

---

## 11. Database Schema (Detailed)

(See earlier planning discussion for full field-level detail.)

---

## 12. Indexing & Performance (Minimum Set)

Recommended indexes:

## Events

- (calendar_id, start_at)
- (calendar_id, start_date)

## Tasks

- (space_id, status, due_at)
- (space_id, status, due_date)

## Lists

- (list_id, is_checked, sort_order)

## Sync

- (tenant_id, id)
- (space_id, id)

## General

- (space_id, updated_at) on major tables

---

## 13. Future-Proofing Notes

This design intentionally supports:

- multi-user expansion
- richer sharing
- migration to object storage later
- advanced recurrence
- notification digests
- search indexing
- eventual E2E encryption if ever required
- multi-device sync at scale

---

## 14. Open Questions / Future Enhancements

Potential future work:

- Calendar-level permissions (currently space-level)
- Digest notification mode
- Full-text search
- Natural language event creation
- External calendar import (ICS)
- Advanced recurrence UI
- Attachment thumbnails
- Conflict resolution improvements
- Data retention policies
- Partitioning change_log if needed

---

## End of Baseline Plan
