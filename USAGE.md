# Nagger API Usage

Nagger is a local JSON API for one-shot reminders. Start it with:

```bash
dotnet run --project src/Nagger.Host
```

The development launch profile listens on `http://localhost:5246`. Without a
launch profile, the default address is `http://127.0.0.1:5000`.

## MCP Server

MCP-compatible clients can connect with the streamable-HTTP transport at
`http://localhost:5246/mcp` when using the development launch profile. The
endpoint exposes these tools:

| Tool | Purpose |
| --- | --- |
| `create_one_shot_task` | Create a one-shot reminder from `title`, `dueAt`, and `reminderPolicy`. |
| `complete_one_shot_task` | Complete an active reminder by `id`. |
| `pause_one_shot_task` | Pause an active reminder by `id`. |
| `resume_one_shot_task` | Resume a paused reminder by `id`. |
| `cancel_one_shot_task` | Cancel an active or paused reminder by `id`. |
| `list_one_shot_tasks` | Discover active and paused reminders and their lifecycle-tool `id` values. |
| `create_recurring_task` | Create a recurring template from `title`, `startDate`, `recurrenceEvery`, `recurrenceUnit`, and `reminderPolicy`. |
| `complete_recurring_task` | Complete a recurring template's current active instance by template `id`; schedules the next instance. |
| `pause_recurring_task` | Pause a recurring template and its current instance by template `id`. |
| `resume_recurring_task` | Resume a paused recurring template and its current instance by template `id`. |
| `cancel_recurring_task` | Cancel a recurring template and all its instances by template `id`. |
| `list_recurring_tasks` | Discover recurring templates and their lifecycle-tool `id` values. |
| `get_morning_report` | Read the morning report for a `YYYY-MM-DD` `date`. |

Tool results contain structured task and report data using the same fields as
the REST API. Invalid inputs, invalid state transitions, and unknown task IDs
are returned as MCP tool errors. This endpoint is local-only: it has no
authentication or authorization and must not be exposed on an untrusted
network.

All request and response bodies are JSON. Timestamps use ISO-8601 with an
explicit UTC offset, for example `2026-08-04T09:00:00+03:00`.

For running Nagger as a persistent MCP server for Hermes (build, systemd user
unit, SQLite location, Hermes `mcp_servers` wiring), see
[`hermes-integration.md`](docs/hermes-integration.md).

## Create A Reminder

`POST /tasks/one-shot`

Request payload:

```json
{
  "title": "Pay rent",
  "dueAt": "2026-08-04T09:00:00+03:00",
  "reminderPolicy": "once"
}
```

Required fields:

| Field | Type | Rules |
| --- | --- | --- |
| `title` | string | Nonempty; surrounding whitespace is trimmed. |
| `dueAt` | string | ISO-8601 date-time with an explicit offset or `Z`, not in the past. |
| `reminderPolicy` | string | One of `none`, `once`, or `weekly-until-done`. |

Successful response: `201 Created`

```json
{
  "id": 1,
  "title": "Pay rent",
  "type": "one-shot",
  "status": "active",
  "dueAt": "2026-08-04T09:00:00+03:00",
  "reminderPolicy": "once",
  "createdAt": "2026-08-03T10:00:00+00:00",
  "updatedAt": "2026-08-03T10:00:00+00:00",
  "completedAt": null,
  "cancelledAt": null
}
```

## Change Reminder State

These endpoints have no request body and return the full task representation
with `200 OK`.

| Action | Endpoint | Allowed current status | Resulting status |
| --- | --- | --- | --- |
| Complete | `POST /tasks/{id}/complete` | `active` | `done` |
| Pause | `POST /tasks/{id}/pause` | `active` | `paused` |
| Resume | `POST /tasks/{id}/resume` | `paused` | `active` |
| Cancel | `POST /tasks/{id}/cancel` | `active`, `paused` | `cancelled` |

Completing sets `completedAt`; cancelling sets `cancelledAt`; pausing and
resuming leave both fields `null`. Only active reminders appear in reports.

Example:

```bash
curl --request POST http://localhost:5246/tasks/1/pause
```

## List Open Reminders

`GET /tasks/one-shot`

Returns `200 OK` with an array of the full task representation used by the
create and lifecycle endpoints. The array contains active and paused reminders
only, ordered by ascending `id`; completed and cancelled reminders are omitted.
Use the returned `id` to select a reminder for a lifecycle action. When no open
reminders exist, the response is `[]`.

```bash
curl http://localhost:5246/tasks/one-shot
```

## Recurring Tasks

Recurring tasks are templates that repeatedly generate recurring-task instances. Creating a template immediately creates the first instance due on the start date; completing the template's current instance schedules the next one. Instances live in their own store and never appear in `/tasks/one-shot`. Reports include recurring obligations under their template id, distinguished from one-shot tasks by a `type` field.

### Create A Recurring Template

`POST /tasks/recurring`

Request payload:

```json
{
  "title": "Team sync",
  "startDate": "2026-08-06",
  "recurrence": {
    "every": 1,
    "unit": "weeks"
  },
  "reminderPolicy": "once"
}
```

Required fields:

| Field | Type | Rules |
| --- | --- | --- |
| `title` | string | Nonempty; surrounding whitespace is trimmed. |
| `startDate` | string | YYYY-MM-DD date, not in the past. |
| `recurrence.every` | integer | Positive interval between recurrences. |
| `recurrence.unit` | string | One of `days`, `weeks`, or `months`. |
| `reminderPolicy` | string | One of `none`, `once`, or `weekly-until-done`. |

Successful response: `201 Created`

```json
{
  "id": 1,
  "title": "Team sync",
  "startDate": "2026-08-06",
  "recurrence": {
    "every": 1,
    "unit": "weeks"
  },
  "reminderPolicy": "once",
  "status": "active",
  "createdAt": "2026-08-03T10:00:00+00:00",
  "updatedAt": "2026-08-03T10:00:00+00:00",
  "cancelledAt": null
}
```

### Manage A Recurring Template Or Instance

These endpoints have no request body and return `200 OK` with the affected representation.

| Action | Endpoint | `id` refers to | Result |
| --- | --- | --- | --- |
| Complete | `POST /tasks/recurring/{id}/complete` | the template id | Completes the template's current active instance and creates the next instance (completion date + interval). Returns the completed recurring instance. |
| Pause | `POST /tasks/recurring/{id}/pause` | the template id | Sets the template to `paused` and pauses its current instance. Returns the template. |
| Resume | `POST /tasks/recurring/{id}/resume` | the template id | Sets the template back to `active` and resumes its current instance. Returns the template. |
| Cancel | `POST /tasks/recurring/{id}/cancel` | the template id | Sets the template to `cancelled`, sets `cancelledAt`, and cancels all its open instances. Returns the template. |

All lifecycle endpoints resolve `{id}` as the **template id**. Completing a template with no active instance, pausing a paused template, or resuming an active template returns a `400 Bad Request` validation error. An unknown template id returns `404 Not Found`.

### List Recurring Templates

`GET /tasks/recurring`

Returns `200 OK` with an array of template representations ordered by ascending `id`; `[]` when none exist. Use the returned `id` as the template id for all recurring lifecycle actions (pause, resume, cancel, and complete).

```bash
curl http://localhost:5246/tasks/recurring
```

## Get A Morning Report

`GET /reports/morning?date=YYYY-MM-DD`

Example:

```bash
curl 'http://localhost:5246/reports/morning?date=2026-08-04'
```

Response: `200 OK`

```json
{
  "schemaVersion": "3",
  "generatedAt": "2026-08-03T10:00:00+00:00",
  "date": "2026-08-04",
  "summary": {
    "dueToday": 1,
    "overdue": 1,
    "upcoming": 1
  },
  "items": [
    {
      "id": 2,
      "title": "Submit expense report",
      "dueAt": "2026-08-02T09:00:00+03:00",
      "type": "one-shot",
      "dueState": "overdue",
      "daysOverdue": 2,
      "daysUntilDue": null,
      "reminderPolicy": "none"
    },
    {
      "id": 1,
      "title": "Pay rent",
      "dueAt": "2026-08-04T09:00:00+03:00",
      "type": "one-shot",
      "dueState": "due_today",
      "daysOverdue": null,
      "daysUntilDue": null,
      "reminderPolicy": "once"
    },
    {
      "id": 3,
      "title": "Team sync",
      "dueAt": "2026-08-07T09:00:00+03:00",
      "type": "recurring",
      "dueState": "upcoming",
      "daysOverdue": null,
      "daysUntilDue": 3,
      "reminderPolicy": "weekly-until-done"
    }
  ]
}
```

The `items` array is ordered chronologically by due timestamp (ascending), so
overdue reminders come first (most overdue first), then due-today reminders,
then upcoming reminders, each earliest due first. Within a single due date,
items are ordered by due time. Items with an identical due timestamp have no
specified relative order. Ordering never changes the `summary` counts or any
reminder state.

The configured `Nagger:TimeZone` (default `Europe/Helsinki`) determines each
reminder's local due date. The report classifies active reminders as:

| Classification | Rule | Included in `items` |
| --- | --- | --- |
| `due_today` | Local due date equals requested date | Yes |
| `overdue` | Local due date precedes requested date | Yes, with `daysOverdue` |
| Upcoming | Local due date is 1 through 7 calendar days after requested date | Yes, with `daysUntilDue`; later tasks are excluded |

`daysOverdue` is positive only for overdue items, and `daysUntilDue` is 1 through 7 only for upcoming items; both are `null` for due-today items. The report is read-only and does not alter reminder state or timestamps. `reminderPolicy` remains task metadata for future reminder delivery and does not affect report visibility.

## Errors

Invalid payloads, unsupported state transitions, and a missing or malformed
report `date` return `400 Bad Request`:

```json
{
  "errors": {
    "dueAt": [
      "Due timestamp must be an ISO-8601 value with an explicit UTC offset."
    ]
  }
}
```

An action for an unknown task ID returns `404 Not Found`. Unexpected server
failures return `500 Internal Server Error` without implementation details.

## Configuration

| Setting | Default | Environment variable |
| --- | --- | --- |
| Database path | `nagger.db` | `Nagger__DatabasePath` |
| Report timezone | `Europe/Helsinki` | `Nagger__TimeZone` |
