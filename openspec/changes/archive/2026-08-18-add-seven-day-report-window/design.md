## Context

The existing Core morning-report query reads active tasks, converts each due timestamp to the configured IANA timezone, and compares its local calendar date with the requested report date. It emits detailed items only for due-today and overdue tasks, while counting every future active task as upcoming. REST and MCP independently map the shared Core report response. Report reads are explicitly read-only.

The product direction is a pull-based morning digest, not reminder delivery. An assistant reads the report and presents the relevant work to the user. `reminderPolicy` remains stored task metadata for later delivery and recurring-task capabilities and has no role in current report visibility.

## Goals / Non-Goals

**Goals:**

- Expose active tasks as planning items from seven local calendar days before their due date through completion.
- Provide deterministic `daysUntilDue` data for upcoming tasks and preserve `daysOverdue` for overdue tasks.
- Limit the upcoming summary to the same seven-day visibility window as the detailed items.
- Publish the changed report contract as schema version `2` through both REST and MCP without report-side writes.

**Non-Goals:**

- Implement reminder delivery, acknowledgement, or reminder timestamp updates.
- Change `reminderPolicy` validation, persistence, or behavior.
- Add recurring tasks, task editing, or new persistence schema.
- Change task lifecycle behavior or include non-active tasks in reports.

## Decisions

### Use an inclusive seven-calendar-day window

The report will compare `DateOnly` values after converting due timestamps to the configured timezone. A task is an upcoming report item when its local due date is greater than the report date and no later than `reportDate.AddDays(7)`. This makes a task due on August 11 visible in the August 4 report and avoids ambiguous 168-hour behavior across daylight-saving transitions.

An active task with a later due date remains classified as upcoming internally but is excluded from both `items` and `summary.upcoming`. Using all future tasks, as today, was rejected because it makes the digest an unbounded backlog rather than a planning view.

### Make report item timing fields mutually exclusive

The Core report item and REST/MCP response models will expose both `daysOverdue` and `daysUntilDue`. For an overdue item, `daysOverdue` is positive and `daysUntilDue` is null. For a due-today item, both are null. For an upcoming item, `daysUntilDue` is between 1 and 7 inclusive and `daysOverdue` is null. This keeps the stable item shape explicit and prevents report consumers from deriving durations themselves.

### Bump the report schema version to 2

The response's selection semantics change and a required item field is added. The Core report will therefore emit `schemaVersion` `"2"`, which REST and MCP will pass through. Keeping version `1` was rejected because existing consumers could reasonably assume that upcoming counts include all future active tasks and that detailed items are only due or overdue.

### Preserve existing report purity and task policy storage

The report query will only read task data. It will not update `lastReminderAt`, `updatedAt`, or any lifecycle status. `reminderPolicy` continues to be included in task and report representations but does not decide visibility; this avoids conflating future delivery behavior with the current pull-report workflow.

## Risks / Trade-offs

- [Consumers interpret `upcoming` as all future work] → Schema version `2`, contract documentation, and integration tests make the bounded meaning explicit.
- [Timezone or daylight-saving edge cases shift the visibility date] → Continue converting timestamps into the configured IANA timezone and comparing `DateOnly` values rather than elapsed durations.
- [The report becomes less useful for distant planning] → Distant active tasks remain available through task listing; the morning report stays focused on the next week.
- [REST and MCP representations drift] → Update both adapters from the shared Core report model and cover each contract with integration tests.

## Migration Plan

1. Release the Core, REST, and MCP report changes together with schema version `2`.
2. Update documented report examples and assistant consumers to read `daysUntilDue` and treat `summary.upcoming` as a seven-day count.
3. No database migration is required because task persistence is unchanged.
4. Roll back by redeploying the prior application version; no persisted data requires conversion.

## Open Questions

None.
