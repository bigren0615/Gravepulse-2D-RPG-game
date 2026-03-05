using System.Collections;
using UnityEngine;

/// <summary>
/// Player health system - handles health, damage, death, and visual effects
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Visual Effects")]
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.1f;
    public int flashCount = 2;

    [Header("Invincibility")]
    [Tooltip("Invincibility time after taking damage (in seconds)")]
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    // Component references
    private PlayerController controller;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;

        controller = GetComponent<PlayerController>();
        spriteRenderer = controller.spriteRenderer;
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else
            Debug.LogError("Player needs a SpriteRenderer!");
    }

    /// <summary>
    /// Apply damage to the player
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Don't take damage if already dead or invincible
        if (isDead || isInvincible) return;

        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! Current HP: {currentHealth}/{maxHealth}");

        // Play damage sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Punch1);
        }

        // Visual feedback
        if (!isFlashing)
        {
            StartCoroutine(DamageFlash());
        }

        // Start invincibility
        StartCoroutine(InvincibilityCoroutine());

        // Check for death
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Player healed {amount}! Current HP: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Get current health
    /// </summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Get max health
    /// </summary>
    public float GetMaxHealth() => maxHealth;

    /// <summary>
    /// Check if player is dead
    /// </summary>
    public bool IsDead() => isDead;

    /// <summary>
    /// Check if player is currently invincible
    /// </summary>
    public bool IsInvincible() => isInvincible;

    private IEnumerator DamageFlash()
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = damageFlashColor;

            yield return new WaitForSeconds(flashDuration);

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            yield return new WaitForSeconds(flashDuration);
        }

        isFlashing = false;
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player has died!");

        // Stop player movement
        if (controller != null)
        {
            controller.moveSpeed = 0f;
        }

        // Disable player controls and colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        // Play death animation if available
        if (animator != null)
        {
            // Check if the animator has a "Dead" trigger
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "Dead" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("Dead");
                    return;
                }
            }

            // If no Dead trigger, try isDead boolean
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "isDead" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("isDead", true);
                    return;
                }
            }

            Debug.LogWarning("No 'Dead' trigger or 'isDead' bool found in animator!");
        }

        // TODO: Game over logic, respawn, etc.
    }
}
