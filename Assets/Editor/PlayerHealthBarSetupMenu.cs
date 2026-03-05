using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor menu for easy player health bar UI setup
/// </summary>
public class PlayerHealthBarSetupMenu
{
    [MenuItem("GameObject/UI/Player Health Bar", false, 11)]
    public static void CreatePlayerHealthBar()
    {
        // Check if a Canvas exists, if not create one
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Set up Canvas Scaler for FIXED pixel size (won't scale with resolution)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✓ Created Canvas with Constant Pixel Size scaling (TRUE fixed size)");
        }
        else
        {
            // Check existing canvas scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    Debug.Log("✓ Canvas uses Constant Pixel Size - health bar will be fixed size");
                }
                else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    Debug.Log("⚠️ Canvas uses Scale With Screen Size - health bar will scale with resolution");
                    Debug.Log("   To use fixed pixel size, click the 'Fix Canvas Scaler' button in PlayerHealthBar inspector");
                }
            }
            else
            {
                Debug.LogWarning("Canvas exists but has no CanvasScaler. Adding Constant Pixel Size scaler...");
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            }
        }

        // Check if player health bar already exists
        PlayerHealthBarUI existingHealthBar = GameObject.FindObjectOfType<PlayerHealthBarUI>();
        if (existingHealthBar != null)
        {
            Debug.LogWarning("Player Health Bar UI already exists in the scene!");
            Selection.activeGameObject = existingHealthBar.gameObject;
            return;
        }

        // Create player health bar container
        GameObject healthBarContainer = new GameObject("PlayerHealthBar");
        healthBarContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform containerRect = healthBarContainer.AddComponent<RectTransform>();
        
        // Position at top-left corner with some padding (will be managed by script)
        containerRect.anchorMin = new Vector2(0f, 1f); // Top-left
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = new Vector2(20f, -20f); // Default 20px padding
        containerRect.sizeDelta = new Vector2(200f, 30f); // Default size (adjustable in inspector)

        // Create background image
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBarContainer.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark background
        bgImage.sprite = CreateWhiteSprite();

        // Create border/box image (must be before Fill so Fill renders on top)
        GameObject box = new GameObject("Box");
        box.transform.SetParent(healthBarContainer.transform, false);
        RectTransform boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = Vector2.zero;
        boxRect.anchorMax = Vector2.one;
        boxRect.sizeDelta = Vector2.zero;
        boxRect.anchoredPosition = Vector2.zero;
        
        Image boxImage = box.AddComponent<Image>();
        boxImage.color = Color.white; // White to show sprite's native color
        boxImage.sprite = CreateWhiteSprite();
        boxImage.type = Image.Type.Sliced;

        // Create fill image (must be after Box so it renders on top)
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(healthBarContainer.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-4f, -4f); // 2px inset on all sides
        fillRect.anchoredPosition = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.white; // White to show sprite's native color
        fillImage.sprite = CreateWhiteSprite();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        
        // Add the PlayerHealthBarUI component
        PlayerHealthBarUI healthBarUI = healthBarContainer.AddComponent<PlayerHealthBarUI>();
        healthBarUI.healthBarBackground = bgImage;
        healthBarUI.healthBarFill = fillImage;
        healthBarUI.healthBarBox = boxImage;
        healthBarUI.useNativeSpriteColors = true; // Enable by default to prevent black bar issue
        
        // Set default fill padding (adjustable in inspector)
        healthBarUI.fillPaddingLeft = 2f;
        healthBarUI.fillPaddingRight = 2f;
        healthBarUI.fillPaddingTop = 2f;
        healthBarUI.fillPaddingBottom = 2f;
        
        // Apply the padding settings
        healthBarUI.ApplySizeAndPosition();

        // Select the created object
        Selection.activeGameObject = healthBarContainer;

        Debug.Log("════════════════════════════════════════════════");
        Debug.Log("✓ Player Health Bar UI created successfully!");
        Debug.Log("✓ Position: Top-left corner of screen");
        Debug.Log("✓ Size Mode: CONSTANT PIXEL SIZE (won't change with resolution)");
        Debug.Log("✓ 'Use Native Sprite Colors' is enabled by default");
        Debug.Log("");
        Debug.Log("Configure in Inspector:");
        Debug.Log("  - Adjust 'Health Bar Width' and 'Health Bar Height' to resize");
        Debug.Log("  - Adjust 'Padding Left' and 'Padding Top' to reposition");
        Debug.Log("  - Adjust 'Fill Padding' values to control spacing from box border");
        Debug.Log("  - Assign custom sprites to Background, Fill, and Box child objects");
        Debug.Log("  - Each child has an Image component - set its 'Source Image' field");
        Debug.Log("");
        Debug.Log("IMPORTANT - Editor vs Game:");
        Debug.Log("  - The Game view zoom (1x, 2x, etc.) affects preview size");
        Debug.Log("  - Set Game view to '1x' to see actual size");
        Debug.Log("  - In actual builds, the size will be exactly as specified in pixels");
        Debug.Log("════════════════════════════════════════════════");
    }

    [MenuItem("Tools/Player Health Bar/Setup Player Health Bar UI")]
    public static void SetupFromMenu()
    {
        CreatePlayerHealthBar();
    }

    [MenuItem("Tools/Player Health Bar/Select Player Health Bar")]
    public static void SelectPlayerHealthBar()
    {
        PlayerHealthBarUI healthBar = GameObject.FindObjectOfType<PlayerHealthBarUI>();
        if (healthBar != null)
        {
            Selection.activeGameObject = healthBar.gameObject;
            Debug.Log("Selected Player Health Bar UI");
        }
        else
        {
            Debug.LogWarning("No Player Health Bar UI found in scene. Create one first!");
        }
    }

    [MenuItem("Tools/Player Health Bar/Select Player Health Bar", true)]
    public static bool SelectPlayerHealthBarValidation()
    {
        return GameObject.FindObjectOfType<PlayerHealthBarUI>() != null;
    }

    /// <summary>
    /// Create a simple white sprite for UI elements
    /// </summary>
    private static Sprite CreateWhiteSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }
}
