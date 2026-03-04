# Damage Display System Setup Guide
## Zenless Zone Zero Style - Yellow Damage Numbers

This guide will help you set up the damage display system in your Unity project.

---

## 📋 What's Been Created

Three new scripts have been added to your project:

1. **DamageText.cs** - Handles individual damage number animation
2. **DamageTextManager.cs** - Manages damage text pooling and spawning
3. **EnemyHealth.cs** - Updated to trigger damage display when enemies take damage

---

## 🎨 Setting Up the Damage Text Prefab

### Step 1: Create TextMeshPro Font Asset (m5x7)

**IMPORTANT: Do this FIRST before creating the prefab!**

1. **Import your custom font:**
   - Your m5x7.ttf font is already in `Assets/Fonts/`

2. **Create TextMeshPro Font Asset:**
   - In Unity, go to **Window → TextMeshPro → Font Asset Creator**
   - **Source Font File:** Drag `Assets/Fonts/m5x7.ttf` into this field
   - **Sampling Point Size:** Set to **Auto Sizing** or **32-64** for crisp pixels
   - **Character Set:** Select **ASCII** (or Unicode if you need more characters)
   - **Render Mode:** Select **SDFAA** for sharp edges at any scale
   - **Atlas Resolution:** 512x512 or 1024x1024
   - Click **"Generate Font Atlas"** at the bottom
   - Once generated, click **"Save"** and save as `Assets/Fonts/m5x7 SDF` (or similar name)

3. **Note:** The font asset (m5x7 SDF.asset) will be used in the next step!

### Step 2: Create the Damage Text Prefab

1. **Create a new GameObject:**
   - In the Hierarchy, right-click → Create Empty
   - Name it `DamageText`

2. **Add TextMeshPro component:**
   - Select the `DamageText` GameObject
   - Click "Add Component" → Search for "TextMeshPro - Text"
   - If prompted to import TMP Essentials, click "Import TMP Essentials"

3. **Configure TextMeshPro Settings:**
   - **Font Asset:** Drag your **m5x7 SDF** font asset here (created in Step 1)
   - **Text:** Set to "99" (placeholder)
   - **Font Size:** 4-6 (for world space - pixel fonts look best at specific sizes)
   - **Vertex Color:** Yellow/Gold (#FFE644) - gradient will be applied at runtime
   - **Alignment:** Center (both horizontal and vertical)
   - **Sorting Layer:** Make sure it's above your game objects (e.g., "UI" or "Effects")
   - **Order in Layer:** Set to a high value like 100
   - **Enable/Vertex Gradient:** Can leave OFF - the script will enable it with ZZZ gradient at runtime

4. **Configure TextMeshPro Rect Transform:**
   - **Width:** 5
   - **Height:** 2

5. **Add the DamageText component:**
   - Click "Add Component" → Search for "DamageText"
   - The script will automatically attach

6. **Configure DamageText Component Settings (Optional):**
   - **Float Speed:** 1.5 (how fast text moves up)
   - **Lifetime:** 1.2 seconds (how long the text stays visible)
   - **Scale In Time:** 0.15 seconds (punch-in animation)
   - **Fade Out Time:** 0.4 seconds (fade duration)
   - **Start Scale:** 0.5 (initial size)
   - **Peak Scale:** 1.3 (punch effect peak)
   - **End Scale:** 1.0 (final size)
   - **Horizontal Drift Range:** 0.3 (random side-to-side movement)

7. **Make it a Prefab:**
   - Drag the `DamageText` GameObject from Hierarchy into your `Assets/Prefab` folder
   - Delete the GameObject from the Hierarchy (the prefab is saved)

---

## 🎮 Setting Up the Damage Text Manager

### Step 2: Add Manager to Scene

1. **Create a GameObject for the Manager:**
   - In the Hierarchy, right-click → Create Empty
   - Name it `DamageTextManager`

2. **Add the DamageTextManager component:**
   - Select the `DamageTextManager` GameObject
   - Click "Add Component" → Search for "DamageTextManager"

3. **Configure DamageTextManager Settings:**
   - **Damage Text Prefab:** Drag your `DamageText` prefab here (REQUIRED!)
   - **ZZZ Style Gradient Colors:**
     - **Damage Color Top:** RGB(255, 242, 102) or #FFF266 (bright yellow/white top)
     - **Damage Color Bottom:** RGB(255, 191, 26) or #FFBF1A (deep gold bottom)
     - **Critical Color Top:** RGB(255, 153, 0) or #FF9900 (bright orange)
     - **Critical Color Bottom:** RGB(255, 77, 0) or #FF4D00 (deep orange/red)
   - **Initial Pool Size:** 10 (start with 10 damage texts ready)
   - **Max Pool Size:** 30 (maximum before recycling)
   - **Vertical Offset:** 0.5 (spawn above the enemy)
   - **Horizontal Spread:** 0.2 (random spread for visual variety)

---

## ✅ Testing

### Step 3: Test the Damage Display

1. **Make sure you have:**
   - ✓ DamageText prefab created and saved
   - ✓ DamageTextManager GameObject in your scene
   - ✓ DamageText prefab assigned to DamageTextManager

2. **Play the game:**
   - Start your game
   - Attack an enemy
   - You should see **yellow gradient damage numbers** appear and float upward!
   - The numbers should have a gradient from bright yellow (top) to deep gold (bottom)

3. **If nothing appears, check:**
   - Console for error messages
   - DamageTextManager has the prefab assigned
   - TextMeshPro sorting layer is visible
   - Camera can see the damage text (check Z position and layers)

---

## 🎨 Customization

### Change Gradient Colors

Edit the gradient colors in **DamageTextManager** (ZZZ uses top-to-bottom gradients):

**Yellow (default):**
- Top: RGB(255, 242, 102) - Bright yellow/white
- Bottom: RGB(255, 191, 26) - Deep gold

**Orange (critical):**
- Top: RGB(255, 153, 0) - Bright orange
- Bottom: RGB(255, 77, 0) - Deep orange/red

**Red (aggressive look):**
- Top: RGB(255, 100, 100) - Light red
- Bottom: RGB(200, 0, 0) - Dark red

**White (classic):**
- Top: RGB(255, 255, 255) - Pure white
- Bottom: RGB(200, 200, 200) - Light gray

### Adjust Animation

Edit these values in the **DamageText** component on your prefab:
- **Float Speed:** How fast numbers rise (1.5 = moderate, 2.5 = fast)
- **Lifetime:** How long they stay (1.2 = quick, 2.0 = longer)
- **Peak Scale:** Size punch effect (1.3 = subtle, 1.8 = dramatic)
- **Horizontal Drift:** Side movement (0.3 = subtle, 0.8 = more random)

### Font Customization

**Using m5x7 Font (Already Configured):**
- The m5x7.ttf pixel font is perfect for retro-style damage numbers
- Best sizes: 4, 6, or 8 (pixel fonts look best at specific multiples)
- Already set up in the DamageText prefab!

**Want to use a different font?**
1. Create a new TMP Font Asset (Window → TextMeshPro → Font Asset Creator)
2. Select your DamageText prefab
3. In the TextMeshPro component:
   - Change **Font Asset** to your new TMP font asset
   - Adjust **Font Size** (3-8 recommended for world space)
   - Add **Outline** for better visibility: 
     - In Material Preset dropdown, select "LiberationSans SDF - Outline"
     - Or manually adjust Outline in Material settings

---

## 🔧 Advanced: Critical Hits (Future Implementation)

The system supports critical hits! To use them:

```csharp
// In your damage dealing code:
bool isCritical = Random.value < 0.2f; // 20% crit chance
float damage = isCritical ? baseDamage * 2f : baseDamage;

// Show damage with critical flag
DamageTextManager.Instance.ShowDamage(damage, enemyPosition, isCritical);
```

---

## 📝 Notes

- The system uses **object pooling** for performance - damage texts are reused
- **Sorting layers** are important - make sure damage text renders above everything
- For **3D games**, you may need to adjust the TextMeshPro settings or use TextMeshPro - Text (UI) with a Canvas
- The **Zenless Zone Zero style** features:
  - Smooth pop-up animations with scale punch effect
  - Bright yellow **gradient colors** (light to dark, top to bottom)
  - Pixel-perfect **m5x7 font** for retro aesthetic
  - Floating upward movement with subtle horizontal drift
- **Gradient is applied at runtime** - the DamageText script automatically creates a vertical gradient from top color to bottom color

---

## 🐛 Troubleshooting

### I don't see any damage numbers!

1. Check Console for errors
2. Verify DamageTextManager prefab is assigned
3. Check TextMeshPro sorting layer (should be visible)
4. Make sure DamageTextManager.Instance is not null

### Numbers appear but in the wrong place!

1. Adjust **Vertical Offset** in DamageTextManager (try 0.5 to 1.0)
2. Check your enemy's **pivot point** - damage spawns at transform.position

### Numbers are too small/large!

1. Adjust **Font Size** in TextMeshPro (try 4, 6, or 8 for pixel fonts)
2. Modify **Scale** values in DamageText component
3. For m5x7 font, use sizes that are multiples: 4, 6, 8, 12

### I don't see the gradient effect!

1. The gradient is applied **at runtime** by the script
2. Check that DamageText.cs and DamageTextManager.cs are updated with gradient support
3. In Play Mode, select a damage text object and check the TextMeshPro component - you should see "Enable Vertex Gradient" is ON
4. Make sure you're using the updated scripts (they should have `Show()` method with topColor and bottomColor parameters)

### The font looks blurry!

1. For pixel fonts like m5x7, use **SDFAA** render mode when creating the font asset
2. Set **Point Size** to a multiple of the font's native size (try 32, 48, or 64)
3. In the DamageText prefab, set Font Size to clean multiples: 4, 6, or 8

### Performance issues with many damage numbers?

1. Increase **Initial Pool Size** to reduce runtime instantiation
2. Decrease **Lifetime** so they disappear faster
3. Reduce **Max Pool Size** if you have hundreds of enemies

---

## 🎉 Enjoy!

Your damage display is now set up in Zenless Zone Zero style! The bright yellow **gradient damage numbers** with the **m5x7 pixel font** will pop up whenever enemies take damage, adding visual feedback to your combat system.

### Quick Visual Check:
- ✅ Numbers should be **yellow gradient** (bright at top, gold at bottom)
- ✅ Numbers should use **m5x7 pixel font** (retro/crispy look)
- ✅ Numbers should **float upward** with a pop/punch animation
- ✅ Numbers should **fade out** smoothly at the end

For questions or issues, check the Unity Console for detailed error messages.

---

## 🚀 Quick Setup Summary

**For the impatient - follow these steps in order:**

1. **Create m5x7 TMP Font Asset:**
   - Window → TextMeshPro → Font Asset Creator
   - Source: `Assets/Fonts/m5x7.ttf`
   - Sampling: Auto or 32-64
   - Render Mode: SDFAA
   - Generate & Save as `m5x7 SDF`

2. **Create DamageText Prefab:**
   - Create Empty GameObject named "DamageText"
   - Add TextMeshPro - Text component
   - Set Font Asset to `m5x7 SDF`
   - Set Font Size to 6, Center alignment
   - Add DamageText component
   - Save as prefab in Assets/Prefab

3. **Setup Manager:**
   - Create Empty GameObject named "DamageTextManager"
   - Add DamageTextManager component
   - Assign DamageText prefab
   - Leave gradient colors at default (already ZZZ style!)

4. **Test:**
   - Play game
   - Attack enemy
   - See yellow gradient numbers! 🎉
