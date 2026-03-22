# Skill: UI Toolkit Element

## Purpose
Generate project-consistent UXML and USS for Island Odyssey's UI Toolkit UI. All output must match the game's existing pixel-art style, spacing, sizing, anchoring, sprite usage, and current runtime scaling behavior.

Do not invent:
- a different coordinate space
- a different scaling model
- default Unity visual styling
- wrapper patterns not already needed
- inline `style="..."` overrides unless the user explicitly asks for them

---

## When to Use
Use this skill when:
- creating a new panel, menu, overlay, HUD block, tooltip, popup, modal, or framed UI element
- adding a new UI Toolkit component such as a progress bar, slot, label, framed box, warning indicator, or button
- extending an existing `.uss`
- generating a new `.uxml` + `.uss` pair for a UI Toolkit element
- the user says:
  - "make a UI for X"
  - "add a screen for Y"
  - "create a HUD element"
  - "make a UI Toolkit panel"

---

## Required Inputs
The request should specify, or the skill should infer:
- what the element is
- where it lives on screen
- what it contains
- whether it is sprite-framed or color-based
- whether it must visually match an existing element already in the game
- whether it is a new UI Toolkit element or a replacement/migration of an existing uGUI element

---

## Sprite Assets — Complete Reference

### In-game UI sprites — individual PNG files
- Located at `Assets/Resources/UI/Sprites/`
- Referenced in USS via `resource("UI/Sprites/<name>")` — no extension needed
- All sprite names below are confirmed and must be treated as the source of truth
- All of these sprites may be referenced in UI Toolkit
- Do not invent alternate sprite names
- Do not substitute Unity default visuals when one of these sprites is appropriate

| Sprite name | Size | Border L/R/T/B (px) | 9-slice | Purpose |
|---|---|---|---|---|
| `ui_slot_normal` | 20×20 | 0/0/0/0 | no | Inventory / chest slot, default state |
| `ui_slot_highlight` | 20×20 | 0/0/0/0 | no | Inventory slot, hovered |
| `ui_slot_select` | 20×20 | 0/0/0/0 | no | Toolbar slot, selected |
| `ui_slot_building_normal` | 20×20 | 0/0/0/0 | no | Building menu slot, default |
| `ui_slot_building_highlight` | 20×20 | 0/0/0/0 | no | Building menu slot, hovered |
| `ui_background` | 24×24 | 5/5/5/5 | yes | Panel background with fill — stretches to any size |
| `ui_background_nofill` | 24×24 | 5/5/5/5 | yes | Panel outline only, transparent center |
| `ui_craft_slot_normal` | 20×20 | 0/0/0/0 | no | Crafting recipe slot |

### UI Toolkit sprite reference rule
All confirmed sprites from this sheet may be used in UI Toolkit via `background-image: resource(...)`.

Use:
```css
background-image: resource("path/to/sprite");
```

Rules:
- use the actual named sprite generated from the `.aseprite` asset
- do not invent resource names
- do not assume a different sprite sheet
- use slot sprites as fixed-size visual elements
- use framed background sprites for scalable framed containers only when appropriate

### Sprite behavior rules

There are two valid usage categories:

#### 1. Fixed-size sprites
These are valid UI Toolkit sprites, but they are not stretchable:
- `ui_slot_normal`
- `ui_slot_highlight`
- `ui_slot_select`
- `ui_slot_building_normal`
- `ui_slot_building_highlight`
- `ui_craft_slot_normal`

Rules:
- render at `20×20px`
- do not apply Unity slice properties
- do not stretch
- use for slots, selection states, crafting cells, and other fixed UI cells

#### 2. Scalable 9-slice sprites
These are valid both as UI Toolkit sprites and as scalable framed containers:
- `ui_background`
- `ui_background_nofill`

Rules:
- base sprite size is `24×24px`
- border values are `5/5/5/5`
- these are the only confirmed 9-slice sprites in this sheet
- use them for panels, frames, tooltips, boxes, and scalable containers

---

## Single Source of Truth — Authored Layout Space

### Authored UI Space: **540×270**
- All UI Toolkit layout values are authored in a logical **540×270** screen space.
- All USS `px` values are written relative to this **540×270** authored space.
- The game runs in **16:9** at fullscreen resolutions such as:
  - `1920×1080`
  - `2560×1440`
- New UI must be authored to visually match the existing HUD and hotbar at this **540×270** reference size.
- Do not switch to `320×180` or any other layout space unless the user explicitly says a specific UI document uses a different setup.

### Important distinction
There are two separate concepts:

#### 1. Authored layout space
This is the screen coordinate space used to design the UI:
- **540×270**

#### 2. Pixel-art asset sizes
These are the literal component and sprite sizes placed inside that authored layout:
- icon: `16×16`
- slot: `20×20`
- framed panel sprite: `24×24`
- hotbar: `140×30`

These are not contradictory.
A `16×16` icon is placed inside a `540×270` authored layout.

---

## Runtime Scaling — Current Project Truth

### UI Toolkit PanelSettings currently used
UI Toolkit runtime currently uses `PanelSettings` with:

- `m_ScaleMode: 0`
- `m_Scale: 1`
- `m_ReferenceResolution: {x: 540, y: 270}`
- `m_ScreenMatchMode: 0`
- `m_Match: 0.5`

### Interpretation rule
Even though the asset contains `ReferenceResolution`, `ScreenMatchMode`, and `Match`, the current `PanelSettings` uses:

- **Scale Mode = Constant Pixel Size**
- **Scale = 1**

So the current project truth is:

- author UI in **540×270**
- sizes and anchors are designed against that authored space
- runtime visual matching must be judged against the actual in-game result
- do not invent a second scaling model in the generated output
- do not output scripts unless explicitly asked

### Critical rule
This skill generates **UXML and USS only**.
It does **not** generate or assume custom runtime scaling scripts unless the user explicitly asks.

---

## UI Builder Preview Rule

### Game View is the source of truth
UI Builder preview is **not** the final authority for runtime visual size.

UI Builder can be misleading because:
- `Match Game View` does **not** faithfully represent runtime panel scaling behavior
- Builder preview may show authored pixels inside a large preview canvas
- Builder may make correctly sized runtime UI look tiny or stretched

### Therefore
- Final size and visual correctness must be judged in **Game View / Play Mode**
- UI Builder is for:
  - hierarchy editing
  - anchors
  - spacing
  - rough composition
  - checking structure
- Game View is for:
  - final scale judgment
  - HUD matching
  - spacing judgment
  - readability
  - visual comparison against the hotbar and existing HUD

### Builder recommendation
For editing, UI Builder is most useful when:
- canvas size is set to **540×270**
- `Match Game View` is **off**

---

## Existing Visual Scale Reference

These existing in-game elements define the target visual scale and must be treated as correct:

- **Hotbar**: `140×30px`, bottom-center, already visually correct in-game
- HUD text is already visually correct in runtime
- UI is authored around a **540×270** layout reference
- Pixel-art assets are based on **16px-style sprites**

New UI must feel visually consistent with:
- the hotbar
- current HUD text
- current menu scale
- current panel/frame art

If the user asks for a new HUD element and gives no exact dimensions, infer a size that feels proportional to:
- the `140×30` hotbar
- current HUD text
- current in-game spacing

---

## Pixel-Art Design System Constants

These values are fixed unless the user explicitly overrides them.

### Grid and Sizing
- Preferred layout step: **4px**
- Fine spacing allowed where already established: **2px**
- Icon size: **16×16px**
- Slot size: **20×20px**
- Framed panel sprite base size: **24×24px**
- Border width: **2px** or **4px**
- Never exceed authored screen bounds of **540×270**

### Font
The project font is `ThaleahFat_UIToolkit`, loaded via:
```css
-unity-font-definition: resource("Fonts/ThaleahFat_UIToolkit");
```
Apply this on the root overlay element of any new panel or menu so all child labels inherit it automatically. Do not set it per-label.

### Typography
Allowed font sizes only:
- `7px` — tiny tooltip/item labels only when space is tight
- `8px` — standard HUD/body/button label text
- `10px` — section hint/secondary text
- `12px` — panel title
- `16px` — large header

Text rules:
- default text color: `rgba(255,255,255,0.95)`
- disabled/hint text: `rgba(255,255,255,0.65)`
- button labels are centered and bold where appropriate

### Colors
Use these unless the user explicitly requests another treatment:

- panel background fallback: `rgba(0,0,0,0.72)`
- lighter panel background: `rgba(0,0,0,0.60)`
- fullscreen dim overlay: `rgba(0,0,0,0.62)`
- lighter modal overlay: `rgba(0,0,0,0.39)`
- empty slot background: `rgba(0,0,0,0.50)`
- filled slot tint: `rgba(255,255,255,0.08)`
- border default: `rgba(255,255,255,0.12)`
- border focused/selected: `rgba(255,255,255,0.35)`

### Shape Rules
- no border radius anywhere
- hard corners only
- pixel-art means no rounded styling
- use only `px`
- never use `%`, `em`, or `rem`

### Spacing
- standard panel padding: `8px`
- compact padding: `4px`
- slot gap: `2px`
- row gap: `4px` or `8px`
- button gap: `8px`

---

## Existing In-Game UI Inventory

These elements already exist. Do not recreate them in UI Toolkit unless the user explicitly asks to migrate or replace them.

| Element | System | Always visible | Notes |
|---|---|---:|---|
| Toolbar / hotbar (6 slots) | uGUI Canvas | Yes | `140×30`, bottom-center, already visually correct |
| MainInvGroup (inventory + crafting) | uGUI Canvas | No | toggled by I |
| BuildingMenu | UI Toolkit UIDocument | No | migrated — toggled by B, only when hammer held |
| NotificationParent | uGUI Canvas | Runtime spawner | bottom-right |
| InfoUI (hunger, thirst, clock, FPS, day, fill) | uGUI Canvas | Yes | currently plain text |
| MapContainer | uGUI Canvas | No | toggled by M |
| Tooltip | Separate Canvas | No | item name + type |
| AdminConsole | Separate Canvas | No | dev tool |
| PauseMenu | UI Toolkit UIDocument | No | existing |
| MainMenu | UI Toolkit UIDocument | — | existing |
| LoadingOverlay | UI Toolkit UIDocument | — | existing |
| StatBars (hunger, thirst) | UI Toolkit UIDocument | Yes | bottom-left, 80×16px frame |

### Valid new UI candidates
These are valid candidates for new UI if requested:
- stamina display
- hunger/thirst warning visuals
- progress bars
- real options panels
- HUD embellishments that do not duplicate existing uGUI systems unless migration is requested

---

## Layout Anchoring in 540×270 Space

Use these anchor patterns in authored space:

| Position | USS pattern |
|---|---|
| Top-left HUD | `position: absolute; left: 4px; top: 4px;` |
| Top-right HUD | `position: absolute; right: 4px; top: 4px;` |
| Bottom-left HUD | `position: absolute; left: 4px; bottom: 4px;` |
| Bottom-center HUD | `position: absolute; left: 0; right: 0; bottom: 8px; align-items: center;` |
| Bottom-right HUD | `position: absolute; right: 4px; bottom: 4px;` |
| Fullscreen overlay | `position: absolute; left: 0; top: 0; right: 0; bottom: 0;` |
| Centered modal | fullscreen overlay + `align-items: center; justify-content: center;` |

Rules:
- use offsets in multiples of `2px`, preferably `4px`
- keep HUD elements clear of existing occupied zones
- bottom-center is visually reserved by the hotbar
- top-right is visually associated with clock/day UI
- do not place new HUD elements where they collide with existing systems unless explicitly replacing them

---

## Component Construction Rules

### Item Slots
- outer slot is always `20×20`
- inner icon is always `16×16`
- do not resize icon slots
- do not stretch item icons
- slot gap remains `2px`

### Slot Grids
For inventory-style or crafting-style layouts:
- use `flex-direction: row`
- use `flex-wrap: wrap`
- use a `2px` gap
- derive total width from:
  - slot count × `20`
  - gap count × `2`
  - panel padding

### Framed Cells
- framed panel sprite base size is `24×24`
- use this where a slot sits inside a framed/padded container

### Bars and Tracks
For progress bars:
- use widths that match the current HUD scale
- prefer sizes that feel proportional to the `140×30` hotbar
- avoid tiny bars unless the user explicitly asks for a compact HUD
- explicit px widths and heights are preferred
- avoid `%` sizing for fills and tracks
- tracks should usually use explicit width/height
- fills should usually use explicit width/height so C# can resize them later if needed

### Important bar frame rule
`ui_background_nofill` can be used for framed bars, but it is fundamentally a framed panel sprite. It works for the current stat bars, but it is not a dedicated horizontal bar sprite.

Therefore:
- it is acceptable to use `ui_background_nofill` for stat bars if the user wants to match the existing current result
- do not claim it is a perfect purpose-built bar sprite
- if the user asks for the cleanest final art solution for long horizontal bars, prefer a dedicated bar frame sprite if one exists

### Floating Panel Title
Use this pattern when a panel needs a title label that floats above its top border (like the BuildingMenu title).

Structure:
```xml
<ui:VisualElement class="panel-wrapper">   <!-- position: relative; overflow: visible -->
    <ui:VisualElement class="panel">       <!-- the actual panel background -->
        ...
    </ui:VisualElement>
    <ui:Label text="Title" class="panel-title"/>  <!-- sibling, positioned absolute -->
</ui:VisualElement>
```

USS:
```css
.panel-wrapper {
    position: relative;
    overflow: visible;
}

.panel-title {
    position: absolute;
    left: 7px;
    top: -10px;
    height: 14px;
    flex-shrink: 0;
    margin: 0;
    padding-left: 16px;
    padding-right: 16px;
    padding-top: 0;
    padding-bottom: 0;
    background-image: resource("UI/Sprites/ui_background");
    background-repeat: no-repeat;
    background-position: center center;
    -unity-slice-left: 5;
    -unity-slice-right: 5;
    -unity-slice-top: 5;
    -unity-slice-bottom: 5;
    -unity-slice-scale: 1px;
    font-size: 8px;
    color: rgba(255, 255, 255, 1);
    -unity-text-align: middle-center;
}
```

Rules:
- The wrapper must be `position: relative; overflow: visible` so the absolute title can escape the panel bounds upward
- `top: -10px` places the title straddling the top border of the panel
- `height: 14px` keeps the title compact; adjust if the title text is longer
- Use `ui_background` (filled) not `ui_background_nofill` for the title pill
- The title sits outside the panel's padding, so panel content padding does not need to account for it
- `left: 7px` aligns the title slightly inset from the panel's left edge

### Buttons
Do not use Unity default button visuals.

If the user wants a pixel-art button, use the 3-piece pattern:
- left cap
- tiled middle
- right cap

---

## USS Rules

- use only `px`
- never use `%`, `em`, or `rem`
- never use `border-radius`
- fixed-size elements must use `flex-shrink: 0`
- position overlays with:
  - `position: absolute; left: 0; top: 0; right: 0; bottom: 0;`
- strip Unity default button styling if using `<ui:Button>`:
  - `border-width: 0;`
  - `padding: 0;`
  - `margin: 0;`
  - `background-color: rgba(0,0,0,0);`

### Sprite-Based Styling
Use sprite-based backgrounds when art exists.

Example rules:
- `background-image: resource("path/to/sprite")`
- `background-repeat: no-repeat`
- `background-position: center center`

For icons:
- `background-size: 16px 16px`

For horizontally tiled button middles:
- `background-repeat: repeat-x`
- `background-size: 16px 16px`

### 9-Slice Rule
Only apply slice properties to sprites that are confirmed 9-slice.

For `ui_background` and `ui_background_nofill`, use exactly:
```css
-unity-slice-left: 5;
-unity-slice-right: 5;
-unity-slice-top: 5;
-unity-slice-bottom: 5;
-unity-slice-scale: 1px;
```

#### Slice type — stretch vs tiled center
`-unity-slice-type` controls whether the center (and edges) scale or tile:

- `-unity-slice-type: sliced` — default. Center and edges **stretch** to fill the panel.
- `-unity-slice-type: tiled` — center and edges **repeat/tile** instead of stretching.

Use `sliced` by default. Use `tiled` when the design requires the center pattern to repeat rather than scale — for example, a panel background where the fill texture should tile like uGUI Image Type = Tiled.

Example for a tiled-center panel:
```css
background-image: resource("UI/Sprites/ui_background");
-unity-slice-left: 5;
-unity-slice-right: 5;
-unity-slice-top: 5;
-unity-slice-bottom: 5;
-unity-slice-scale: 1px;
-unity-slice-type: tiled;
```

Do not apply slice properties to:
- `ui_slot_normal`
- `ui_slot_highlight`
- `ui_slot_select`
- `ui_slot_building_normal`
- `ui_slot_building_highlight`
- `ui_craft_slot_normal`

These are fixed-size sprites and must render at authored size without stretching.

---

## Output Expectations

When generating UI Toolkit output:
- produce clean, project-consistent UXML and USS
- keep sizes aligned to the current authored space and pixel-art scale
- use confirmed sprite names only
- do not invent new UI art assets unless the user explicitly asks for placeholders
- do not assume hidden runtime scripts
- do not migrate existing uGUI systems unless explicitly requested
- prefer explicit dimensions over ambiguous flexible sizing where pixel precision matters

If the user asks for a complete UI element, generate:
- one `.uxml`
- one `.uss`

Unless the request clearly only needs one of them.

---

## Default Behavior Summary

Unless the user explicitly overrides something:
- author in **540×270**
- match the current in-game hotbar/HUD scale
- use `20×20` slots and `16×16` icons
- use `ui_background` / `ui_background_nofill` for scalable framed containers
- use all other confirmed sprites as fixed-size UI Toolkit sprites where appropriate
- use only `px`
- avoid Unity default styling
- keep layouts compact, clean, readable, and pixel-art consistent
