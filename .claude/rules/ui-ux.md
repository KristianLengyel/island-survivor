# UI and UX Rules

## Priorities
- Clarity first. The player must be able to read critical info at a glance.
- Survival stats (hunger, thirst, stamina) must always be visible during play.
- Critical stat warnings (below 15%) must be unmissable.
- Do not hide gameplay-relevant information behind extra clicks or menus.

## Interaction Consistency
- Interactions use a consistent input scheme — do not introduce new interaction patterns without considering what already exists.
- Left click: use/place. Right click: context action (consume, fill, remove). Middle or scroll: hotbar navigation.
- Hotbar scroll and number key input must be blocked when any menu (inventory, map, crafting, building) is open.
- Tool input (hook throw, fishing cast, axe swing) must be blocked when any menu is open.

## Menus
- Menus are coordinated through `MenuCoordinator`. New menus should register with it.
- Only one non-overlay menu should be open at a time.
- Map open state blocks tool use, hotbar scroll, and hotbar key input.
- Inventory open state blocks tool use and hotbar input.

## Minimap / Player Map
- The map reveals tiles as the player explores (fog of war).
- Tile colors reflect terrain type and biome — ocean depth, beach, grass, biome identity.
- The player marker shows position and rotation.
- Ctrl+M reveals the full map (debug/cheat). M toggles the map.
- Scroll input on the map is reserved for future zoom/pan — do not consume it elsewhere when the map is open.

## Feedback
- Item pickups show a notification (`ItemNotificationUI`).
- Water fill, cooking, and processing should have visible progress or audio feedback.
- Stat changes from eating/drinking should feel responsive, not delayed.

## Tone
- UI should fit a relaxed survival tone — not flashy, not clinical.
- Avoid excessive animations, transitions, or effects that do not serve clarity.
- Prefer pixel-readable fonts and clear icon silhouettes.

## Anti-Patterns
- Menus that can be opened while another menu is already open (unless overlay).
- Tool actions firing while a menu is open.
- Stat bars that are too small to notice at a glance.
- Interaction prompts that appear too late or disappear too fast.
- UI elements that overlap the game world without player control.
