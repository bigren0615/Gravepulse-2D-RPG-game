using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public SpriteRenderer spriteRenderer;

    [Header("Dash")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float lastDashTime = -Mathf.Infinity;
    private Vector2 lastMoveDir = Vector2.down;

    [Header("Dash Effects")]
    public GameObject dashEffectPrefab;

    [Header("Footsteps")]
    public float footstepInterval = 0.8f;
    private float footstepTimer;

    // Enum arrays for footstep sound names (type-safe)
    private SFXType[] grassFootsteps = { SFXType.FootstepGrass1, SFXType.FootstepGrass2, SFXType.FootstepGrass3 };
    private SFXType[] attackSwooshs = { SFXType.AttackSwoosh1, SFXType.AttackSwoosh2, SFXType.AttackSwoosh3 };
    private SFXType[] attackSlashes = { SFXType.Slash1, SFXType.Slash2, SFXType.Slash3 };

    //Ground detection
    private bool isOnGrass;
    private Vector2 lastPosition;

    [Header("Attack")]
    public float attackCooldown = 0.25f;
    public float attackRange = 1.2f; // How far the attack reaches
    public float attackAngle = 90f; // Attack arc in degrees
    public float attackDamage = 10f; // Damage dealt to enemies
    public LayerMask enemyLayer; // What layer enemies are on
    public bool showAttackDebug = true; // Show attack hitbox visualization
    private float lastAttackTime = -Mathf.Infinity;
    private bool facingLocked = false;
    private Vector2 attackDir;
    private HashSet<Collider2D> hitEnemiesThisAttack = new HashSet<Collider2D>(); // Track hit enemies

    private Vector2 movementInput;
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Automatically fetch SpriteRenderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            Debug.LogError("SpriteRenderer not found on Player! Please add one.");

        lastPosition = rb.position;
    }

    private void Update()
    {
        // Dash input
        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1)) // Shift or Right Click
            {
                StartCoroutine(Dash());
            }
        }

        // Attack input (LEFT CLICK or Z)
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z))
            {
                Attack();
            }
        }

        ReadInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!isDashing) Move(); // Move first
        HandleFootsteps();       // Footsteps after movement
    }

    // 1️ Read input every frame (responsive)
    private void ReadInput()
    {
        if (isDashing) return; // Ignore normal input during dash

        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // Normalize so diagonal is not faster
        movementInput = movementInput.normalized;

        // Check if we're currently in the Attack state
        bool isInAttackState = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

        // Update lastMoveDir only when input is not zero
        if (!facingLocked && !isInAttackState && movementInput != Vector2.zero)
        {
            lastMoveDir = movementInput;
        }

        // Base sprite faces LEFT; flipX when moving right
        // Don't flip while in attack state OR while facing is locked
        if (!facingLocked && !isInAttackState)
        {
            if (movementInput.x > 0)
                spriteRenderer.flipX = true;
            else if (movementInput.x < 0)
                spriteRenderer.flipX = false;
        }
    }

    // 2️ Physics-based movement
    private void Move()
    {
        rb.MovePosition(
            rb.position + movementInput * moveSpeed * Time.fixedDeltaTime
        );
    }

    // 3️ Animation sync
    private void UpdateAnimation()
    {
        bool isMoving = movementInput != Vector2.zero;

        animator.SetBool("isMoving", isMoving);

        // Check if we're currently in the Attack state (any layer, but typically layer 0)
        bool isInAttackState = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

        // Keep using attackDir if we're locked OR still in attack state
        if (facingLocked || isInAttackState)
        {
            animator.SetFloat("moveX", Mathf.Abs(attackDir.x));
            animator.SetFloat("moveY", attackDir.y);
        }
        else if (isMoving)
        {
            animator.SetFloat("moveX", Mathf.Abs(movementInput.x));
            animator.SetFloat("moveY", movementInput.y);
        }
    }

    // 4️ Dash coroutine with trailing effect
    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        // Dash direction = current input
        // Use last movement direction if input is zero
        Vector2 dashDirection = movementInput != Vector2.zero ? movementInput : lastMoveDir;

        // ---- SPAWN DASH EFFECT ----
        if (dashEffectPrefab != null)
        {
            // Instantiate as child so it follows player
            GameObject dashVFX = Instantiate(dashEffectPrefab, transform.position, Quaternion.identity, transform);

            // Offset behind player based on dash direction
            Vector3 offset = -(Vector3)dashDirection * 2f;
            dashVFX.transform.localPosition = offset;

            // Rotate/flip effect to match dash direction (optional, for directional effects)
            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
            dashVFX.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Play animation from first frame
            Animator vfxAnim = dashVFX.GetComponent<Animator>();
            if (vfxAnim != null)
            {
                vfxAnim.Play(vfxAnim.runtimeAnimatorController.animationClips[0].name, 0, 0f);

                // Destroy after animation length
                float clipLength = vfxAnim.runtimeAnimatorController.animationClips[0].length;
                Destroy(dashVFX, clipLength);
            }
            else
            {
                Destroy(dashVFX, 1f); // fallback
            }
        }

        // ---- PLAY DASH SOUND ----
        AudioManager.Instance.PlaySFX(SFXType.Dash);

        // ---- DASH MOVEMENT ----
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
    }

    // 5 Attack method to trigger attack animation and cooldown
    private void Attack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
        AudioManager.Instance.PlayRandomSFX(attackSwooshs);
    }

    // Called by animation event at the start of the attack animation
    public void AttackStart()
    {
        facingLocked = true;
        hitEnemiesThisAttack.Clear(); // Reset hit tracking for new attack

        // Store FULL attack direction (up/down/left/right)
        attackDir = lastMoveDir;

        // Lock sprite flip to attack direction
        if (attackDir.x > 0)
            spriteRenderer.flipX = true;
        else if (attackDir.x < 0)
            spriteRenderer.flipX = false;
    }

    public void AttackEnd()
    {
        facingLocked = false;
    }

    // Called by animation event at the moment of impact (when sword actually hits)
    public void AttackHit()
    {
        DetectAndDamageEnemies();
    }

    // Detect enemies in attack range and damage them
    private void DetectAndDamageEnemies()
    {
        // Get all colliders in attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        bool hitAnyEnemy = false; // Track if we hit at least one enemy

        foreach (Collider2D enemy in hitEnemies)
        {
            // Skip if already hit this attack
            if (hitEnemiesThisAttack.Contains(enemy))
                continue;

            // Calculate direction to enemy
            Vector2 dirToEnemy = (enemy.transform.position - transform.position).normalized;

            // Check if enemy is within attack angle
            float angleToEnemy = Vector2.Angle(attackDir, dirToEnemy);
            
            if (angleToEnemy <= attackAngle / 2f)
            {
                // Enemy is within attack arc - deal damage
                EnemyHealth enemyScript = enemy.GetComponent<EnemyHealth>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(attackDamage);
                    hitEnemiesThisAttack.Add(enemy); // Mark as hit
                    hitAnyEnemy = true; // Mark that we hit an enemy
                    
                    // Visual feedback for debugging
                    if (showAttackDebug)
                    {
                        Debug.DrawLine(transform.position, enemy.transform.position, Color.red, 0.5f);
                    }
                }
            }
        }

        // Play slash sound if we hit any enemy
        if (hitAnyEnemy)
        {
            AudioManager.Instance.PlayRandomSFX(attackSlashes);
        }
    }

    // 6 Footstep sounds based on movement and timing
    private void HandleFootsteps()
    {
        // Do not play footsteps while dashing
        if (isDashing)
        {
            footstepTimer = 0f;
            return;
        }

        float moved = Vector2.Distance(rb.position, lastPosition);
        lastPosition = rb.position;

        // Only play when moving
        if (moved > 0.01f)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                if (isOnGrass)
                {
                    AudioManager.Instance.PlayRandomSFX(grassFootsteps);
                }

                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f; // reset when idle
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Grass"))
        {
            isOnGrass = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Grass"))
        {
            isOnGrass = false;
        }
    }

    // Debug visualization of attack hitbox
    private void OnDrawGizmosSelected()
    {
        if (!showAttackDebug) return;

        // Draw attack range circle
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw attack arc
        Vector2 direction = facingLocked ? attackDir : lastMoveDir;
        Vector3 playerPos = transform.position;
        
        // Draw center line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerPos, playerPos + (Vector3)direction * attackRange);

        // Draw attack cone edges
        Gizmos.color = Color.cyan;
        float halfAngle = attackAngle / 2f;
        
        // Left edge
        Vector2 leftDir = Rotate(direction, -halfAngle);
        Gizmos.DrawLine(playerPos, playerPos + (Vector3)leftDir * attackRange);
        
        // Right edge
        Vector2 rightDir = Rotate(direction, halfAngle);
        Gizmos.DrawLine(playerPos, playerPos + (Vector3)rightDir * attackRange);
        
        // Draw arc
        int segments = 20;
        Vector3 previousPoint = playerPos + (Vector3)leftDir * attackRange;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
            Vector2 dir = Rotate(direction, angle);
            Vector3 point = playerPos + (Vector3)dir * attackRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }

    // Helper function to rotate a vector by an angle
    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
