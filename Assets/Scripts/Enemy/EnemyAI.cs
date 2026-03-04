using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy AI brain - handles patrol, chase, search behaviors and decision making
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolRadius = 3f;
    public float waitTime = 1f;

    [Header("Chase Settings")]
    public float chaseRadius = 5f;
    public float stopDistance = 0.5f;
    [Range(0f, 360f)]
    public float fieldOfViewAngle = 110f;
    public float memoryDuration = 3f;
    public float searchDuration = 8f;
    public float searchRadius = 2.5f;

    [Header("Stuck Detection")]
    public float stuckThreshold = 0.1f;
    public float stuckTimeLimit = 1.5f;

    // Component references
    private EnemyController controller;
    private BubbleController bubbleController;
    private EnemyCombat combat;
    
    // State
    private Vector3 startPosition;
    private Vector3 targetPoint;
    private bool waiting = false;
    private float waitTimer = 0f;
    
    // Chase state
    private bool isChasing = false;
    private Vector3 lastKnownPlayerPosition;
    private float lostSightTimer = 0f;
    private bool hasPlayerInMemory = false;
    
    // Search state
    private bool isSearching = false;
    private Vector3 searchCenter;
    private float searchTimer = 0f;
    
    // Stuck detection
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    
    // Bubble tracking
    private bool hasPlayedSpottedSound = false;
    private bool hasPlayedQuestionBubble = false;

    void Start()
    {
        controller = GetComponent<EnemyController>();
        bubbleController = GetComponent<BubbleController>();
        combat = GetComponent<EnemyCombat>();
        
        if (bubbleController == null)
            Debug.LogWarning("BubbleController component not found on " + gameObject.name);
        
        startPosition = transform.position;
        lastPosition = transform.position;
        PickRandomPoint(startPosition, patrolRadius);
    }

    void Update()
    {
        // Don't update AI if dead
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null && health.IsDead())
            return;

        GameObject player = controller.GetPlayer();
        if (player != null)
        {
            UpdatePlayerDetection(player);
        }
        else
        {
            // Try to find player again if lost reference
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                controller.player = foundPlayer;
            }
            isChasing = false;
            hasPlayerInMemory = false;
        }

        // Execute behavior based on state
        if (isChasing)
        {
            ChasePlayer();
            if (combat != null && !combat.IsInCombat())
                combat.EnterCombat();
        }
        else if (isSearching)
        {
            SearchForPlayer();
        }
        else
        {
            Patrol();
            if (combat != null && combat.IsInCombat())
                combat.ExitCombat();
            
            // Reset bubble flags when back to patrol
            hasPlayedSpottedSound = false;
            hasPlayedQuestionBubble = false;
        }

        DetectStuck();
    }

    // ========== PLAYER DETECTION ==========

    private void UpdatePlayerDetection(GameObject player)
    {
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.y);
        float distToPlayer = Vector2.Distance(enemyPos2D, playerPos2D);

        bool inRange = distToPlayer <= chaseRadius;
        bool inFOV = IsPlayerInFieldOfView(player);
        bool hasLineOfSight = HasClearLineOfSight(playerPos2D);
        bool canSeePlayer = inRange && inFOV && hasLineOfSight;

        if (canSeePlayer)
        {
            OnPlayerSpotted(player.transform.position);
        }
        else if (hasPlayerInMemory)
        {
            OnPlayerMemory(enemyPos2D);
        }
        else
        {
            isChasing = false;
        }
    }

    private void OnPlayerSpotted(Vector3 playerPosition)
    {
        lastKnownPlayerPosition = playerPosition;
        hasPlayerInMemory = true;
        lostSightTimer = 0f;
        
        if (!isChasing && !hasPlayedSpottedSound)
        {
            AudioManager.Instance.PlaySFX(SFXType.Suspense);
            hasPlayedSpottedSound = true;
            
            if (bubbleController != null)
                bubbleController.ShowSuspenseBubble();
        }
        
        isChasing = true;
        isSearching = false;
    }

    private void OnPlayerMemory(Vector2 enemyPos2D)
    {
        lostSightTimer += Time.deltaTime;

        if (lostSightTimer >= memoryDuration)
        {
            hasPlayerInMemory = false;
            isChasing = false;
            EnterSearchMode(lastKnownPlayerPosition);
        }
        else
        {
            float distToLastKnown = Vector2.Distance(enemyPos2D,
                new Vector2(lastKnownPlayerPosition.x, lastKnownPlayerPosition.y));

            if (distToLastKnown < stopDistance * 2f)
            {
                hasPlayerInMemory = false;
                isChasing = false;
                EnterSearchMode(lastKnownPlayerPosition);
            }
            else
            {
                isChasing = true;
            }
        }
    }

    private bool IsPlayerInFieldOfView(GameObject player)
    {
        if (player == null) return false;
        
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.y);
        Vector2 dirToPlayer = (playerPos2D - enemyPos2D).normalized;
        Vector2 facingDir = controller.GetFacingDirection();

        float angle = Vector2.Angle(facingDir, dirToPlayer);
        return angle <= fieldOfViewAngle / 2f;
    }

    private bool HasClearLineOfSight(Vector2 playerPos2D)
    {
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        RaycastHit2D hit = Physics2D.Linecast(enemyPos2D, playerPos2D, LayerMask.GetMask("Solid"));
        return hit.collider == null;
    }

    // ========== BEHAVIORS ==========

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

        controller.MoveToward(targetPoint);

        if (Vector3.Distance(transform.position, targetPoint) < 0.05f)
        {
            waiting = true;
            waitTimer = waitTime;
        }
    }

    private void ChasePlayer()
    {
        GameObject player = controller.GetPlayer();
        if (player == null) return;

        Vector3 targetPos = hasPlayerInMemory && lostSightTimer > 0f
            ? lastKnownPlayerPosition
            : player.transform.position;

        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.y);
        float dist = Vector2.Distance(enemyPos2D, targetPos2D);

        if (dist > stopDistance)
            controller.MoveToward(targetPos);
    }

    private void EnterSearchMode(Vector3 searchPosition)
    {
        isSearching = true;
        searchCenter = searchPosition;
        searchTimer = 0f;
        PickRandomPoint(searchCenter, searchRadius);
        waiting = false;
        
        if (!hasPlayedQuestionBubble)
        {
            if (bubbleController != null)
                bubbleController.ShowQuestionBubble();
            hasPlayedQuestionBubble = true;
        }
    }

    private void SearchForPlayer()
    {
        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            isSearching = false;
            PickRandomPoint(startPosition, patrolRadius);
            return;
        }

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

        controller.MoveToward(targetPoint);

        if (Vector3.Distance(transform.position, targetPoint) < 0.05f)
        {
            waiting = true;
            waitTimer = waitTime * 0.7f;
        }
    }

    // ========== HELPERS ==========

    private void PickRandomPoint(Vector3 center, float radius)
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 potentialPoint = center + new Vector3(randomCircle.x, randomCircle.y, 0f);

            if (!Physics2D.OverlapCircle(potentialPoint, 0.2f, LayerMask.GetMask("Solid")))
            {
                targetPoint = potentialPoint;
                return;
            }
        }
        targetPoint = center;
    }

    private void DetectStuck()
    {
        float distMoved = Vector3.Distance(transform.position, lastPosition);

        if (distMoved < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckTimeLimit)
            {
                if (!isChasing)
                {
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
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    // ========== PUBLIC INTERFACE ==========

    public bool IsChasing() => isChasing;
    public bool IsSearching() => isSearching;
    public Vector3 GetStartPosition() => startPosition;

    // ========== DEBUG VISUALIZATION ==========

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            startPosition = transform.position;
        }

        // Chase radius
        if (isChasing)
            Gizmos.color = Color.red;
        else if (isSearching)
            Gizmos.color = new Color(1f, 0.5f, 0f);
        else
            Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        if (!Application.isPlaying)
            return;

        // Patrol radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, patrolRadius);

        // Search mode
        if (isSearching)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(searchCenter, searchRadius);
            Gizmos.DrawSphere(searchCenter, 0.15f);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(transform.position, searchCenter);
        }

        // Target point
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawSphere(targetPoint, 0.05f);

        // FOV cone
        DrawFieldOfViewCone();

        // Player detection
        GameObject player = controller != null ? controller.GetPlayer() : GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.y);
            float distToPlayer = Vector2.Distance(enemyPos2D, playerPos2D);

            if (distToPlayer <= chaseRadius)
            {
                bool hasLOS = HasClearLineOfSight(playerPos2D);
                bool inFOV = IsPlayerInFieldOfView(player);

                if (!hasLOS)
                {
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
                    Gizmos.DrawLine(transform.position, player.transform.position);
                }
                else if (!inFOV)
                {
                    Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                    Gizmos.DrawLine(transform.position, player.transform.position);
                }
            }
        }

        // Chase visualization
        if (isChasing && player != null)
        {
            if (lostSightTimer > 0f && hasPlayerInMemory)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.2f);

                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
        }
    }

    private void DrawFieldOfViewCone()
    {
        if (controller == null) return;
        
        Vector3 enemyPos = transform.position;
        Vector2 facingDir2D = controller.GetFacingDirection();
        Vector3 facingDir3D = new Vector3(facingDir2D.x, facingDir2D.y, 0f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);

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

        Gizmos.color = Color.yellow;
        Vector3 leftEdge = Quaternion.Euler(0, 0, -halfAngle) * facingDir3D * chaseRadius;
        Vector3 rightEdge = Quaternion.Euler(0, 0, halfAngle) * facingDir3D * chaseRadius;
        Gizmos.DrawLine(enemyPos, enemyPos + leftEdge);
        Gizmos.DrawLine(enemyPos, enemyPos + rightEdge);
    }
}
