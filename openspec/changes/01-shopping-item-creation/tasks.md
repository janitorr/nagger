## 1. Core domain model and port

- [ ] 1.1 Add `ShoppingItem` record and `ShoppingItemStatus` enum (`Needed`/`Bought`/`Cancelled`) with contract-value mapping in `src/Nagger.Core/Shopping/Domain/ShoppingItem.cs`; verify a Core test maps each status to/from `needed`/`bought`/`cancelled`
- [ ] 1.2 Add a `ReNeeded(now)` behavior method that sets `status: needed`, updates `updatedAt`, and clears `completedAt`/`cancelledAt`; verify a Core test covers the flip from `bought`
- [ ] 1.3 Declare the `IShoppingItemStore` port (`GetByNameAsync`, `AddAsync`, `UpdateAsync`) in `src/Nagger.Core/Shopping/Ports.cs`; verify `dotnet build Nagger.slnx` succeeds

## 2. Core add command and handler

- [ ] 2.1 Add `AddShoppingItemsCommand`, `AddShoppingItemInput` (`name`, `stores?`), and the upsert handler in `src/Nagger.Core/Shopping/AddShoppingItems.cs`; verify Core tests (in-memory store) cover: new name creates `needed` with given/empty stores, already-`needed` name is a no-op, and `bought`/`cancelled` flips to `needed` preserving stores
- [ ] 2.2 Add input validation for an empty/missing name and empty store entries; verify Core tests assert a structured `ValidationException` and no persistence for those inputs
- [ ] 2.3 Verify case-insensitive name matching (re-add differing only in case is treated as the same item); verify a Core test covers it

## 3. Host persistence

- [ ] 3.1 Add the EF `ShoppingItem` entity and `NaggerDbContext` mapping (unique `Name` index with NOCASE collation, JSON value converter for `stores`); verify `dotnet build Nagger.slnx` succeeds
- [ ] 3.2 Add the `AddShoppingItems` EF migration under `src/Nagger.Host/Infrastructure/Migrations/`; verify a Host test creates a fresh temp SQLite database and the migration applies on startup
- [ ] 3.3 Implement `SqliteShoppingItemStore` implementing `IShoppingItemStore`; verify a Host integration test persists and reads back an item through the store

## 4. Host REST endpoint

- [ ] 4.1 Add `ShoppingEndpoints.cs` with `POST /shopping-items` returning `201 Created` and a camelCase `ShoppingItemResponse`, and register it in `Program.cs`; verify a Host integration test creates a single item and asserts the response fields
- [ ] 4.2 Verify batch creation and duplicate handling through REST; verify Host integration tests for adding multiple items in one request and for re-adding an already-needed name returning the existing item without a duplicate

## 5. MCP tool

- [ ] 5.1 Add `McpShoppingTools` with an `add_shopping_items` tool (`names`, optional `stores`) and its response type, registered in `Program.cs`; verify a Host MCP test adds items and returns structured content
- [ ] 5.2 Verify the MCP tool surfaces validation errors the same way the existing task tools do; verify a Host MCP test for an empty name

## 6. Documentation and verification

- [ ] 6.1 Document `POST /shopping-items` in `USAGE.md`; verify the endpoint/payload/example matches the implemented behavior
- [ ] 6.2 Run `dotnet test Nagger.slnx` and verify all Core and Host tests pass
- [ ] 6.3 Run `dotnet stryker` (Core) and add tests for any newly surviving mutants until the mutation score stays at or above the `break` threshold (target 80%)
