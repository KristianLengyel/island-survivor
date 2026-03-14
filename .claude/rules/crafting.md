# Crafting and Progression Rules

## Tier Logic
- Progression moves from basic survival tools → processing stations → automation/convenience.
- Early tier: axe, hook, basic food, rope, planks.
- Mid tier: workbench, grill, water purifier, planter, fishing rod, catching net.
- Late tier: advanced materials (metal ingot, copper ingot), refined stations, lamp, hinge, bolt.
- Do not add late-tier shortcuts that bypass mid-tier effort.

## Recipe Design
- Every recipe must have a clear purpose and a clear place in the player's progression.
- Avoid dead-end recipes — items that produce something with no downstream use.
- Avoid item redundancy unless the items serve meaningfully different roles.
- New recipes must use existing items where possible. Do not add new raw materials casually.
- Recipes should be readable at a glance — ingredient lists should not exceed 4-5 distinct item types.

## The Hook
- The Hook (Plastic and Scrap tiers) is the primary early-game ocean tool.
- It must remain craftable from early materials.
- Do not gate the Hook behind mid-tier resources.

## Farming
- Farming is the intended long-term food source, not a shortcut.
- Crops require planting, watering (manually or via rain), and time.
- Farming should not be trivially available from day one — it requires a Planter, which requires crafting.
- Crop yield and growth time should reward sustained investment.

## Processing Stations
- Grill: cooks raw food into higher-nutrition meals. Requires Plank fuel.
- Water Purifier: converts ocean water to drinkable water. Requires Plank fuel.
- Water Barrel: collects rainwater passively. No active fuel.
- Each station has a distinct role. Do not merge their functions.

## Consistency
- Any new item must exist in: `Resources/Items/`, have a recipe if craftable, have `FoodData` if consumable, and be saveable if it can be placed.
- Crafting UI, inventory, placement, and save/load must all be aware of new items. Check all four.
- Item names are currently used as IDs in save data — do not rename existing item assets.
