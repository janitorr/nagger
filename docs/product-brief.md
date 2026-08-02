---
title: Nagger Product Brief
created: 2026-08-02
updated: 2026-08-02
tags:
  - type/product-brief
  - project/hermes
  - topic/reminders
  - topic/shopping
status: draft
---

# Nagger Product Brief

## Purpose

Create a deterministic local service that manages personal reminders, then exposes clean JSON for the Hermes Morning Digest to summarize.

A shopping ledger is documented as a future phase, but is not part of the first implementation.

The service exists so the morning update can report useful things without an LLM guessing from free-form notes.

## Problem statement

Important personal obligations can disappear into notes, memory, or an incomplete calendar. The user needs a dependable external memory that makes due work visible, keeps following up when asked, and preserves a clear record of what happened without requiring manual list maintenance.

## Users and consumers

- **Primary user:** an AI assistant that creates and manages reminders on the person’s behalf through validated service commands.
- **Beneficiary:** one person managing personal tasks and recurring obligations; they receive the reminders and remain the source of task intent.
- **Morning Digest:** a user-facing briefing created by the AI assistant from the deterministic report.
- **Future local client:** may read task state and submit validated commands; it never infers task state from prose.

## Primary use cases

| Use case                          | Problem solved                                                     | Successful outcome                                                                                                                          |
| --------------------------------- | ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| Capture a one-shot task           | A specific obligation may be forgotten.                            | The assistant creates a task from the person’s explicit request, with a due date and reminder policy.                                       |
| Capture a recurring task          | Repeated maintenance work is easy to lose track of.                | The assistant creates an interval task, such as every two or six months; completion schedules the next occurrence from the completion date. |
| Generate the morning digest       | The person should not reconstruct priorities from scattered notes. | The AI assistant reads the deterministic report and creates a concise Morning Digest for the person, covering due-today and overdue tasks.  |
| Receive follow-up reminders       | A due item can remain unfinished after its first reminder.         | `weekly-until-done` tasks are eligible for weekly follow-up until completed or cancelled.                                                   |
| Complete, pause, or cancel a task | The system must reflect reality without deleting history.          | The assistant submits an explicit validated command; terminal task records remain intact.                                                   |

## Product principles

- **Deterministic state.** Reminder status, timestamps, due dates, and transitions are explicit and machine-readable.
- **Readable state.** Data should be inspectable and understandable, not hidden behind opaque behavior.
- **Machine-readable report output.** Morning Digest receives structured data optimized for summarization.
- **No vibe-based scheduling.** Code computes due state. LLMs do not infer reminders from prose.
- **Local-first behavior.** Core task and shopping behavior should not depend on a cloud service.

## Scope

### In scope

- One-shot tasks.
- Recurring tasks.
- Weekly reminder escalation until done.
- Explicit task statuses.
- Created/updated/completed timestamp tracking.
- Morning Digest report endpoint.

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

- `one-shot` — has a specific due date/time.
- `recurring` — repeats by a configured cadence.

Task identity:

- `id` is a stable, service-assigned numeric row identifier, optimized for lookup and mutation.
- `title` is the human-readable task description. It is not required to be unique and may be edited.
- API paths use the numeric `id`; callers must not infer identity from the title.

Task statuses:

- `active` — included in due-state calculation and reports.
- `paused` — ignored until resumed.
- `done` — completed, no longer reminded.
- `cancelled` — abandoned, no longer reminded.

Reminder policies:

- `none` — report only by due state, no reminder escalation.
- `once` — remind once when due.
- `weekly-until-done` — after the first reminder, remind weekly until status changes.

### Shopping — future phase

Shopping items represent things to buy.

Shopping statuses:

- `needed` — should appear in shopping reports.
- `bought` — completed.
- `cancelled` — no longer needed.

Shopping items may include preferred store, category, quantity, and priority.

## Data format

Use a structured representation per item. The examples below are illustrative; the product requirement is storage-agnostic.

### One-shot task example

```yaml
kind: task
id: 42
title: Renew passport
type: one-shot
status: active
created_at: 2026-08-02T09:20:00+03:00
updated_at: 2026-08-02T09:20:00+03:00
completed_at: null
cancelled_at: null

schedule:
  due_at: 2026-09-02T09:00:00+03:00
  reminder_policy: weekly-until-done
  next_reminder_at: 2026-09-02T09:00:00+03:00
  last_reminded_at: null

reporting:
  include_in_morning_update: true
  priority: normal
  report_when: due-or-overdue
```

### Recurring task example

```yaml
kind: task
id: 43
title: Replace filter
type: recurring
status: active
created_at: 2026-08-02T09:20:00+03:00
updated_at: 2026-08-02T09:20:00+03:00
last_completed_at: null
cancelled_at: null

schedule:
  recurrence:
    every: 1
    unit: months
  next_due_at: 2026-09-02T09:00:00+03:00
  reminder_policy: weekly-until-done
  next_reminder_at: 2026-09-02T09:00:00+03:00
  last_reminded_at: null

reporting:
  include_in_morning_update: true
  priority: normal
  report_when: due-or-overdue
```

### Future shopping item example

```yaml
kind: shopping-item
id: milk
name: Milk
status: needed
created_at: 2026-08-02T09:30:00+03:00
updated_at: 2026-08-02T09:30:00+03:00
completed_at: null
cancelled_at: null

quantity: 1
unit: carton
category: dairy
store: Lidl Länsikeskus
priority: normal

reporting:
  include_in_morning_update: true
```

## Deterministic task behavior

### Due-state calculation

For each active task, compare the calendar date of its due timestamp with the requested report date:

- Use `due_at` for a `one-shot` task and `next_due_at` for a `recurring` task.
- Due date before the report date: `overdue`.
- Due date equal to the report date: `due_today`.
- Due date after the report date: `upcoming`.
- If status is not `active`, exclude from normal due reports.

Time of day may order items within a report, but it does not change a task from `due_today` to `overdue`.

### Time basis

- Due-state calculation and requested report dates use the service’s configured IANA timezone, initially `Europe/Helsinki`.
- Stored timestamps use ISO-8601 date-time values with an explicit UTC offset.
- The service timezone gives the report date an unambiguous meaning through midnight and daylight-saving transitions.

### First-version recurrence

Recurring tasks use an interval:

```yaml
recurrence:
  every: 2
  unit: months
```

- `every` is a positive integer.
- Supported `unit` values are `days`, `weeks`, and `months`.
- Completing a recurring task keeps its status `active`, sets `last_completed_at`, and calculates `next_due_at` from the completion timestamp.
- For month-based recurrence, if the target day does not exist in the target month, use that month’s last day.
- Complex calendar rules, holiday exclusions, and natural-language recurrence remain out of scope for the first version.

### Report reads and reminder emission

Report endpoints are read-only. Generating, previewing, retrying, or reading a Morning Digest report must not update task state, reminder timestamps, or shopping state.

The delivery mechanism records an actual reminder separately through `POST /tasks/{id}/reminders/emitted`. When that command succeeds:

- Set `last_reminded_at` to current timestamp.
- Set `updated_at` to current timestamp.
- If `reminder_policy` is `weekly-until-done`, set `next_reminder_at` to current timestamp plus 7 days.
- If `reminder_policy` is `once`, clear `next_reminder_at` or mark reminder as sent.

### Task state transitions

Task status changes use strict transitions:

| Current status | Allowed command | Result |
|---|---|---|
| `active` | `pause` | `paused` |
| `active` | `complete` | `done` for one-shot; next occurrence for recurring |
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
- Set `completed_at` to current timestamp.
- Set `updated_at` to current timestamp.
- Exclude from future reminder reports.

When a recurring task is completed:

- Keep `status: active`.
- Set `last_completed_at` to current timestamp.
- Set `updated_at` to current timestamp.
- Calculate `next_due_at` from the completion timestamp and its recurrence interval.
- Reset `next_reminder_at` to the new `next_due_at`.
- Clear `last_reminded_at`.

## Future shopping behavior

Shopping items are simpler than tasks.

- `needed` items appear in shopping reports.
- `bought` and `cancelled` items are excluded from normal morning reports.
- Completing an item sets `status: bought`, `completed_at`, and `updated_at`.
- LLM-created shopping items must go through the service API for validation and normalization.

## Service shape

The product needs a stable interface for reading reports and changing state. Technology and deployment decisions are tracked in [[Nagger Product Design]].

### First-version access boundary

The first version listens on localhost only. It is intended for Hermes and other local processes on the Pi, not direct LAN or public access.

Authentication, remote access, and multi-user support are deferred until a real consumer requires them.

## API sketch

### Report endpoints

```http
GET /report/morning?date=2026-08-03
GET /tasks/report?date=2026-08-03
```

### Task creation endpoints

```http
POST /tasks/one-shot
POST /tasks/recurring
```

The endpoint determines the task kind and validates its required schedule fields. One-shot creation requires `due_at`; recurring creation requires an interval recurrence and `next_due_at`. Both endpoints require an explicit `reminder_policy`; no endpoint supplies a hidden default.

### Existing task endpoints

```http
GET /tasks
GET /tasks/{id}
PATCH /tasks/{id}
POST /tasks/{id}/complete
POST /tasks/{id}/pause
POST /tasks/{id}/resume
POST /tasks/{id}/cancel
POST /tasks/{id}/reminders/emitted
```

### Future shopping endpoints

```http
GET /shopping/report?date=2026-08-03
GET /shopping/items
GET /shopping/items/{id}
POST /shopping/items
PATCH /shopping/items/{id}
POST /shopping/items/{id}/bought
POST /shopping/items/{id}/cancel
```

## Morning Digest JSON shape

The Morning Digest should consume JSON like this and summarize it in plain language.

Report contract rules:

- Every report includes `schema_version` and `generated_at`.
- Consumers must ignore unknown fields.
- A breaking change to required fields requires a new major `schema_version`.
- Versioning exists so a Hermes skill or other future consumer can reliably interpret the report; it does not require a migration framework.

```json
{
  "schema_version": 1,
  "generated_at": "2026-08-03T07:00:00+03:00",
  "date": "2026-08-03",
  "tasks": {
    "summary": {
      "due_today": 1,
      "overdue": 2,
      "upcoming": 3
    },
    "items": [
      {
        "id": 42,
        "title": "Renew passport",
        "type": "one-shot",
        "status": "active",
        "due_state": "overdue",
        "due_at": "2026-08-01T09:00:00+03:00",
        "days_overdue": 2,
        "next_reminder_at": "2026-08-03T09:00:00+03:00",
        "priority": "normal",
        "morning_update_hint": "Mention briefly."
      }
    ]
  }
}
```

## AI interaction constraints

Allowed:

- Read report JSON and summarize it.
- Create new tasks through the service API when the person explicitly requests them and required fields are supplied.
- Draft state changes for user approval.

Not allowed by default:

- Direct storage edits bypassing validation.
- Silent creation of cron jobs, services, or persistent automation.
- Guessing recurrence from prose.
- Marking tasks complete without explicit user instruction.

## First version acceptance criteria

- A user can define one-shot and recurring tasks using the chosen storage/input format.
- The service returns deterministic due-state JSON for Morning Digest.
- Status changes update `updated_at` and relevant completion/cancellation timestamps.
- Reminder emission updates `last_reminded_at` and `next_reminder_at` deterministically.
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
