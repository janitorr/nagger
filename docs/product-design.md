---
title: Nagger Product Design
created: 2026-08-02
updated: 2026-08-03
tags:
  - type/product-design
  - project/hermes
  - topic/reminders
  - topic/sqlite
status: draft
---

# Nagger Product Design

## Purpose

Capture the current implementation direction for Nagger without turning a small local reminder service into a planning monument.

Product requirements live in [[Nagger Product Brief]]. This document records technical choices and the smallest iteration that will prove them. It does not redefine product behavior.

## Current decisions

| Area | Decision |
|---|---|
| Service | ASP.NET Core Minimal API on .NET, running on ARM64 Linux |
| Architecture | Ports-and-adapters style with a `Core` product module and a `Host` adapter/composition module |
| Feature organisation | Vertical feature slices inside `Core` |
| Slice navigation | A command/query, its handler, and small feature-specific validation live in the same file by default |
| Database | Local SQLite database file |
| Data access | EF Core with the SQLite provider and versioned migrations |
| Production hosting | `systemd` runs Nagger Host |
| Access boundary | Nagger listens on localhost only in the first version |

## Solution shape

```text
src/
├─ Nagger.Core/             Product behavior, task model, feature ports
└─ Nagger.Host/             HTTP and SQLite adapters; runtime composition
```

Dependency direction:

```text
Nagger.Host ───────► Nagger.Core
```

`Nagger.Core` must not reference ASP.NET Core, EF Core, the SQLite provider, environment configuration, or the system clock. It owns task behavior and declares only the ports it needs.

`Nagger.Host` is both adapter boundary and composition root. It maps HTTP requests to Core features, implements Core persistence/time ports, applies runtime configuration, and owns SQLite mappings and migrations.

## Core feature organisation

Features remain vertical inside `Nagger.Core`:

```text
Nagger.Core/
└─ Features/
   └─ Tasks/
      ├─ CreateOneShotTask.cs
      ├─ CreateRecurringTask.cs
      ├─ CompleteTask.cs
      ├─ PauseTask.cs
      ├─ ResumeTask.cs
      ├─ CancelTask.cs
      ├─ EmitReminder.cs
      ├─ GetTask.cs
      ├─ ListTasks.cs
      └─ GetMorningReport.cs
```

A typical feature file keeps its request, result, handler, and small feature-specific validation together. Split it only when it becomes hard to navigate or review. Folder ceremony is not an achievement.

Shared task rules and model types move out only when multiple features genuinely need them.

## Runtime topology

```text
systemd → Nagger.Host → local SQLite database file
```

The same simple topology is used locally and on the Pi. The database file remains local to the service host and can be inspected with SQLite tooling when needed.

## Persistence

SQLite is the canonical store. EF Core migrations define and evolve its schema; the SQLite provider supplies persistence.

The first iteration should establish only the schema needed for its one end-to-end path. It must not pre-design an event store, shopping tables, concurrency policy, or a full reminder delivery ledger before the service has earned them.

## First iteration: one thin vertical slice

Prove the architecture with one real path:

```text
Create one-shot task
        ↓
Persist it in SQLite
        ↓
Read it through the Morning Digest report
        ↓
Inspect it through SQLite tooling
```

Initial endpoints:

```http
POST /tasks/one-shot
GET /reports/morning?date=YYYY-MM-DD
```

This iteration proves:

- Core/Host port-and-adapter boundary;
- vertical-slice navigation convention;
- EF Core/SQLite migrations and data access;
- deterministic due-state report output;
- direct state inspection when needed.

The report endpoint remains read-only. A report read must not update reminder timestamps or task state.

## Deliberately deferred until iteration teaches us something

- recurring task implementation;
- event/history table design;
- reminder delivery idempotency and delivery references;
- editing semantics;
- detailed AI write-authority policy;
- report ordering and upcoming-item policy;
- production backup/restore automation;
- shopping ledger implementation.

These are real decisions, but not prerequisites for proving the first vertical slice.

## Quality attributes

| Attribute | Design response |
|---|---|
| Determinism | Core owns due-state and state-transition rules; reports are pure reads. |
| Inspectability | The SQLite database file can be inspected directly with SQLite tooling. |
| Testability | Core has no real clock or database dependency; Host integration tests use SQLite. |
| Local availability | Pi production uses a local Host and SQLite database without cloud dependency. |
| Operability | `systemd` owns the single production process. |
| Modifiability | Core ports keep HTTP and SQLite details out of product behavior. |

## When not to use this design

Do not expand this into separate task/shopping services, event-driven plumbing, cloud sync, or remote access until a real consumer requires it.

Use a simpler single-process utility instead if the HTTP API and persistent service operation no longer provide enough value to justify their existence.
