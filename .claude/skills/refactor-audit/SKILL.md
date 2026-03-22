# Skill: Refactor Audit

## Purpose
Scan all C# scripts in the project for repeated code patterns and produce a ranked, safety-tiered report of what is worth extracting into reusable utilities, base classes, or interfaces. Phase 1 is read-only analysis only. No edits are made until the user explicitly approves specific items.

## When to Use
- When the codebase has grown and duplication is suspected.
- Before a large feature addition, to reduce the surface area of repetition.
- After a batch of new systems have been added without shared utilities.

## Out of Scope (Never Touch)
- `Assets/MapGenerationV3/` — threaded generation code, high complexity, no automated tests.
- `Assets/Scripts/SaveSystem/` — save format stability is critical; field renames break existing saves.
- Any serialized field name on a MonoBehaviour or ScriptableObject — renaming breaks prefab/scene references.
- Any item asset name — item names are save IDs.
- Any file the user explicitly excludes in this session.

## Process

### Phase 1 — Analysis (Read-Only)

1. **Collect all C# files**
   - Glob `Assets/Scripts/**/*.cs`, excluding SaveSystem and MapGenerationV3.
   - Read each file. Build a catalogue of: class name, base class, key methods, serialized fields, patterns used.

2. **Identify repeated patterns across files. Look for:**
   - `FindAnyObjectByType<T>()` calls scattered across multiple classes (should be centralised or injected).
   - Identical or near-identical `Awake` / `Start` caching blocks.
   - Audio play calls with the same `AudioManager.instance.PlaySound(...)` signature repeated with minor variation.
   - Stat change patterns (hunger/thirst/stamina modify + clamp) duplicated across consumers.
   - Tag comparison patterns (`CompareTag("X")`) used the same way in many places — candidate for a constant.
   - Physics overlap scan boilerplate (`OverlapCircle` + loop + GetComponentInParent) repeated across tools.
   - Null-guard + cached-ref patterns repeated in the same shape in multiple classes.
   - Animation trigger calls (`animator.SetBool / SetTrigger`) with the same parameter name strings in multiple classes — candidate for string constants.
   - `ISaveableComponent` / `SaveableEntity` patterns implemented inconsistently.

3. **Score each candidate by two axes:**
   - **Impact** (how many files/classes benefit): High = 4+ files, Medium = 2-3, Low = 1.
   - **Safety** (risk of breaking Unity serialization, save data, or inspector refs):
     - Safe: static utility method, extension method, interface with no serialized fields.
     - Moderate: new abstract base class (changes component identity, verify prefabs), new shared ScriptableObject field.
     - Risky: renaming serialized fields, moving components, changing existing class hierarchy.

4. **Produce the report** in this format:

---

## Refactor Audit Report

### Summary
- Files scanned: N
- Candidates found: N
- Safe / Moderate / Risky: N / N / N

### Ranked Candidates

For each candidate:

**[ID]. [Short name]** — Safety: Safe / Moderate / Risky | Impact: High / Medium / Low

- **Pattern**: what the repeated code does.
- **Found in**: list of files and approximate line numbers.
- **Proposed fix**: what to extract (static method, interface, base class, constant, etc.) and where to put it.
- **Risk note**: what could break and how to verify.
- **Estimated effort**: trivial / small / medium.

---

### Phase 2 — Apply (Only on User Approval)

After presenting the report, ask the user:
> "Which candidates would you like me to apply? List IDs or say 'all safe ones'."

For each approved candidate:
1. Read every affected file in full before editing.
2. Make the change. Show the full modified file for any file touched.
3. Do not rename serialized fields.
4. Do not change class hierarchy in a way that breaks existing prefab component references.

After all approved candidates are applied, produce a **Unity Editor Checklist** — a numbered, step-by-step list of every manual action needed in the Unity editor. Be specific: name the exact prefab or scene, the exact component, and the exact field. Use this format for each step:

```
[ ] Prefab: Assets/Prefabs/Foo.prefab
    Component: BarComponent
    Action: Assign [SpriteRenderer "Main"] to the "renderers" array slot 0.

[ ] Scene: Game.unity
    GameObject: Player
    Component: PlayerCarryController
    Action: Re-assign the Building Tilemap field — it will be cleared if the script was replaced.

[ ] All prefabs that had [OldScript] component:
    Action: Verify the component is still present and no fields are missing (pink/missing script = broken).
```

Group the checklist into sections:
- **Prefab re-wiring** (inspector field reassignments)
- **Scene re-wiring** (GameObject inspector assignments in open scenes)
- **Verify no missing scripts** (prefabs/scenes where component identity may have changed)
- **Play-mode smoke tests** (what to test in Play mode to confirm nothing broke)

If a change required no Unity editor action at all, say so explicitly so the user knows they can skip the editor for that item.

## Output Rules
- Phase 1 produces only the report. No code edits.
- Phase 2 edits are applied one candidate at a time, in safety order (Safe first).
- Never produce partial snippets — always full file content for any edited file.
- No comments added to code unless the user asks.
- Flag any change that could affect save compatibility before applying it.
- Always end Phase 2 with the Unity Editor Checklist even if it is empty.
