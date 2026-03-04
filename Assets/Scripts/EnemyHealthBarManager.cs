using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public Vector2 healthBarWorldSize = new Vector2(1.2f, 0.15f); // Size in world units
    public float canvasWorldScale = 0.01f; // Scale factor for canvas
    
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
            Debug.Log("Creating default world-space health bar prefab...");
            healthBarPrefab = CreateDefaultHealthBarPrefab();
        }
    }

    /// <summary>
    /// Create a default world-space health bar prefab at runtime
    /// </summary>
    private GameObject CreateDefaultHealthBarPrefab()
    {
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
        
        // Set up RectTransform
        RectTransform rectTransform = prefab.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(120f, 18f); // Pixel size (will be scaled)
        
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
        healthImage.type = Image.Type.Filled;
        healthImage.fillMethod = Image.FillMethod.Horizontal;
        healthImage.fillOrigin = (int)Image.OriginHorizontal.Left;
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
        healthBarComponent.healthBarSize = healthBarWorldSize;

        return prefab;
    }

    /// <summary>
    /// Register an enemy to have a health bar
    /// </summary>
    /// <param name="showImmediately">Whether to show the health bar immediately (false = wait for combat)</param>
    public EnemyHealthBar RegisterEnemy(Transform enemyTransform, float currentHealth, float maxHealth, bool showImmediately = false)
    {
        if (activeHealthBars.ContainsKey(enemyTransform))
        {
            // Already registered, just return existing
            return activeHealthBars[enemyTransform];
        }

        // Create new health bar in world space
        GameObject healthBarObj = Instantiate(healthBarPrefab);
        healthBarObj.transform.SetParent(transform); // Parent to manager for organization
        
        EnemyHealthBar healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
        
        if (healthBar == null)
        {
            Debug.LogError("Health bar prefab must have EnemyHealthBar component!");
            Destroy(healthBarObj);
            return null;
        }

        // Initialize health bar with enemy reference (hidden by default for combat)
        healthBar.Initialize(enemyTransform, showImmediately);
        healthBar.SetHealth(currentHealth / maxHealth);

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
        if (activeHealthBars.ContainsKey(enemyTransform))
        {
            activeHealthBars[enemyTransform].SetHealth(currentHealth / maxHealth);
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
