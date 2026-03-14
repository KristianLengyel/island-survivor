# Skill: Feature Spec to Implementation

## Purpose
Turn a feature request into a complete implementation plan before writing any code. Ensures nothing is missed and the feature fits the existing architecture.

## When to Use
- Any feature that touches more than one system.
- Any feature that affects save/load, inventory, UI, or map generation.
- Any time the scope of a request is unclear before implementation.

## Required Inputs
- Feature description (what the player sees and does).
- Any constraints or preferences (tone, progression tier, performance limits).

## Process

1. **Player-facing behavior** — describe exactly what the player experiences. What triggers it, what they see, what they can do.

2. **Required data** — what new fields, ScriptableObjects, or data structures are needed.

3. **Affected systems** — list every system that needs to change or be aware of the feature (inventory, crafting, save, map, UI, input, tools, stats, weather, etc.).

4. **Files likely involved** — specific `.cs` files, prefabs, and ScriptableObject assets.

5. **Save/load impact** — is new state persisted? Is it additive? Is there migration risk? What is the default for old saves?

6. **UI/feedback** — what does the player see? Notifications, stat bars, indicators, sounds?

7. **Input** — does the feature consume any existing input (scroll, LMB, RMB, interact)? Does it need to be blocked by menus?

8. **Edge cases** — what happens if the player is mid-action? What happens on scene reload? What happens if the relevant object is destroyed?

9. **Implementation steps** — ordered list of specific code changes. Each step should reference a file and a method.

10. **Testing checklist** — concrete in-editor steps to verify the feature works correctly.

## Expected Output
A structured document covering all 10 sections above, ready to hand to implementation.

## Constraints
- Do not write code in this step unless the implementation is trivially small.
- Flag any save/load risk explicitly — do not bury it in a list.
- Flag any input conflicts with existing bindings.
- Flag any performance concern for hot paths (Update, FixedUpdate, generation thread).
