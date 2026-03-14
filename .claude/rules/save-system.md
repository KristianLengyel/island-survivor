# Save System Rules

## Architecture
- Save orchestration lives in `SaveGameManager.cs`.
- Serializable data classes live in `SaveGameData.cs`.
- Disk I/O is handled by `SaveIO.cs` (JSON to `Application.persistentDataPath`).
- Saveable world objects are marked with `SaveableEntity`. `ISaveableComponent` is optional — components on a saved object can implement it for per-component save/load, but it is not required.
- `SaveGameManager.Start()` is a coroutine that yields one frame before loading, so all other `Start()` methods run first.

## Compatibility
- Save files are JSON. Existing players will have save files on disk.
- Adding a new field to a serialized class is safe — JSON deserialization will default-initialize missing fields.
- Removing or renaming a field silently drops that data for existing saves. Always flag this risk.
- Changing the meaning of an existing field without migration will corrupt existing saves. Always flag this risk.
- Do not change the type of an existing serialized field.

## What Is Persisted
- Player stats: hunger, thirst, stamina, health.
- Inventory contents (player and chests).
- Placed objects (chests, planters, grills, barrels, etc.) with their state.
- World state: felled tree indices, felled rock indices per chunk.
- Day/night cycle state including day count and weather.
- Explored map tiles (bit-packed visited array in `PlayerMapData`).

## ID Stability
- `SaveableEntity` uses a string ID to identify world objects across saves.
- These IDs must remain stable. Do not change how they are assigned or formatted.
- Item names are currently used as item IDs in inventory save data. Do not rename item ScriptableObject assets.

## Safe Change Patterns
- Adding a new optional field: safe, provide a sensible default.
- Adding a new `ISaveableComponent` to an existing prefab: safe if it handles missing data gracefully.
- Splitting a field into two: migration required — handle the old field explicitly.
- Removing a field that was never saved: safe.
- Removing a field that is in existing saves: data is lost silently — warn the user.

## Format Versioning (Partial)
- `SaveGameData` has a `version` field (currently `1`) but it is never read or validated during load — it is effectively unused.
- Do not rely on this field for migration logic until it is wired into `SaveIO` or `SaveGameManager.Load()`.
- All changes must still be backwards-compatible by default.
- If a breaking change is unavoidable, the version field is the right place to gate migration — implement the check before making the breaking change.

## SaveGameManager.Start() Timing
- `SaveGameManager.Start()` is a coroutine. It first waits for `MapGeneratorV3.IsGenerating` to become false (may be many frames), then yields one additional frame, then calls `Load()`.
- Do not assume load completes in the same frame as `Start()`. Do not assume it completes after exactly one frame.
