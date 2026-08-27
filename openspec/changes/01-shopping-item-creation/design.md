## Context

The service uses a ports-and-adapters split: `Nagger.Core` owns behavior and ports (no EF, no clock, no config), `Nagger.Host` adapts REST/MCP/SQLite. Feature slices live as vertical files in `Core` (command/query + handler + validation in one file), with the domain model in `Core/*/Domain/`. See proposal.md for motivation and `openspec/specs/shopping-item-creation/spec.md` for the required behavior.

This slice only introduces creation/upsert; `buy`/`unbuy`/`cancel` transitions and the store-grouped listing are later slices. The full status set (`needed`/`bought`/`cancelled`) still needs to exist now, because the add upsert flips `bought`/`cancelled` records back to `needed`.

## Goals / Non-Goals

**Goals:**
- A `ShoppingItem` domain model with name, stores list, status, and timestamps.
- An add-by-name upsert: create new, no-op when already `needed`, flip `bought`/`cancelled` back to `needed`.
- `POST /shopping-items` (batch) and an `add_shopping_items` MCP tool over one Core command.
- Persistence: EF entity, migration, SQLite adapter; `stores` stored as a JSON array.

**Non-Goals:**
- Lifecycle transitions (`buy`/`unbuy`/`cancel`) — slice 02.
- Store-grouped listing and the store-visit-order config — slice 03.
- Morning-report integration — slice 04.
- Updating `stores` (or renaming) an existing item — future editing slice; re-add preserves existing stores.
- Purchase history / event store.

## Decisions

**1. New `Shopping/` slice folder in Core, port in `Shopping/Ports.cs`.** Mirrors the existing `Tasks/` organization rather than growing `Tasks/Ports.cs`, keeping one concern per area.

- Alternative: extend the existing `Tasks/Ports.cs` — rejected because shopping is a distinct domain area per the product brief.

**2. `ShoppingItem` is a long-lived record keyed by a unique, case-insensitive `name`; a numeric `id` is the stable identity.** Name is the natural key for add-by-name; `id` matches the task pattern and future-proofs references. Name is stored as entered (case preserved for display); matching is case-insensitive.

- Alternative: no numeric id, string-name PK — rejected for consistency with tasks and to allow future renames without rewriting references.

**3. Case-insensitive name handling via SQLite `NOCASE` collation on `Name`, with a unique index.** NOCASE gives ASCII case-folding, adequate for a local personal list. The handler also does a lookup-first upsert, so a duplicate never reaches the unique index in the single-writer local case.

- Alternative: separate normalized column + index — more moving parts; rejected for a local single-process store.

**4. `stores` persisted as a JSON-encoded text column via an EF value converter** (`IReadOnlyList<string>` ↔ JSON string), matching the brief's "JSON array" intent.

- Alternative: child `ShoppingItemStore` table — rejected as overkill for a small personal list with no cross-item store queries yet.

**5. Batch command `AddShoppingItemsCommand` with `IReadOnlyList<AddShoppingItemInput>` (name, stores?).** One endpoint handles "coffee, milk, yogurt". The handler upserts each input independently and returns the resulting items. A provided `stores` applies only to newly created items; re-adds preserve the stored `stores` (see Non-Goals).

**6. Domain model exposes a `ReNeeded(now)` method** (sets `status: needed`, updates `updatedAt`, clears `completedAt`/`cancelledAt`) following the existing `TaskItem` record-with-behavior style. `Needed`/`Bought`/`Cancelled` are all declared now; only the `Needed` transition method ships in this slice.

**7. Separate `McpShoppingTools` class** with an `add_shopping_items` tool (`names: string[]`, optional `stores: string[]` applied to all), registered alongside `McpTaskTools`. If the MCP SDK's `WithTools<T>` is single-type rather than additive, fall back to adding the tool to `McpTaskTools` — resolved in the apply phase.

## Risks / Trade-offs

- [NOCASE is ASCII-only] → acceptable for personal Finnish/English item names; revisit if non-ASCII case-folding ever matters.
- [Check-then-upsert race] → acceptable for a localhost single-writer SQLite; the unique index still prevents a true duplicate row.
- [Re-add preserves stores, no way to edit yet] → intentional deferral; the editing slice (later) will add store updates.

## Migration Plan

- Add an EF migration `AddShoppingItems` (new `ShoppingItems` table) under `src/Nagger.Host/Infrastructure/Migrations/`; migrations apply automatically on Host startup.
- Rollback: revert the migration/commit; no data migration or backfill needed.
