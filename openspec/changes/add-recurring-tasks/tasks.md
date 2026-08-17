## 1. Core Domain

- [x] 1.1 Add RecurringTaskTemplate record with fields: Id, Title, StartDate, Recurrence, ReminderPolicy, Status, CreatedAt, UpdatedAt, CancelledAt
- [x] 1.2 Add RecurrenceRule record with Every (int) and Unit (enum: Days, Weeks, Months)
- [x] 1.3 Add RecurringTaskStatus enum with Active, Paused, Cancelled
- [x] 1.4 Add RecurringTaskNotFoundException
- [x] 1.5 Add IRecurringTaskTemplateStore port interface with AddAsync, GetByIdAsync, UpdateAsync, GetAllAsync methods

## 2. Recurrence Logic

- [x] 2.1 Implement RecurrenceCalculator.CalculateNextDue(DateOnly completionDate, RecurrenceRule rule) method
- [x] 2.2 Handle month edge cases (e.g., Jan 31 + 1 month = Feb 28)

## 3. Template Lifecycle Handlers

- [x] 3.1 CreateRecurringTaskHandler: creates template and first instance
- [x] 3.2 CompleteRecurringTaskHandler: completes instance, creates next instance
- [x] 3.3 PauseRecurringTaskHandler: pauses template and current instance
- [x] 3.4 ResumeRecurringTaskHandler: resumes template and current instance
- [x] 3.5 CancelRecurringTaskHandler: cancels template and all its instances
- [x] 3.6 ListRecurringTemplatesHandler: lists all templates

## 4. Instance Modifications

- [x] 4.1 Add RecurringTaskId (nullable long) field to TaskItem record
- [x] 4.2 Modify CompleteOneShotTaskHandler to detect recurring instances and create next instance

## 5. Infrastructure

- [x] 5.1 Create recurring_task_templates database migration
- [x] 5.2 Add recurring_task_id column to one_shot_tasks table migration
- [x] 5.3 Implement SqliteRecurringTaskTemplateStore
- [x] 5.4 Register IRecurringTaskTemplateStore in DI container

## 6. API Layer

- [x] 6.1 Create RecurringTaskEndpoints with POST /tasks/recurring, GET /tasks/recurring
- [x] 6.2 Add POST /tasks/recurring/{id}/complete endpoint
- [x] 6.3 Add POST /tasks/recurring/{id}/pause endpoint
- [x] 6.4 Add POST /tasks/recurring/{id}/resume endpoint
- [x] 6.5 Add POST /tasks/recurring/{id}/cancel endpoint
- [x] 6.6 Register recurring endpoints in Program.cs

## 7. MCP Integration

- [x] 7.1 Add create_recurring_task MCP tool
- [x] 7.2 Add complete_recurring_task MCP tool
- [x] 7.3 Add pause_recurring_task MCP tool
- [x] 7.4 Add resume_recurring_task MCP tool
- [x] 7.5 Add cancel_recurring_task MCP tool
- [x] 7.6 Add list_recurring_tasks MCP tool

## 8. Testing

- [x] 8.1 Core unit tests for RecurrenceCalculator
- [x] 8.2 Core unit tests for recurring template lifecycle handlers
- [x] 8.3 Host integration tests for recurring endpoints
- [x] 8.4 MCP integration tests for recurring tools
