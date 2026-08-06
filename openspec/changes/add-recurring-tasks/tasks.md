## 1. Core Domain

- [ ] 1.1 Add RecurringTaskTemplate record with fields: Id, Title, StartDate, Recurrence, ReminderPolicy, Status, CreatedAt, UpdatedAt, CancelledAt
- [ ] 1.2 Add RecurrenceRule record with Every (int) and Unit (enum: Days, Weeks, Months)
- [ ] 1.3 Add RecurringTaskStatus enum with Active, Paused, Cancelled
- [ ] 1.4 Add RecurringTaskNotFoundException
- [ ] 1.5 Add IRecurringTaskTemplateStore port interface with AddAsync, GetByIdAsync, UpdateAsync, GetAllAsync methods

## 2. Recurrence Logic

- [ ] 2.1 Implement RecurrenceCalculator.CalculateNextDue(DateOnly completionDate, RecurrenceRule rule) method
- [ ] 2.2 Handle month edge cases (e.g., Jan 31 + 1 month = Feb 28)

## 3. Template Lifecycle Handlers

- [ ] 3.1 CreateRecurringTaskHandler: creates template and first instance
- [ ] 3.2 CompleteRecurringTaskHandler: completes instance, creates next instance
- [ ] 3.3 PauseRecurringTaskHandler: pauses template and current instance
- [ ] 3.4 ResumeRecurringTaskHandler: resumes template and current instance
- [ ] 3.5 CancelRecurringTaskHandler: cancels template and all its instances
- [ ] 3.6 ListRecurringTemplatesHandler: lists all templates

## 4. Instance Modifications

- [ ] 4.1 Add RecurringTaskId (nullable long) field to TaskItem record
- [ ] 4.2 Modify CompleteOneShotTaskHandler to detect recurring instances and create next instance

## 5. Infrastructure

- [ ] 5.1 Create recurring_task_templates database migration
- [ ] 5.2 Add recurring_task_id column to one_shot_tasks table migration
- [ ] 5.3 Implement SqliteRecurringTaskTemplateStore
- [ ] 5.4 Register IRecurringTaskTemplateStore in DI container

## 6. API Layer

- [ ] 6.1 Create RecurringTaskEndpoints with POST /tasks/recurring, GET /tasks/recurring
- [ ] 6.2 Add POST /tasks/recurring/{id}/complete endpoint
- [ ] 6.3 Add POST /tasks/recurring/{id}/pause endpoint
- [ ] 6.4 Add POST /tasks/recurring/{id}/resume endpoint
- [ ] 6.5 Add POST /tasks/recurring/{id}/cancel endpoint
- [ ] 6.6 Register recurring endpoints in Program.cs

## 7. MCP Integration

- [ ] 7.1 Add create_recurring_task MCP tool
- [ ] 7.2 Add complete_recurring_task MCP tool
- [ ] 7.3 Add pause_recurring_task MCP tool
- [ ] 7.4 Add resume_recurring_task MCP tool
- [ ] 7.5 Add cancel_recurring_task MCP tool
- [ ] 7.6 Add list_recurring_tasks MCP tool

## 8. Testing

- [ ] 8.1 Core unit tests for RecurrenceCalculator
- [ ] 8.2 Core unit tests for recurring template lifecycle handlers
- [ ] 8.3 Host integration tests for recurring endpoints
- [ ] 8.4 MCP integration tests for recurring tools
