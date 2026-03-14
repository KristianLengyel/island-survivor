# Skill: Procedural World Review

## Purpose
Review the world generation and biome design to ensure the generated world is varied, fair, navigable, and performs within budget.

## When to Use
- After changing any `MapSettingsV3` parameters.
- After adding a new biome or modifying `BiomeDefinitionV3`.
- After changes to noise, cellular automata, distance field, or decorator placement.
- When players report navigation problems, inaccessible areas, or unfair starts.
- Before shipping a new map size preset.

## Required Inputs
- Current `MapSettingsV3` values (island count, spacing, size range, CA iterations, biome definitions).
- Description of any recent generation changes.
- Optionally: screenshots or seed values that demonstrate a problem.

## Process

1. **Island variety** — are islands meaningfully different in size, shape, and biome? Do any biomes appear too rarely or too dominantly?

2. **Island separation** — is the minimum spacing enforced? Are there any cases where islands visually touch or are close enough to confuse navigation?

3. **Coastline quality** — are coastlines smooth and readable? Are there single-tile spurs, isolated pixels, or peninsulas too narrow to walk?

4. **Spawn fairness** — is the starting dock area surrounded by navigable ocean? Are resource-bearing islands reachable within reasonable Hook range from the dock? Is wood and stone available without extreme travel?

5. **Biome identity** — does each biome feel distinct (tile appearance, palm/rock density, colors)? Can the player tell biomes apart on the minimap?

6. **Resource distribution** — are key early resources (wood, stone) distributed across the world or clustered? Is any single island the only source of a critical resource?

7. **Navigation** — can the player reach all islands using the Hook? Are there ocean gaps that are too wide? Are any islands isolated behind impassable areas?

8. **Chunk streaming** — do chunks load and unload at appropriate distances? Do decorators (palms, rocks) appear and disappear smoothly? Are felled resources preserved correctly?

9. **Performance** — is the generation time acceptable for each map size? Are there per-tile allocations or redundant passes that could be reduced?

10. **Output** — produce:
    - Per-section assessment (pass / warning / fail).
    - Specific parameter recommendations for any failing section.
    - Seed or configuration that reproduces any identified problem.
    - Performance notes if generation time is flagged.

## Constraints
- Do not change `MapDataV3` array structure without understanding the full pipeline dependency chain.
- Do not insert new generation stages without mapping their dependencies on existing data arrays.
- Cellular automata and morphology changes affect all islands simultaneously — test with multiple seeds.
- Determinism must be preserved — any change must produce the same output for the same seed.
