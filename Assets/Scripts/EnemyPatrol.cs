using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolRadius = 3f;
    public float speed = 2f;
    public float waitTime = 1f;

    [Header("Chase Settings")]
    public float chaseRadius = 5f; // Enemy detects player within this radius
    public float stopDistance = 0.5f; // Minimum distance to stop near player
    [Range(0f, 360f)]
    public float fieldOfViewAngle = 110f; // Enemy can only see within this angle (in degrees)
    public float memoryDuration = 3f; // How long to remember player after losing sight (seconds)
    public float searchDuration = 8f; // How long to search around last known position before returning to spawn
    public float searchRadius = 2.5f; // Radius to patrol while searching for player

    [Header("Obstacle Avoidance")]
    public float lookAheadDistance = 0.5f; // How far to check for obstacles
    public float obstacleCheckRadius = 0.15f; // Radius for obstacle detection
    public float stuckThreshold = 0.1f; // Distance threshold to detect if stuck
    public float stuckTimeLimit = 1.5f; // Time before considering enemy stuck

    [Header("References")]
    public GameObject player; // Assign manually or leave blank to auto-find by tag

    private Vector3 startPosition;
    private Vector3 targetPoint;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool waiting = false;
    private float waitTimer = 0f;

    private bool isChasing = false;
    private Vector2 facingDirection = Vector2.down; // Track which way enemy is facing
    
    // Chase memory system
    private Vector3 lastKnownPlayerPosition;
    private float lostSightTimer = 0f;
    private bool hasPlayerInMemory = false;
    
    // Search mode system
    private bool isSearching = false;
    private Vector3 searchCenter; // Where to search around
    private float searchTimer = 0f;
    
    // Stuck detection
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private Vector2 avoidanceDirection = Vector2.zero; // Current avoidance direction

    void Start()
    {
        startPosition = transform.position;
        lastPosition = transform.position;
        PickRandomPoint(startPosition, patrolRadius);

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            Debug.LogError("Enemy needs a SpriteRenderer!");

        // Auto-find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                Debug.LogWarning("No player found! Assign in Inspector or tag the player as 'Player'.");
        }
    }

    void Update()
    {
        // Try to find player again if lost reference
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            // Use Vector2.Distance to ignore Z-axis (important for 2D games!)
            Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.y);
            float distToPlayer = Vector2.Distance(enemyPos2D, playerPos2D);
            
            // Check both distance AND field of view AND line of sight
            bool inRange = distToPlayer <= chaseRadius;
            bool inFOV = IsPlayerInFieldOfView(playerPos2D);
            bool hasLineOfSight = HasClearLineOfSight(playerPos2D);
            bool canSeePlayer = inRange && inFOV && hasLineOfSight;
            
            // Update chase memory
            if (canSeePlayer)
            {
                // Can see player - update last known position and reset timer
                lastKnownPlayerPosition = player.transform.position;
                hasPlayerInMemory = true;
                lostSightTimer = 0f;
                isChasing = true;
                isSearching = false; // Exit search mode if spotted player again
            }
            else if (hasPlayerInMemory)
            {
                // Lost sight but still have memory
                lostSightTimer += Time.deltaTime;
                
                if (lostSightTimer >= memoryDuration)
                {
                    // Memory expired - enter search mode
                    hasPlayerInMemory = false;
                    isChasing = false;
                    EnterSearchMode(lastKnownPlayerPosition);
                }
                else
                {
                    // Still remember - check if close to last known position
                    float distToLastKnown = Vector2.Distance(enemyPos2D, 
                        new Vector2(lastKnownPlayerPosition.x, lastKnownPlayerPosition.y));
                    
                    if (distToLastKnown < stopDistance * 2f)
                    {
                        // Reached last known position and player not there - enter search mode
                        hasPlayerInMemory = false;
                        isChasing = false;
                        EnterSearchMode(lastKnownPlayerPosition);
                    }
                    else
                    {
                        // Continue chasing to last known position
                        isChasing = true;
                    }
                }
            }
            else
            {
                isChasing = false;
            }
            
            // Debug logging (comment out after testing)
            // Debug.Log($"CanSee: {canSeePlayer} | Memory: {hasPlayerInMemory} | Timer: {lostSightTimer:F1} | Chasing: {isChasing}");
        }
        else
        {
            isChasing = false;
            hasPlayerInMemory = false;
        }

        if (isChasing)
        {
            ChasePlayer();
        }
        else if (isSearching)
        {
            SearchForPlayer();
        }
        else
        {
            Patrol();
        }
        
        // Detect if stuck
        DetectStuck();
    }

    // ---------------- Patrol ----------------
    private void Patrol()
    {
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                PickRandomPoint(startPosition, patrolRadius);
            }
            return;
        }

        MoveToward(targetPoint);

        // Reached target point
        if (Vector3.Distance(transform.position, targetPoint) < 0.05f)
        {
            waiting = true;
            waitTimer = waitTime;
        }
    }

    // ---------------- Search Mode ----------------
    private void EnterSearchMode(Vector3 searchPosition)
    {
        isSearching = true;
        searchCenter = searchPosition;
        searchTimer = 0f;
        // Pick first search point
        PickRandomPoint(searchCenter, searchRadius);
        waiting = false;
    }
    
    private void SearchForPlayer()
    {
        searchTimer += Time.deltaTime;
        
        // Check if search time expired
        if (searchTimer >= searchDuration)
        {
            // Give up searching, return to normal patrol
            isSearching = false;
            PickRandomPoint(startPosition, patrolRadius);
            return;
        }
        
        // Search patrol behavior (same as normal patrol but around search center)
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                PickRandomPoint(searchCenter, searchRadius);
            }
            return;
        }

        MoveToward(targetPoint);

        // Reached target point
        if (Vector3.Distance(transform.position, targetPoint) < 0.05f)
        {
            waiting = true;
            waitTimer = waitTime * 0.7f; // Slightly faster search patrol
        }
    }

    // ---------------- Chase ----------------
    private void ChasePlayer()
    {
        if (player == null) return;

        // Use last known position if player out of sight, otherwise use current position
        Vector3 targetPos = hasPlayerInMemory && lostSightTimer > 0f 
            ? lastKnownPlayerPosition 
            : player.transform.position;
        
        // Use Vector2.Distance to ignore Z-axis
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.y);
        float dist = Vector2.Distance(enemyPos2D, targetPos2D);

        // Stop when close to target
        if (dist > stopDistance)
            MoveToward(targetPos);
    }

    // ---------------- Move Helper ----------------
    private void MoveToward(Vector3 destination)
    {
        Vector3 dir = destination - transform.position;
        Vector2 moveDir2D = new Vector2(dir.x, dir.y).normalized;
        
        // Smart obstacle avoidance
        Vector2 finalDirection = GetSmartMovementDirection(moveDir2D);
        
        if (finalDirection == Vector2.zero)
        {
            // Completely stuck - handle based on state
            if (!isChasing)
            {
                if (isSearching)
                    PickRandomPoint(searchCenter, searchRadius);
                else
                    PickRandomPoint(startPosition, patrolRadius);
            }
            return;
        }
        
        // Apply movement
        Vector3 movement = new Vector3(finalDirection.x, finalDirection.y, 0f) * speed * Time.deltaTime;
        transform.position += movement;
        
        // Update animator with the actual movement direction
        if (animator != null)
        {
            animator.SetFloat("moveX", Mathf.Abs(finalDirection.x));
            animator.SetFloat("moveY", finalDirection.y);
            animator.SetBool("isAttacking", false);
            animator.SetBool("isDead", false);
        }

        // Flip sprite for left/right
        if (spriteRenderer != null)
        {
            if (finalDirection.x > 0.01f)
                spriteRenderer.flipX = true;
            else if (finalDirection.x < -0.01f)
                spriteRenderer.flipX = false;
        }

        // Update facing direction for FOV calculation
        // Only update when not avoiding obstacles during chase to maintain chase focus
        if (finalDirection.sqrMagnitude > 0.01f)
        {
            // During patrol, always update facing
            // During chase, only update if moving in roughly the intended direction
            if (!isChasing || avoidanceDirection == Vector2.zero)
            {
                facingDirection = finalDirection;
            }
        }
    }
    
    // ---------------- Smart Obstacle Avoidance ----------------
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
        // Test multiple angles around the desired direction
        float[] testAngles = { 15f, -15f, 30f, -30f, 45f, -45f, 60f, -60f, 75f, -75f, 90f, -90f, 120f, -120f, 150f, -150f };
        
        foreach (float angle in testAngles)
        {
            Vector2 testDir = RotateVector(desiredDirection, angle);
            
            if (IsPathClear(currentPos2D, testDir, lookAheadDistance))
            {
                // Found a clear direction!
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
        // Try to move away from the nearest obstacle
        Vector2 perpendicular1 = new Vector2(-blockedDirection.y, blockedDirection.x);
        Vector2 perpendicular2 = new Vector2(blockedDirection.y, -blockedDirection.x);
        
        if (IsPathClear(currentPos, perpendicular1, lookAheadDistance * 0.5f))
            return perpendicular1;
        if (IsPathClear(currentPos, perpendicular2, lookAheadDistance * 0.5f))
            return perpendicular2;
            
        // Last resort: move backward
        if (IsPathClear(currentPos, -blockedDirection, lookAheadDistance * 0.5f))
            return -blockedDirection;
            
        return Vector2.zero;
    }
    
    // ---------------- Stuck Detection ----------------
    private void DetectStuck()
    {
        float distMoved = Vector3.Distance(transform.position, lastPosition);
        
        if (distMoved < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer >= stuckTimeLimit)
            {
                // We're stuck! Take action
                if (!isChasing)
                {
                    // Pick a new patrol point
                    if (isSearching)
                        PickRandomPoint(searchCenter, searchRadius);
                    else
                        PickRandomPoint(startPosition, patrolRadius);
                }
                stuckTimer = 0f;
            }
        }
        else
        {
            // Moving properly, reset timer
            stuckTimer = 0f;
        }
        
        lastPosition = transform.position;
    }

    // ---------------- Field of View Check ----------------
    private bool IsPlayerInFieldOfView(Vector2 playerPos2D)
    {
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 dirToPlayer = (playerPos2D - enemyPos2D).normalized;
        
        // Calculate angle between facing direction and direction to player
        float angle = Vector2.Angle(facingDirection, dirToPlayer);
        
        // Check if angle is within half the field of view
        return angle <= fieldOfViewAngle / 2f;
    }
    
    // ---------------- Line of Sight Check ----------------
    private bool HasClearLineOfSight(Vector2 playerPos2D)
    {
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        
        // Perform raycast from enemy to player
        RaycastHit2D hit = Physics2D.Linecast(enemyPos2D, playerPos2D, LayerMask.GetMask("Solid"));
        
        // If raycast hit something, line of sight is blocked
        // If it didn't hit anything (hit.collider == null), we have clear line of sight
        return hit.collider == null;
    }

    // ---------------- Pick Random Patrol Point ----------------
    private void PickRandomPoint(Vector3 center, float radius)
    {
        int maxAttempts = 10; // try 10 times to find a free spot
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 potentialPoint = center + new Vector3(randomCircle.x, randomCircle.y, 0f);

            // Only pick free spot
            if (!Physics2D.OverlapCircle(potentialPoint, 0.2f, LayerMask.GetMask("Solid")))
            {
                targetPoint = potentialPoint;
                return;
            }
        }

        // fallback
        targetPoint = center;
    }

    // ---------------- Debug Gizmos ----------------
    private void OnDrawGizmosSelected()
    {
        // Chase radius (color indicates state)
        if (isChasing)
            Gizmos.color = Color.red; // Chasing
        else if (isSearching)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange - Searching
        else
            Gizmos.color = Color.yellow; // Normal patrol
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        if (!Application.isPlaying)
            return;

        // Draw Field of View cone
        DrawFieldOfViewCone();

        // Spawn patrol radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, patrolRadius);
        
        // Search mode visualization
        if (isSearching)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(searchCenter, searchRadius);
            Gizmos.DrawSphere(searchCenter, 0.15f);
            
            // Draw timer progress
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawLine(transform.position, searchCenter);
        }

        // Next patrol target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawSphere(targetPoint, 0.05f);
        
        // Draw line of sight to player (debug)
        if (player != null)
        {
            Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.y);
            float distToPlayer = Vector2.Distance(enemyPos2D, playerPos2D);
            
            // Only draw if player is in range
            if (distToPlayer <= chaseRadius)
            {
                bool hasLOS = HasClearLineOfSight(playerPos2D);
                bool inFOV = IsPlayerInFieldOfView(playerPos2D);
                
                // Draw line from enemy to player with color indicating status
                if (!hasLOS)
                {
                    // Line of sight blocked - draw grey dashed-looking line
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
                    Gizmos.DrawLine(transform.position, player.transform.position);
                }
                else if (!inFOV)
                {
                    // Has LOS but outside FOV - draw white transparent line
                    Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                    Gizmos.DrawLine(transform.position, player.transform.position);
                }
            }
        }
        
        // Draw chase visualization
        if (isChasing && player != null)
        {
            if (lostSightTimer > 0f && hasPlayerInMemory)
            {
                // Chasing to last known position (lost sight but has memory)
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.2f);
                
                // Also draw faded line to actual player position
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Faded red
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
            else
            {
                // Can see player - direct chase
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
        }
    }

    private void DrawFieldOfViewCone()
    {
        Vector3 enemyPos = transform.position;
        Vector3 facingDir3D = new Vector3(facingDirection.x, facingDirection.y, 0f);
        
        // Draw the field of view cone
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Semi-transparent yellow
        
        float halfAngle = fieldOfViewAngle / 2f;
        int segments = 20;
        Vector3 previousPoint = enemyPos;
        
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -halfAngle + (fieldOfViewAngle * i / segments);
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * facingDir3D;
            Vector3 point = enemyPos + direction * chaseRadius;
            
            if (i > 0)
            {
                Gizmos.DrawLine(enemyPos, point);
                Gizmos.DrawLine(previousPoint, point);
            }
            previousPoint = point;
        }
        
        // Draw the two edge lines more prominently
        Gizmos.color = Color.yellow;
        Vector3 leftEdge = Quaternion.Euler(0, 0, -halfAngle) * facingDir3D * chaseRadius;
        Vector3 rightEdge = Quaternion.Euler(0, 0, halfAngle) * facingDir3D * chaseRadius;
        Gizmos.DrawLine(enemyPos, enemyPos + leftEdge);
        Gizmos.DrawLine(enemyPos, enemyPos + rightEdge);
    }
}