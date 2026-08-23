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

Product requirements live in [[Nagger Product Brief]]. This document records technical choices and the proven vertical slice. It does not redefine product behavior.

## Current decisions

| Area | Decision |
|---|---|
| Service | ASP.NET Core host on .NET, running on ARM64 Linux |
| Architecture | Ports-and-adapters style with a `Core` product module and a `Host` adapter/composition module |
| Feature organisation | Vertical feature slices inside `Core` |
| Slice navigation | A command/query, its handler, and small feature-specific validation live in the same file by default |
| Database | Local SQLite database file |
| Data access | EF Core with the SQLite provider and versioned migrations |
| Inbound adapters | REST endpoints and a streamable-HTTP MCP endpoint in the same Host process |
| Production hosting | Planned: `systemd` runs Nagger Host |
| Access boundary | Nagger listens on localhost only in the first version |
| Quality gates | Core and Host tests run in CI; Core mutation testing runs for pull requests |

## Solution shape

```text
src/
├─ Nagger.Core/             Product behavior, task model, feature ports
└─ Nagger.Host/             REST, MCP, SQLite adapters; runtime composition
```

Dependency direction:

```text
Nagger.Host ───────► Nagger.Core
```

`Nagger.Core` must not reference ASP.NET Core, EF Core, the SQLite provider, environment configuration, or the system clock. It owns task behavior and declares only the ports it needs.

`Nagger.Host` is both adapter boundary and composition root. It maps REST and MCP requests to Core features, implements Core persistence/time ports, applies runtime configuration, and owns SQLite mappings and migrations.

## Core feature organisation

Features remain vertical inside `Nagger.Core`:

```text
Nagger.Core/
└─ Tasks/
   ├─ CreateOneShotTask.cs
   ├─ ManageOneShotTaskLifecycle.cs
   ├─ MorningReport.cs
   ├─ Ports.cs
   └─ TaskItem.cs
```

The current implementation proves one-shot creation, lifecycle transitions, and morning reports. Recurring tasks, editing, and listing become new feature slices when their product contracts are settled.

Shared task rules and model types move out only when multiple features genuinely need them.

## Planned production topology

```text
systemd → Nagger.Host → local SQLite database file
```

Local development already uses the Host and SQLite topology. The planned Pi deployment keeps that topology simple; the database file remains local to the service host and can be inspected with SQLite tooling when needed.

## Persistence

SQLite is the canonical store. EF Core migrations define and evolve its schema; the SQLite provider supplies persistence.

The proven slice establishes only the schema needed for one-shot tasks and their lifecycle. It does not pre-design an event store, shopping tables, or a concurrency policy before the service has earned them.

## Proven first vertical slice

The first end-to-end path is complete:

```text
Create one-shot task
        ↓
Persist it in SQLite
        ↓
Read it through the Morning Digest report
        ↓
Manage its lifecycle through REST or MCP
        ↓
Inspect it through SQLite tooling when needed
```

Available REST endpoints:

```http
POST /tasks/one-shot
POST /tasks/{id}/complete
POST /tasks/{id}/pause
POST /tasks/{id}/resume
POST /tasks/{id}/cancel
GET /reports/morning?date=YYYY-MM-DD
```

The same task behavior is exposed as MCP tools through the streamable-HTTP `/mcp` endpoint. MCP does not introduce a second task model or bypass Core validation.

This proved:

- the Core/Host ports-and-adapters boundary;
- vertical-slice navigation and lifecycle behavior;
- EF Core/SQLite migrations and data access;
- deterministic due-state report output;
- REST and MCP integration over the same Core operations;
- direct state inspection when needed.

The report endpoint remains read-only. A report read must not update task state or timestamps.

## Deliberately deferred until iteration teaches us something

- recurring task implementation;
- event/history table design;
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
| Testability | Core has no real clock or database dependency; Host integration tests cover REST and MCP against SQLite. |
| Regression resistance | CI runs the Core and Host suites; PRs run Core mutation testing with an enforced score floor. |
| Local availability | The intended Pi deployment uses a local Host and SQLite database without cloud dependency. |
| Operability | Planned production deployment uses one `systemd`-managed process. |
| Modifiability | Core ports keep REST, MCP, and SQLite details out of product behavior. |

## When not to use this design

Do not expand this into separate task/shopping services, event-driven plumbing, cloud sync, or remote access until a real consumer requires it.

Use a simpler single-process utility instead if the HTTP API and persistent service operation no longer provide enough value to justify their existence.
