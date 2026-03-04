using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy health system - handles health, damage, death, and visual effects
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Header("Health Bar")]
    [Tooltip("Size category for this enemy's health bar")]
    public EnemySize enemySize = EnemySize.Medium;

    [Header("Visual Effects")]
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.1f;
    public int flashCount = 2;
    
    private Color originalColor;
    private bool isFlashing = false;
    private bool isDead = false;

    // Component references
    private EnemyController controller;
    private EnemyCombat combat;
    private EnemyHealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        
        controller = GetComponent<EnemyController>();
        combat = GetComponent<EnemyCombat>();
        
        if (controller.spriteRenderer != null)
            originalColor = controller.spriteRenderer.color;
        else
            Debug.LogError("Enemy needs a SpriteRenderer!");

        RegisterHealthBar();
    }

    private void RegisterHealthBar()
    {
        if (EnemyHealthBarManager.Instance != null)
        {
            healthBar = EnemyHealthBarManager.Instance.RegisterEnemy(
                transform, 
                currentHealth, 
                maxHealth, 
                enemySize, 
                showImmediately: false
            );
        }
        else
        {
            Debug.LogWarning("EnemyHealthBarManager not found in scene! Health bars will not appear.");
        }
    }

    /// <summary>
    /// Apply damage to this enemy
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " took " + damage + " damage! Current HP: " + currentHealth + "/" + maxHealth);

        UpdateHealthBar();

        if (!isFlashing)
        {
            StartCoroutine(DamageFlash());
        }

        // Enter combat when hit
        if (combat != null && !combat.IsInCombat())
        {
            combat.EnterCombat();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Heal the enemy
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
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
    /// Check if enemy is dead
    /// </summary>
    public bool IsDead() => isDead;

    /// <summary>
    /// Get the health bar reference
    /// </summary>
    public EnemyHealthBar GetHealthBar() => healthBar;

    private void UpdateHealthBar()
    {
        if (EnemyHealthBarManager.Instance != null)
        {
            EnemyHealthBarManager.Instance.UpdateEnemyHealth(transform, currentHealth, maxHealth);
        }
    }

    private IEnumerator DamageFlash()
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            if (controller.spriteRenderer != null)
                controller.spriteRenderer.color = damageFlashColor;

            yield return new WaitForSeconds(flashDuration);

            if (controller.spriteRenderer != null)
                controller.spriteRenderer.color = originalColor;

            yield return new WaitForSeconds(flashDuration);
        }

        isFlashing = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " has died!");
        
        // Unregister health bar
        if (EnemyHealthBarManager.Instance != null)
        {
            EnemyHealthBarManager.Instance.UnregisterEnemy(transform);
        }
        
        // Exit combat when dying
        if (combat != null && combat.IsInCombat())
        {
            combat.ExitCombat();
        }

        // Disable movement
        controller.speed = 0f;
        controller.Stop();

        // Disable colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        // Play death animation
        if (controller.animator != null)
        {
            controller.animator.SetTrigger("Dead");
            
            Vector2 facingDir = controller.GetFacingDirection();
            controller.animator.SetFloat("moveX", Mathf.Abs(facingDir.x));
            controller.animator.SetFloat("moveY", facingDir.y);

            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            Destroy(gameObject, 1f);
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(0.1f);

        AnimatorStateInfo stateInfo = controller.animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        
        yield return new WaitForSeconds(animationLength);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Unregister health bar when destroyed
        if (EnemyHealthBarManager.Instance != null)
        {
            EnemyHealthBarManager.Instance.UnregisterEnemy(transform);
        }
        
        // Make sure to exit combat
        if (combat != null && combat.IsInCombat() && GameManager.Instance != null)
        {
            GameManager.Instance.ExitCombat(gameObject);
        }
    }
}
