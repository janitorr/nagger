## ADDED Requirements

### Requirement: Order report items chronologically
The service SHALL order the `items` array chronologically by due timestamp so that overdue items come first (most overdue first), followed by due-today items, followed by upcoming items, earliest due first. Within a single due date, items SHALL be ordered by due time ascending. Ordering SHALL NOT change the `summary` counts or any task state. The relative order of items with an identical due timestamp is unspecified.

#### Scenario: Order mixed due states chronologically
- **WHEN** an active one-shot task is overdue, another is due today, and a third is upcoming
- **THEN** the report `items` array lists the overdue task first, the due-today task second, and the upcoming task third

#### Scenario: Order same-day items by due time
- **WHEN** two active tasks share the same due date but have different due times
- **THEN** the task with the earlier due time appears before the task with the later due time

#### Scenario: Interleave one-shot and recurring items chronologically
- **WHEN** a recurring item is due earlier than a one-shot item
- **THEN** the recurring item appears before the later-due one-shot item, regardless of type or id
