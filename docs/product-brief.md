---
title: Nagger Product Brief
created: 2026-08-02
updated: 2026-09-02
tags:
  - type/product-brief
  - project/hermes
  - topic/reminders
  - topic/shopping
status: draft
---

# Nagger Product Brief

## Purpose

Create a deterministic local service that manages personal reminders and exposes them through REST and MCP for an assistant to use.

The current release ships the one-shot task lifecycle, recurring task templates with their instances, and the deterministic morning report. The morning report is the only delivery channel; a shopping ledger remains a planned product capability rather than implied shipping behavior.

The service exists so the morning update can report useful things without an LLM guessing from free-form notes.

## Problem statement

Important personal obligations can disappear into notes, memory, or an incomplete calendar. The user needs a dependable external memory that makes due work visible, keeps following up when asked, and preserves a clear record of what happened without requiring manual list maintenance.

## Users and consumers

- **Primary user:** an AI assistant that creates and manages reminders on the person’s behalf through validated REST or MCP commands.
- **Beneficiary:** one person managing personal tasks and recurring obligations; they receive the reminders and remain the source of task intent.
- **Morning Digest:** a user-facing briefing created by the AI assistant from the deterministic report.
- **Future local client:** may read task state and submit validated commands; it never infers task state from prose.

## Primary use cases

| Use case                          | Problem solved                                                     | Successful outcome                                                                                                                          |
| --------------------------------- | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| Capture a one-shot task           | A specific obligation may be forgotten.                            | The assistant creates a task from the person’s explicit request, with a due date.                                       |
| Capture a recurring task | Repeated maintenance work is easy to lose track of.             | The assistant creates a recurring template; its instances are scheduled on a configured cadence.                                  |
| Generate the morning digest       | The person should not reconstruct priorities from scattered notes. | The AI assistant reads the deterministic report and creates a concise Morning Digest for the person, covering overdue, due-today, and next-seven-day tasks. |
| Complete, pause, or cancel a task | The system must reflect reality without deleting history.          | The assistant submits an explicit validated command; terminal task records remain intact.                                                   |

## Product principles

- **Deterministic state.** Reminder status, timestamps, due dates, and transitions are explicit and machine-readable.
- **Readable state.** Data should be inspectable and understandable, not hidden behind opaque behavior.
- **Machine-readable report output.** Morning Digest receives structured data optimized for summarization.
- **No vibe-based scheduling.** Code computes due state. LLMs do not infer reminders from prose.
- **Local-first behavior.** Core task and shopping behavior should not depend on a cloud service.

## Scope

### Available now

- One-shot tasks.
- Explicit task statuses.
- Created/updated/completed timestamp tracking.
- Morning Digest report endpoint.
- REST and MCP task operations.
- Recurring task templates and their instances.

### Planned next

- Deployment automation.

### Future phase: Shopping ledger

- Shopping items with explicit status and timestamp tracking.
- Shopping items included in a future Morning Digest report contract.
- Assisted shopping-item creation through validated service boundaries.

### Out of scope for first version

- Full calendar integration.
- Complex recurrence rules such as third Thursday excluding holidays.
- Shopping ledger behavior, endpoints, and Morning Digest output.
- Multi-user conflict handling.
- Store inventory, prices, barcode scanning, or product matching.
- Direct LLM edits to persistent state.
- Telegram buttons or chat commands, unless added later.

## Domains

### Tasks

Tasks represent things that need doing.

Task kinds:

- `one-shot` — has a specific due date/time. Available now.
- `recurring` — a template that generates instances on a configured cadence. Available now.

Task identity:

- `id` is a stable, service-assigned numeric row identifier, optimized for lookup and mutation.
- `title` is the human-readable task description. It is not required to be unique and may be edited.
- API paths use the numeric `id`; callers must not infer identity from the title.

Task statuses:

- `active` — included in due-state calculation and reports.
- `paused` — ignored until resumed.
- `done` — completed, no longer reminded.
- `cancelled` — abandoned, no longer reminded.

### Shopping — future phase

Shopping items represent things to buy.

Shopping statuses:

- `needed` — should appear in shopping reports.
- `bought` — completed.
- `cancelled` — no longer needed.

Shopping items may include preferred store, category, quantity, and priority.

## Data format

The public REST and MCP representations use camelCase. The examples below describe observable product state; SQLite remains an implementation detail.

### One-shot task example

```yaml
kind: task
id: 42
title: Renew passport
type: one-shot
status: active
createdAt: 2026-08-02T09:20:00+03:00
updatedAt: 2026-08-02T09:20:00+03:00
completedAt: null
cancelledAt: null

schedule:
  dueAt: 2026-09-02T09:00:00+03:00

reporting:
  includeInMorningUpdate: true
  priority: normal
  reportWhen: due-or-overdue
```

### Recurring task example

A recurring template generates instances on a cadence. Creating the template immediately creates its first instance, due on the template's `startDate`:

```yaml
kind: task
id: 43
title: Replace filter
type: recurring
status: active
createdAt: 2026-08-02T09:20:00+03:00
updatedAt: 2026-08-02T09:20:00+03:00
cancelledAt: null
startDate: 2026-08-02

schedule:
  recurrence:
    every: 1
    unit: months

reporting:
  includeInMorningUpdate: true
  priority: normal
  reportWhen: due-or-overdue
```

### Future shopping item example *(planned)*

```yaml
kind: shopping-item
id: milk
name: Milk
status: needed
createdAt: 2026-08-02T09:30:00+03:00
updatedAt: 2026-08-02T09:30:00+03:00
completedAt: null
cancelledAt: null

quantity: 1
unit: carton
category: dairy
store: Lidl Länsikeskus
priority: normal

reporting:
  includeInMorningUpdate: true
```

## Deterministic task behavior

### Due-state calculation

For each active one-shot task, compare the calendar date of its `dueAt` timestamp with the requested report date:

- Due date before the report date: `overdue`.
- Due date equal to the report date: `due_today`.
- Due date after the report date: `upcoming`.
- If status is not `active`, exclude from normal due reports.

The report includes all active overdue and due-today tasks. It includes upcoming tasks only when their local due date is within the next seven calendar days, with `daysUntilDue` from 1 through 7; later tasks are excluded from both items and the upcoming summary count. Overdue items provide positive `daysOverdue` and null `daysUntilDue`; due-today items set both fields to null.

Recurring instances apply the same rule to their `dueAt`. They appear in reports under their template id, distinguished from one-shot tasks by a `type` field.

Time of day may order items within a report, but it does not change a task from `due_today` to `overdue`.

### Time basis

- Due-state calculation and requested report dates use the service’s configured IANA timezone, initially `Europe/Helsinki`.
- Stored timestamps use ISO-8601 date-time values with an explicit UTC offset.
- The service timezone gives the report date an unambiguous meaning through midnight and daylight-saving transitions.

### Recurrence

Recurring tasks are templates that repeatedly generate instances on an interval:

```yaml
recurrence:
  every: 2
  unit: months
```

- `every` is a positive integer.
- Supported `unit` values are `days`, `weeks`, and `months`.
- Creating a template immediately creates its first instance, due on the template's `startDate`.
- Completing the template's current active instance schedules the next instance from the completion timestamp; the template stays `active`.
- For month-based recurrence, if the target day does not exist in the target month, use that month’s last day.
- Complex calendar rules, holiday exclusions, and natural-language recurrence remain out of scope for the first version.

### Report reads

Report endpoints are read-only. Generating, previewing, retrying, or reading a Morning Digest report must not update task state or timestamps.

### Task state transitions

Task status changes use strict transitions:

| Current status | Allowed command | Result |
|---|---|---|
| `active` | `pause` | `paused` |
| `active` | `complete` | `done` for one-shot; recurring instance completes and schedules the next |
| `active` | `cancel` | `cancelled` |
| `paused` | `resume` | `active` |
| `paused` | `cancel` | `cancelled` |

- A paused task must be resumed before it can be completed.
- `done` and `cancelled` tasks are terminal and cannot be resumed, completed, or reactivated.
- A new task is created through its appropriate creation endpoint; terminal task history is retained unchanged.
- Commands that do not match an allowed transition are rejected with a structured validation error.

### Completion transition

When a one-shot task is completed:

- Set `status: done`.
- Set `completedAt` to current timestamp.
- Set `updatedAt` to current timestamp.
- Exclude from future reminder reports.

### Recurring completion

When the current active instance of a recurring template is completed:

- Keep the template `status: active`.
- Set the completed instance `status: done` and `completedAt` to the current timestamp.
- Set the template `updatedAt` to the current timestamp.
- Create the next instance, due from the completion timestamp and the template's recurrence interval.

## Future shopping behavior

Shopping items are simpler than tasks.

- `needed` items appear in shopping reports.
- `bought` and `cancelled` items are excluded from normal morning reports.
- Completing an item sets `status: bought`, `completedAt`, and `updatedAt`.
- LLM-created shopping items must go through the service API for validation and normalization.

## Service shape

Nagger offers a stable interface for reading reports and changing state. REST remains available for local clients; MCP gives a personal-assistant LLM the same validated capabilities through tool calls. Technology and deployment decisions are tracked in [[Nagger Product Design]].

### Access boundary

Nagger listens on localhost only. It is intended for Hermes and other local processes on the Pi, not direct LAN or public access.

Authentication, remote access, and multi-user support are deferred until a real consumer requires them.

## Available interfaces

### REST

```http
POST /tasks/one-shot
POST /tasks/{id}/complete
POST /tasks/{id}/pause
POST /tasks/{id}/resume
POST /tasks/{id}/cancel
GET /reports/morning?date=2026-08-03
```

One-shot creation requires `title` and `dueAt`. Lifecycle operations use the numeric task `id` and return the updated task.

### MCP

MCP-compatible clients connect through streamable HTTP at `/mcp`. The server exposes tools for creating, completing, pausing, resuming, and cancelling one-shot and recurring tasks, plus listing and `get_morning_report`. Tool results use the same observable task and report fields as REST.

### Planned interfaces

Task editing and shopping endpoints remain planned. They are not part of the current public contract.

## Morning Digest JSON shape

The Morning Digest consumes this deterministic report and summarizes it in plain language.

Report contract rules:

- Every report includes `schemaVersion` and `generatedAt`.
- Consumers must ignore unknown fields.
- A breaking change to required fields requires a new major `schemaVersion`.
- Versioning lets a Hermes skill or other consumer interpret the report reliably; it does not require a migration framework.

```json
{
  "schemaVersion": "4",
  "generatedAt": "2026-08-03T07:00:00+03:00",
  "date": "2026-08-03",
  "summary": {
    "dueToday": 1,
    "overdue": 2,
    "upcoming": 3
  },
  "items": [
    {
      "id": 42,
      "title": "Renew passport",
      "dueAt": "2026-08-01T09:00:00+03:00",
      "type": "one-shot",
      "dueState": "overdue",
      "daysOverdue": 2,
      "daysUntilDue": null
    }
  ]
}
```

## AI interaction constraints

Allowed:

- Read report JSON and summarize it.
- Create one-shot tasks and request lifecycle changes through REST or MCP when the person explicitly instructs the assistant.
- Return validation errors rather than guessing missing schedule values.

Not allowed by default:

- Direct storage edits that bypass validation.
- Silent creation of cron jobs, services, or persistent automation.
- Guessing recurrence from prose.
- Marking tasks complete without explicit user instruction.

## Current acceptance criteria

- A user can create one-shot tasks with an explicit due timestamp through REST or MCP.
- A user can create recurring templates and complete their instances through REST or MCP; completing an instance schedules the next one deterministically.
- The service returns deterministic due-state JSON for Morning Digest through REST or MCP.
- Allowed lifecycle changes update `updatedAt` and relevant completion/cancellation timestamps.
- Report reads are pure and do not alter task state or timestamps.
- No LLM inference is required to decide what is due.
- Core behavior works without cloud dependency.

## Open questions

- How much write access should the LLM have through the service API?

## Future shopping questions

- Should shopping additions require approval every time, or allow trusted low-risk auto-add?
- Should tasks and shopping items live in one collection, separate collections, or separate stores?

## When not to use this approach

- If complex recurrence rules are required immediately.
- If multiple people edit the same data concurrently.
- If mobile-first shopping-list UX is the primary requirement.
- If the system needs external integrations before the local workflow is proven.
