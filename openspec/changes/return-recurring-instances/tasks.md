## 1. Core result types

- [ ] 1.1 Add `CreateRecurringTaskResult(Template, FirstInstance)`, change `CreateRecurringTaskCommand` to `ICommand<CreateRecurringTaskResult>`, and update the handler to capture the `instanceStore.AddAsync` return; update Core create tests to assert the returned `FirstInstance`. Verify with `dotnet test tests/Nagger.Core.Tests` (create tests green).
- [ ] 1.2 Add `CompleteRecurringTaskResult(CompletedInstance, NextInstance)`, change `CompleteRecurringTaskCommand` to `ICommand<CompleteRecurringTaskResult>`, and update the handler to capture the `instanceStore.AddAsync` return; update Core complete tests to assert the returned `NextInstance`. Verify with `dotnet test tests/Nagger.Core.Tests` (complete tests green).

## 2. REST surface

- [ ] 2.1 Add `RecurringCreationResponse`/`RecurringCompletionResponse` records and map the create and complete endpoints to the `{ template, firstInstance }` / `{ completedInstance, nextInstance }` envelopes; update the ApiTests create/complete tests and the `CreateRecurringTemplateAsync` helper to read the nested template id. Verify with `dotnet test tests/Nagger.Host.Tests`.

## 3. MCP surface

- [ ] 3.1 Add `McpRecurringCreationResponse`/`McpRecurringCompletionResponse` records, remap `create_recurring_task`/`complete_recurring_task`, and rewrite their `[Description]` strings to state the response contains both objects (and how to read `firstInstance.dueAt`/`nextInstance.dueAt`); update the McpTests create/complete tests and the `CreateRecurringTaskAsync` helper. Verify with `dotnet test tests/Nagger.Host.Tests`.

## 4. Docs

- [ ] 4.1 Update `USAGE.md` recurring create/complete examples and the lifecycle table row to show the new envelope responses. Verify the documented JSON matches the actual endpoint output.

## 5. Final verification

- [ ] 5.1 Run `dotnet build Nagger.slnx` and `dotnet test Nagger.slnx` and confirm the full solution builds and all tests pass.
- [ ] 5.2 Run `dotnet stryker` and add tests for any newly surviving mutants so the mutation score stays at or above the 75% threshold (target 80%).
