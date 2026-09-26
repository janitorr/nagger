# Tasks

## 1. Core shopping-item domain and operations

- [x] 1.1 Add a `ShoppingItem` domain entity (`id`, `name`) and an `IShoppingItemStore` port in `Ports.cs` (`AddAsync`, `GetByNameAsync`, `RemoveAsync`, `GetAllAsync`); verify the Core project builds.
- [x] 1.2 Implement `AddShoppingItem` (trim + nonempty validation, case-insensitive idempotent add returning the existing item) and add Core tests covering new add, idempotent add on case/whitespace variants, and empty-name rejection; verify `dotnet test tests/Nagger.Core.Tests` passes.
- [x] 1.3 Implement `RemoveShoppingItem` (case-insensitive idempotent remove, no-op for an absent name) and add Core tests covering remove and absent-name no-op; verify `dotnet test tests/Nagger.Core.Tests` passes.
- [x] 1.4 Implement `ListShoppingItems` returning items in ascending `id` order and add Core tests covering insertion order and an empty result; verify `dotnet test tests/Nagger.Core.Tests` passes.

## 2. Morning report shopping section

- [x] 2.1 Add a `shopping` array to `MorningReport` (ordered ascending `id`), bump `schemaVersion` to `"5"`, and read open items via `IShoppingItemStore.GetAllAsync` in `MorningReportHandler`; add Core tests covering items present, an empty list, and that report reads do not change shopping state; verify `dotnet test tests/Nagger.Core.Tests` passes.

## 3. SQLite persistence

- [x] 3.1 Map `ShoppingItem` in `NaggerDbContext` with a `COLLATE NOCASE` unique index on `Name` and add a migration under `src/Nagger.Host/Infrastructure/Migrations/`; verify `dotnet build Nagger.slnx` succeeds and the host applies the migration on startup.
- [x] 3.2 Implement `SqliteShoppingItemStore` with case-insensitive `GetByNameAsync`; add a Host test exercising add/remove/list and case-insensitive uniqueness against a temporary SQLite database; verify `dotnet test tests/Nagger.Host.Tests` passes.

## 4. HTTP and MCP surfaces

- [x] 4.1 Add shopping REST endpoints (`POST /shopping`, `GET /shopping`, `DELETE /shopping/{name}`) and register the service group; document the endpoints, payloads, and status codes in `USAGE.md`; verify the documented examples run against a local host.
- [x] 4.2 Add MCP tools `add_shopping_item`, `remove_shopping_item`, and `list_shopping_items`, and update `get_morning_report` to return the shopping section; document the tools in `USAGE.md`; verify `dotnet build Nagger.slnx` succeeds.
- [x] 4.3 Add Host integration tests covering the shopping endpoints, MCP tools, and the report's `shopping` section (present items and empty list); verify `dotnet test tests/Nagger.Host.Tests` passes.

## 5. Release gates

- [x] 5.1 Run `dotnet csharpier format .`, `dotnet build Nagger.slnx`, `dotnet test Nagger.slnx`, and `dotnet stryker`; fix any newly surviving mutants and confirm the Core mutation score stays at or above 75% (target 80%).
