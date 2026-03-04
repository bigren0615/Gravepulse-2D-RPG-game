using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core enemy controller - handles movement, animation, and visual updates
/// This is the main component that other enemy scripts reference
/// </summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    
    [Header("Obstacle Avoidance")]
    public float lookAheadDistance = 0.5f;
    public float obstacleCheckRadius = 0.15f;
    
    [Header("Enemy Separation")]
    public float separationDistance = 0.8f;
    public float separationForce = 1.5f;
    public LayerMask enemyLayer;

    [Header("References")]
    public GameObject player; // Auto-found by tag if not assigned
    
    // Component references (cached for performance)
    [HideInInspector] public Animator animator;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    
    // Current state
    private Vector2 facingDirection = Vector2.down;
    private Vector2 avoidanceDirection = Vector2.zero;
    private Vector3 lastPosition;
    
    // References to other enemy components
    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;
    private EnemyCombat enemyCombat;

    void Awake()
    {
        // Cache component references
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
            Debug.LogError("Enemy needs a SpriteRenderer!");
        
        lastPosition = transform.position;
    }

    void Start()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                Debug.LogWarning("No player found! Assign in Inspector or tag the player as 'Player'.");
        }
        
        // Get references to other enemy components
        enemyAI = GetComponent<EnemyAI>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCombat = GetComponent<EnemyCombat>();
        
        // Validate enemy layer configuration
        ValidateEnemyLayer();
    }
    
    private void ValidateEnemyLayer()
    {
        // Check if enemyLayer is set (not 0 which means "Nothing")
        if (enemyLayer.value == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Enemy Layer is not set! Enemies may stack together. Please set the Enemy Layer in the Inspector to the layer your enemies are on.");
        }
        
        // Also check if THIS enemy is on a layer included in the enemyLayer mask
        int myLayer = gameObject.layer;
        if (!IsLayerInMask(myLayer, enemyLayer))
        {
            Debug.LogWarning($"[{gameObject.name}] This enemy is on layer '{LayerMask.LayerToName(myLayer)}' but enemyLayer mask doesn't include it. Separation won't work!");
        }
    }
    
    private bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    /// <summary>
    /// Move toward a destination with smart obstacle avoidance
    /// </summary>
    public bool MoveToward(Vector3 destination, bool updateFacing = true)
    {
        Vector3 dir = destination - transform.position;
        Vector2 moveDir2D = new Vector2(dir.x, dir.y).normalized;

        // Smart obstacle avoidance
        Vector2 finalDirection = GetSmartMovementDirection(moveDir2D);

        if (finalDirection == Vector2.zero)
        {
            return false; // Completely stuck
        }

        // Apply enemy separation to prevent stacking
        // Important: Don't normalize after adding separation - we want to preserve separation strength!
        Vector2 separationVector = GetSeparationVector();
        
        if (separationVector.sqrMagnitude > 0.01f)
        {
            // Add separation force without normalizing to maintain its strength
            finalDirection = finalDirection.normalized + separationVector;
            
            // Only normalize if the combined vector is too large (> 2.0)
            // This preserves separation strength while preventing excessive movement
            if (finalDirection.magnitude > 2.0f)
            {
                finalDirection = finalDirection.normalized;
            }
        }
        else
        {
            // No separation needed, just use normalized direction
            finalDirection = finalDirection.normalized;
        }

        // Apply movement
        Vector3 movement = new Vector3(finalDirection.x, finalDirection.y, 0f) * speed * Time.deltaTime;
        transform.position += movement;

        // Update animator
        UpdateAnimation(finalDirection, false, false);

        // Flip sprite for left/right
        FlipSprite(finalDirection);

        // Update facing direction for FOV calculation
        if (updateFacing && finalDirection.sqrMagnitude > 0.01f)
        {
            facingDirection = finalDirection;
        }

        lastPosition = transform.position;
        return true;
    }

    /// <summary>
    /// Update animation parameters
    /// </summary>
    public void UpdateAnimation(Vector2 moveDirection, bool isAttacking, bool isDead)
    {
        if (animator != null)
        {
            animator.SetFloat("moveX", Mathf.Abs(moveDirection.x));
            animator.SetFloat("moveY", moveDirection.y);
            animator.SetBool("isAttacking", isAttacking);
            animator.SetBool("isDead", isDead);
        }
    }

    /// <summary>
    /// Flip sprite based on movement direction
    /// </summary>
    public void FlipSprite(Vector2 direction)
    {
        if (spriteRenderer != null)
        {
            if (direction.x > 0.01f)
                spriteRenderer.flipX = true;
            else if (direction.x < -0.01f)
                spriteRenderer.flipX = false;
        }
    }

    /// <summary>
    /// Stop all movement
    /// </summary>
    public void Stop()
    {
        UpdateAnimation(Vector2.zero, false, false);
    }

    /// <summary>
    /// Get the current facing direction (for FOV calculations)
    /// </summary>
    public Vector2 GetFacingDirection()
    {
        return facingDirection;
    }

    /// <summary>
    /// Set facing direction manually
    /// </summary>
    public void SetFacingDirection(Vector2 direction)
    {
        facingDirection = direction;
    }

    /// <summary>
    /// Get player reference
    /// </summary>
    public GameObject GetPlayer()
    {
        return player;
    }

    // ========== PRIVATE HELPER METHODS ==========

    private Vector2 GetSmartMovementDirection(Vector2 desiredDirection)
    {
        Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.y);

        // First check if desired direction is clear
        if (IsPathClear(currentPos2D, desiredDirection, lookAheadDistance))
        {
            avoidanceDirection = Vector2.zero; // Reset avoidance
            return desiredDirection;
        }

        // Try to find best alternative direction
        float[] testAngles = { 15f, -15f, 30f, -30f, 45f, -45f, 60f, -60f, 75f, -75f, 90f, -90f, 120f, -120f, 150f, -150f };

        foreach (float angle in testAngles)
        {
            Vector2 testDir = RotateVector(desiredDirection, angle);

            if (IsPathClear(currentPos2D, testDir, lookAheadDistance))
            {
                avoidanceDirection = testDir;
                return testDir;
            }
        }

        // If still stuck, try moving perpendicular to nearest obstacle
        Vector2 escapeDir = GetEscapeDirection(currentPos2D, desiredDirection);
        if (escapeDir != Vector2.zero)
        {
            return escapeDir;
        }

        return Vector2.zero; // Completely stuck
    }

    private bool IsPathClear(Vector2 startPos, Vector2 direction, float distance)
    {
        // Check multiple points along the path
        int checkPoints = 3;
        for (int i = 1; i <= checkPoints; i++)
        {
            float checkDist = (distance / checkPoints) * i;
            Vector2 checkPos = startPos + direction * checkDist;
            Vector3 checkPos3D = new Vector3(checkPos.x, checkPos.y, transform.position.z);

            if (Physics2D.OverlapCircle(checkPos3D, obstacleCheckRadius, LayerMask.GetMask("Solid")))
            {
                return false; // Path blocked
            }
        }
        return true; // Path clear
    }

    private Vector2 GetSeparationVector()
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationDistance, enemyLayer);
        
        Vector2 separationVector = Vector2.zero;
        int separationCount = 0;

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            if (enemyCollider.gameObject == gameObject)
                continue;

            Vector2 enemyPos = new Vector2(enemyCollider.transform.position.x, enemyCollider.transform.position.y);
            Vector2 myPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 awayFromEnemy = myPos - enemyPos;
            float distance = awayFromEnemy.magnitude;

            if (distance < separationDistance && distance > 0.01f)
            {
                float strength = (separationDistance - distance) / separationDistance;
                separationVector += awayFromEnemy.normalized * strength;
                separationCount++;
            }
        }

        if (separationCount > 0)
        {
            separationVector = (separationVector / separationCount) * separationForce;
        }

        return separationVector;
    }

    private Vector2 RotateVector(Vector2 vec, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            vec.x * cos - vec.y * sin,
            vec.x * sin + vec.y * cos
        );
    }

    private Vector2 GetEscapeDirection(Vector2 currentPos, Vector2 blockedDirection)
    {
        Vector2 perpendicular1 = new Vector2(-blockedDirection.y, blockedDirection.x);
        Vector2 perpendicular2 = new Vector2(blockedDirection.y, -blockedDirection.x);

        if (IsPathClear(currentPos, perpendicular1, lookAheadDistance * 0.5f))
            return perpendicular1;
        if (IsPathClear(currentPos, perpendicular2, lookAheadDistance * 0.5f))
            return perpendicular2;

        if (IsPathClear(currentPos, -blockedDirection, lookAheadDistance * 0.5f))
            return -blockedDirection;

        return Vector2.zero;
    }

    // ========== DEBUG VISUALIZATION ==========

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        // Draw enemy separation radius
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f); // Cyan, semi-transparent
        Gizmos.DrawWireSphere(transform.position, separationDistance);
    }
}
