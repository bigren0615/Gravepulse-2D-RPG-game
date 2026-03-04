using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable component for displaying emotion/status bubbles above any GameObject.
/// Can be used on enemies, NPCs, players, or any other entity.
/// </summary>
public class BubbleController : MonoBehaviour
{
    [Header("Bubble Prefabs")]
    [Tooltip("Prefab for the suspense/alert bubble (!)")]
    public GameObject suspenseBubblePrefab;
    
    [Tooltip("Prefab for the question/confused bubble (?)")]
    public GameObject questionBubblePrefab;
    
    [Tooltip("Add more bubble types here as needed")]
    public GameObject exclamationBubblePrefab;
    public GameObject heartBubblePrefab;
    public GameObject angerBubblePrefab;
    
    [Header("Settings")]
    [Tooltip("Offset position above the entity (local space)")]
    public Vector3 bubbleOffset = new Vector3(0f, 0.8f, 0f);
    
    [Tooltip("If true, bubbles will follow the entity as it moves")]
    public bool followEntity = true;
    
    [Tooltip("Fallback duration if bubble has no animator or animation clip")]
    public float defaultBubbleDuration = 1f;
    
    [Tooltip("Prevent the same bubble type from spawning multiple times simultaneously")]
    public bool preventDuplicates = true;
    
    // Active bubble tracking
    private GameObject currentSuspenseBubble = null;
    private GameObject currentQuestionBubble = null;
    private GameObject currentExclamationBubble = null;
    private GameObject currentHeartBubble = null;
    private GameObject currentAngerBubble = null;
    
    /// <summary>
    /// Shows a suspense/alert bubble (!) above the entity
    /// </summary>
    public void ShowSuspenseBubble()
    {
        ShowBubble(suspenseBubblePrefab, ref currentSuspenseBubble);
    }
    
    /// <summary>
    /// Shows a question/confused bubble (?) above the entity
    /// </summary>
    public void ShowQuestionBubble()
    {
        ShowBubble(questionBubblePrefab, ref currentQuestionBubble);
    }
    
    /// <summary>
    /// Shows an exclamation bubble above the entity
    /// </summary>
    public void ShowExclamationBubble()
    {
        ShowBubble(exclamationBubblePrefab, ref currentExclamationBubble);
    }
    
    /// <summary>
    /// Shows a heart bubble above the entity
    /// </summary>
    public void ShowHeartBubble()
    {
        ShowBubble(heartBubblePrefab, ref currentHeartBubble);
    }
    
    /// <summary>
    /// Shows an anger bubble above the entity
    /// </summary>
    public void ShowAngerBubble()
    {
        ShowBubble(angerBubblePrefab, ref currentAngerBubble);
    }
    
    /// <summary>
    /// Generic method to show any bubble
    /// </summary>
    /// <param name="bubblePrefab">The prefab to instantiate</param>
    /// <param name="currentBubbleRef">Reference to track the current bubble instance</param>
    private void ShowBubble(GameObject bubblePrefab, ref GameObject currentBubbleRef)
    {
        // Validation
        if (bubblePrefab == null)
        {
            Debug.LogWarning($"[BubbleController] Bubble prefab not assigned on {gameObject.name}");
            return;
        }
        
        // Check for duplicates
        if (preventDuplicates && currentBubbleRef != null)
        {
            // Bubble already active, don't spawn another
            return;
        }
        
        // Destroy existing bubble if any
        if (currentBubbleRef != null)
        {
            Destroy(currentBubbleRef);
            currentBubbleRef = null;
        }
        
        // Calculate spawn position
        Vector3 spawnPosition = transform.position + bubbleOffset;
        
        // Instantiate bubble
        if (followEntity)
        {
            // Parent to this transform so it follows the entity
            currentBubbleRef = Instantiate(bubblePrefab, spawnPosition, Quaternion.identity, transform);
        }
        else
        {
            // Spawn at position without parenting
            currentBubbleRef = Instantiate(bubblePrefab, spawnPosition, Quaternion.identity);
        }
        
        // Get animation duration and auto-destroy
        float bubbleDuration = GetBubbleDuration(currentBubbleRef);
        Destroy(currentBubbleRef, bubbleDuration);
        
        // Clear reference after destruction
        StartCoroutine(ClearBubbleReference(currentBubbleRef, bubbleDuration));
    }
    
    /// <summary>
    /// Gets the duration of a bubble's animation, or returns default duration
    /// </summary>
    private float GetBubbleDuration(GameObject bubble)
    {
        if (bubble == null)
            return defaultBubbleDuration;
        
        Animator bubbleAnimator = bubble.GetComponent<Animator>();
        
        if (bubbleAnimator != null && bubbleAnimator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = bubbleAnimator.runtimeAnimatorController.animationClips;
            
            if (clips.Length > 0)
            {
                // Play the first animation clip
                bubbleAnimator.Play(clips[0].name, 0, 0f);
                return clips[0].length;
            }
        }
        
        // Fallback to default duration
        return defaultBubbleDuration;
    }
    
    /// <summary>
    /// Clears bubble reference after it's destroyed
    /// </summary>
    private IEnumerator ClearBubbleReference(GameObject bubbleInstance, float delay)
    {
        // Wait for bubble to be destroyed
        yield return new WaitForSeconds(delay + 0.1f);
        
        // Clear the appropriate reference
        if (currentSuspenseBubble == bubbleInstance)
            currentSuspenseBubble = null;
        else if (currentQuestionBubble == bubbleInstance)
            currentQuestionBubble = null;
        else if (currentExclamationBubble == bubbleInstance)
            currentExclamationBubble = null;
        else if (currentHeartBubble == bubbleInstance)
            currentHeartBubble = null;
        else if (currentAngerBubble == bubbleInstance)
            currentAngerBubble = null;
    }
    
    /// <summary>
    /// Hides/destroys the currently active suspense bubble
    /// </summary>
    public void HideSuspenseBubble()
    {
        HideBubble(ref currentSuspenseBubble);
    }
    
    /// <summary>
    /// Hides/destroys the currently active question bubble
    /// </summary>
    public void HideQuestionBubble()
    {
        HideBubble(ref currentQuestionBubble);
    }
    
    /// <summary>
    /// Hides all currently active bubbles
    /// </summary>
    public void HideAllBubbles()
    {
        HideBubble(ref currentSuspenseBubble);
        HideBubble(ref currentQuestionBubble);
        HideBubble(ref currentExclamationBubble);
        HideBubble(ref currentHeartBubble);
        HideBubble(ref currentAngerBubble);
    }
    
    /// <summary>
    /// Generic method to hide/destroy a bubble
    /// </summary>
    private void HideBubble(ref GameObject bubbleRef)
    {
        if (bubbleRef != null)
        {
            Destroy(bubbleRef);
            bubbleRef = null;
        }
    }
    
    /// <summary>
    /// Check if any bubble is currently active
    /// </summary>
    public bool HasActiveBubble()
    {
        return currentSuspenseBubble != null 
            || currentQuestionBubble != null 
            || currentExclamationBubble != null
            || currentHeartBubble != null
            || currentAngerBubble != null;
    }
    
    /// <summary>
    /// Check if a specific bubble type is currently active
    /// </summary>
    public bool IsSuspenseBubbleActive() => currentSuspenseBubble != null;
    public bool IsQuestionBubbleActive() => currentQuestionBubble != null;
    public bool IsExclamationBubbleActive() => currentExclamationBubble != null;
    public bool IsHeartBubbleActive() => currentHeartBubble != null;
    public bool IsAngerBubbleActive() => currentAngerBubble != null;
}
