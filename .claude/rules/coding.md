# Coding Rules

## Output
- Always produce full, compilable C# code for any file being modified.
- Never produce pseudo-code, partial snippets, or placeholder logic unless the user explicitly asks for a sketch.
- When a change touches multiple methods in a file, show the whole file.

## Comments
- Never add comments to generated code unless explicitly asked.
- Do not add XML doc comments, inline explanations, or TODO comments unless requested.

## Naming and Structure
- Match the existing naming convention exactly — field names, method names, class names, file names.
- Do not rename anything unless the user asks, even if a different name would be cleaner.
- Do not reorganize field order, method order, or `using` blocks unless asked.
- Do not extract helpers or utilities for one-off operations.

## Unity-Specific
- Respect Unity serialization. Do not make serialized fields private without `[SerializeField]`. Do not break inspector references.
- Inspector-assigned references (`[SerializeField]` or `public`) must not be silently replaced with `FindAnyObjectByType` in new code. `SaveGameManager` uses `FindAnyObjectByType` as an `Awake` fallback for legacy reasons — do not expand this pattern to new systems.
- `GetComponent` calls in hot paths should be cached in `Awake` or `Initialize`, not called per-frame.
- Avoid allocations in `Update`, `FixedUpdate`, or any per-frame path — no LINQ, no `new List<>()`, no string concatenation.
- Use `CompareTag` instead of `tag ==` for tag checks.

## Architecture
- Do not introduce new singletons or manager classes unless the user requests one. The existing codebase already has singletons (`GameManager`, `GameInput`, `MenuCoordinator`, `AudioManager`) — work with those rather than adding more.
- Prefer passing dependencies via `Initialize()` methods or inspector references over static access.
- Prefer `ScriptableObject` for data that designers should configure.
- Keep MonoBehaviours focused. Do not merge unrelated responsibilities into one class.
- Avoid fragile coupling — prefer events or interfaces over direct cross-system calls where the systems are unrelated.

## Performance
- Flat array indexing (`[y * width + x]`) is preferred over 2D arrays in hot generation paths.
- Avoid repeated dictionary lookups in loops — cache the result.
- Background thread work in generation must not touch Unity API. Only the main thread may call Unity APIs.

## Safety
- Do not use `object.name` as a stable ID in any system that persists to disk or cross-frame state.
- Do not use magic strings for tags, layer names, or resource paths — unless they already exist in the codebase as-is.
