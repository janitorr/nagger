## Context

`src/Nagger.Core/Tasks/` currently holds 15 files in a single namespace (`Nagger.Core.Tasks`): the shared domain model, the ports (`Ports.cs`), and the Mediator vertical slices. The `.editorconfig` added in `add-dotnet-editorconfig` enables IDE0130, so "namespace matches folder structure" is now an enforced convention. See proposal.md - Why for motivation.

## Goals / Non-Goals

**Goals:**

- Move the shared domain model into `Tasks/Domain/` under `Nagger.Core.Tasks.Domain`.
- Keep the vertical slices and `Ports.cs` at `Tasks/` root in `Nagger.Core.Tasks`.
- Preserve file history (`git mv`) and keep the change purely mechanical — no behavior change.

**Non-Goals:**

- No deduplication of the parallel one-shot/recurring hierarchies (deliberately separate concepts per the `separate-recurring-task-instances` design).
- No further splitting of slices into per-feature folders.
- No EF model, migration, or snapshot changes.

## Decisions

### Decision: `Domain/` subfolder, not `Model/`

The moved types are domain records, enums, value objects, and helpers (`TaskItem`, `RecurringTaskTemplate`, `RecurringTaskInstance`, `ReminderPolicy`, `RecurrenceCalculator`, `DateOnlyExtensions`, `Validation`). `Domain/` describes the layer more accurately than `Model/`, which could be read as persistence or DTO models.

- Alternative (rejected): `Model/`. Ambiguous with EF entities and API DTOs already present in Host.

### Decision: Sub-namespace `Nagger.Core.Tasks.Domain` matching the folder

Keeps IDE0130 satisfied and makes the layering visible in `using` directives. Files in `Tasks/` root remain `Nagger.Core.Tasks`.

- Alternative (rejected): keep a single flat namespace while introducing a folder. Violates the IDE0130 convention just added and hides the layer boundary from `using` statements.

### Decision: Slices and `Ports.cs` stay at `Tasks/` root

The vertical slices are the application entry points named by AGENTS.md ("vertical feature slices in `Tasks/`"), and `Ports.cs` is a convention-stable seam ("ports in `Ports.cs`"). Moving either would churn more files than necessary without clarifying anything.

- Alternative (rejected): full layer split with `Application/` and `Ports/` folders. More movement for no additional clarity; the root already reduces to 8 files after the domain move.

### Decision: Move via `git mv`, no EF churn

Use `git mv` to preserve history. EF mappings store `ReminderPolicy`/`Status`/`RecurrenceUnit` as `string` columns, so `NaggerDbContext`, migrations, and the model snapshot never reference the moved enum types and need no edits.

## Risks / Trade-offs

- [A moved type is missed and a file breaks compilation] → The compiler surfaces any missing `using` immediately; `dotnet build Nagger.slnx` is the verification gate.
- [Some consumer references only ports and does not need the new using] → Adding an unused `using` is harmless, but the implementation should add it only where a moved type is actually referenced to keep the diff minimal.
