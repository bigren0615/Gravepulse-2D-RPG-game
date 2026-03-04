using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Individual enemy health bar that floats above each enemy
/// Zenless Zone Zero style with line connecting enemy to health bar at top-right
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image healthBarFill;
    public Image healthBarBackground;
    public Image borderImage;
    public RectTransform healthBarContainer;
    public CanvasGroup canvasGroup;
    public Canvas canvas; // Canvas component reference

    [Header("Line Connection")]
    public LineRenderer connectionLine;
    public Transform lineStartPoint; // Point on the health bar (bottom-left)
    public Transform lineEndPoint; // Point on the enemy (center)

    [Header("Positioning")]
    public Vector3 offsetFromEnemy = new Vector3(0.8f, 1.2f, 0f); // Top-right offset
    public float distanceFromEnemy = 1.5f; // How far the health bar floats
    public bool billboardToCamera = true; // Always face camera

    [Header("Settings")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.3f;
    public Vector2 healthBarSize = new Vector2(1.2f, 0.15f); // World space size
    public Color healthColor = new Color(0.2f, 1f, 0.4f); // Bright green
    public Color damageColor = new Color(1f, 0.3f, 0.2f); // Red
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color borderColor = new Color(1f, 1f, 1f, 0.8f);
    public Color lineColor = new Color(0.6f, 0.9f, 1f, 0.6f); // Cyan tint

    [Header("Animation")]
    public float smoothSpeed = 10f;
    public float damageFlashDuration = 0.15f;

    private Transform enemyTransform;
    private Camera mainCamera;
    private float currentHealth = 1f;
    private float targetHealth = 1f;
    private float currentAlpha = 0f;
    private float targetAlpha = 0f;
    private bool isVisible = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Get Canvas component
        canvas = GetComponent<Canvas>();
        
        SetupHealthBar();
        SetupLine();
        
        // Start with everything disabled (but GameObject stays active for coroutines)
        HideCompletely();
    }

    private void SetupHealthBar()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        
        // Set initial colors
        if (healthBarFill != null)
            healthBarFill.color = healthColor;
        
        if (healthBarBackground != null)
            healthBarBackground.color = backgroundColor;
        
        if (borderImage != null)
            borderImage.color = borderColor;
    }

    private void SetupLine()
    {
        if (connectionLine == null)
        {
            GameObject lineObj = new GameObject("ConnectionLine");
            lineObj.transform.SetParent(transform);
            connectionLine = lineObj.AddComponent<LineRenderer>();
        }

        // Configure line renderer for modern look
        connectionLine.startWidth = 0.04f; // Slightly thicker for visibility
        connectionLine.endWidth = 0.02f; // Tapers towards enemy
        connectionLine.positionCount = 2;
        connectionLine.useWorldSpace = true;
        
        // Create material for line
        connectionLine.material = new Material(Shader.Find("Sprites/Default"));
        connectionLine.startColor = lineColor;
        connectionLine.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.3f); // Fade at enemy
        
        // Sorting - render behind health bar but above sprites
        connectionLine.sortingLayerName = "Default";
        connectionLine.sortingOrder = 10;
        
        connectionLine.enabled = false;
    }

    /// <summary>
    /// Initialize the health bar with enemy reference
    /// </summary>
    /// <param name="enemy">The enemy transform to track</param>
    /// <param name="initialHealth">Initial health value (0-1)</param>
    /// <param name="showImmediately">Whether to show the health bar immediately (false = wait for combat)</param>
    public void Initialize(Transform enemy, float initialHealth, bool showImmediately = false)
    {
        enemyTransform = enemy;
        // Set health immediately without lerp animation
        currentHealth = Mathf.Clamp01(initialHealth);
        targetHealth = currentHealth;
        
        if (healthBarFill != null)
            healthBarFill.fillAmount = currentHealth;
        
        if (showImmediately)
        {
            Show();
        }
    }

    /// <summary>
    /// Update health value (0-1)
    /// </summary>
    /// <param name="healthPercent">Health percentage (0-1)</param>
    /// <param name="immediate">If true, sets health immediately without lerp animation</param>
    public void SetHealth(float healthPercent, bool immediate = false)
    {
        targetHealth = Mathf.Clamp01(healthPercent);
        
        // Flash red on damage
        if (healthPercent < currentHealth && !isDead())
        {
            if (healthBarFill != null)
                StartCoroutine(DamageFlash());
        }
        
        // Always update currentHealth and fillAmount immediately
        currentHealth = targetHealth;
        if (healthBarFill != null)
            healthBarFill.fillAmount = currentHealth;
    }

    /// <summary>
    /// Show the health bar
    /// </summary>
    public void Show()
    {
        if (!isVisible)
        {
            isVisible = true;
            targetAlpha = 1f;
            
            // Enable canvas for rendering
            if (canvas != null)
                canvas.enabled = true;
            
            // Enable raycast blocking during display
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
            
            // Enable all Image components
            if (healthBarFill != null)
            {
                healthBarFill.enabled = true;
                healthBarFill.fillAmount = currentHealth;
            }
            if (healthBarBackground != null)
                healthBarBackground.enabled = true;
            if (borderImage != null)
                borderImage.enabled = true;
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeIn());
            
            if (connectionLine != null)
                connectionLine.enabled = true;
        }
    }

    /// <summary>
    /// Hide the health bar
    /// </summary>
    public void Hide()
    {
        if (isVisible)
        {
            isVisible = false;
            targetAlpha = 0f;
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            // Only start coroutine if GameObject is active
            if (gameObject.activeInHierarchy)
            {
                fadeCoroutine = StartCoroutine(FadeOut());
            }
            else
            {
                // If not active, just hide immediately
                HideCompletely();
            }
        }
    }

    private void Update()
    {
        if (enemyTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        // Only update if visible or in the process of fading
        if (!isVisible && canvasGroup != null && canvasGroup.alpha <= 0f)
            return;

        // Follow enemy position
        UpdatePosition();

        // Billboard effect - always face camera
        if (billboardToCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        // Smooth health bar animation
        if (Mathf.Abs(currentHealth - targetHealth) > 0.001f)
        {
            currentHealth = Mathf.Lerp(currentHealth, targetHealth, Time.deltaTime * smoothSpeed);
            
            if (healthBarFill != null)
                healthBarFill.fillAmount = currentHealth;
        }

        // Update line position
        UpdateLinePosition();
    }

    private void UpdatePosition()
    {
        // Position health bar above and to the right of enemy
        Vector3 targetPosition = enemyTransform.position + offsetFromEnemy;
        transform.position = targetPosition;
    }

    private void UpdateLinePosition()
    {
        if (connectionLine == null || !connectionLine.enabled || enemyTransform == null)
            return;

        // Start point: bottom-left corner of health bar
        Vector3 healthBarStartPos = transform.position;
        if (lineStartPoint != null)
            healthBarStartPos = lineStartPoint.position;
        else
            healthBarStartPos = transform.position + new Vector3(-healthBarSize.x * 0.5f, -healthBarSize.y * 0.5f, 0f);
        
        // End point: center-top of enemy
        Vector3 enemyWorldPos = enemyTransform.position + Vector3.up * 0.3f;

        // Set line positions (start at health bar, end at enemy)
        connectionLine.SetPosition(0, healthBarStartPos);
        connectionLine.SetPosition(1, enemyWorldPos);
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeInDuration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        currentAlpha = targetAlpha;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeOutDuration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        currentAlpha = targetAlpha;
        
        HideCompletely();
    }
    
    /// <summary>
    /// Completely hide all visual elements
    /// </summary>
    private void HideCompletely()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        
        if (connectionLine != null)
            connectionLine.enabled = false;
        
        // Disable canvas to prevent any rendering
        if (canvas != null)
            canvas.enabled = false;
        
        // Extra safety: disable all Image components
        if (healthBarFill != null)
            healthBarFill.enabled = false;
        if (healthBarBackground != null)
            healthBarBackground.enabled = false;
        if (borderImage != null)
            borderImage.enabled = false;
    }

    private IEnumerator DamageFlash()
    {
        Color originalColor = healthBarFill.color;
        healthBarFill.color = damageColor;
        
        yield return new WaitForSeconds(damageFlashDuration);
        
        healthBarFill.color = healthColor;
    }

    private bool isDead()
    {
        return targetHealth <= 0f;
    }

    private void OnDestroy()
    {
        if (connectionLine != null)
            Destroy(connectionLine.gameObject);
    }
}
