# ZZZ-Style Dash Button Setup Guide

> Implements a **Zenless Zone Zero**-style circular dash button with radial cooldown sweep.

---

## What was changed in code

| File | Change |
|------|--------|
| `Scripts/Player/PlayerController.cs` | Added 3 public properties: `DashCooldownProgress`, `IsDashReady`, `DashCooldownDuration` |
| `Scripts/UI/DashButtonUI.cs` | **New** – drives the radial overlay, tints, and ready-pulse |

---

## Step 1 – Prepare the circle sprite

Unity's built-in **UI/Knob** sprite (`Assets/UI Default/Knob`) is a perfect circle.  
Alternatively use any circular sprite you already have.

---

## Step 2 – Rebuild the Dash button hierarchy

Inside your **MobileUI Canvas** find the old Dash Button and **replace** it with this structure
(or rename / reparent the existing objects):

```
DashButton          ← RectTransform • Image (circle bg) • OnScreenButton • DashButtonUI
├── ReadyRing       ← Image  (circle, white, alpha 0)   ← FIRST child = renders BEHIND
├── Background      ← Image  (circle sprite, light grey #C8C8C8)
├── CooldownMask    ← Image  (circle sprite, black, Filled/Radial360)
└── Icon            ← Image  (up-arrow.png)
```

> **Why this order?** `ReadyRing` is a filled white circle drawn *behind* `Background`.
> `Background` covers its centre, so only the ring pixels that expand *outside* the button
> boundary are ever visible — a pure outline ring with no extra sprites needed.

### DashButton root object
| Component | Settings |
|-----------|----------|
| `RectTransform` | Width/Height **120 × 120** (adjust to taste) |
| `Image` | Sprite = circle knob, Color = light grey `(200,200,200,255)`, Raycast Target = ✔ |
| `OnScreenButton` | Control Path = `<Gamepad>/buttonEast` (or whatever dash binding you use) |
| `DashButtonUI` | Wire all fields (see Step 3) |

### Background child
| Field | Value |
|-------|-------|
| Sprite | built-in `Knob` (or any filled circle) |
| Color | `#C8C8C8 FF` (light grey) |
| Raycast Target | ✘ |
| Rect size | **120 × 120** (same as parent) |

### Icon child
| Field | Value |
|-------|-------|
| Sprite | `up-arrow` (Assets/Sprites/Ui/up-arrow.png) |
| Color | White `#FFFFFF FF` |
| Rect size | **70 × 70** |
| Raycast Target | ✘ |

### CooldownMask child (THE SWEEP OVERLAY)
| Field | Value |
|-------|-------|
| Sprite | Same circle sprite as Background |
| Image Type | **Filled** |
| Fill Method | **Radial 360** |
| Fill Origin | **Top** |
| Clockwise | ✔ |
| Color | `#000000` alpha **180** (~70%) |
| Fill Amount | **0** (starts empty — script controls it) |
| Raycast Target | ✘ |
| Rect size | **120 × 120** |

> **How it works:** fill amount = **1** the instant the dash is used (full dark circle),
> sweeps *away* clockwise down to **0** as the cooldown expires — just like ZZZ's dodge ring.

### ReadyRing child  (⚠️ must be the **first** child — lowest in draw order)
| Field | Value |
|-------|-------|
| Sprite | Same circle sprite as Background |
| Color | White, alpha **0** (transparent at rest) |
| Raycast Target | ✘ |
| Rect size | **120 × 120** (same as parent — script enforces this at runtime) |

> The `Background` sibling drawn on top hides the ring’s filled centre.
> Only the border that grows **beyond 120×120** is ever visible — a clean outline ring.

---

## Step 3 – Wire DashButtonUI in the Inspector

On the **DashButton** root, select `DashButtonUI` and fill in:

| Field | Drag in |
|-------|----------|
| `Player Controller` | The **Player** GameObject (has `PlayerController`) |
| `Background Image` | `DashButton/Background` |
| `Icon Image` | `DashButton/Icon` |
| `Cooldown Mask Image` | `DashButton/CooldownMask` |
| `Ready Flash Image` | `DashButton/ReadyRing` |

Leave the colour/pulse fields at their defaults or tune them.

---

## Step 4 – Remove / disable legacy button overhead

If the old button was a `Button` (`UnityEngine.UI.Button`) component, remove it —  
`OnScreenButton` from Input System is enough and the `DashButtonUI` script handles all visual feedback.

---

## Visual reference

```
         ┌──────────────────┐
         │                  │
         │   ░░░░░░░░░██    │  ← dark sweep shrinks clockwise
         │   ░░░░  ↑  ██    │  ← icon (up-arrow) always visible
         │   ░░░░░░░░░██    │
         │                  │
         └──────────────────┘
  [cooldown]           [ready → pulse ✨]
```

---

## Tweakable parameters on `DashButtonUI`

| Parameter | Default | Effect |
|-----------|---------|--------|
| `Bg Ready Color` | `#C8C8C8` | Button colour when dash is available |
| `Bg Cooldown Color` | `#737373` | Button colour at full cooldown |
| `Overlay Color` | `#000000 B8` | Darkness of the sweep |
| `Ring Expand Multiplier` | 1.4 | How far the outline ring expands (1.4 = 40% past button edge) |
| `Pulse Duration` | 0.35 s | Speed of the outline ring ripple |
