## 1. Core Task Query

- [x] 1.1 Extend `ITaskStore` with a read-only operation that returns active and paused one-shot tasks in ascending ID order.
- [x] 1.2 Implement `ListOpenOneShotTasksQuery` and its Core handler without modifying task state or timestamps.
- [x] 1.3 Add Core tests for returning active and paused tasks, excluding terminal tasks, preserving order, and leaving task data unchanged.

## 2. Host Transports

- [x] 2.1 Implement the SQLite task-store open-task query using the existing status persistence values and ascending ID order.
- [x] 2.2 Map `GET /tasks/one-shot` to the Core query and return the established task response array.
- [x] 2.3 Add the read-only, zero-argument `list_one_shot_tasks` MCP tool returning structured task response data and documenting lifecycle IDs.

## 3. Contract Coverage And Documentation

- [x] 3.1 Add REST integration tests for listing open tasks and for an empty open-task list.
- [x] 3.2 Add MCP integration tests for tool discovery, listing open tasks, and an empty open-task list.
- [x] 3.3 Update `USAGE.md` with the REST endpoint and MCP tool, including their open-task scope and ID-discovery purpose.
- [x] 3.4 Run `dotnet test Nagger.slnx` and `openspec validate add-task-listing --strict`.
