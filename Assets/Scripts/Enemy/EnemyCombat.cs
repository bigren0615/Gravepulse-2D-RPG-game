using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy combat coordinator - manages combat state, attack behavior, and GameManager integration
/// </summary>
[RequireComponent(typeof(EnemyController), typeof(EnemyHealth))]
public class EnemyCombat : MonoBehaviour
{
    [Header("Combat Mode Settings")]
    [Tooltip("Distance from player to enter combat mode instead of chase mode")]
    public float combatModeRadius = 2f;
    
    [Header("Attack Settings")]
    [Tooltip("Range where enemy can hit the player")]
    public float attackHitboxRadius = 1.2f;
    
    [Tooltip("Minimum time between attacks")]
    public float minAttackInterval = 1f;
    
    [Tooltip("Maximum time between attacks")]
    public float maxAttackInterval = 3f;
    
    [Tooltip("Time before attack to call readyAttack (for dodge system)")]
    public float readyAttackWarningTime = 1f;
    
    [Header("Attack Damage")]
    public float attackDamage = 10f;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;

    // Component references
    private EnemyHealth health;
    private EnemyController controller;
    private Animator animator;
    private GameObject player;

    // State tracking
    private bool isInCombat = false;
    private bool isInCombatMode = false; // New: Combat mode (close range) vs chase mode
    private bool isAttacking = false;
    private bool hasHitPlayerThisAttack = false;
    
    // Attack timing
    private Coroutine attackCoroutine;
    private float nextAttackTime = 0f;
    
    // Attack direction (stored when attack starts)
    private Vector2 attackDirection = Vector2.down;

    void Start()
    {
        health = GetComponent<EnemyHealth>();
        controller = GetComponent<EnemyController>();
        animator = GetComponent<Animator>();
        player = controller.GetPlayer();
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    void Update()
    {
        // Don't update combat if dead
        if (health != null && health.IsDead())
        {
            if (isInCombat)
                ExitCombat();
            return;
        }

        // Update player reference if lost
        if (player == null)
        {
            player = controller.GetPlayer();
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            UpdateCombatMode();
        }
    }

    /// <summary>
    /// Check if enemy should be in combat mode (close range) or chase mode
    /// </summary>
    private void UpdateCombatMode()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        
        // Enter combat mode if within combat radius
        if (!isInCombatMode && distanceToPlayer <= combatModeRadius)
        {
            EnterCombatMode();
        }
        // Exit combat mode if player leaves combat range
        // Note: We wait for attacks to finish before exiting, but attacks are NEVER cancelled
        // - attackCoroutine != null: attack sequence preparing (ready warning phase)
        // - isAttacking = true: attack animation executing
        // Once an attack starts, it will complete regardless of player distance
        else if (isInCombatMode && distanceToPlayer > combatModeRadius && !isAttacking && attackCoroutine == null)
        {
            ExitCombatMode();
        }

        // If in combat mode and player is in attack range, attack
        // Multiple safety checks to prevent overlapping attacks:
        // 1. Must be in combat mode
        // 2. Must NOT be currently attacking (isAttacking = false)
        // 3. Must have waited for cooldown (Time.time >= nextAttackTime)
        // 4. Must not have an attack sequence in progress (attackCoroutine = null)
        if (isInCombatMode && !isAttacking && Time.time >= nextAttackTime && attackCoroutine == null)
        {
            if (distanceToPlayer <= attackHitboxRadius)
            {
                Debug.Log($"{gameObject.name}: Starting new attack sequence. Current time: {Time.time:F2}, Next attack time: {nextAttackTime:F2}");
                StartAttackSequence();
            }
        }
    }

    /// <summary>
    /// Enter combat mode (close range fighting)
    /// Combat mode is SEPARATE from combat state
    /// - Combat MODE = close range (within combatModeRadius) - enables attacking
    /// - Combat STATE = enemy has spotted player - triggers battle music via GameManager
    /// </summary>
    private void EnterCombatMode()
    {
        isInCombatMode = true;
        
        // DO NOT automatically enter combat state here!
        // Combat state should only be entered when:
        // 1. Enemy spots the player (OnPlayerSpotted in EnemyAI)
        // 2. Player hits the enemy (ForceSpotPlayer in EnemyAI)
    }

    /// <summary>
    /// Exit combat mode and return to chase mode
    /// ATTACKS ARE NEVER CANCELLED - once started, they always complete
    /// </summary>
    private void ExitCombatMode()
    {
        isInCombatMode = false;
        
        // Attacks cannot be interrupted once they start
        // Even if player moves away, the attack sequence will complete naturally
        // No cancellation logic here - let animation events handle the full lifecycle
    }

    /// <summary>
    /// Start the attack sequence with ready warning
    /// </summary>
    private void StartAttackSequence()
    {
        attackCoroutine = StartCoroutine(AttackSequenceCoroutine());
    }

    /// <summary>
    /// Coroutine that handles attack timing and ready warning
    /// </summary>
    private IEnumerator AttackSequenceCoroutine()
    {
        // Calculate random attack interval
        float attackInterval = Random.Range(minAttackInterval, maxAttackInterval);
        
        // Wait for ready warning time before signaling
        float delayBeforeReady = Mathf.Max(0f, attackInterval - readyAttackWarningTime);
        if (delayBeforeReady > 0f)
        {
            yield return new WaitForSeconds(delayBeforeReady);
        }
        
        // Once attack sequence starts, FULLY COMMIT to it
        // NO CANCELLATION except for death or destroyed player
        // This ensures attack animations always complete once started
        if (health != null && health.IsDead())
        {
            attackCoroutine = null;
            yield break;
        }
        
        if (player == null)
        {
            attackCoroutine = null;
            yield break;
        }
        
        // Call ready attack warning (for future dodge system)
        ReadyAttack();
        
        // Wait remaining time before actual attack
        yield return new WaitForSeconds(readyAttackWarningTime);
        
        // Final check before executing attack - only for death/null player
        // Don't check combat mode - attack is already committed!
        if (health != null && health.IsDead())
        {
            attackCoroutine = null;
            yield break;
        }
        
        if (player == null)
        {
            attackCoroutine = null;
            yield break;
        }
        
        // Execute attack
        ExecuteAttack();
        
        // DON'T set nextAttackTime here - let AttackEnd() do it after animation completes
        // This prevents new attacks from starting before current attack finishes
        
        attackCoroutine = null;
    }

    /// <summary>
    /// Called 1 second before enemy attacks (for dodge system)
    /// </summary>
    private void ReadyAttack()
    {
        Debug.Log($"{gameObject.name}: Enemy ready to attack!");
    }

    /// <summary>
    /// Execute the attack animation
    /// </summary>
    private void ExecuteAttack()
    {
        isAttacking = true;
        hasHitPlayerThisAttack = false;
        
        // Calculate direction TO PLAYER for the attack (not just current facing)
        if (player != null)
        {
            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            attackDirection = directionToPlayer;
        }
        else
        {
            // Fallback to current facing direction if player is null
            attackDirection = controller.GetFacingDirection();
        }
        
        // Update animator with attack direction
        UpdateAnimatorForAttack();
        UpdateAnimatorAttackState(true);
        
        // Lock sprite flip to attack direction
        controller.FlipSprite(attackDirection);
        
        Debug.Log($"{gameObject.name}: ========== ATTACK START ==========");
        Debug.Log($"{gameObject.name}: Direction TO PLAYER: {attackDirection}");
        Debug.Log($"{gameObject.name}: Setting animator - moveX: {Mathf.Abs(attackDirection.x)}, moveY: {attackDirection.y}, isAttacking: TRUE");
        
        // Safety failsafe: If animation events aren't set up, reset after animation length
        // This prevents getting stuck in attacking state
        StartCoroutine(AttackFailsafe(3f)); // 3 second failsafe (increased from 2)
    }
    
    /// <summary>
    /// Update animator moveX/moveY to match attack direction
    /// </summary>
    private void UpdateAnimatorForAttack()
    {
        if (animator != null)
        {
            animator.SetFloat("moveX", Mathf.Abs(attackDirection.x));
            animator.SetFloat("moveY", attackDirection.y);
        }
    }
    
    /// <summary>
    /// Failsafe to reset attack state if animation events don't fire
    /// </summary>
    private IEnumerator AttackFailsafe(float maxDuration)
    {
        yield return new WaitForSeconds(maxDuration);
        
        // If still attacking after max duration, animation events probably aren't set up
        if (isAttacking)
        {
            Debug.LogWarning($"{gameObject.name}: Attack failsafe triggered! Animation events may not be set up properly.");
            AttackEnd();
        }
    }

    /// <summary>
    /// Update animator isAttacking parameter
    /// </summary>
    private void UpdateAnimatorAttackState(bool attacking)
    {
        if (animator != null)
        {
            animator.SetBool("isAttacking", attacking);
        }
    }

    // ========== ANIMATION EVENT CALLBACKS ==========
    // These are called by animation events in the Unity Animator

    /// <summary>
    /// Called by animation event at the start of attack animation
    /// </summary>
    public void AttackStart()
    {
        // Guard against duplicate events (check if already attacking)
        if (!isAttacking)
            return;
            
        hasHitPlayerThisAttack = false;
        Debug.Log($"{gameObject.name}: AttackStart event fired!");
    }

    /// <summary>
    /// Called by animation event when attack should deal damage
    /// </summary>
    public void AttackHit()
    {
        Debug.Log($"{gameObject.name}: AttackHit event fired!");
        
        if (hasHitPlayerThisAttack)
            return;
        
        // Check if player is in attack range
        if (player == null)
            return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        
        if (distanceToPlayer <= attackHitboxRadius)
        {
            hasHitPlayerThisAttack = true;
            Debug.Log($"{gameObject.name}: Enemy hit player! (Distance: {distanceToPlayer:F2})");
            
            // Deal damage to player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Player doesn't have PlayerHealth component!");
            }
        }
        else
        {
            Debug.Log($"{gameObject.name}: Attack missed - player too far (Distance: {distanceToPlayer:F2})");
        }
    }

    /// <summary>
    /// Called by animation event at the end of attack animation
    /// </summary>
    public void AttackEnd()
    {
        // Guard against duplicate events
        if (!isAttacking)
            return;
            
        Debug.Log($"{gameObject.name}: AttackEnd event fired! isAttacking set to FALSE");
        isAttacking = false;
        UpdateAnimatorAttackState(false);
        
        // Set next attack time AFTER animation completes
        // This ensures the cooldown only starts after the current attack fully finishes
        nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval);
        Debug.Log($"{gameObject.name}: Next attack allowed at: {nextAttackTime:F2} (current time: {Time.time:F2})");
    }
    
    /// <summary>
    /// Get the attack direction for animator use
    /// </summary>
    public Vector2 GetAttackDirection() => attackDirection;

    // ========== PUBLIC INTERFACE ==========

    /// <summary>
    /// Enter combat state (for GameManager integration)
    /// </summary>
    public void EnterCombat()
    {
        if (isInCombat) return;
        
        isInCombat = true;
        
        // Show health bar when entering combat
        if (health != null)
        {
            EnemyHealthBar healthBar = health.GetHealthBar();
            if (healthBar != null)
            {
                healthBar.Show();
            }
        }
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterCombat(gameObject);
        }
    }

    /// <summary>
    /// Exit combat state (for GameManager integration)
    /// </summary>
    public void ExitCombat()
    {
        if (!isInCombat) return;
        
        isInCombat = false;
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitCombat(gameObject);
        }
    }

    /// <summary>
    /// Check if currently in combat
    /// </summary>
    public bool IsInCombat() => isInCombat;

    /// <summary>
    /// Check if in combat mode (close range attack mode)
    /// </summary>
    public bool IsInCombatMode() => isInCombatMode;

    /// <summary>
    /// Check if currently attacking
    /// </summary>
    public bool IsAttacking() => isAttacking;

    private void OnDestroy()
    {
        // Make sure to exit combat when destroyed
        if (isInCombat && GameManager.Instance != null)
        {
            GameManager.Instance.ExitCombat(gameObject);
        }
    }

    // ========== DEBUG VISUALIZATION ==========

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        // Combat mode radius (yellow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, combatModeRadius);
        
        // Attack hitbox radius (red)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackHitboxRadius);
        
        // Draw line to player if in range during play mode
        if (Application.isPlaying && player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            if (distanceToPlayer <= combatModeRadius)
            {
                Gizmos.color = isInCombatMode ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
        }
    }
}
