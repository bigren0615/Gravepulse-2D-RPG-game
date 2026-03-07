<div align="center">

# ⚔️ Gravepulse (WORK IN PROGRESS)

### *2D Top-Down Real-Time Action RPG*

![Unity](https://img.shields.io/badge/Unity-6-grey?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-In%20Development-orange)
![Progress](https://img.shields.io/badge/Overall%20Progress-85%25-blue)

> **Goal:** Real-time physics-driven top-down action RPG with fluid combat, dodge mechanics, and responsive input.

</div>

---

<a id="play-online"></a>

## 🌐 Play Online

> **Play the latest build directly in your browser — no download required!**

🎮 **[Click here to play online](https://bigren0615.github.io/Gravepulse-2D-RPG-game/)**

---

<a id="controls"></a>

## 🕹️ Controls

| Action | Key / Input |
|---|---|
| Move Up | `W` / `↑` |
| Move Down | `S` / `↓` |
| Move Left | `A` / `←` |
| Move Right | `D` / `→` |
| Attack | `Left Mouse Button` / `Z` |
| Dash / Dodge | `Right Mouse Button` / `Shift` |
| Parry Attack | `Space` |

---

## 📖 Menu

> **Quick navigation — click any item to jump to that section.**

- [🌐 Play Online](#play-online)
- [🕹️ Controls](#controls)
- [🎯 Design Pillars](#design-pillars)
- [📊 Overall Progress](#overall-progress)
- [📋 Update Log](#update-log)
- [🗺️ Development Phases](#development-phases)
- [⚔️ Core Feature Status](#core-feature-status)
- [🗓️ Upcoming Phases](#upcoming-phases)
- [📁 Project Structure](#project-structure)
- [🎨 Credits & Assets](#credits--assets)

---

<a id="design-pillars"></a>

## 🎯 Design Pillars

| ❌ What We Are NOT Building | ✅ What We ARE Building |
|---|---|
| Pokémon-style turn-based RPG | Real-time input combat |
| Grid / coroutine movement | Physics-driven movement |
| Static isMoving locks | Combat that interrupts movement |
| Flat basic attacks | Dash / knockback / hitstop system |

---

<a id="overall-progress"></a>

## 📊 Overall Progress

```
Total Completion  █████████████████░░░  85%
```

---

<a id="update-log"></a>

## 📋 Update Log

<details open>
<summary>🆕 v0.2.0-Alpha — Parry System Update - 7 March 2026</summary>

- Added parry mechanic (Space) — succeeds only during enemy yellow warning window
- Enemy attacks now show a warning indicator before striking (yellow = parryable, red = dodge only)
- Parry triggers hitstop + ZZZ-style camera punch zoom via `GameManager.TriggerHitstop()`
- Parry spark VFX effect (`ParrySparkEffect.cs`) with streaking sparks, ring flash, and glint stars
- Parry animations added for player (Down / Left / Up) and goblin parried state (Down / Left / Up)
- Enemy enters post-parry stagger after a successful parry
- Added new SFX: Clash (sword-clash), Slice (attack warning), Swoosh (dramatic whoosh)

</details>

<details>
<summary>v0.1.0-Alpha — Initial Release - 6 March 2026</summary>

- Added player character (Skull Knight) with walk, attack, and death animations
- Physics-driven movement using Rigidbody2D
- Top-down environment with floor and nature tilesets
- Basic melee attack system with hitbox detection
- Dash mechanic with cooldown
- Goblin enemy with patrol, chase, and attack AI
- Emotion bubble system (! alert, ? suspense)
- Ambient and battle background music
- Footstep, attack, and dash sound effects
- Sorting layers configured for proper depth rendering
- WebGL build deployed to GitHub Pages

</details>

---

<a id="development-phases"></a>

## 🗺️ Development Phases

### 🧱 Phase 0 — Mindset & Architecture
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] Real-time input model locked in
- [x] Physics-driven movement chosen over grid-based
- [x] Combat-interruptible movement design confirmed
- [x] Dash / knockback / hitstop planned for later phases

---

### 🛠️ Phase 1 — Project & Scene Setup
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] Project created (2D Built-in Render Pipeline)
- [x] `MainScene.unity` created and saved
- [x] Main Camera set to Orthographic, position `(0, 0, -10)`
- [x] Camera size configured for top-down clarity

---

### 🗂️ Phase 2 — Scene Hierarchy
```
███████████████░░░░░░░   65%  🔄 IN PROGRESS
```
- [x] `MainScene` exists
- [x] `Environment` node with Tilemap assets
- [x] `Player` GameObject present
- [ ] `Enemies` node created
- [ ] `NPCs` node created
- [ ] `BattleZones` node created
- [ ] `UI` node created

<details>
<summary>📌 Target Hierarchy</summary>

```
MainScene
├── Environment
│   ├── Tilemap_Floor     ✅
│   ├── Tilemap_Walls     ✅
│   └── Props             ⏳
├── Player                ✅
├── Enemies               ❌
├── NPCs                  ❌
├── BattleZones           ❌
└── UI                    ❌
```
</details>

---

### 🎨 Phase 3 — Sorting Layers
```
████████░░░░░░░░░░░░░░   35%  🔄 IN PROGRESS
```
- [x] Floor layer
- [ ] Characters layer
- [ ] Props layer
- [ ] Effects layer
- [ ] UI layer

---

### 🧱 Phase 4 — Physics Layers & Collision Matrix
```
████████░░░░░░░░░░░░░░   35%  🔄 IN PROGRESS
```
- [x] Player layer defined
- [ ] Enemy layer defined
- [ ] Solid layer defined
- [ ] Interactable layer defined
- [ ] BattleZone layer defined
- [ ] Collision matrix configured

<details>
<summary>📌 Target Collision Matrix</summary>

| Layer | Player | Enemy | Solid | Interactable | BattleZone |
|---|:---:|:---:|:---:|:---:|:---:|
| **Player** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Enemy** | ✅ | ❌ | ✅ | ❌ | ❌ |
| **Solid** | ✅ | ✅ | ❌ | ❌ | ❌ |
</details>

---

### 🌍 Phase 5 — Tilemap & Environment
```
██████████████████░░░░   80%  🔄 IN PROGRESS
```
- [x] Floor Tilemap with 458 tiles (TilesetFloor)
- [x] Nature Tilemap with 382 tiles (TilesetNature)
- [x] Floor Tilemap (no collider, Sorting Layer: Floor)
- [ ] Walls Tilemap with `TilemapCollider2D` + `CompositeCollider2D`
- [ ] Rigidbody2D (Static) on Walls
- [ ] Props with individual `BoxCollider2D` / `CircleCollider2D`

---

### 🧍 Phase 6 — Player Setup
```
█████████████████████░   92%  🔄 IN PROGRESS
```
- [x] `Player` GameObject named and in scene
- [x] `SpriteRenderer` attached (skull knight sprites)
- [x] `Animator` attached with `PlayerAnimator.controller`
- [x] `Rigidbody2D` — Dynamic, Gravity Scale: 0, Freeze Rotation Z ✅, Interpolate ✅
- [x] `Collider2D` attached
- [ ] Collider adjusted to feet (smaller than sprite to avoid wall snagging)

---

### 🏃 Phase 7 — Real-Time Movement & Attack Input
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] `PlayerController.cs` implemented
- [x] `Input.GetAxisRaw` for instant response
- [x] `rb.MovePosition` physics-based movement (no coroutines!)
- [x] Diagonal normalization (no speed boost)
- [x] Animation sync via blend trees (`isMoving`, `moveX`, `moveY` params)
- [x] Sprite flip (`flipX`) for left ↔ right mirroring
- [x] Dash / dodge system (Left Shift or Right-Click, with VFX + SFX)
- [x] Attack input (Left Click or Z key)
- [x] Face-lock during attack animation
- [x] Movement cancellation during attack (facing locked to attack direction)
- [x] Parry input (Space) — succeeds only during enemy yellow warning window
- [x] Face-lock during parry animation

<details>
<summary>📌 PlayerController.cs (key structure)</summary>

```csharp
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public SpriteRenderer spriteRenderer;

    [Header("Dash")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;
    public GameObject dashEffectPrefab;

    [Header("Attack")]
    public float attackCooldown = 0.25f;
    public float attackRange = 1.2f;
    public float attackAngle = 90f;
    public float attackDamage = 10f;
    public LayerMask enemyLayer;

    [Header("Footsteps")]
    public float footstepInterval = 0.8f;

    // Type-safe enum arrays for SFX
    private SFXType[] grassFootsteps = { SFXType.FootstepGrass1, SFXType.FootstepGrass2, SFXType.FootstepGrass3 };
    private SFXType[] attackSwooshs  = { SFXType.AttackSwoosh1,  SFXType.AttackSwoosh2,  SFXType.AttackSwoosh3  };
    private SFXType[] attackSlashes  = { SFXType.Slash1,          SFXType.Slash2,          SFXType.Slash3          };

    private void Update()
    {
        // Dash input: Left Shift or Right-Click
        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1))
                StartCoroutine(Dash());

        // Attack input: Left Click or Z
        if (Time.time >= lastAttackTime + attackCooldown)
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z))
                Attack();

        ReadInput();
        UpdateAnimation();
    }

    // Called by animation event — locks facing and clears hit set
    public void AttackStart() { facingLocked = true; attackDir = lastMoveDir; hitEnemiesThisAttack.Clear(); }
    public void AttackEnd()   { facingLocked = false; }

    // Called by animation event at moment of impact
    public void AttackHit()   { DetectAndDamageEnemies(); }

    private void DetectAndDamageEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        bool hitAny = false;
        foreach (var enemy in hits)
        {
            if (hitEnemiesThisAttack.Contains(enemy)) continue;
            float angle = Vector2.Angle(attackDir, (enemy.transform.position - transform.position).normalized);
            if (angle <= attackAngle / 2f)
            {
                enemy.GetComponent<EnemyPatrol>()?.TakeDamage(attackDamage);
                hitEnemiesThisAttack.Add(enemy);
                hitAny = true;
            }
        }
        if (hitAny) AudioManager.Instance.PlayRandomSFX(attackSlashes);
    }
}
```
</details>

---

### ⚔️ Phase 8 — Combat System
```
████████████████████░░   80%  🔄 IN PROGRESS
```
- [x] Attack input (Left Click / Z key)
- [x] Arc-based hitbox detection (`Physics2D.OverlapCircleAll` + angle check)
- [x] Attack damage applied to enemies
- [x] Per-attack enemy hit tracking (no double-hit in one swing)
- [x] Enemy HP system (`maxHealth`, `currentHealth`, `TakeDamage`)
- [x] Enemy death state (`isDead` flag + death animation trigger)
- [x] Attack animation events (`AttackStart`, `AttackHit`, `AttackEnd`)
- [x] Attack SFX (swoosh ×3, slash/impact ×3)
- [x] Attack debug visualization (Gizmos arc)
- [x] Enemy attack back — `EnemyCombat.cs` deals damage to player via `PlayerHealth.TakeDamage()`
- [x] Player invincibility frames (brief I-frames after taking damage, configurable duration)
- [x] Parry system (Space) — counter during enemy yellow warning window
- [x] Hitstop (freeze frames + ZZZ-style camera punch zoom) — via `GameManager.TriggerHitstop()`
- [ ] Knockback on enemy hit
- [ ] Dodge I-frames during dash
- [ ] Combo system

---

### 🤖 Phase 9 — Enemy AI
```
█████████████████████░   92%  🔄 IN PROGRESS
```
- [x] `EnemyAI.cs` — Goblin patrol / chase / search brain (refactored from `EnemyPatrol.cs`)
- [x] `EnemyController.cs` — Physics movement and sprite facing, decoupled from AI
- [x] `EnemyCombat.cs` — Close-range combat mode with attack sequences and player damage
- [x] `EnemyHealth.cs` — Health, damage flash, death state
- [x] Random patrol within patrol radius
- [x] Player detection via chase radius + field-of-view angle
- [x] Line-of-sight check (no chasing through walls)
- [x] Chase memory system (remembers player position after losing sight)
- [x] Search mode (patrol last known position before giving up)
- [x] Smart obstacle avoidance (multi-angle steering)
- [x] Enemy separation (no stacking on each other)
- [x] Enemy health + damage flash visual feedback
- [x] Enemy death with death animation + combat exit
- [x] Emotion bubbles: `!` when spotting player, `?` when losing track
- [x] Goblin sprite set (Walk / Attack / Dead — Down / Left / Up)
- [x] Goblin Animator controller (`GoblinAC.controller`)
- [x] Enemy attack back (deal damage to player via `PlayerHealth.TakeDamage()`)
- [ ] Multiple enemy types

---

### 🧩 Phase 10 — System Architecture
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] `GameManager.cs` — Singleton, combat state tracking
- [x] Dynamic music switching: ambient BGM ↔ battle BGM (crossfade)
- [x] `AudioManager.cs` — Singleton with `Sound[]` arrays + `AudioSource` pooling
- [x] Type-safe sound enums (`SFXType`, `MusicType`) via `SoundEnums.cs`
- [x] `PlaySFX(SFXType)`, `PlayRandomSFX(SFXType[])`, `CrossfadeMusic()` API
- [x] `BubbleController.cs` — Reusable emotion bubble system
- [x] Bubble prefabs: `SuspenseBubble.prefab` (!), `QuestionBubble.prefab` (?)
- [x] Bubble emote sprite sheet (`bubble emotes july update.png`)

---

### 🎯 Phase 10.5 — Enemy Health Bar System (Zenless Zone Zero Style)
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] `EnemyHealthBar.cs` — Individual health bar UI component with animations
- [x] `EnemyHealthBarManager.cs` — Singleton manager for all enemy health bars
- [x] `HealthBarSetupHelper.cs` — Auto-setup utility for easy scene configuration
- [x] Line connectors from enemies to health bars (LineRenderer)
- [x] World-space floating health bars above each enemy (top-right offset)
- [x] Combat-triggered visibility (health bars only show during combat)
- [x] Smooth fade in/out animations
- [x] Damage flash feedback (red flash on hit)
- [x] Auto-registration system (enemies register on spawn)
- [x] Auto-cleanup (health bars removed when enemy dies)
- [x] Resolution-independent UI (CanvasScaler)
- [x] Editor menu integration for easy setup
- [x] Comprehensive documentation (`HEALTHBAR_SETUP.md`)

**Visual Features:**
- Health bars only appear during combat (enemy detection or taking damage)
- Health bars float above each enemy individually (Zenless Zone Zero inspired)
- Elegant line connectors from enemy to their health bar
- Smooth health transitions and fade effects
- Billboard effect (health bars always face camera)
- Customizable position offset, colors, and scale

**Setup:** See `HEALTHBAR_SETUP.md` for full setup guide and customization options.

---

### 🧍 Phase 10.6 — Player Health & Combat System
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] `PlayerHealth.cs` — Full player health system
- [x] `TakeDamage(float damage)` with configurable invincibility frames (default 0.5 s)
- [x] Damage flash visual feedback (red colour, configurable flash count)
- [x] Player death handling — movement disabled, colliders disabled, animator `Dead` / `isDead` trigger
- [x] `Heal(float amount)` method
- [x] Player hit SFX (`SFXType.Punch1`)
- [x] `PlayerHealthBarUI.cs` — Screen-space HUD health bar (top-left)
- [x] Smooth fill animation with colour gradient (green → yellow → red)
- [x] `PlayerHealthBarUI` singleton for global access from `PlayerHealth`
- [x] Resolution-independent (CanvasScaler / anchored RectTransform)
- [x] Editor inspector helper buttons (Auto-Find, Test Health Bar)

---

### 🔢 Phase 10.7 — Floating Damage Numbers (Zenless Zone Zero Style)
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] `DamageText.cs` — Individual damage number animation (float up + fade out)
- [x] `DamageTextManager.cs` — Object-pooling manager (configurable pool size)
- [x] Yellow/gold gradient for enemy taking damage
- [x] Red gradient for player taking damage
- [x] Orange gradient for critical hits
- [x] World-space spawn at hit position with vertical offset + horizontal spread
- [x] `DamageType` enum (`Enemy`, `Player`, `Critical`) for easy extensibility
- [x] `ShowDamageCustom()` API for arbitrary gradient colours
- [x] Integrated into `EnemyHealth.TakeDamage()` and `PlayerHealth.TakeDamage()`

---

### 🥊 Phase 10.8 — Parry System (Zenless Zone Zero Style)
```
██████████████████████  100%  ✅ COMPLETE
```
- [x] Parry input (Space) — succeeds only during enemy's yellow warning window
- [x] Enemy attack warning indicator — yellow (parryable) or red (dodge only), shown before each attack
- [x] `EnemyCombat.TryParry()` — validates timing window and transitions enemy to stagger
- [x] `EnemyCombat.IsParryable()` / `IsInReadyWindow()` — public API for player detection
- [x] Post-parry enemy stagger state (blocks movement + attack, configurable duration)
- [x] Directional parry facing — player faces toward enemy when parrying
- [x] Parry animation events: `ParryImpact()` (clash frame) and `ParryEnd()` (animation end)
- [x] Hitstop via `GameManager.TriggerHitstop()` — ZZZ-style camera punch zoom on parry impact
- [x] `ParrySparkEffect.cs` — procedural ZZZ-style weapon clash sparkle (no prefab needed)
  - Three-layer VFX: streaking sparks (28 burst) + ring flash bloom + glint stars (6 burst)
  - `useUnscaledTime = true` — visible during hitstop (timeScale = 0)
  - Sorts above player sprite on the Effects layer
- [x] Parry SFX: `SFXType.Clash` (sword-clash impact)
- [x] Warning indicator SFX: `SFXType.Slice` (pre-attack signal)
- [x] Player parry animations: `Player_ParryDown.anim`, `Player_ParryLeft.anim`, `Player_ParryUp.anim`
- [x] Enemy parried animations: `Goblin_ParriedDown.anim`, `Goblin_ParriedLeft.anim`, `Goblin_ParriedUp.anim`

---

<a id="core-feature-status"></a>

## ⚔️ Core Feature Status

### 🕹️ Movement
```
██████████████████████  100%  ✅ COMPLETE
```
| Feature | Status |
|---|:---:|
| 8-directional real-time movement | ✅ |
| Physics-based (`Rigidbody2D`) | ✅ |
| Diagonal normalization | ✅ |
| Wall collision | ✅ |
| Sprite flip for left ↔ right mirroring | ✅ |
| Dash / Dodge roll (Left Shift or Right-Click) | ✅ |
| Dash VFX trailing effect | ✅ |
| Face-lock during attack | ✅ |
| Knockback on hit | ❌ |

---

### 🎬 Animation
```
█████████████████████░   92%  🔄 IN PROGRESS
```
| Animation | Status |
|---|:---:|
| Idle Down / Left / Up | ✅ |
| Walk Down / Left / Up | ✅ |
| Attack Down / Left / Up | ✅ |
| Blend-tree directional blending | ✅ |
| Dash VFX effect animation | ✅ |
| Goblin Walk Down / Left / Up | ✅ |
| Goblin Attack Down / Left / Up | ✅ |
| Goblin Dead Down / Left / Up | ✅ |
| Emotion bubbles (! and ?) | ✅ |
| Player Parry Down / Left / Up | ✅ |
| Goblin Parried Down / Left / Up | ✅ |
| Dodge / Roll player animation | ❌ |
| Player Hit / Hurt animation | ❌ |
| Player Death animation | ❌ |

---

### ⚔️ Combat
```
████████████████████░░   80%  🔄 IN PROGRESS
```
| Feature | Status |
|---|:---:|
| Attack animations (Down/Left/Up) | ✅ |
| Attack input detection (Left Click / Z) | ✅ |
| Arc-based hitbox system | ✅ |
| Damage calculation | ✅ |
| Per-swing hit tracking (no double-hit) | ✅ |
| Enemy HP system | ✅ |
| Enemy death state + animation | ✅ |
| Enemy attack back (deal damage to player) | ✅ |
| Player HP system + damage flash | ✅ |
| Player invincibility frames (after hit) | ✅ |
| Floating damage numbers (ZZZ style) | ✅ |
| Enemy attack warning indicator (yellow/red) | ✅ |
| Parry system (Space, during yellow window) | ✅ |
| Hitstop (freeze frames + camera punch zoom) | ✅ |
| Knockback on enemy | ❌ |
| Dodge I-frames during dash | ⏳ (dash done, I-frames pending) |
| Combo system | ❌ |

---

### 🤖 Enemy AI
```
█████████████████████░   92%  🔄 IN PROGRESS
```
| Feature | Status |
|---|:---:|
| Random patrol within radius | ✅ |
| Player detection (radius + FOV angle) | ✅ |
| Line-of-sight check | ✅ |
| Chase memory (remembers last position) | ✅ |
| Search mode (patrol last known position) | ✅ |
| Smart obstacle avoidance | ✅ |
| Enemy separation (no stacking) | ✅ |
| Health + damage flash feedback | ✅ |
| Death state + death animation | ✅ |
| Emotion bubbles (! / ?) | ✅ |
| Combat music trigger | ✅ |
| Enemy attack back (deal damage to player) | ✅ |
| Multiple enemy types | ❌ |

---

### 🧩 System Architecture
```
██████████████████████  100%  ✅ COMPLETE
```
| Feature | Status |
|---|:---:|
| `GameManager` singleton (combat state tracking) | ✅ |
| Dynamic music: ambient ↔ battle crossfade | ✅ |
| `AudioManager` singleton (pooled AudioSources) | ✅ |
| Type-safe sound enums (`SFXType` / `MusicType`) | ✅ |
| `BubbleController` (reusable emotion bubble system) | ✅ |
| Suspense bubble prefab (!) | ✅ |
| Question bubble prefab (?) | ✅ |
| `PlayerHealth` singleton (HP, invincibility, death) | ✅ |
| `DamageTextManager` singleton (pooled damage numbers) | ✅ |
| `GameManager.TriggerHitstop()` (freeze frames + camera zoom) | ✅ |
| `ParrySparkEffect` (procedural clash VFX, no prefab needed) | ✅ |

---

### 🖥️ UI / Menu
```
████████████████░░░░░░   72%  🔄 IN PROGRESS
```
| Feature | Status |
|---|:---:|
| Enemy Health Bars (Zenless Zone Zero style) | ✅ |
| Health bar line connectors | ✅ |
| Health bar manager system | ✅ |
| Player HP bar (top-left HUD, smooth + colour gradient) | ✅ |
| Floating damage numbers (ZZZ style, pooled) | ✅ |
| Main Menu screen | ❌ |
| Pause Menu | ❌ |
| Game Over screen | ❌ |

---

### 🔊 Sound Effects & Music
```
███████████████████░░░   88%  🔄 IN PROGRESS
```
| Feature | Status |
|---|:---:|
| Player footstep SFX (grass ×3) | ✅ |
| Dodge / Dash SFX | ✅ |
| Attack swoosh SFX (×3) | ✅ |
| Slash / Impact SFX (×3) | ✅ |
| Enemy spotted SFX (suspense) | ✅ |
| Player hit / take-damage SFX (`Punch1`) | ✅ |
| Enemy attack warning SFX (`Slice`) | ✅ |
| Parry clash SFX (`Clash`) | ✅ |
| Ambient background music | ✅ |
| Battle background music | ✅ |
| Dynamic music crossfade (ambient ↔ battle) | ✅ |
| UI interaction SFX | ❌ |

---

<a id="upcoming-phases"></a>

## 🗓️ Upcoming Phases

| Phase | Feature | Priority |
|---|---|:---:|
| **Phase 11** | Combat Polish (knockback, dodge I-frames, combo system) | 🔴 High |
| **Phase 12** | ~~Enemy Attack Back~~ ✅ → `EnemyCombat.cs` complete | ✅ Done |
| **Phase 13** | ~~Player HP / Hurt / Death system~~ ✅ → `PlayerHealth.cs` complete | ✅ Done |
| **Phase 14** | ~~Player HP Bar~~ ✅ → Main Menu, Pause Menu, Game Over screen | 🟠 Medium |
| **Phase 15** | Multiple enemy types | 🟠 Medium |
| **Phase 16** | ~~Sound Effects~~ ✅ → UI interaction SFX | 🟡 Low |
| **Phase 17** | Polish (combo system, screenshake, VFX) | 🟡 Low |

---

<a id="project-structure"></a>

## 📁 Project Structure

```
Assets/
├── Animations/
│   ├── Player/
│   │   ├── PlayerAnimator.controller        ✅
│   │   ├── Player_IdleDown.anim             ✅
│   │   ├── Player_IdleLeft.anim             ✅
│   │   ├── Player_IdleUp.anim               ✅
│   │   ├── Player_WalkDown.anim             ✅
│   │   ├── Player_WalkLeft.anim             ✅
│   │   ├── Player_walkUp.anim               ✅
│   │   ├── Player_AttackDown.anim           ✅
│   │   ├── Player_AttackLeft.anim           ✅
│   │   ├── Player_AttackUp.anim             ✅
│   │   ├── Player_ParryDown.anim            ✅
│   │   ├── Player_ParryLeft.anim            ✅
│   │   └── Player_ParryUp.anim              ✅
│   └── Goblin/
│       ├── GoblinAC.controller              ✅
│       ├── Goblin_WalkDown.anim             ✅
│       ├── Goblin_WalkLeft.anim             ✅
│       ├── Goblin_WalkUp.anim               ✅
│       ├── Goblin_AttackDown.anim           ✅
│       ├── Goblin_AttackLeft.anim           ✅
│       ├── Goblin_AttackUp.anim             ✅
│       ├── Goblin_DeadDown.anim             ✅
│       ├── Goblin_DeadLeft.anim             ✅
│       ├── Goblin_DeadUp.anim               ✅
│       ├── Goblin_ParriedDown.anim          ✅
│       ├── Goblin_ParriedLeft.anim          ✅
│       └── Goblin_ParriedUp.anim            ✅
├── Effects/
│   ├── Dash/
│   │   ├── DashEffect.controller            ✅
│   │   ├── Dash.anim                        ✅
│   │   └── FX033_01..10.png                 ✅ (10 VFX frames)
│   ├── Question/                            ✅ (question bubble animation)
│   └── Suspense/                            ✅ (suspense bubble animation)
├── Scripts/
│   ├── Core/
│   │   ├── AudioManager.cs                  ✅
│   │   ├── GameManager.cs                   ✅  (combat state, hitstop camera zoom)
│   │   └── SoundEnums.cs                    ✅
│   ├── Enemy/
│   │   ├── EnemyAI.cs                       ✅  (patrol / chase / search brain)
│   │   ├── EnemyCombat.cs                   ✅  (close-range combat, parry window, stagger)
│   │   ├── EnemyController.cs               ✅  (physics movement & sprite facing)
│   │   └── EnemyHealth.cs                   ✅  (health, damage flash, death)
│   ├── Player/
│   │   ├── ParrySparkEffect.cs              ✅  (procedural ZZZ-style clash VFX)
│   │   ├── PlayerController.cs              ✅  (movement, dash, attack, parry input)
│   │   └── PlayerHealth.cs                  ✅  (HP, invincibility, death)
│   └── UI/
│       ├── BubbleController.cs              ✅
│       ├── DamageText.cs                    ✅  (floating damage number animation)
│       ├── DamageTextManager.cs             ✅  (object-pooling manager)
│       ├── EnemyHealthBar.cs                ✅
│       ├── EnemyHealthBarManager.cs         ✅
│       ├── HealthBarSetupHelper.cs          ✅
│       ├── HealthBarSystemValidator.cs      ✅
│       └── PlayerHealthBarUI.cs             ✅  (player HUD health bar)
├── Sounds/
│   ├── BGM/
│   │   ├── priscilasousa-loop-edm-*.mp3     ✅ (Ambient BGM)
│   │   └── soulfuljamtracks-edm-loop-*.mp3  ✅ (Battle BGM)
│   └── SFX/
│       ├── Clash/
│       │   └── u_fe12rqkbth-sword-clash-*.mp3        ✅ (parry clash SFX)
│       ├── Slash/
│       │   └── dragon-studio-*.mp3 (×3)     ✅ (slash/impact SFX)
│       ├── Slice/
│       │   └── dragon-studio-sword-slice-*.mp3        ✅ (attack warning SFX)
│       ├── Swoosh/
│       │   └── dragon-studio-dramatic-whoosh-*.mp3   ✅ (dramatic whoosh SFX)
│       ├── freesound_community-rustling-grass-*.mp3  ✅ (×3 footstep clips)
│       ├── zapsplat_cartoon_fast_whoosh_*.mp3        ✅ (dash SFX)
│       ├── konpeito_sound-knife_swish03-*.mp3        ✅ (attack swoosh ×1)
│       ├── oxidvideos-sword-swing-*.mp3              ✅ (attack swoosh ×1)
│       ├── freesound_community-sword-sound-*.mp3     ✅ (attack swoosh ×1)
│       └── brvhrtz-stab-*.mp3                        ✅ (suspense SFX)
├── Sprites/
│   ├── Enemies/
│   │   └── Goblin/
│   │       ├── D_Walk.png / D_Attack.png / D_Death.png  ✅
│   │       ├── S_Walk.png / S_Attack.png / S_Death.png  ✅
│   │       └── U_Walk.png / U_Attack.png / U_Death.png  ✅
│   ├── SpriteSheet.png
│   ├── TilesetFloor.png
│   ├── TilesetNature.png
│   ├── bubble emotes july update.png        ✅
│   ├── skull knight.png                     ✅
│   ├── skull knight 1.png                   ✅
│   └── skull knight 2.png                   ✅
├── Tiles/
│   ├── TilesetFloor_0..457.asset            ✅
│   └── TilesetNature_0..381.asset           ✅
├── Perfab/
│   ├── Effect/
│   │   └── DashEffect.prefab                ✅
│   ├── Enemies/
│   │   └── EnemyGoblin.prefab               ✅
│   └── Ui/
│       ├── DamageText.prefab                ✅
│       ├── QuestionBubble.prefab            ✅
│       └── SuspenseBubble.prefab            ✅
└── MainScene.unity                          ✅
```

---

<a id="credits--assets"></a>

## 🎨 Credits & Assets

> Add credits here as you integrate third-party assets into the project.

| Asset | Author / Source | License | Usage |
|---|---|---|---|
| *(Skull Knight sprite)* | *(add source)* | *(add license)* | Player character |
| *(Floor tileset)* | *(add source)* | *(add license)* | Environment floor tiles |
| *(Nature tileset)* | *(add source)* | *(add license)* | Environment props / nature tiles |
| *(Goblin sprite sheets)* | *(add source)* | *(add license)* | Goblin enemy character |
| *(Bubble emotes sprite sheet)* | *(add source)* | *(add license)* | Emotion bubbles (!, ?) |
| Rustling Grass SFX (×3) | freesound_community via Freesound.org | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | Player footstep sounds on grass |
| Fast Whoosh / Swipe SFX | ZapSplat (zapsplat.com) | ZapSplat Standard License | Dash / dodge sound effect |
| Sword Swing SFX (×3) | Various (freesound_community, oxidvideos, konpeito_sound) | *(verify licenses)* | Attack swoosh sound effects |
| Sword Slash / Slice SFX (×3) | Various (dragon-studio, universfield) via Freesound.org | *(verify licenses)* | Hit / impact sound effects |
| Stab SFX | brvhrtz via Freesound.org | *(verify license)* | Enemy spotted suspense sound |
| Sword Slice SFX | dragon-studio via Freesound.org | *(verify license)* | Enemy attack warning indicator sound |
| Sword Clash SFX | u_fe12rqkbth via Freesound.org | *(verify license)* | Parry clash impact sound |
| Dramatic Whoosh SFX | dragon-studio via Freesound.org | *(verify license)* | Dramatic swoosh sound |
| Ambient BGM loop | priscilasousa via Freesound.org | *(verify license)* | Ambient background music |
| Battle BGM loop | soulfuljamtracks via Freesound.org | *(verify license)* | Battle background music |
| *(add asset)* | *(add source)* | *(add license)* | *(add usage)* |

---

<div align="center">

*Built with ❤️ in Unity*

</div>
