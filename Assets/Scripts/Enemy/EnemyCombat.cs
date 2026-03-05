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
        else if (isInCombatMode && distanceToPlayer > combatModeRadius)
        {
            ExitCombatMode();
        }

        // If in combat mode and player is in attack range, attack
        // Check if no attack is in progress (attackCoroutine == null prevents restarting)
        if (isInCombatMode && !isAttacking && Time.time >= nextAttackTime && attackCoroutine == null)
        {
            if (distanceToPlayer <= attackHitboxRadius)
            {
                StartAttackSequence();
            }
        }
    }

    /// <summary>
    /// Enter combat mode (close range fighting)
    /// </summary>
    private void EnterCombatMode()
    {
        isInCombatMode = true;
        
        // Make sure we're in combat state for GameManager
        if (!isInCombat)
            EnterCombat();
    }

    /// <summary>
    /// Exit combat mode and return to chase mode
    /// </summary>
    private void ExitCombatMode()
    {
        isInCombatMode = false;
        
        // Don't cancel attack if already executing (isAttacking = true)
        // Let the attack complete naturally via animation events
        if (!isAttacking && attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        // Only reset isAttacking if not currently in an attack animation
        if (!isAttacking)
        {
            UpdateAnimatorAttackState(false);
        }
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
        
        // Check if still in combat mode before warning
        if (!isInCombatMode)
        {
            attackCoroutine = null;
            yield break;
        }
        
        // Call ready attack warning (for future dodge system)
        ReadyAttack();
        
        // Wait remaining time before actual attack
        yield return new WaitForSeconds(readyAttackWarningTime);
        
        // Check if still in combat before executing attack
        // Once we get here, we commit to the attack regardless of distance
        if (!isInCombatMode)
        {
            attackCoroutine = null;
            yield break;
        }
        
        // Execute attack
        ExecuteAttack();
        
        // Set next attack time
        nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval);
        
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
        UpdateAnimatorAttackState(true);
        
        Debug.Log($"{gameObject.name}: Executing attack! isAttacking set to TRUE");
        
        // Debug: Check animator state
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"{gameObject.name}: Current animator state: {stateInfo.fullPathHash}, IsName('Attack'): {stateInfo.IsName("Attack")}");
            Debug.Log($"{gameObject.name}: Animator parameters - moveX: {animator.GetFloat("moveX")}, moveY: {animator.GetFloat("moveY")}, isAttacking: {animator.GetBool("isAttacking")}");
        }
        
        // Safety fallback: If animation events aren't set up, reset after animation length
        // This prevents getting stuck in attacking state
        StartCoroutine(AttackFailsafe(2f)); // 2 second failsafe
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
        hasHitPlayerThisAttack = false;
    }

    /// <summary>
    /// Called by animation event when attack should deal damage
    /// </summary>
    public void AttackHit()
    {
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
            
            // TODO: Deal damage to player when player health system is implemented
            // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(attackDamage);
            // }
        }
    }

    /// <summary>
    /// Called by animation event at the end of attack animation
    /// </summary>
    public void AttackEnd()
    {
        Debug.Log($"{gameObject.name}: Attack ended! isAttacking set to FALSE");
        isAttacking = false;
        UpdateAnimatorAttackState(false);
    }

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
