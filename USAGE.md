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
| `get_morning_report` | Read the morning report for a `YYYY-MM-DD` `date`. |

Tool results contain structured task and report data using the same fields as
the REST API. Invalid inputs, invalid state transitions, and unknown task IDs
are returned as MCP tool errors. This endpoint is local-only: it has no
authentication or authorization and must not be exposed on an untrusted
network.

All request and response bodies are JSON. Timestamps use ISO-8601 with an
explicit UTC offset, for example `2026-08-04T09:00:00+03:00`.

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
| `dueAt` | string | ISO-8601 date-time with an explicit offset or `Z`. |
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

## Get A Morning Report

`GET /reports/morning?date=YYYY-MM-DD`

Example:

```bash
curl 'http://localhost:5246/reports/morning?date=2026-08-04'
```

Response: `200 OK`

```json
{
  "schemaVersion": "1",
  "generatedAt": "2026-08-03T10:00:00+00:00",
  "date": "2026-08-04",
  "summary": {
    "dueToday": 1,
    "overdue": 1,
    "upcoming": 1
  },
  "items": [
    {
      "id": 1,
      "title": "Pay rent",
      "dueAt": "2026-08-04T09:00:00+03:00",
      "dueState": "due_today",
      "daysOverdue": null,
      "reminderPolicy": "once"
    },
    {
      "id": 2,
      "title": "Submit expense report",
      "dueAt": "2026-08-02T09:00:00+03:00",
      "dueState": "overdue",
      "daysOverdue": 2,
      "reminderPolicy": "none"
    }
  ]
}
```

The configured `Nagger:TimeZone` (default `Europe/Helsinki`) determines each
reminder's local due date. The report classifies active reminders as:

| Classification | Rule | Included in `items` |
| --- | --- | --- |
| `due_today` | Local due date equals requested date | Yes |
| `overdue` | Local due date precedes requested date | Yes, with `daysOverdue` |
| Upcoming | Local due date follows requested date | No; counted only in `summary.upcoming` |

The report is read-only and does not alter reminder state or timestamps.

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
