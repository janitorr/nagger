# Nagger API Usage

Nagger is a local JSON API for one-shot reminders. Start it with:

```bash
dotnet run --project src/Nagger.Host
```

The development launch profile listens on `http://localhost:5246`. Without a
launch profile, the default address is `http://127.0.0.1:5000`.

All request and response bodies are JSON. Timestamps use ISO-8601 with an
explicit UTC offset, for example `2026-08-04T09:00:00+03:00`.

## Create A Reminder

`POST /tasks/one-shot`

Request payload:

```json
{
  "title": "Pay rent",
  "due_at": "2026-08-04T09:00:00+03:00",
  "reminder_policy": "once"
}
```

Required fields:

| Field | Type | Rules |
| --- | --- | --- |
| `title` | string | Nonempty; surrounding whitespace is trimmed. |
| `due_at` | string | ISO-8601 date-time with an explicit offset or `Z`. |
| `reminder_policy` | string | One of `none`, `once`, or `weekly-until-done`. |

Successful response: `201 Created`

```json
{
  "id": 1,
  "title": "Pay rent",
  "type": "one-shot",
  "status": "active",
  "due_at": "2026-08-04T09:00:00+03:00",
  "reminder_policy": "once",
  "created_at": "2026-08-03T10:00:00+00:00",
  "updated_at": "2026-08-03T10:00:00+00:00",
  "completed_at": null,
  "cancelled_at": null
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

Completing sets `completed_at`; cancelling sets `cancelled_at`; pausing and
resuming leave both fields `null`. Only active reminders appear in reports.

Example:

```bash
curl --request POST http://localhost:5246/tasks/1/pause
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
  "schema_version": "1",
  "generated_at": "2026-08-03T10:00:00+00:00",
  "date": "2026-08-04",
  "summary": {
    "due_today": 1,
    "overdue": 1,
    "upcoming": 1
  },
  "items": [
    {
      "id": 1,
      "title": "Pay rent",
      "due_at": "2026-08-04T09:00:00+03:00",
      "due_state": "due_today",
      "days_overdue": null,
      "reminder_policy": "once"
    },
    {
      "id": 2,
      "title": "Submit expense report",
      "due_at": "2026-08-02T09:00:00+03:00",
      "due_state": "overdue",
      "days_overdue": 2,
      "reminder_policy": "none"
    }
  ]
}
```

The configured `Nagger:TimeZone` (default `Europe/Helsinki`) determines each
reminder's local due date. The report classifies active reminders as:

| Classification | Rule | Included in `items` |
| --- | --- | --- |
| `due_today` | Local due date equals requested date | Yes |
| `overdue` | Local due date precedes requested date | Yes, with `days_overdue` |
| Upcoming | Local due date follows requested date | No; counted only in `summary.upcoming` |

The report is read-only and does not alter reminder state or timestamps.

## Errors

Invalid payloads, unsupported state transitions, and a missing or malformed
report `date` return `400 Bad Request`:

```json
{
  "errors": {
    "due_at": [
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
