## 1. API Contract Simplification

- [x] 1.1 Remove explicit JSON property-name attributes from Host API request and response contracts so the default camelCase policy defines every JSON field name.
- [x] 1.2 Bind `POST /tasks/one-shot` directly to `CreateOneShotTaskCommand` and remove `CreateTaskRequest` and its mapping.
- [x] 1.3 Change Core creation validation error keys from snake_case to the camelCase request field names.

## 2. Contract Verification

- [x] 2.1 Update Host integration tests to send camelCase creation fields and assert camelCase task, lifecycle, validation-error, and Morning Report fields.
- [x] 2.2 Add or update coverage proving snake_case creation fields are no longer part of the supported contract.

## 3. Documentation And Validation

- [x] 3.1 Update `README.md` and `USAGE.md` JSON examples and field references to camelCase.
- [x] 3.2 Run `dotnet test Nagger.slnx` and `openspec validate simplify-task-api-contracts --strict`.
