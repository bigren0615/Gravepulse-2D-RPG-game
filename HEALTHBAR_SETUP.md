# 🎯 Enemy Health Bar System - Zenless Zone Zero Style (World Space)

## ✨ Features
- **Floating Above Enemies**: Health bars float above each enemy at their top-right
- **Combat-Triggered Display**: Health bars only appear when enemies enter combat (detected by player or take damage)
- **Visual Connection**: Elegant line connects enemy to their health bar
- **Smooth Animations**: Fade in/out effects and smooth health transitions
- **Damage Feedback**: Red flash when taking damage
- **Auto-Management**: Automatically tracks all enemies and removes bars when defeated
- **Billboard Effect**: Health bars always face the camera

## 🚀 Quick Setup (2 Steps)

### Step 1: Create the Health Bar Manager
1. In Unity, create an **empty GameObject** in your scene
2. Name it "HealthBarSetup"
3. Add component: **HealthBarSetupHelper**
4. The system will auto-setup on play!

**OR** use the editor menu:
1. GameObject → UI → **Enemy Health Bar System**
2. Done!

**OR** manual method:
1. Create an empty GameObject in your scene
2. Name it "EnemyHealthBarManager"
3. Add component: **EnemyHealthBarManager**
4. Done!

### Step 2: Play the Game
That's it! When you play:
- Health bars remain hidden until combat starts
- **Enter combat** by attacking an enemy or getting close enough to be detected
- Health bars appear floating above each enemy (top-right)
- Lines connect each enemy to their health bar
- Everything is managed automatically

## 🎨 Customization

### Adjusting Health Bar Position
Select the **EnemyHealthBarManager** in your scene and modify:

#### World Space Settings
- **Offset From Enemy**: 3D Position offset from enemy center
  - Default: `(0.8, 1.2, 0)` = Top-right, slightly above
  - X: Right/left (positive = right)
  - Y: Up/down (positive = up)
  - Z: Forward/back (usually keep at 0)

- **Health Bar World Size**: Size in world units
  - Default: `(1.2, 0.15)` = Width and height
  - Make larger for bigger enemies, smaller for smaller ones

- **Canvas World Scale**: Overall scale multiplier
  - Default: `0.01` = Good for 2D games
  - Adjust if health bars appear too large/small

### Customizing Colors & Style
The health bar colors are set in the `EnemyHealthBar` script:

Edit in `EnemyHealthBar.cs` (lines ~30-35):
```csharp
public Color healthColor = new Color(0.2f, 1f, 0.4f); // Bright green
public Color damageColor = new Color(1f, 0.3f, 0.2f); // Red flash
public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark background
public Color borderColor = new Color(1f, 1f, 1f, 0.8f); // White border
public Color lineColor = new Color(0.6f, 0.9f, 1f, 0.6f); // Cyan line
```

### Advanced Customization

#### Line Appearance
In `EnemyHealthBar.cs`, `SetupLine()` method:
```csharp
connectionLine.startWidth = 0.04f; // Thickness at health bar
connectionLine.endWidth = 0.02f;   // Thickness at enemy (tapers)
```

#### Animation Speed
```csharp
public float smoothSpeed = 10f; // Health bar fill speed
public float fadeInDuration = 0.2f;
public float fadeOutDuration = 0.3f;
```

#### Billboard Behavior
```csharp
public bool billboardToCamera = true; // Always face camera
```

## 🎮 How It Works

### Automatic Registration
When an enemy spawns:
1. `EnemyPatrol.Start()` automatically registers with the health bar manager
2. A world-space canvas health bar is created (hidden initially)
3. Health bar waits in standby mode

### Combat Detection
Health bars appear when:
1. **Player enters enemy detection range** → Enemy starts chasing → EnterCombat() called → Health bar shows
2. **Enemy takes damage** → EnterCombat() called → Health bar shows

Once shown in combat, health bars remain visible until the enemy dies.

### Health Updates
When an enemy takes damage:
1. `EnemyPatrol.TakeDamage()` is called
2. Health bar is updated via `EnemyHealthBarManager.UpdateEnemyHealth()`
3. Visual feedback: red flash + smooth fill animation

### Cleanup
When an enemy dies:
1. Health bar fades out smoothly
2. Line connection disappears
3. UI element is destroyed after fade animation

### Position Tracking
Every frame:
1. Health bar follows enemy position with offset
2. Billboard effect rotates to face camera
3. Line updates to connect properly

## 🐛 Troubleshooting

### Health bars not appearing?
**Check:**
1. Is there an `EnemyHealthBarManager` GameObject in your scene?
2. Does it have the `EnemyHealthBarManager` component?
3. **Are you in combat?** Health bars only show when:
   - You attack an enemy, OR
   - You get close enough for the enemy to detect you
4. Run the game and check Console for warnings

**Quick Fix:**
- GameObject → UI → Enemy Health Bar System
- Or add `HealthBarSetupHelper` component to any object

### Health bars too big/small?
**Adjust:**
1. Select `EnemyHealthBarManager`
2. Change **Canvas World Scale** (0.01 default)
   - Smaller value = smaller bars
   - Larger value = bigger bars

### Health bars in wrong position?
**Adjust:**
1. Select `EnemyHealthBarManager`
2. Change **Offset From Enemy**
   - Increase X for more right
   - Increase Y for higher
   - Example: `(1.0, 1.5, 0)` = Higher and more right

### Lines not showing?
The line renderer is created automatically. If issues occur:
1. Check that Line Renderer components are enabled
2. Verify the shader is "Sprites/Default"
3. Try adjusting line width in code (see Advanced Customization)

### Health bars not facing camera?
1. Ensure `billboardToCamera` is true in `EnemyHealthBar` component
2. Check that Camera.main is working (main camera tagged properly)

### Performance concerns?
The system is highly optimized:
- Uses efficient dictionary lookups
- Smooth lerp for animations
- Lines only render when visible
- World-space rendering is lightweight

For 50+ enemies:
- System handles it fine
- Each health bar is independent
- No screen-space layout calculations needed

## 📝 Script Reference

### Main Scripts
1. **EnemyHealthBar.cs** - Individual world-space health bar component
2. **EnemyHealthBarManager.cs** - Manages all health bars (Singleton)
3. **EnemyPatrol.cs** - Enemy AI (includes health bar integration)
4. **HealthBarSetupHelper.cs** - Optional setup automation

### Key Methods

#### EnemyHealthBarManager
```csharp
// Register a new enemy
RegisterEnemy(Transform enemy, float currentHealth, float maxHealth)

// Update enemy health
UpdateEnemyHealth(Transform enemy, float currentHealth, float maxHealth)

// Remove enemy health bar
UnregisterEnemy(Transform enemy)

// Clear all health bars
ClearAllHealthBars()
```

#### EnemyHealthBar
```csharp
// Initialize with enemy reference
Initialize(Transform enemy)

// Update health (0-1 normalized)
SetHealth(float healthPercent)

// Show/hide with fade
Show()
Hide()
```

## 🎨 Visual Style - Zenless Zone Zero Inspiration

This system replicates the clean, modern UI style from Zenless Zone Zero:
- ✓ Health bars float above each enemy (top-right position)
- ✓ Sleek lines connecting enemies to their bars
- ✓ Smooth fade animations
- ✓ Clean, minimal design
- ✓ Doesn't obstruct gameplay
- ✓ Billboard effect keeps bars readable

## 💡 Tips for Best Results

1. **Camera Setup**: Works with both orthographic and perspective cameras
2. **World Scale**: Adjust `canvasWorldScale` based on your game's unit scale
3. **Offset Tuning**: Fine-tune `offsetFromEnemy` for perfect positioning
4. **Line Style**: Customize line colors to match your game's aesthetic
5. **Billboard**: Keep enabled for best readability from all angles

## 🎯 Positioning Guide

### Example Offset Values
```csharp
// Directly above enemy (centered)
offsetFromEnemy = new Vector3(0f, 1.5f, 0f);

// Top-right (default - ZZZ style)
offsetFromEnemy = new Vector3(0.8f, 1.2f, 0f);

// Far right, higher up
offsetFromEnemy = new Vector3(1.5f, 2.0f, 0f);

// Left side
offsetFromEnemy = new Vector3(-1.0f, 1.2f, 0f);
```

## 🔄 Differences from Screen-Space Version

**World-Space (Current)**:
- ✓ Floats above each enemy individually
- ✓ Follows enemy movement automatically
- ✓ Better for action games
- ✓ More immersive
- ✓ Zenless Zone Zero accurate

**Screen-Space (Not Used)**:
- Health bars in corner of screen
- Better for strategy/many enemies
- Takes up UI space

---

**Enjoy your beautiful Zenless Zone Zero-style floating health bars! 🎮✨**
