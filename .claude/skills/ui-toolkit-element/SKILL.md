# Skill: UI Toolkit Element

## Purpose
Generate pixel-perfect UXML and USS for Island Survivor's UI Toolkit setup. All output must conform to the established pixel grid and design language without introducing new patterns or scaling assumptions.

## When to Use
- Creating a new panel, menu, or overlay.
- Adding a new UI component (slot, button, label, progress bar, tooltip, etc.).
- Extending an existing `.uss` with new classes.
- When the user says "make a UI for X" or "add a screen for Y".

## Required Inputs
- What the element is (e.g. "crafting panel", "stat bar", "tooltip").
- Where it lives on screen (anchor: top-left, bottom-center, fullscreen overlay, etc.).
- What it contains (slots, labels, buttons, icons, progress bars, etc.).
- Whether it needs sprite-based styling or solid-color styling.

---

## UI Resolution — Single Source of Truth

### UI Toolkit Logical Space: **320×180px**
- `PixelPerfectPanelScaler.cs` sets `panelSettings.scale = min(floor(screenW/320), floor(screenH/180))` at runtime.
- This locks the logical coordinate space to exactly **320×180** on any standard 16:9 screen (1280×720 → scale=4, 1920×1080 → scale=6, 2560×1440 → scale=8).
- All UXML/USS px values are in **320×180 space**. A 60px bar = 60 logical pixels = 60×scale physical pixels.
- `PanelSettings` has `m_ScaleMode: 0` (ConstantPixelSize). The `referenceResolution` field in PanelSettings is irrelevant — the scaler overrides scale directly.
- There is no `.screen` or `.viewport` wrapper pattern. Use `position: absolute` on HUD elements directly.

### UI Builder Preview
- UI Builder shows the wrong size by default (PanelSettings reference 1200×800). Always enable **Match Game View** in the UI Builder canvas toolbar to preview at actual logical resolution.
- Do not trust UI Builder dimensions when Match Game View is off.

### uGUI Canvas
- The in-game uGUI Canvas uses its own CanvasScaler — its reference resolution is separate from UI Toolkit and has not been confirmed. Do not assume uGUI and UI Toolkit share the same px coordinate space.

---

## Design System Constants

These values are fixed. Never deviate from them unless the user explicitly overrides.

### Pixel Grid (UI space = 320×180)
- Base unit: **4px** — all sizes, padding, margins, and gaps must be multiples of 4.
- Icon size: **16×16px** (matches PPU=16 sprite assets).
- Slot size: **20×20px** (2px padding on each side around a 16×16 icon).
- Panel cell size: **24×24px** (4px border on each side around a 20×20 slot).
- Border width for panel frames: **2px** (inner pixel line) or **4px** (thick frame).
- Screen limit: **320px wide, 180px tall** — no element may exceed this.

### UI Toolkit Confirmed Sizes (320×180 space)
These are confirmed measurements for UI Toolkit elements only. uGUI measurements are in a different coordinate space and are not listed here.

| Element | Size | Anchor / Position |
|---|---|---|
| PauseMenu panel | 220px wide | centered modal |
| StatBars hunger frame | 68×28 | bottom-left, left:4 bottom:4 |
| StatBars thirst frame | 68×28 | bottom-left, below hunger + 8px gap |
| LoadingOverlay panel | 220×70 | center |

### Typography
- Button label: `font-size: 8px`, bold, centered.
- Body / item count / HUD text: `font-size: 8px`.
- Section hint / secondary: `font-size: 10px`.
- Panel title: `font-size: 12px` or `16px` for large headers.
- All text: `color: rgba(255,255,255,0.95)` default; `rgba(255,255,255,0.65)` for disabled/hint.
- Font size 7px is used in the scene for very small labels (item names in tooltips) — use only where space is tight.

### Colors
- Panel background: `rgba(0,0,0,0.72)` standard; `rgba(0,0,0,0.60)` lighter.
- Full-screen dim/overlay: `rgba(0,0,0,0.62)` (matches AdminConsole), `rgba(0,0,0,0.39)` (matches PauseMenu).
- Slot background (empty): `rgba(0,0,0,0.50)`.
- Slot background (filled): `rgba(255,255,255,0.08)`.
- Border (default): `rgba(255,255,255,0.12)`.
- Border (focused/selected): `rgba(255,255,255,0.35)`.
- No border-radius anywhere. Pixel art means hard corners only (`border-radius: 0`).

### Spacing
- Inner panel padding: `8px` on all sides.
- Gap between slots in a grid: `2px` (matches GridLayoutGroup spacing in scene).
- Gap between stacked rows: `4px` or `8px`.
- Margin between buttons: `8px`.

---

## Existing In-Game UI Inventory (uGUI — do not duplicate)

These elements already exist on the uGUI Canvas. Do not recreate them in UI Toolkit unless explicitly asked to migrate.

| Element | System | Always visible | Notes |
|---|---|---|---|
| Toolbar (6 slots) | uGUI Canvas | Yes | 140×30, bottom-center, y=16 |
| MainInvGroup (inventory + crafting) | uGUI Canvas | No | toggled by I key |
| BuildingMenu | uGUI Canvas | No | toggled by B key, only when hammer held |
| NotificationParent | uGUI Canvas | Yes (spawner) | bottom-right, notifications spawned at runtime |
| InfoUI (hunger, thirst, clock, FPS, day, fill) | uGUI Canvas | Yes | 6 plain TMP_Text labels, no bars |
| MapContainer | uGUI Canvas | No | toggled by M key |
| Tooltip | **Separate Canvas** (TooltipCanvas) | No | item name + type, ContentSizeFitter |
| AdminConsole | **Separate Canvas** (AdminConsoleCanvas) | No | item spawner dev tool |
| PauseMenu | UI Toolkit UIDocument | No | UXML/USS at Assets/UI/PauseMenu/ |
| MainMenu | UI Toolkit UIDocument | — | UXML/USS at Assets/UI/MainMenu/ |
| LoadingOverlay | UI Toolkit UIDocument | — | UXML/USS at Assets/UI/MainMenu/ |

**Gaps with no existing UI:**
- No stamina display exists in the scene despite `PlayerStats` tracking it.
- No critical-warning visuals for hunger/thirst at <15%.
- No progress bars anywhere — all stats are plain text.
- Options panels in both MainMenu and PauseMenu are placeholders.

---

## USS Rules

- Use `px` for all values. Never use `%`, `em`, or `rem`.
- Never use `border-radius` (always 0 or omitted).
- Strip Unity defaults from buttons: `border-width: 0; padding: 0; margin: 0; background-color: rgba(0,0,0,0)`.
- Use `background-image: resource("path/to/sprite")` for sprite-based elements.
  - `background-size: 16px 16px` for icon slots.
  - `background-repeat: no-repeat; background-position: center center` for single icons.
- For horizontally tiling (stretchable) button bodies: `background-repeat: repeat-x; background-size: 16px 16px`.
- Always set `flex-shrink: 0` on fixed-size elements to prevent flex squishing.
- Position overlays with `position: absolute; left: 0; top: 0; right: 0; bottom: 0`.
- Pixel-perfect buttons use the 3-piece pattern (capLeft / mid / capRight) matching the MainMenu `.pxBtn` pattern.
- USS class names: `camelCase` matching project convention.

---

## UXML Rules

- Root element is always `<ui:UXML>` with the standard Unity namespaces (copy header from existing files).
- Use `name="PascalCase"` for elements that will be referenced from C#.
- Use `class="camelCase"` for USS-only styling.
- Prefer `<ui:VisualElement>` for containers, `<ui:Label>` for text, `<ui:Button>` for interactable buttons.
- Do not use `<ui:TextField>`, `<ui:Toggle>`, `<ui:Slider>` or other Unity default controls unless the user asks — they carry default styling that conflicts with the pixel art look.
- For pixel-art buttons, use the 3-child pattern (no default Button element):
  ```xml
  <ui:VisualElement class="pxBtn" name="MyBtn">
      <ui:VisualElement class="capLeft" />
      <ui:VisualElement class="mid">
          <ui:Label class="pxBtnLabel" text="Label" />
      </ui:VisualElement>
      <ui:VisualElement class="capRight" />
  </ui:VisualElement>
  ```
- For item slots: a 20×20 outer container with an inner 16×16 icon element.
- For inventory grids: use `flex-wrap: wrap` with `flex-direction: row` and `2px` gap on the container.

---

## Screen Layout Anchoring (UI Toolkit — 540×270 space)

The `.screen` div is always **540×270px**, centered in the viewport via `.viewport`. All game UI lives inside it.

| Position | USS pattern |
|---|---|
| Top-left HUD | `position: absolute; left: 4px; top: 4px` |
| Top-right HUD | `position: absolute; right: 4px; top: 4px` |
| Bottom-center (e.g. hotbar) | `position: absolute; bottom: 8px; left: 0; right: 0; align-items: center` |
| Bottom-right (e.g. notifications) | `position: absolute; bottom: 60px; right: 40px` |
| Centered modal | `position: absolute; left: 0; right: 0; top: 0; bottom: 0; align-items: center; justify-content: center` |

All offset values must be multiples of 4px.

### Reserved Screen Zones in 320×180 space
uGUI element exact sizes in UI Toolkit px are unconfirmed — treat these as approximate zones.
- **Bottom-center**: toolbar zone (existing uGUI)
- **Bottom-left, y=0–68px**: stat bars zone — StatBars UIDocument occupies this (UI Toolkit)
- **Top-right**: day count zone (existing uGUI)

---

## Sprite Assets — Complete Reference

### `ui_elements.aseprite` — main in-game UI sprite sheet
**guid:** `5a46f34410e28fb49a501adb5519e8bb`
Canvas: 168×24px. PPU=16, point-filter. All sprites are confirmed named sub-sprites.

| Sprite name | Size | Border L/R/T/B (px) | 9-slice | Purpose |
|---|---|---|---|---|
| `ui_slot_normal` | 20×20 | 0/0/0/0 | no | Inventory / chest slot, default state |
| `ui_slot_highlight` | 20×20 | 0/0/0/0 | no | Inventory slot, hovered |
| `ui_slot_select` | 20×20 | 0/0/0/0 | no | Toolbar slot, selected |
| `ui_slot_building_normal` | 20×20 | 0/0/0/0 | no | Building menu slot, default |
| `ui_slot_building_highlight` | 20×20 | 0/0/0/0 | no | Building menu slot, hovered |
| `ui_background` | 24×24 | 5/5/5/5 | **yes** | Panel background with fill — stretches to any size |
| `ui_background_nofill` | 24×24 | 5/5/5/5 | **yes** | Panel outline only, transparent center |
| `ui_craft_slot_normal` | 20×20 | 0/0/0/0 | no | Crafting recipe slot |

**For UI Toolkit:** reference these as `background-image: resource("path")` using the sprite's asset path. Since these come from an `.aseprite` file, Unity generates sub-assets — reference by the named sprite path in the project.

**9-slice USS rule:** Sprites with border values must include Unity's slice properties in USS so they stretch correctly. For `ui_background` and `ui_background_nofill`:
```css
-unity-slice-left: 5;
-unity-slice-right: 5;
-unity-slice-top: 5;
-unity-slice-bottom: 5;
-unity-slice-scale: 1px;
```
Sprites with border 0/0/0/0 (all slot sprites) must **not** include slice properties — they render as fixed 20×20 images and must never be stretched.

### MainMenu button sprites — 3-piece pixel button
Located at `Assets/Resources/UI/MainMenu/`. Each is 16×16px, PPU=16, point-filter, **no** 9-slice.
Referenced in USS as `resource("UI/MainMenu/btn_left_idle")` etc.

| Set | Files |
|---|---|
| Left cap | `btn_left_idle/hover/pressed/disabled` |
| Middle (tiles horizontally) | `btn_mid_idle/hover/pressed/disabled` |
| Right cap | `btn_right_idle/hover/pressed/disabled` |

### Other referenced sprites
| File | Content | Used in UI |
|---|---|---|
| `Assets/Sprites/Items.png` | 16×16 item icons, grid sheet | Inventory slot icons, crafting icons |
| `Assets/Sprites/tools.png` | Tool icons | Hotbar tool display |
| `Assets/Sprites/crafting_categories.png` | Small category icons | CraftingSlot category display |
| `Assets/Sprites/map_player_marker.png` | Player arrow | Minimap player indicator |

### Panel background usage rule
- Use `ui_background` (filled) for panels with a solid interior (toolbar, inventory background, building menu).
- Use `ui_background_nofill` (outline only) for floating panels or tooltips where the game world should show through.
- Never use solid `background-color` as a substitute for a sprite panel — use the sprite and tint it if needed.

---

## Process

1. **Identify the system** — is this UI Toolkit (new panel/menu) or does it replace something on the uGUI Canvas?
2. **Check the reserved zones** — does the new element conflict with existing uGUI HUD positions?
3. **Clarify layout** — confirm anchor position, element list, and whether sprite assets exist.
4. **Compute sizes** — derive all widths/heights from the base grid (slots × 20 + gaps × 2 + padding × 2).
5. **Write USS first** — define all classes before writing UXML.
6. **Write UXML** — structure matches the visual hierarchy. Name only elements that C# needs to query.
7. **Flag sprite dependencies** — list any `resource("...")` paths that need art assets to exist.
8. **Flag C# wiring** — list element `name` attributes the programmer needs to hook up, and what event or query each needs.

---

## Output Format

Always produce:
- Full `.uss` file content (or the new classes to append to an existing one).
- Full `.uxml` file content.
- A short **Wiring Notes** section listing:
  - Which named elements need C# `Query<>()` calls.
  - Which buttons need `RegisterCallback<ClickEvent>`.
  - Any sprite assets that must exist at the `resource()` paths used.
  - Whether `MenuCoordinator` registration is needed.

---

## Constraints
- UI Toolkit screen is always 540×270px. Never exceed this.
- Never use Unity's default Button visual — always strip it or use the 3-piece pattern.
- Never use `font-size` values outside: 7, 8, 10, 12, 16px.
- Never add border-radius.
- Never use non-px units.
- Item icon slots are always 20×20 with a 16×16 inner icon — do not resize them.
- Slot gaps are always 2px (matching the GridLayoutGroup spacing in the existing uGUI canvas).
- Do not create C# files in this skill — output is UXML and USS only.
- Do not recreate uGUI elements (Toolbar, Inventory, InfoUI, etc.) in UI Toolkit unless the user explicitly asks to migrate them.
