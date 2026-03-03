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

    [Header("References")]
    public GameObject player; // Assign manually or leave blank to auto-find by tag

    private Vector3 startPosition;
    private Vector3 targetPoint;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool waiting = false;
    private float waitTimer = 0f;

    private bool isChasing = false;

    void Start()
    {
        startPosition = transform.position;
        PickRandomPoint();

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
            isChasing = distToPlayer <= chaseRadius;
            
            // Debug logging (comment out after testing)
            Debug.Log($"Enemy->Player distance: {distToPlayer:F2} | Chase radius: {chaseRadius} | Chasing: {isChasing}");
        }
        else
        {
            isChasing = false;
        }

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
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
                PickRandomPoint();
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

    // ---------------- Chase ----------------
    private void ChasePlayer()
    {
        if (player == null) return;

        Vector3 playerPos = player.transform.position;
        // Use Vector2.Distance to ignore Z-axis
        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 playerPos2D = new Vector2(playerPos.x, playerPos.y);
        float dist = Vector2.Distance(enemyPos2D, playerPos2D);

        // Stop when close to player
        if (dist > stopDistance)
            MoveToward(playerPos);
    }

    // ---------------- Move Helper ----------------
    private void MoveToward(Vector3 destination)
    {
        Vector3 dir = destination - transform.position;
        Vector3 moveDir = dir.normalized;

        Vector3 nextPos = transform.position + moveDir * speed * Time.deltaTime;

        // Check collision with Solid layer
        if (!Physics2D.OverlapCircle(nextPos, 0.1f, LayerMask.GetMask("Solid")))
        {
            transform.position = nextPos;
        }
        else
        {
            // If blocked, try moving around the obstacle
            // Try perpendicular directions
            Vector3 altDir1 = new Vector3(-moveDir.y, moveDir.x, 0f); // 90 degrees
            Vector3 altDir2 = new Vector3(moveDir.y, -moveDir.x, 0f); // -90 degrees
            
            Vector3 altPos1 = transform.position + altDir1 * speed * Time.deltaTime;
            Vector3 altPos2 = transform.position + altDir2 * speed * Time.deltaTime;
            
            if (!Physics2D.OverlapCircle(altPos1, 0.1f, LayerMask.GetMask("Solid")))
            {
                transform.position = altPos1;
            }
            else if (!Physics2D.OverlapCircle(altPos2, 0.1f, LayerMask.GetMask("Solid")))
            {
                transform.position = altPos2;
            }
            else if (!isChasing)
            {
                // If completely blocked while patrolling, pick new random point
                PickRandomPoint();
            }
        }

        // Update animator
        if (animator != null)
        {
            animator.SetFloat("moveX", Mathf.Abs(moveDir.x));
            animator.SetFloat("moveY", moveDir.y);
            animator.SetBool("isAttacking", false);
            animator.SetBool("isDead", false);
        }

        // Flip sprite for left/right
        if (spriteRenderer != null)
        {
            if (moveDir.x > 0.01f)
                spriteRenderer.flipX = true;
            else if (moveDir.x < -0.01f)
                spriteRenderer.flipX = false;
        }
    }

    // ---------------- Pick Random Patrol Point ----------------
    private void PickRandomPoint()
    {
        int maxAttempts = 10; // try 10 times to find a free spot
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 potentialPoint = startPosition + new Vector3(randomCircle.x, randomCircle.y, 0f);

            // Only pick free spot
            if (!Physics2D.OverlapCircle(potentialPoint, 0.2f, LayerMask.GetMask("Solid")))
            {
                targetPoint = potentialPoint;
                return;
            }
        }

        // fallback
        targetPoint = startPosition;
    }

    // ---------------- Debug Gizmos ----------------
    private void OnDrawGizmosSelected()
    {
        // Chase radius (always visible - shows detection range)
        Gizmos.color = isChasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        if (!Application.isPlaying)
            return;

        // Patrol radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, patrolRadius);

        // Next patrol target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawSphere(targetPoint, 0.05f);
        
        // Draw line to player when chasing
        if (isChasing && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.transform.position);
        }
    }
}