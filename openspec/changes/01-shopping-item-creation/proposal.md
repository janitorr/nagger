## Why

The product brief reserves a "Future phase: Shopping ledger" so a person can track what they need to buy without an LLM reconstructing a shopping list from free-form notes. This first slice establishes the shopping item as a long-lived record and lets the user (or their assistant) add items by name with low friction, mirroring the deterministic, validated task workflow already proven in this service.

## What Changes

- Introduces a `ShoppingItem` domain model with `name`, a `stores` list, and a `needed`/`bought`/`cancelled` status, alongside creation and update timestamps.
- Adds an add-by-name operation that is an upsert: adding a name already on the list is a no-op; adding a name that was previously `bought` or `cancelled` flips the existing record back to `needed`, preserving its remembered stores.
- Adds a `stores` field as a JSON string array (empty array = no preference / generic), using the store vocabulary `S-Group`, `K-Group`, `Lidl`.
- Exposes `POST /shopping-items` (batch of `{name, stores?}`) and an `add_shopping_items` MCP tool over the same Core command.
- Adds an `IShoppingItemStore` port in Core, an EF entity plus migration, and a SQLite adapter in Host.

## Capabilities

### New Capabilities
- `shopping-item-creation`: creating and persisting needed shopping items through the service API, including add-by-name upsert semantics and duplicate handling.

### Modified Capabilities
<!-- none -->

## Impact

- New Core files under `src/Nagger.Core/Shopping/` (model, command, handler, port).
- New Host files: `ShoppingEndpoints.cs`, `McpShoppingTools.cs` (or extension of the existing MCP tool class), `SqliteShoppingItemStore.cs`, an EF `ShoppingItem` entity, and a migration.
- New REST endpoint `POST /shopping-items` and MCP tool `add_shopping_items`.
- No changes to existing task, recurring, or report contracts.
