# Island Survivor — Game Overview

## Concept

A 2D top-down survival game set across a procedurally generated world of many
islands. The player wakes up stranded on a small wooden dock/platform (2×2
tiles) surrounded by ocean, with nothing, and must survive by gathering
resources, crafting tools, building shelter, and managing their basic needs —
all while the environment works against them.

## Starting Situation

The player begins on a small **2×2 wooden dock/platform** at the center of the
map — the remnant of whatever brought them here. Dozens of islands are scattered
across the ocean around them. They must venture out, gather what the islands
offer, and build up from scratch.

## World Structure

The map is a large ocean scattered with **many procedurally generated islands**
(~80 per map). Islands vary in size and biome:

- **Biomes:** Tropical, Temperate, Desert, Arctic, Volcanic
- **Map sizes:** 256 / 512 / 1024 / 2048 tiles (selectable)
- Island spacing enforced so they never merge; coastlines shaped with cellular
  automata and domain warping
- Ocean has shallow, medium, and deep water layers
- Islands contain trees, rocks, foliage, and other resources

## Core Gameplay Loop

1. **Explore** the islands — travel across the ocean, discover new islands,
   chop trees, mine rocks, and forage resources
2. **Hook** floating resources out of the water using the Hook — the primary
   tool for collecting ocean items
3. **Craft** tools, items, and structures from gathered materials
4. **Build** a base/shelter to store items and stay safe
5. **Sustain** hunger and thirst — eat food, drink water, or face stat penalties
6. **Survive** weather events (rain, fog, thunderstorms) and the day/night cycle
7. **Farm** crops for a renewable food supply as natural resources thin out

## Tools & Gathering

### Hook (primary ocean tool)
The main tool for collecting resources from water:

- Hold to aim and charge (up to ~7 tiles range)
- Releases and flies through air, then enters water
- Hold to pull the hook back — collects floating items along the way
- Works on floating resources, seaweed, barrels drifting in water
- Comes in two tiers: **Plastic Hook** (basic) and **Scrap Hook** (upgraded)

### Fishing Rod
- Cast into water near shore (aim + throw mechanic)
- Wait 4–15 seconds for a fish to bite, then pull to catch
- Yields Raw Cod

### Catching Net
- Placed on water tiles as a stationary trap
- Floating items drift in automatically; interact to collect

### Other Tools
- **Axe** — chops trees (drops Planks, Leaves)
- **Shovel** — mines rocks (drops Rock, Sand, Dirt)
- **Hammer** — places and removes floor tiles

## Resources & Items

**Raw materials** — gathered from the environment:
- Wood (Planks), Leaves, Rock, Sand, Dirt, Rope, Scrap, Plastic, Glass
- Metal Ore, Copper Ore
- Seaweed (ocean)

**Refined materials** — processed from raw:
- Metal Ingot, Copper Ingot

**Crafted components** — used in recipes:
- Nails, Bolt, Hinge, Hook (Plastic / Scrap), Rope, Cup

**Tools:**
- Axe, Shovel, Hammer, Fishing Rod, Catching Net, Plastic Hook, Scrap Hook

**Placeables** — structures and stations:
- Workbench, Chest, Planter, Water Barrel, Fire Brazier, Simple Grill,
  Simple Water Purifier, Lamp

**Food (raw & cooked):**
- Fish: Raw Cod, Cooked Cod
- Vegetables: Carrot, Onion, Potato, Tomato (each has a baked variant)
- All cookable on the Simple Grill (requires Plank fuel)

**Consumables:**
- Water Bottle, Cup, Seaweed, Palm Sapling

## Fishing & Ocean Gathering

Three distinct ways to interact with water resources:

- **Hook** — the primary tool; thrown into water to pull floating items back
  to the player
- **Fishing Rod** — cast and wait for fish; active catching with a timed window
- **Catching Net** — placed on water as a passive trap; collect periodically

Fish roam the water as live entities with simple AI. Caught fish can be eaten
raw or cooked on the grill for better restoration values.

## Farming

Crops (Carrot, Onion, Potato, Tomato) are planted in Planters:

- Plant a crop → growth timer begins (~60 seconds default)
- Requires watering — either from rain or manually with a Cup / Water Bottle
- 3 visual growth stages; harvest when mature (yields 1–3 items)
- Cycle restarts after harvest

## Crafting & Processing

- **Workbench** — unlocks advanced crafting recipes
- **Simple Grill** — cooks raw food into higher-value meals (Plank as fuel)
- **Simple Water Purifier** — converts ocean water to drinkable water
  (Plank as fuel, ~10 seconds)
- **Water Barrel** — collects rainwater passively
- **Fire Brazier** — light source and warmth

Recipes require combinations of raw materials, refined materials, and
crafted components.

## Player Stats

- **Hunger** — max 100, drains at 0.1/sec; restored by eating food
- **Thirst** — max 100, drains at 0.2/sec (faster than hunger); restored by
  drinking water
- **Stamina** — max 100, consumed by tool use, regenerates at 8/sec when idle
- Warning UI triggers at 15% on any stat

## Day/Night & Weather

**Day/Night:**
- Full 24-hour cycle (configurable; default ~60 seconds per in-game day)
- Starts at 6:00 AM; smooth light gradient transitions through all hours

**Weather (decided each day):**
- **Clear/Sunny** — default, most common
- **Rainy** — rain particles active; crops watered automatically; Water Barrel
  fills passively
- **Foggy** — fog particles active; reduced visibility; crops not watered
- **Thunderstorm** — occurs on some rainy days; lightning flashes every 6–18
  seconds with thunder delay; dramatic light changes (visual/audio only)

## Building

**Floor tiles** (placed with Hammer):
- Wooden and stone floor variants
- Requires resources (planks, stone) per tile
- Pillar tiles auto-place under water for supported floors
- Placement range: 2 tiles from player

**Objects** (placed from inventory):
- Grid-snapped placement near player
- Some require a floor tile underneath; Catching Net requires water
- Remove with Hammer (hold RMB); returns resources
- Cannot remove a Chest that still contains items

## Key Features

- Procedurally generated world with many islands, multiple biomes, and
  chunk-based streaming (world feels large and explorable)
- Day/night cycle with dynamic weather (rain, fog, thunderstorms)
- Inventory system with containers (chests), crafting, and item stacking
- Save/load system — world state, player stats, and placed objects persist
- Resource nodes (trees, rocks) track felled/mined state per chunk and
  respawn over time
- Stamina system alongside hunger and thirst

## Tone & Feel

Relaxed survival with escalating challenge. The islands start generous but
resources thin out, forcing the player to explore further, plan, build, and
adapt. Think early Stardew Valley meets classic top-down survival (e.g.,
Don't Starve, but less punishing).

## Tech Stack

- Unity 2D (top-down)
- C# scripting
- JSON save system (persistent data path)
- Async/threaded map generation with chunk streaming
