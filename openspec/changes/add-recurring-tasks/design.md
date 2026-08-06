## Context

Nagger currently has a complete one-shot task system with separate Core and Host layers. The existing architecture uses vertical slices in `src/Nagger.Core/Tasks/` with ports defined in `Ports.cs`. The Host layer implements these ports with SQLite persistence. Morning reports are read-only and aggregate task data.

## Goals / Non-Goals

**Goals:**
- Maintain complete separation between one-shot and recurring task concerns
- Preserve existing one-shot task behavior unchanged
- Use dates only (no times) for recurrence calculations
- Keep recurring task templates invisible in reports - only instances appear

**Non-Goals:**
- Template modification after creation
- Complex recurrence rules (third Thursday, etc.)
- Time-based scheduling
- Reminder delivery (LLM handles this via reports)

## Decisions

### Separate Vertical Slices
**Decision:** Implement recurring tasks as a completely separate vertical slice from one-shot tasks.

**Rationale:** Maintains clean architectural boundaries. One-shot handlers know nothing about recurring tasks, and recurring handlers know nothing about one-shot tasks. This prevents conditional logic based on task type.

**Alternatives considered:**
- Unified task type with discriminator: Rejected because it would require conditional logic in handlers
- Template + generated instances in same table: Rejected because it pollutes the one-shot model

### Template + Instance Model
**Decision:** Recurring tasks are templates that generate one-shot task instances. Templates are never shown in reports - only instances are.

**Rationale:** 
- Matches the product brief's model
- Reuses existing one-shot task infrastructure for instances
- Templates manage the recurrence logic, instances are just regular tasks

**How it works:**
- Create template → creates first instance with due date = start date
- Complete instance → marks done, calculates next due date (completion date + recurrence), creates new instance
- Pause template → pauses template AND current instance
- Cancel template → cancels template AND all its instances

### Date-Only Recurrence
**Decision:** All recurrence calculations use dates only (no times).

**Rationale:** Simplifies the model and matches your requirement that "we just use dates here, this is not a calendar."

**Implementation:**
- Templates store start date as DateOnly
- Instances store due date as DateTimeOffset at midnight in configured timezone
- Next due date = completion date + recurrence interval

### Instance Creation Timing
**Decision:** First instance is created immediately when template is created.

**Rationale:** Ensures the first due date is visible in reports immediately. Matches your preference for Option A.

### Instance Completion via One-Shot Endpoint
**Decision:** Completing a recurring-generated instance uses the existing one-shot complete endpoint, which then triggers next instance creation.

**Rationale:** 
- LLM uses MCP to mark tasks done (per your input)
- One-shot complete endpoint handles the instance completion
- Modified to detect recurring instances and create next one

**Wait - this violates separation!** Actually, this requires the one-shot handler to know about recurring templates. Let me reconsider...

**Revised Decision:** Use separate endpoints for recurring instance completion.

**Rationale:** Maintains true separation. One-shot complete endpoint only handles true one-shot tasks. Recurring instances are completed via `/tasks/recurring/{id}/complete`.

### Pause/Resume/Cancel Behavior
**Decision:** 
- Pause template → pauses template AND its current active instance
- Resume template → resumes template AND its current paused instance  
- Cancel template → cancels template AND all its instances

**Rationale:** Matches your inputs: "B" for pause, "B" for cancel, and "lets keep it simple for now, cancel or pause all"

## Risks / Trade-offs

[Risk: Instance endpoint confusion] → Use clear naming: `/tasks/recurring/{id}/complete` for recurring instances, `/tasks/one-shot/{id}/complete` for one-shot tasks

[Risk: Template-instance relationship complexity] → Store `RecurringTaskId` as nullable FK on one-shot tasks to link instances to templates

[Risk: Overdue instances accumulating] → Acceptable per your input: "if there is an instance that has passed lets keep nagging until user does it"

## Migration Plan

1. Add new database table for recurring task templates
2. Add nullable `RecurringTaskId` column to existing one_shot_tasks table
3. Deploy new endpoints alongside existing ones
4. No data migration needed (new feature)

Rollback: Drop new table, remove column from one_shot_tasks, remove new endpoints
