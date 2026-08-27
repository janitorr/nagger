# Shopping Ledger — Implementation Plan

Reference only. Each change below becomes its own OpenSpec proposal, called
one at a time. Numbered prefix = implementation order.

## Locked decisions

| Topic | Decision |
|---|---|
| Stores | JSON array of strings, `[]` = generic (anywhere) |
| Vocabulary | `S-Group`, `K-Group`, `Lidl` |
| On-demand list | Grouped by store in visit order; each item once, under earliest acceptable store; generic → first stop |
| Visit order | Config `Nagger:ShoppingStoreOrder = ["Lidl","S-Group","K-Group"]` |
| Duplicate add | No-op if already `needed` |
| Un-buy | Allowed (`bought → needed`) |
| Re-add known item | One long-lived row per name; flip status back to `needed` (no history) |
| Fields | Name + store only (no quantity/unit/category/priority) |
| Morning digest | Names only, no stores/amounts |
| Approval | None — "we need coffee, milk, yogurt" just adds |

## Model

    ShoppingItem(id, name, stores[], status, createdAt, updatedAt, completedAt?, cancelledAt?)
    name   = unique, case-insensitive, one row per name
    status = needed | bought | cancelled

    add(name, stores?)  no row           → create needed
                        needed           → no-op
                        bought/cancelled → flip to needed, keep remembered stores

    buy    : needed → bought    (completedAt = now)
    unbuy  : bought → needed    (completedAt = null)
    cancel : needed → cancelled (cancelledAt = now)

## Target API (accumulates across changes)

    POST /shopping-items               { "items": [{"name":"coffee"},{"name":"milk","stores":["Lidl"]}] }
    POST /shopping-items/{name}/buy
    POST /shopping-items/{name}/unbuy
    POST /shopping-items/{name}/cancel
    GET  /shopping-items               flat list with status
    GET  /reports/shopping             grouped needed list
    GET  /reports/morning              gains "shopping" names section

    MCP tools: add_shopping_items, buy_shopping_item, unbuy_shopping_item,
               cancel_shopping_item, list_shopping_items, get_shopping_list

## Changes (in order)

### 01-shopping-item-creation
- New domain: `ShoppingItem`, `ShoppingItemStatus` (+ contract mapping), stores array.
- `AddShoppingItemsCommand` (batch {name, stores?}) + handler: upsert-by-name.
- Port `IShoppingItemStore` (GetByNameAsync, AddAsync, UpdateAsync).
- Host: EF entity + migration + `SqliteShoppingItemStore`.
- REST `POST /shopping-items`; MCP `add_shopping_items`.
- Spec: `shopping-item-creation`.

### 02-shopping-item-lifecycle
- Domain transitions: `Buy`, `Unbuy`, `Cancel` on `ShoppingItem` (strict, ValidationException on invalid).
- Commands + handlers; REST `/buy|/unbuy|/cancel`; MCP tools.
- Spec: `shopping-item-lifecycle`.

### 03-shopping-item-listing
- `ShoppingListQuery` (store order passed in → Core stays config-free).
- Assignment: needed item → earliest acceptable store; generic → first store; group in visit order.
- REST `GET /shopping-items` + `GET /reports/shopping`; MCP `list_shopping_items`, `get_shopping_list`.
- Config `Nagger:ShoppingStoreOrder`, stores validated against it.
- Spec: `shopping-item-listing`.

### 04-shopping-in-morning-report
- `MorningReport` gains `shopping: [names]` (needed items only); schemaVersion 4 → 5.
- Update REST + MCP report responses; digest consumer tolerates additive field.
- Spec: extend `morning-task-report` (or new `shopping-in-morning-report`).

## Deferred (do not build now)
- Purchase history / event-store table
- quantity/unit/category/priority
- Rename/edit semantics
- Approval workflow

## Open details to settle per-change
- 01: name normalization (store as entered, match case-insensitively)
- 03: include empty store groups in the on-demand list, or only non-empty?
- 04: spec placement (extend morning-task-report vs new capability)
