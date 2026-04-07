# Feature Spec: Water Infrastructure System

## Overview
Mid-to-late tier automation layer for water management. Player builds a pipe network from an ocean-adjacent pump to a purifier and/or sprinklers, eliminating manual water collection for farming and drinking.

## Stations

**Water Pump** — placed adjacent to an ocean tile, runs continuously with no fuel. Powers the connected pipe network.

**Water Pipe** — grid-placed connector. Adjacent pipes (4-directional) auto-form a network. Sprite auto-selects based on neighbors (straight, corner, T, cross).

**Pipe Water Purifier** — draws water from the network automatically on a timer. No manual bottle insertion or fuel required. Player collects output by interacting with a water container (same UX as existing Simple Water Purifier).

**Water Sprinkler** — placed on the network. Automatically calls `Water()` on all Planter instances within a configurable radius every N seconds while a pump is connected.

**Water Tank** *(optional/stretch)* — buffer station. Stores water units so the network has reserves when no pump is reachable. Player can also fill/drain manually.

## Suggested Recipes

| Station | Ingredients |
|---|---|
| Water Pipe | 1× Plank + 1× Scrap |
| Water Pump | 3× Plank + 2× Metal Ingot + 1× Hinge |
| Pipe Purifier | 2× Plank + 2× Metal Ingot + 1× Glass |
| Sprinkler | 2× Metal Ingot + 1× Hinge + 2× Nails |
| Water Tank | 3× Plank + 2× Metal Ingot |

## Architecture

- `IPipeConnectable` interface — `Vector2Int GridPosition`, `bool IsPump`, `bool IsActive`
- `PipeNetwork` MonoBehaviour singleton — `Dictionary<Vector2Int, IPipeConnectable>` registry, BFS `HasActivePump(pos)` method
- Components register `OnEnable`, deregister `OnDisable`
- Network graph is NOT saved — fully reconstructed from placed object positions on load

## New Files

- `Assets/Scripts/Stations/WaterPump.cs`
- `Assets/Scripts/Stations/WaterPipe.cs`
- `Assets/Scripts/Stations/PipeWaterPurifier.cs`
- `Assets/Scripts/Stations/WaterSprinkler.cs`
- `Assets/Scripts/Stations/WaterTank.cs` (optional)
- `Assets/Scripts/Stations/PipeNetwork.cs`
- `Assets/Scripts/Stations/IPipeConnectable.cs`
- Item + Recipe assets for each station under `Resources/Items/` and `Resources/CraftingRecipes/`
- Prefabs with `SaveableEntity` + `ISaveableComponent` under `Assets/Prefabs/`

## Save/Load

Fully additive — all state lives in `WorldObjectData.components[]` via `ISaveableComponent`. Old saves unaffected.

- `PipeWaterPurifier` saves: `purifyTimer`, `hasPurifiedWater`
- `WaterSprinkler` saves: `sprayTimer`
- `WaterTank` saves: `storedUnits`
- `WaterPump`: ocean adjacency recomputed on `Start()` — nothing to save
- `WaterPipe`: no state to save

## Key Edge Cases

- Pump placed where ocean later doesn't exist (seed change): becomes inactive, network loses supply
- Pipe removed mid-network: consumers stop within one spray interval (no explicit event needed)
- Chunk unload containing pipe: `OnDisable` deregisters; consumers gracefully stop
- New Game: `PipeNetwork` registry must be cleared — use scene-lifetime MonoBehaviour, not static class
- Two pumps on same network: both register, BFS finds either — redundancy works naturally

## Implementation Order

1. `IPipeConnectable.cs` — interface definition
2. `PipeNetwork.cs` — MonoBehaviour singleton with registry + BFS
3. `WaterPipe.cs` — registers position, picks sprite from neighbors
4. `WaterPump.cs` — checks ocean adjacency on Start, IInteractable + ISaveableComponent
5. `PipeWaterPurifier.cs` — auto-purifies when network has pump, player collects output
6. `WaterSprinkler.cs` — auto-waters Planters in radius on interval
7. `WaterTank.cs` — buffer storage, optional
8. Item + Recipe assets (create in Unity Editor)
9. Prefabs (create in Unity Editor)
10. Add to building menu if using hammer placement

## Testing Checklist

- [ ] Pump adjacent to ocean → active state
- [ ] Pump not adjacent to ocean → inactive, HasActivePump returns false
- [ ] Pump → 3 pipes → sprinkler → HasActivePump returns true at sprinkler
- [ ] Remove middle pipe → sprinkler stops firing
- [ ] Sprinkler fires and calls Water() on all Planters in radius
- [ ] PipeWaterPurifier purifies without manual input
- [ ] Player collects purified water with Cup/Water Bottle
- [ ] Save mid-purification → reload → resumes from saved timer
- [ ] Save mid-spray-timer → reload → continues correctly
- [ ] New Game → registry empty, no stale refs
- [ ] Pump on chunk boundary → unload → consumer stops → reload → resumes
