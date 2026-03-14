# Skill: Economy Audit

## Purpose
Audit the current state of the resource economy, crafting tree, item usefulness, and survival stat balance. Identify problems before they become player-facing issues.

## When to Use
- Before adding a new item or recipe (check it fits).
- After a batch of content additions (check for imbalance).
- When a player reports the game feels grindy or trivial at a particular stage.
- When planning a new tier of content.

## Required Inputs
- Current item list (from `Assets/Resources/Items/`).
- Current recipe list (from `Assets/Resources/CraftingRecipes/` and `ProcessingRecipes/`).
- Current food data (from `Assets/Resources/FoodData/`).
- Optionally: player-reported pain points or specific systems to focus on.

## Process

1. **Item inventory** — list all items by category (raw, refined, component, tool, placeable, food, consumable). Flag any with no clear purpose.

2. **Recipe tree** — map the full crafting dependency graph. Identify:
   - Dead ends (items produced but not used anywhere).
   - Bottlenecks (one item required by everything in a tier).
   - Missing links (logical recipes that don't exist).

3. **Progression path** — trace the intended day 1 → week 1 → late-game path. Identify:
   - Gaps where progression stalls.
   - Shortcuts that bypass intended effort.
   - Items locked behind the wrong tier.

4. **Food and water balance** — calculate approximate hunger/thirst drain vs. available food per day. Is starvation a realistic early-game risk? Is food trivially abundant by mid-game?

5. **Tool coverage** — does every tool have a clear primary role? Are any tools redundant or outclassed?

6. **Renewable systems** — at what point does farming become viable? Is fishing meaningful? Are catching nets worth building?

7. **Output** — produce the following:
   - Bottleneck table (resource, recipes depending on it, risk level).
   - Dead-end item list.
   - Suggested new recipes or drops to fill gaps.
   - Suggested balance changes (drop rates, yield amounts, growth times, nutrition values).
   - Progression timeline recommendation (what should be achievable by day 1, 3, 7, etc.).

## Constraints
- Do not rename items — they are save IDs.
- Flag any change that affects what is stored in save files.
- Changes to drop rates or yield amounts do not affect save compatibility.
- Changes to item `maxFillCapacity` or food `hungerValue` do not affect save compatibility.
