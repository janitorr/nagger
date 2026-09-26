# Design

## Context

See proposal.md for motivation. Nagger's Core owns behavior as vertical slices under `src/Nagger.Core/Tasks/`, with persistence behind ports implemented by SQLite adapters in `src/Nagger.Host/Infrastructure/`. The morning report is a read-only query that already combines two readers (`ITaskStore`, `IRecurringTaskInstanceReader`). A shopping item is a new domain concept with no due date and no lifecycle beyond "present" or "absent", so it does not fit `TaskItem` or `RecurringTaskInstance`.

## Goals / Non-Goals

**Goals:**

- Add a dateless, name-keyed shopping item with idempotent add and remove.
- Surface open items on the morning report without making the report stateful.

**Non-Goals:**

- No quantities, prices, categories, due dates, or notes.
- No history or archive of removed items.
- No cross-device sync (unchanged from the rest of Nagger).

## Decisions

**Name is the identity.** Add and remove are keyed by `name` rather than `id`, because the user's mental model is "got milk" / "ran out of milk", not opaque ids. A durable numeric `id` still exists for stable insertion ordering (ascending) and as a primary key. Alternatives considered: id-keyed like tasks (rejected — tasks need ids because titles are not unique; shopping names are unique by design).

**Idempotent operations.** Adding a name already on the list returns the existing item (no duplicate); removing an absent name succeeds with no effect. This keeps the list a trustworthy signal and matches the casual, repetitive way a user reports being low on something. Alternatives considered: erroring on duplicate/absent (rejected — friendlier for an assistant and avoids LLM retry loops).

**Case-insensitive, whitespace-trimmed matching.** Names are trimmed on write and matched with `StringComparer.OrdinalIgnoreCase`, so "milk", "Milk", and " milk " coalesce. In SQLite this is enforced with a `COLLATE NOCASE` unique index on the `Name` column while storing the name with its original case for display. Alternatives considered: normalizing to lowercase on write (rejected — loses display case); comparing in Core by loading all items (rejected — unnecessary O(n) scan).

**Separate report section, not merged into `items`.** Shopping items have no due date, so they cannot be classified `overdue`/`due_today`/`upcoming`. They surface as a dedicated `shopping` array of `{ id, name }` objects ordered by ascending `id`, reusing the Core `ShoppingItem` type for its elements. `schemaVersion` becomes `"5"`. The report remains read-only — reading items changes no state.

**Minimal item representation.** A shopping item exposes only `id` and `name`, with no timestamps, because there is no lifecycle to timestamp and no update path. Alternatives considered: mirroring the task representation's `createdAt`/`updatedAt` (rejected — `updatedAt` is meaningless for an item that only appears and disappears).

**One Core store port.** A single `IShoppingItemStore` exposes `AddAsync`, `GetByNameAsync`, `RemoveAsync`, and `GetAllAsync`; the morning-report handler reuses `GetAllAsync` rather than a separate reader interface, since list and report read the same open set. (The recurring-task split into store + reader exists because instances and templates are distinct aggregates; shopping has one.)

**Follow the existing vertical-slice shape.** Three slices — `AddShoppingItem`, `RemoveShoppingItem`, `ListShoppingItems` — mirror `CreateOneShotTask` / `ManageOneShotTaskLifecycle` / `ListOpenOneShotTasks`. Validation throws the existing `ValidationException` so the host's `IExceptionHandler` and MCP error mapping handle empty names without new error plumbing.

## Risks / Trade-offs

- [SQLite `NOCASE` only folds ASCII case] → Acceptable: household item names are plain ASCII; accented case is not a realistic collision for this domain.
- [Two items can't share a name] → Users distinguish by naming ("whole milk" vs "milk"); out of scope to model variants.
- [No history means no "did I keep running out of X?"] → Accepted by decision: the list is current-needs only.
- [`schemaVersion` 4 → 5 is a contract change] → The only consumer is the LLM digest, updated in the same change; the report endpoint keeps backward-compatible field names otherwise.
