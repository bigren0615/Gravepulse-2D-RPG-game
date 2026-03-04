using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles individual damage text animation - inspired by Zenless Zone Zero
/// Features: smooth upward float, scale animation, fade out, and slight random movement
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageText : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How fast the text moves upward")]
    public float floatSpeed = 1.5f;
    
    [Tooltip("Total lifetime of the damage text")]
    public float lifetime = 1.2f;
    
    [Tooltip("Time to scale in at the start")]
    public float scaleInTime = 0.15f;
    
    [Tooltip("Time to fade out at the end")]
    public float fadeOutTime = 0.4f;
    
    [Header("Scale Animation")]
    [Tooltip("Starting scale (punch in effect)")]
    public float startScale = 0.5f;
    
    [Tooltip("Peak scale during animation")]
    public float peakScale = 1.3f;
    
    [Tooltip("Final scale")]
    public float endScale = 1.0f;
    
    [Header("Movement")]
    [Tooltip("Random horizontal drift range")]
    public float horizontalDriftRange = 0.3f;
    
    [Tooltip("Curve for vertical movement")]
    public AnimationCurve verticalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private TextMeshPro textMesh;
    private Color originalColor;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float horizontalDrift;
    private bool isAnimating = false;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            originalColor = textMesh.color;
        }
    }

    /// <summary>
    /// Initialize and display damage text with optional gradient
    /// </summary>
    public void Show(float damageAmount, Vector3 worldPosition, Color topColor, Color bottomColor)
    {
        // Set text
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
        
        // Apply gradient (ZZZ style)
        textMesh.enableVertexGradient = true;
        textMesh.colorGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);
        originalColor = topColor; // Use top color for fade calculations
        
        // Set position
        transform.position = worldPosition;
        startPosition = worldPosition;
        
        // Calculate random drift and target position
        horizontalDrift = Random.Range(-horizontalDriftRange, horizontalDriftRange);
        targetPosition = startPosition + new Vector3(horizontalDrift, floatSpeed * lifetime, 0);
        
        // Start animation
        if (isAnimating)
        {
            StopAllCoroutines();
        }
        StartCoroutine(AnimateText());
    }
    
    /// <summary>
    /// Show with solid color (backwards compatibility)
    /// </summary>
    public void Show(float damageAmount, Vector3 worldPosition, Color color)
    {
        Show(damageAmount, worldPosition, color, color);
    }
    
    private IEnumerator AnimateText()
    {
        isAnimating = true;
        float elapsed = 0f;
        
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            
            // === SCALE ANIMATION ===
            float scale;
            if (elapsed < scaleInTime)
            {
                // Scale in with punch effect
                float scaleT = elapsed / scaleInTime;
                scale = Mathf.Lerp(startScale, peakScale, scaleT);
            }
            else
            {
                // Settle to normal scale
                float scaleT = (elapsed - scaleInTime) / (lifetime - scaleInTime);
                scale = Mathf.Lerp(peakScale, endScale, scaleT);
            }
            transform.localScale = Vector3.one * scale;
            
            // === MOVEMENT ===
            // Use curve for smooth vertical movement
            float curveValue = verticalCurve.Evaluate(t);
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, curveValue);
            transform.position = currentPos;
            
            // === FADE OUT ===
            float alpha = 1f;
            if (elapsed > lifetime - fadeOutTime)
            {
                // Fade out in the last portion
                float fadeT = (elapsed - (lifetime - fadeOutTime)) / fadeOutTime;
                alpha = 1f - fadeT;
            }
            
            // Apply alpha to gradient
            if (textMesh.enableVertexGradient)
            {
                VertexGradient gradient = textMesh.colorGradient;
                gradient.topLeft.a = alpha;
                gradient.topRight.a = alpha;
                gradient.bottomLeft.a = alpha;
                gradient.bottomRight.a = alpha;
                textMesh.colorGradient = gradient;
            }
            else
            {
                Color newColor = originalColor;
                newColor.a = alpha;
                textMesh.color = newColor;
            }
            
            yield return null;
        }
        
        isAnimating = false;
        
        // Return to pool
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ReturnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Force stop animation (for pooling)
    /// </summary>
    public void StopAnimation()
    {
        if (isAnimating)
        {
            StopAllCoroutines();
            isAnimating = false;
        }
    }
}
