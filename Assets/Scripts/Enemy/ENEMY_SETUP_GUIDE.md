# Enemy System Setup Guide

## Overview
Your enemy system has been restructured from a single 918-line monolithic script into **4 focused, modular components**:

1. **EnemyController** - Movement, animations, visuals (~280 lines)
2. **EnemyAI** - Patrol, chase, search behaviors (~390 lines)  
3. **EnemyHealth** - Health, damage, death (~180 lines)
4. **EnemyCombat** - Combat state coordination (~70 lines)

## How to Set Up an Enemy

### Method 1: Update Existing Enemy Prefabs/Objects

Unity will automatically preserve your settings! Just:

1. **Select your enemy GameObject** in the scene or prefab editor
2. **Remove the old EnemyPatrol component** (if Unity hasn't already)
3. **Add the new components in this order:**
   - Add `EnemyController`
   - Add `EnemyAI`
   - Add `EnemyHealth`
   - Add `EnemyCombat`
   - Add `BubbleController` (if not already present)

4. **Configure settings** - All your previous settings from EnemyPatrol are available in the new components:

   **EnemyController:**
   - Speed (previously in EnemyPatrol)
   - Look Ahead Distance
   - Obstacle Check Radius
   - Separation Distance/Force
   - Enemy Layer mask
   - Player reference (auto-found if tagged "Player")

   **EnemyAI:**
   - Patrol Radius
   - Wait Time
   - Chase Radius
   - Stop Distance
   - Field of View Angle
   - Memory Duration
   - Search Duration/Radius
   - Stuck detection settings

   **EnemyHealth:**
   - Max Health
   - Enemy Size (for health bar sizing)
   - Damage Flash Color/Duration/Count

   **EnemyCombat:**
   - (No settings - automatically coordinates with GameManager)

### Method 2: Create New Enemy from Scratch

1. Create a new GameObject with:
   - SpriteRenderer
   - Animator
   - Collider2D (for collision)
   - Rigidbody2D (set to Kinematic, optional)

2. Add the 4 enemy components (see above)

3. Add BubbleController for visual feedback

4. Configure all the settings to your liking

## Required Components

Each enemy **must have**:
- ✅ Animator (for animations)
- ✅ SpriteRenderer (for visuals)
- ✅ EnemyController
- ✅ EnemyAI
- ✅ EnemyHealth
- ✅ EnemyCombat

Optional but recommended:
- BubbleController (for suspense/question bubbles)

## Benefits of the New System

✨ **Modular** - Each script has one clear responsibility
✨ **Maintainable** - Much easier to find and modify specific features
✨ **Extensible** - Easy to add new behaviors without touching existing code
✨ **Debuggable** - Smaller scripts are easier to debug
✨ **Reusable** - Components can be mixed and matched
✨ **Team-Friendly** - Multiple people can work on different components

## Migration Checklist

- [ ] Update enemy prefabs with new components
- [ ] Remove old EnemyPatrol component
- [ ] Test enemy patrol behavior
- [ ] Test enemy chase/search behavior  
- [ ] Test enemy health/damage/death
- [ ] Test combat music transitions
- [ ] Test health bar display
- [ ] Verify no console errors

## Troubleshooting

**Q: Enemy doesn't move**
- Check that EnemyController.speed > 0
- Verify Animator is assigned and has proper animation states

**Q: Enemy doesn't chase player**
- Check that player is tagged as "Player"
- Verify Chase Radius in EnemyAI
- Check Field of View Angle setting
- Use Scene Gizmos to visualize detection range (select enemy while game is running)

**Q: Health bar doesn't appear**
- Make sure EnemyHealthBarManager exists in scene
- Verify health bar appears when enemy takes damage
- Check that EnemyCombat component is present

**Q: Battle music doesn't play**
- Verify GameManager exists in scene
- Check AudioManager has battle music assigned
- Ensure EnemyCombat component is present on enemies

## Reference

The old EnemyPatrol.cs has been archived as `EnemyPatrol.OLD.cs.txt` in the Enemy folder for reference.

## Next Steps

Consider these optional improvements:
1. Extract player combat from PlayerController into PlayerCombat (~150 lines)
2. Add more enemy types with different AI behaviors
3. Create boss enemies with custom scripts extending the base components
4. Add special abilities or attack patterns to enemies

---

**Enjoy your clean, organized codebase! 🎮**
