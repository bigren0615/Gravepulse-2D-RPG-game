using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enemy size categories for health bar scaling
/// </summary>
public enum EnemySize
{
    Small,      // Small enemies (slimes, rats, etc.)
    Medium,     // Standard enemies (default)
    Large,      // Large enemies (ogres, golems, etc.)
    Boss,       // Boss enemies (extra wide health bars)
    Custom      // Use custom size
}

/// <summary>
/// Manages all enemy health bars
/// Creates world-space health bars that float above each enemy (Zenless Zone Zero style)
/// </summary>
public class EnemyHealthBarManager : MonoBehaviour
{
    public static EnemyHealthBarManager Instance { get; private set; }

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab;
    
    [Header("World Space Settings")]
    public Vector3 offsetFromEnemy = new Vector3(0.8f, 1.2f, 0f); // Top-right offset from enemy
    public Vector2 healthBarWorldSize = new Vector2(1.2f, 0.15f); // Default size (Medium)
    public float canvasWorldScale = 0.01f; // Scale factor for canvas
    
    [Header("Size Presets (Width, Height)")]
    [Tooltip("Health bar size for small enemies (slimes, rats, etc.) - Default: (0.8, 0.12)")]
    public Vector2 smallEnemySize = new Vector2(0.8f, 0.12f);
    
    [Tooltip("Health bar size for medium/standard enemies - Default: (1.2, 0.15)")]
    public Vector2 mediumEnemySize = new Vector2(1.2f, 0.15f);
    
    [Tooltip("Health bar size for large enemies (ogres, golems, etc.) - Default: (1.6, 0.18)")]
    public Vector2 largeEnemySize = new Vector2(1.6f, 0.18f);
    
    [Tooltip("Health bar size for boss enemies - Default: (2.4, 0.22)")]
    public Vector2 bossEnemySize = new Vector2(2.4f, 0.22f);
    
    private Dictionary<Transform, EnemyHealthBar> activeHealthBars = new Dictionary<Transform, EnemyHealthBar>();
    private Camera mainCamera;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCamera = Camera.main;
        CreateHealthBarPrefabIfNeeded();
    }

    private void CreateHealthBarPrefabIfNeeded()
    {
        if (healthBarPrefab == null)
        {
            healthBarPrefab = CreateDefaultHealthBarPrefab();
        }
    }

    /// <summary>
    /// Create a simple white sprite for UI elements
    /// </summary>
    private Sprite CreateWhiteSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }

    /// <summary>
    /// Get the size for a specific enemy type
    /// </summary>
    private Vector2 GetSizeForEnemyType(EnemySize enemySize, Vector2? customSize = null)
    {
        switch (enemySize)
        {
            case EnemySize.Small:
                return smallEnemySize;
            case EnemySize.Medium:
                return mediumEnemySize;
            case EnemySize.Large:
                return largeEnemySize;
            case EnemySize.Boss:
                return bossEnemySize;
            case EnemySize.Custom:
                return customSize ?? mediumEnemySize;
            default:
                return mediumEnemySize;
        }
    }

    /// <summary>
    /// Create a default world-space health bar prefab at runtime
    /// </summary>
    private GameObject CreateDefaultHealthBarPrefab()
    {
        return CreateHealthBarPrefabWithSize(healthBarWorldSize);
    }

    /// <summary>
    /// Create a health bar prefab with specific size
    /// </summary>
    private GameObject CreateHealthBarPrefabWithSize(Vector2 barSize)
    {
        // Create a white sprite for all UI images
        Sprite whiteSprite = CreateWhiteSprite();
        
        // Create main health bar object
        GameObject prefab = new GameObject("EnemyHealthBar_WorldSpace");
        
        // Add canvas for world-space rendering
        Canvas canvas = prefab.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Add canvas scaler for consistent sizing
        CanvasScaler scaler = prefab.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        
        // Add graphic raycaster (for potential UI interaction)
        GraphicRaycaster raycaster = prefab.AddComponent<GraphicRaycaster>();
        
        // Set up RectTransform - convert world size to pixel size based on scale
        RectTransform rectTransform = prefab.GetComponent<RectTransform>();
        // Calculate pixel size: world size / canvas scale
        float pixelWidth = barSize.x / canvasWorldScale;
        float pixelHeight = barSize.y / canvasWorldScale;
        rectTransform.sizeDelta = new Vector2(pixelWidth, pixelHeight);
        
        // Scale canvas to world size
        prefab.transform.localScale = Vector3.one * canvasWorldScale;

        // Add canvas group for fading
        CanvasGroup canvasGroup = prefab.AddComponent<CanvasGroup>();

        // Create border (outer glow)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(prefab.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3f, -3f);
        borderRect.offsetMax = new Vector2(3f, 3f);
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.sprite = whiteSprite;
        borderImage.color = new Color(1f, 1f, 1f, 0.4f);

        // Create background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(prefab.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = whiteSprite;
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Create health bar fill container
        GameObject healthBarObj = new GameObject("HealthBarFill");
        healthBarObj.transform.SetParent(prefab.transform, false);
        RectTransform healthRect = healthBarObj.AddComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0f, 0f);
        healthRect.anchorMax = new Vector2(1f, 1f);
        healthRect.offsetMin = new Vector2(3f, 3f);
        healthRect.offsetMax = new Vector2(-3f, -3f);
        
        Image healthImage = healthBarObj.AddComponent<Image>();
        healthImage.sprite = whiteSprite;
        healthImage.type = Image.Type.Filled;
        healthImage.fillMethod = Image.FillMethod.Horizontal;
        healthImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthImage.fillAmount = 1.0f; // Initialize to full
        healthImage.color = new Color(0.2f, 1f, 0.4f); // Bright green

        // Create line start point (bottom-left corner)
        GameObject lineStartObj = new GameObject("LineStartPoint");
        lineStartObj.transform.SetParent(prefab.transform, false);
        RectTransform lineStartRect = lineStartObj.AddComponent<RectTransform>();
        lineStartRect.anchorMin = new Vector2(0f, 0f);
        lineStartRect.anchorMax = new Vector2(0f, 0f);
        lineStartRect.pivot = new Vector2(0f, 0f);
        lineStartRect.anchoredPosition = Vector2.zero;

        // Add health bar component
        EnemyHealthBar healthBarComponent = prefab.AddComponent<EnemyHealthBar>();
        healthBarComponent.healthBarFill = healthImage;
        healthBarComponent.healthBarBackground = bgImage;
        healthBarComponent.borderImage = borderImage;
        healthBarComponent.healthBarContainer = rectTransform;
        healthBarComponent.canvasGroup = canvasGroup;
        healthBarComponent.canvas = canvas; // Set canvas reference
        healthBarComponent.lineStartPoint = lineStartObj.transform;
        healthBarComponent.offsetFromEnemy = offsetFromEnemy;
        healthBarComponent.healthBarSize = barSize;

        return prefab;
    }

    /// <summary>
    /// Register an enemy to have a health bar (uses Medium size by default)
    /// </summary>
    /// <param name="showImmediately">Whether to show the health bar immediately (false = wait for combat)</param>
    public EnemyHealthBar RegisterEnemy(Transform enemyTransform, float currentHealth, float maxHealth, bool showImmediately = false)
    {
        return RegisterEnemy(enemyTransform, currentHealth, maxHealth, EnemySize.Medium, showImmediately);
    }

    /// <summary>
    /// Register an enemy to have a health bar with specific size preset
    /// </summary>
    /// <param name="enemySize">Size category for the enemy (Small, Medium, Large, Boss)</param>
    /// <param name="showImmediately">Whether to show the health bar immediately (false = wait for combat)</param>
    public EnemyHealthBar RegisterEnemy(Transform enemyTransform, float currentHealth, float maxHealth, EnemySize enemySize, bool showImmediately = false)
    {
        return RegisterEnemyWithCustomSize(enemyTransform, currentHealth, maxHealth, enemySize, null, showImmediately);
    }

    /// <summary>
    /// Register an enemy to have a health bar with custom size
    /// </summary>
    /// <param name="customSize">Custom size for the health bar (use EnemySize.Custom)</param>
    /// <param name="showImmediately">Whether to show the health bar immediately (false = wait for combat)</param>
    public EnemyHealthBar RegisterEnemyWithCustomSize(Transform enemyTransform, float currentHealth, float maxHealth, Vector2 customSize, bool showImmediately = false)
    {
        return RegisterEnemyWithCustomSize(enemyTransform, currentHealth, maxHealth, EnemySize.Custom, customSize, showImmediately);
    }

    /// <summary>
    /// Internal method to register enemy with full control over size
    /// </summary>
    private EnemyHealthBar RegisterEnemyWithCustomSize(Transform enemyTransform, float currentHealth, float maxHealth, EnemySize enemySize, Vector2? customSize, bool showImmediately = false)
    {
        if (activeHealthBars.ContainsKey(enemyTransform))
        {
            // Already registered, just return existing
            return activeHealthBars[enemyTransform];
        }

        // Get the appropriate size for this enemy
        Vector2 barSize = GetSizeForEnemyType(enemySize, customSize);
        
        // Create new health bar with the appropriate size
        GameObject prefabToUse = CreateHealthBarPrefabWithSize(barSize);
        GameObject healthBarObj = Instantiate(prefabToUse);
        Destroy(prefabToUse); // Clean up the temporary prefab
        healthBarObj.transform.SetParent(transform); // Parent to manager for organization
        
        EnemyHealthBar healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
        
        if (healthBar == null)
        {
            Debug.LogError("Health bar prefab must have EnemyHealthBar component!");
            Destroy(healthBarObj);
            return null;
        }

        // Initialize health bar with enemy reference and initial health (hidden by default for combat)
        float initialHealthPercent = currentHealth / maxHealth;
        healthBar.Initialize(enemyTransform, initialHealthPercent, showImmediately);

        // Store reference
        activeHealthBars[enemyTransform] = healthBar;

        return healthBar;
    }

    /// <summary>
    /// Unregister an enemy (when defeated or destroyed)
    /// </summary>
    public void UnregisterEnemy(Transform enemyTransform)
    {
        if (activeHealthBars.ContainsKey(enemyTransform))
        {
            EnemyHealthBar healthBar = activeHealthBars[enemyTransform];
            if (healthBar != null)
            {
                healthBar.Hide();
                Destroy(healthBar.gameObject, 0.5f);
            }
            activeHealthBars.Remove(enemyTransform);
        }
    }

    /// <summary>
    /// Update health for specific enemy
    /// </summary>
    public void UpdateEnemyHealth(Transform enemyTransform, float currentHealth, float maxHealth)
    {
        float healthPercent = currentHealth / maxHealth;
        
        if (activeHealthBars.ContainsKey(enemyTransform))
        {
            EnemyHealthBar healthBar = activeHealthBars[enemyTransform];
            
            if (healthBar != null)
            {
                healthBar.SetHealth(healthPercent);
            }
            else
            {
                // Health bar was destroyed, remove and recreate
                activeHealthBars.Remove(enemyTransform);
                RegisterEnemy(enemyTransform, currentHealth, maxHealth, showImmediately: true);
            }
        }
        else
        {
            // Enemy not registered yet, register and show it (taking damage means in combat)
            RegisterEnemy(enemyTransform, currentHealth, maxHealth, showImmediately: true);
        }
    }

    /// <summary>
    /// Get health bar for specific enemy
    /// </summary>
    public EnemyHealthBar GetHealthBar(Transform enemyTransform)
    {
        if (activeHealthBars.ContainsKey(enemyTransform))
            return activeHealthBars[enemyTransform];
        return null;
    }

    /// <summary>
    /// Clear all health bars
    /// </summary>
    public void ClearAllHealthBars()
    {
        foreach (var kvp in activeHealthBars)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        activeHealthBars.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
