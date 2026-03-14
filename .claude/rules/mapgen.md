# Map Generation Rules

## World Structure
- The world is an ocean filled with many small-to-medium islands. Default: ~80 islands per map.
- Islands must never merge or touch. Minimum spacing must always be enforced.
- Do not treat the world as a single island. Every feature must account for multi-island topology.
- The player starts on a 2×2 wooden dock placed at map center (0,0 in tile space). This is always ocean — not land.

## Generation Pipeline
- Generation runs on a background thread. No Unity API calls are allowed inside the thread.
- All per-tile data lives in `MapDataV3` as flat arrays indexed `[y * size + x]`.
- Use `d.Idx(x, y)` and `d.IdxSafe(x, y)` for indexing — do not rewrite raw index math unless necessary.
- The pipeline order is fixed: noise → threshold → cellular automata → cleanup → distance field → biomes → decorators → chunks → streamer.
- Do not insert new stages between existing ones without understanding the full dependency chain.

## Chunk Streaming
- Chunks load/unload dynamically based on player proximity. Not all tiles are in memory as live GameObjects.
- Decorator GameObjects (palms, rocks) are only spawned when a chunk becomes visible.
- Felled/mined decorator indices are tracked persistently — do not reset them on chunk reload.
- Changes to chunk loading logic must not re-spawn destroyed decorators.

## Biomes
- Each island is assigned one dominant biome: Tropical, Temperate, Desert, Arctic, Volcanic.
- Biome data is in `BiomeDefinitionV3` ScriptableObjects — prefer adding fields there over hardcoding per-biome logic.
- Tilemap tinting colors (`landColor`, `grassColor`) are separate from minimap colors (`mapLandColor`, `mapGrassColor`). Keep them independent.

## Coastlines and Playability
- Beach tiles form a strip between ocean and grass interior. Widths are configurable in `MapSettingsV3`.
- Coastlines must be readable — avoid single-tile spurs, isolated pixels, or unplayable narrow peninsulas.
- Cellular automata and morphology closing smooth the coast. Tuning these affects all islands simultaneously.

## Spawn Fairness
- The dock area at map center must have ocean tiles immediately surrounding it so the Hook is usable from the start.
- Nearby islands should offer wood and stone within reasonable reach from the dock.
- Do not place impassable terrain or generation artifacts at or near map center.

## Seed Stability and Determinism
- Generation must be fully deterministic given the same seed and settings.
- Do not use `System.Random`, `UnityEngine.Random`, or `DateTime.Now` inside the generation thread — use `MapRngV3`.
- Settings changes will naturally change output — that is acceptable. Seed changes must not produce identical maps.

## Memory and Performance
- Map sizes go up to 2048×2048. Per-tile data must stay as value-type flat arrays, not boxed objects.
- Do not allocate new arrays inside the generation loop. Pre-allocate in `MapWorkspaceV3.Ensure()`.
- Tilemap painting happens layered, per-frame budgeted — do not paint all tiles in one frame.
