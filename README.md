<div align="center">

# ⚔️ TopDown Action RPG (WORK IN PROGRESS)

### *2D Top-Down Real-Time Action RPG*

![Unity](https://img.shields.io/badge/Unity-2022+-black?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-In%20Development-orange)
![Progress](https://img.shields.io/badge/Overall%20Progress-45%25-blue)

> **Goal:** Real-time physics-driven top-down action RPG with fluid combat, dodge mechanics, and responsive input.

</div>

---

## 🎯 Design Pillars

| ❌ What We Are NOT Building | ✅ What We ARE Building |
|---|---|
| Pokémon-style turn-based RPG | Real-time input combat |
| Grid / coroutine movement | Physics-driven movement |
| Static isMoving locks | Combat that interrupts movement |
| Flat basic attacks | Dash / knockback / hitstop system |

---

## 📊 Overall Progress

```
Total Completion  █████████░░░░░░░░░░░  45%
```

---

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

### 🏃 Phase 7 — Real-Time Movement
```
█████████████████████░   95%  🔄 IN PROGRESS
```
- [x] `PlayerController.cs` implemented
- [x] `Input.GetAxisRaw` for instant response
- [x] `rb.MovePosition` physics-based movement (no coroutines!)
- [x] Diagonal normalization (no speed boost)
- [x] Animation sync via blend trees (`isMoving`, `moveX`, `moveY` params)
- [x] Sprite flip (`flipX`) for left ↔ right mirroring
- [x] Dash / dodge system (Left Shift or Right-Click, with VFX + SFX)
- [ ] Movement cancellation during hit-stun

<details>
<summary>📌 PlayerController.cs</summary>

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
    private bool isDashing = false;
    private float lastDashTime = -Mathf.Infinity;
    private Vector2 lastMoveDir = Vector2.down;

    [Header("Dash Effects")]
    public GameObject dashEffectPrefab;

    [Header("Audio")]
    public AudioClip dashSFX;
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.8f;
    private AudioSource audioSource;

    private Vector2 movementInput;
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1))
                StartCoroutine(Dash());
        ReadInput();
        UpdateAnimation();
    }

    private void FixedUpdate() { if (!isDashing) Move(); HandleFootsteps(); }

    private void ReadInput()
    {
        if (isDashing) return;
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized;
        if (movementInput != Vector2.zero) lastMoveDir = movementInput;
        if (movementInput.x > 0) spriteRenderer.flipX = true;
        else if (movementInput.x < 0) spriteRenderer.flipX = false;
    }

    private void Move()
    {
        rb.MovePosition(rb.position + movementInput * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateAnimation()
    {
        bool isMoving = movementInput != Vector2.zero;
        animator.SetBool("isMoving", isMoving);
        if (isMoving)
        {
            animator.SetFloat("moveX", Mathf.Abs(movementInput.x));
            animator.SetFloat("moveY", movementInput.y);
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        Vector2 dir = movementInput != Vector2.zero ? movementInput : lastMoveDir;
        if (dashEffectPrefab != null)
        {
            GameObject vfx = Instantiate(dashEffectPrefab, transform.position,
                                         Quaternion.identity, transform);
            vfx.transform.localPosition = -(Vector3)dir * 2f;
            vfx.transform.rotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            Animator vfxAnim = vfx.GetComponent<Animator>();
            if (vfxAnim != null)
            {
                float len = vfxAnim.runtimeAnimatorController.animationClips[0].length;
                vfxAnim.Play(vfxAnim.runtimeAnimatorController.animationClips[0].name, 0, 0f);
                Destroy(vfx, len);
            }
            else Destroy(vfx, 1f);
        }
        if (dashSFX != null && audioSource != null) audioSource.PlayOneShot(dashSFX);
        float start = Time.time;
        while (Time.time < start + dashDuration)
        {
            rb.MovePosition(rb.position + dir * dashSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        isDashing = false;
    }

    // Footsteps play only when moving on grass (OnTriggerStay2D sets isOnGrass)
    private bool isOnGrass;
    private Vector2 lastPosition;
    private float footstepTimer;
    private int lastFootstepIndex = -1;

    private void HandleFootsteps()
    {
        if (isDashing) { footstepTimer = 0f; return; }
        float moved = Vector2.Distance(rb.position, lastPosition);
        lastPosition = rb.position;
        if (moved > 0.01f)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                if (isOnGrass) PlayFootstepGrass();
                footstepTimer = 0f;
            }
        }
        else footstepTimer = 0f;
    }

    private void PlayFootstepGrass()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        int index;
        do { index = Random.Range(0, footstepClips.Length); }
        while (index == lastFootstepIndex && footstepClips.Length > 1);
        lastFootstepIndex = index;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(footstepClips[index], 0.6f);
        audioSource.pitch = 1f;
    }

    private void OnTriggerStay2D(Collider2D other)
    { if (other.CompareTag("Grass")) isOnGrass = true; }
    private void OnTriggerExit2D(Collider2D other)
    { if (other.CompareTag("Grass")) isOnGrass = false; }
}
```
</details>

---

## ⚔️ Core Feature Status

### 🕹️ Movement
```
█████████████████████░   95%  🔄 IN PROGRESS
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
| Knockback on hit | ❌ |

---

### 🎬 Animation
```
█████████████████░░░░░   75%  🔄 IN PROGRESS
```
| Animation | Status |
|---|:---:|
| Idle Down / Left / Up | ✅ |
| Walk Down / Left / Up | ✅ |
| Attack Down / Left / Up | ✅ |
| Blend-tree directional blending | ✅ |
| Dash VFX effect animation | ✅ |
| Dodge / Roll player animation | ❌ |
| Hit / Hurt animation | ❌ |
| Death animation | ❌ |

---

### ⚔️ Combat
```
████░░░░░░░░░░░░░░░░░░   15%  🔄 IN PROGRESS
```
| Feature | Status |
|---|:---:|
| Attack animations (Down/Left/Up) | ✅ |
| Attack input detection | ❌ |
| Hitbox system | ❌ |
| Damage calculation | ❌ |
| Enemy HP system | ❌ |
| Hitstop (freeze frames) | ❌ |
| Knockback on enemy | ❌ |
| Dodge / I-frames | ⏳ (dash done, I-frames pending) |
| Combo system | ❌ |

---

### 🖥️ UI / Menu
```
░░░░░░░░░░░░░░░░░░░░░░    0%  ❌ NOT STARTED
```
| Feature | Status |
|---|:---:|
| Main Menu screen | ❌ |
| HUD (HP bar, stamina/energy bar) | ❌ |
| Pause Menu | ❌ |
| Game Over screen | ❌ |

---

### 🔊 Sound Effects
```
███████░░░░░░░░░░░░░░░   33%  🔄 IN PROGRESS
```
| Feature | Status |
|---|:---:|
| Player footstep SFX (grass) | ✅ |
| Dodge / Dash SFX | ✅ |
| Attack SFX | ❌ |
| Hit / Impact SFX | ❌ |
| Background Music | ❌ |
| UI interaction SFX | ❌ |

---

## 🗓️ Upcoming Phases

| Phase | Feature | Priority |
|---|---|:---:|
| **Phase 8** | Combat System (hitboxes, damage, HP) | 🔴 High |
| **Phase 9** | ~~Dodge / Dash~~ ✅ → I-frames during dash | 🔴 High |
| **Phase 10** | Enemy AI (patrol, chase, attack) | 🟠 Medium |
| **Phase 11** | HUD & UI (HP bar, menus) | 🟠 Medium |
| **Phase 12** | ~~Sound Effects~~ ⏳ → Attack / Hit SFX + BGM | 🟡 Low |
| **Phase 13** | Polish (hitstop, screenshake, VFX) | 🟡 Low |

---

## 📁 Project Structure

```
Assets/
├── Animations/
│   └── Player/
│       ├── PlayerAnimator.controller        ✅
│       ├── Player_IdleDown.anim             ✅
│       ├── Player_IdleLeft.anim             ✅
│       ├── Player_IdleUp.anim               ✅
│       ├── Player_WalkDown.anim             ✅
│       ├── Player_WalkLeft.anim             ✅
│       ├── Player_walkUp.anim               ✅
│       ├── Player_AttackDown.anim           ✅
│       ├── Player_AttackLeft.anim           ✅
│       └── Player_AttackUp.anim             ✅
├── Effects/
│   └── Dash/
│       ├── DashEffect.controller            ✅
│       ├── Dash.anim                        ✅
│       └── FX033_01..10.png                 ✅ (10 VFX frames)
├── Scripts/
│   └── PlayerController.cs                 ✅
├── Sounds/
│   └── SFX/
│       ├── freesound_community-rustling-grass-*.mp3  ✅ (×3 footstep clips)
│       └── zapsplat_cartoon_fast_whoosh_*.mp3        ✅ (dash SFX)
├── Sprites/
│   ├── SpriteSheet.png
│   ├── TilesetFloor.png
│   ├── TilesetNature.png
│   └── skull knight.png                    ✅
├── Tiles/
│   ├── TilesetFloor_0..457.asset           ✅
│   └── TilesetNature_0..381.asset          ✅
├── DashEffect.prefab                       ✅
└── MainScene.unity                         ✅
```

---

## 🎨 Credits & Assets

> Add credits here as you integrate third-party assets into the project.

| Asset | Author / Source | License | Usage |
|---|---|---|---|
| *(Skull Knight sprite)* | *(add source)* | *(add license)* | Player character |
| *(Floor tileset)* | *(add source)* | *(add license)* | Environment floor tiles |
| *(Nature tileset)* | *(add source)* | *(add license)* | Environment props / nature tiles |
| Rustling Grass SFX (×3) | freesound_community via Freesound.org | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | Player footstep sounds on grass |
| Fast Whoosh / Swipe SFX | ZapSplat (zapsplat.com) | ZapSplat Standard License | Dash / dodge sound effect |
| *(add asset)* | *(add source)* | *(add license)* | *(add usage)* |

---

<div align="center">

*Built with ❤️ in Unity*

</div>
