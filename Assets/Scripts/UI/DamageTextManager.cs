using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages damage text pooling and spawning
/// Singleton pattern for easy access from anywhere
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Prefab Reference")]
    [Tooltip("Drag the DamageText prefab here")]
    public GameObject damageTextPrefab;

    [Header("ZZZ Style Settings")]
    [Tooltip("Top color for gradient (Zenless Zone Zero style)")]
    public Color damageColorTop = new Color(1f, 0.95f, 0.4f); // Bright yellow/white
    
    [Tooltip("Bottom color for gradient (Zenless Zone Zero style)")]
    public Color damageColorBottom = new Color(1f, 0.75f, 0.1f); // Deep yellow/gold
    
    [Tooltip("Critical hit top color (optional)")]
    public Color criticalColorTop = new Color(1f, 0.6f, 0f); // Bright orange
    
    [Tooltip("Critical hit bottom color (optional)")]
    public Color criticalColorBottom = new Color(1f, 0.3f, 0f); // Deep orange/red

    [Header("Pooling")]
    [Tooltip("Initial pool size")]
    public int initialPoolSize = 10;
    
    [Tooltip("Maximum pool size before destroying excess")]
    public int maxPoolSize = 30;

    [Header("Positioning")]
    [Tooltip("Vertical offset from hit position")]
    public float verticalOffset = 0.5f;
    
    [Tooltip("Random horizontal spread")]
    public float horizontalSpread = 0.2f;

    // Pool of damage text objects
    private List<DamageText> pool = new List<DamageText>();
    private Transform poolContainer;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DamageTextManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create pool container
        poolContainer = new GameObject("DamageTextPool").transform;
        poolContainer.SetParent(transform);

        // Validate prefab
        if (damageTextPrefab == null)
        {
            Debug.LogError("DamageTextManager: damageTextPrefab is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (damageTextPrefab.GetComponent<DamageText>() == null)
        {
            Debug.LogError("DamageTextManager: damageTextPrefab must have a DamageText component!");
            return;
        }

        // Pre-populate pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewDamageText();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Show damage text at a world position
    /// </summary>
    public void ShowDamage(float damageAmount, Vector3 worldPosition, bool isCritical = false)
    {
        DamageText damageText = GetFromPool();
        if (damageText == null)
        {
            Debug.LogWarning("Failed to get damage text from pool!");
            return;
        }

        // Apply offset and random spread
        Vector3 spawnPosition = worldPosition;
        spawnPosition.y += verticalOffset;
        spawnPosition.x += Random.Range(-horizontalSpread, horizontalSpread);

        // Choose gradient colors
        Color topColor = isCritical ? criticalColorTop : damageColorTop;
        Color bottomColor = isCritical ? criticalColorBottom : damageColorBottom;

        // Activate and show
        damageText.gameObject.SetActive(true);
        damageText.Show(damageAmount, spawnPosition, topColor, bottomColor);
    }

    /// <summary>
    /// Get a damage text from the pool or create a new one
    /// </summary>
    private DamageText GetFromPool()
    {
        // Find inactive damage text in pool
        foreach (DamageText damageText in pool)
        {
            if (!damageText.gameObject.activeInHierarchy)
            {
                return damageText;
            }
        }

        // If pool is full, try to reuse oldest
        if (pool.Count >= maxPoolSize)
        {
            Debug.LogWarning($"Damage text pool is full ({maxPoolSize}). Consider increasing maxPoolSize or reducing damage frequency.");
            // Return the first one and let it reset
            DamageText oldest = pool[0];
            oldest.StopAnimation();
            oldest.gameObject.SetActive(false);
            return oldest;
        }

        // Create new one
        return CreateNewDamageText();
    }

    /// <summary>
    /// Create a new damage text and add to pool
    /// </summary>
    private DamageText CreateNewDamageText()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogError("Cannot create damage text - prefab is null!");
            return null;
        }

        GameObject obj = Instantiate(damageTextPrefab, poolContainer);
        obj.SetActive(false);
        
        DamageText damageText = obj.GetComponent<DamageText>();
        if (damageText != null)
        {
            pool.Add(damageText);
        }
        else
        {
            Debug.LogError("Created damage text object doesn't have DamageText component!");
            Destroy(obj);
            return null;
        }

        return damageText;
    }

    /// <summary>
    /// Return damage text to pool
    /// </summary>
    public void ReturnToPool(DamageText damageText)
    {
        if (damageText != null && damageText.gameObject != null)
        {
            damageText.gameObject.SetActive(false);
            damageText.transform.SetParent(poolContainer);
        }
    }

    /// <summary>
    /// Clear and reset the pool
    /// </summary>
    public void ClearPool()
    {
        foreach (DamageText damageText in pool)
        {
            if (damageText != null && damageText.gameObject != null)
            {
                Destroy(damageText.gameObject);
            }
        }
        pool.Clear();

        // Recreate initial pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewDamageText();
        }
    }

    /// <summary>
    /// Get current pool statistics
    /// </summary>
    public void LogPoolStats()
    {
        int active = 0;
        int inactive = 0;

        foreach (DamageText damageText in pool)
        {
            if (damageText != null && damageText.gameObject != null)
            {
                if (damageText.gameObject.activeInHierarchy)
                    active++;
                else
                    inactive++;
            }
        }

        Debug.Log($"Damage Text Pool: {pool.Count} total | {active} active | {inactive} inactive");
    }
}
