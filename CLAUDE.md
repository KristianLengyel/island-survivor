# Island Survivor — Claude Project Guide

## Identity
2D top-down Unity survival game. C#. Many procedurally generated islands in an ocean world.
Player starts on a 2×2 wooden dock at map center. Core loop: gather → craft → build → survive.

## Critical Rules
- Never add comments to generated code unless explicitly asked.
- Always produce full edited code, not snippets or pseudo-code.
- Match existing naming, file structure, and architecture exactly.
- Read a file before editing it. Never guess at field names or method signatures.
- Check save/load impact before adding or renaming any serialized field.
- Do not introduce new manager singletons. Work with existing patterns.
- Prefer ScriptableObjects for data. Prefer additive changes over rewrites.

## Architecture References
- Map gen: `Assets/MapGenerationV3/` — async threaded, chunk-streamed, `MapDataV3` flat arrays
- Inventory: `InventoryContainer.cs` — shared by player and chests
- Save: `SaveGameManager.cs` + `SaveGameData.cs` — JSON to persistentDataPath. Start() waits for map gen then one extra frame before Load().
- Input: `GameInput.cs` → `LegacyInputBackend.cs` — never read `Input.*` directly
- Scene flow: MainMenu → Loading → Game via `GameBoot` static flags

## Gameplay Constraints
- Hook is the primary early ocean-gathering tool.
- Hunger drains at 0.1/s, thirst at 0.2/s — both must stay meaningful.
- Farming is the intended long-term food solution, not the starting one.
- Islands must never merge. Generation must keep them separated.
- Building is always grid-snapped. Do not break this.
- Weather (rain, fog, thunderstorm) must have gameplay consequences, not just visuals.

## Output Rules
- No comments in code unless asked.
- No partial snippets when full file context is needed.
- No new files unless strictly necessary.
- No backwards-compat shims for removed code.
- No unnecessary abstractions for one-off needs.

## Feature Design Checklist
Before implementing any feature, confirm:
- [ ] Does it fit the relaxed-survival tone?
- [ ] Does it affect save/load? If yes, is it additive?
- [ ] Does it affect inventory, crafting, or placement consistency?
- [ ] Does it interact with chunk streaming or map data?
- [ ] Is it readable in early game?
- [ ] Does it have a clear UI/feedback path?

## Detailed Rules
See `.claude/rules/` for per-domain guidance.
See `.claude/agents/` for specialized subagent roles.
See `.claude/skills/` for structured workflows.
