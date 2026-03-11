using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private PlayerControls controls;
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D col;

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

    // ---- Dash cooldown accessors for UI ----
    /// <summary>0 = cooldown just started, 1 = fully ready (use this for UI fill)</summary>
    public float DashCooldownProgress =>
        Mathf.Clamp01((Time.unscaledTime - lastDashTime) / dashCooldown);
    public bool IsDashReady => !isDashing && Time.unscaledTime >= lastDashTime + dashCooldown;
    public float DashCooldownDuration => dashCooldown;

    // ---- Attack cooldown accessors for UI ----
    /// <summary>0 = cooldown just started, 1 = fully ready (use this for UI fill)</summary>
    public float AttackCooldownProgress =>
        Mathf.Clamp01((Time.unscaledTime - lastAttackTime) / attackCooldown);
    public bool IsAttackReady => Time.unscaledTime >= lastAttackTime + attackCooldown;

    [Header("Dash Effects")]
    public GameObject dashEffectPrefab;

    [Header("Collision")]
    [Tooltip("Solid obstacles layer — walls the player must never enter. Auto-initialised to the 'Solid' layer if left empty.")]
    public LayerMask solidLayer;

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

    // ---- Vital View afterimage trail ----
    private Coroutine afterimageCoroutine;
    private bool wasInVitalView;

    // ---- Parry ----
    private Coroutine parryFacingCoroutine;
    private Coroutine parryHitstopCoroutine;
    [Tooltip("Seconds (real-time) after parry input before hitstop+SFX fires. Tune to match the clash frame of your Parry animation. Set to 0 once you wire the ParryImpact animation event.")]
    [SerializeField] private float parryImpactDelay = 0.1f;

    [Header("Parry Spark VFX")]
    [Tooltip("How far in front of the player (along attackDir) the spark spawns — tune to sit at the weapon tip.")]
    [SerializeField] private float parrySparkOffset = 0.5f;



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        controls = new PlayerControls();

        // Auto-resolve solid layer by name if not set in Inspector
        if (solidLayer.value == 0)
            solidLayer = LayerMask.GetMask("Solid");

        // Automatically fetch SpriteRenderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            Debug.LogError("SpriteRenderer not found on Player! Please add one.");

        lastPosition = rb.position;
    }

    private void Update()
    {
        // Dash input — use unscaled time so cooldown works correctly during Vital View bullet time
        if (!isDashing && Time.unscaledTime >= lastDashTime + dashCooldown)
        {
            if (controls.Player.Dash.triggered) // Shift or Right Click
            {
                // Suppress right-click dash while cursor is visible (Left Alt held)
                bool mouseBlocked = CursorManager.IsCursorVisible &&
                                    Mouse.current != null &&
                                    Mouse.current.rightButton.wasPressedThisFrame;
                if (!mouseBlocked)
                    StartCoroutine(Dash());
            }
        }

        // Attack input (LEFT CLICK or Z)
        // Use unscaled time so the cooldown isn't stretched during Vital View bullet time
        if (Time.unscaledTime >= lastAttackTime + attackCooldown)
        {
            if (controls.Player.Attack.triggered)
            {
                // Suppress left-click attack while cursor is visible (Left Alt held)
                bool mouseBlocked = CursorManager.IsCursorVisible &&
                                    Mouse.current != null &&
                                    Mouse.current.leftButton.wasPressedThisFrame;
                if (!mouseBlocked)
                    Attack();
            }
        }

        // Parry input (Space) — succeeds only when an enemy shows a yellow warning indicator
        if (controls.Player.Parry.triggered)
        {
            AttemptParry();
        }

        ReadInput();
        UpdateAnimation();

        // Keep player animation running at full speed during Vital View bullet time
        bool inVitalView = GameManager.Instance != null && GameManager.Instance.IsVitalViewActive();
        animator.updateMode = inVitalView ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;

        // Start/stop afterimage trail on VitalView edge transitions
        if (inVitalView && !wasInVitalView)
        {
            if (afterimageCoroutine != null) StopCoroutine(afterimageCoroutine);
            afterimageCoroutine = StartCoroutine(AfterimageLoop());
        }
        else if (!inVitalView && wasInVitalView && afterimageCoroutine != null)
        {
            StopCoroutine(afterimageCoroutine);
            afterimageCoroutine = null;
        }
        wasInVitalView = inVitalView;
    }

    private void FixedUpdate()
    {
        if (!isDashing) Move(); // Move first
        HandleFootsteps();       // Footsteps after movement
        DepenetrateFromSolids(); // Continuously push out of any wall overlap (catches dash tunnelling)
    }
    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
        // Safety net: restore enemy collision if the object is disabled or destroyed mid-dash.
        SetEnemyCollisionIgnored(false);
    }

    // 1️ Read input every frame (responsive)
    private void ReadInput()
    {
#if UNITY_WEBGL
        movementInput = Vector2.zero;

        // Keyboard (WASD) — works on desktop browser
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) movementInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed) movementInput.x += 1f;
            if (Keyboard.current.wKey.isPressed) movementInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) movementInput.y -= 1f;
        }

        // Virtual joystick (OnScreenStick feeds into controls.Player.Move) — works on mobile browser
        Vector2 joystickInput = controls.Player.Move.ReadValue<Vector2>();
        if (joystickInput.sqrMagnitude > movementInput.sqrMagnitude)
            movementInput = joystickInput;

        // Normalize diagonal
        movementInput = movementInput.normalized;
#else
    // Full multiplatform for Windows
    if (isDashing) return;

    movementInput = controls.Player.Move.ReadValue<Vector2>();
    movementInput = movementInput.normalized;
#endif

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
        // During Vital View bullet time (timeScale ≈ 0.15), use unscaled delta so player
        // moves at full real-time speed while everything else is slowed down.
        bool inBulletTime = GameManager.Instance != null && GameManager.Instance.IsVitalViewActive();
        float dt = inBulletTime ? Time.unscaledDeltaTime : Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movementInput * moveSpeed * dt);
    }

    // 3️ Animation sync
    private void UpdateAnimation()
    {
        bool isMoving = movementInput != Vector2.zero;

        animator.SetBool("isMoving", isMoving);

        // Check if we're currently in the Attack or Parry state (any layer, but typically layer 0)
        bool isInAttackState = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
        bool isInParryState = animator.GetCurrentAnimatorStateInfo(0).IsName("Parry");

        // Keep using attackDir if we're locked OR still in attack/parry state
        if (facingLocked || isInAttackState || isInParryState)
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
        lastDashTime = Time.unscaledTime; // Unscaled so cooldown is real-time based

        // Dash direction = current input
        // Use last movement direction if input is zero
        Vector2 dashDirection = movementInput != Vector2.zero ? movementInput : lastMoveDir;

        // ---- SPAWN DASH EFFECT ----
        // Declared outside the if-block so the VitalView section can update it
        // if the dodge direction changes after the effect is already spawned.
        GameObject dashVFX = null;
        if (dashEffectPrefab != null)
        {
            // Instantiate as child so it follows player
            dashVFX = Instantiate(dashEffectPrefab, transform.position, Quaternion.identity, transform);

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
                // Always use unscaled time — harmless at normal speed, prevents freezing during Vital View.
                // (The VFX spawns before TriggerVitalView() is called, so checking IsVitalViewActive() here is always false.)
                vfxAnim.updateMode = AnimatorUpdateMode.UnscaledTime;

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

        // ---- CHECK FOR VITAL VIEW (ZZZ-style dodge) ----
        // If any enemy is in its warning window, dashing triggers bullet time
        EnemyCombat[] allEnemies = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None);
        Debug.Log($"[VitalView] Dash! Scanning {allEnemies.Length} enemy/enemies for open ready window...");
        bool vitalViewTriggered = false;
        foreach (EnemyCombat ec in allEnemies)
        {
            Debug.Log($"[VitalView]   '{ec.gameObject.name}' -> IsInReadyWindow = {ec.IsInReadyWindow()}");
            if (ec.IsInReadyWindow())
            {
                Debug.Log($"[VitalView] MATCH — triggering bullet time via '{ec.gameObject.name}'!");

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerVitalView();
                    AudioManager.Instance.PlaySFX(SFXType.Swoosh); // ZZZ-style whoosh cue on perfect dodge
                }
                else
                    Debug.LogError("[VitalView] GameManager.Instance is NULL! Ensure a GameManager exists in the scene.");

                PlayerHealth ph = GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.SetTemporaryInvincible(1.0f); // real-time seconds — matches VitalView duration
                else
                    Debug.LogError("[VitalView] PlayerHealth component not found on this GameObject!");

                // Enhanced ZZZ-style dodge: escape PERPENDICULAR to the attack's path.
                // The old "away from enemy center" vector equals the attack direction
                // (enemy attacks toward the player), so the player was dashing further along
                // the attack axis — not out of it. Stepping 90° off the axis guarantees
                // the player exits the attack's forward area.
                Vector2 attackAxis = ec.GetAttackDirection();
                if (attackAxis == Vector2.zero)
                    attackAxis = ((Vector2)transform.position - (Vector2)ec.transform.position).normalized;
                if (attackAxis == Vector2.zero)
                    attackAxis = lastMoveDir;

                // Two perpendicular escape options
                Vector2 perpCCW = new Vector2(-attackAxis.y, attackAxis.x);  // 90° counter-clockwise
                Vector2 perpCW = new Vector2(attackAxis.y, -attackAxis.x);  // 90° clockwise

                // Pick the perpendicular closest to the player's current movement intent
                Vector2 inputRef = movementInput != Vector2.zero ? movementInput : lastMoveDir;
                Vector2 escapePerp = Vector2.Dot(inputRef, perpCCW) >= Vector2.Dot(inputRef, perpCW)
                    ? perpCCW : perpCW;

                // Blend: mostly perpendicular (exits attack path) + slight away-bias (feels like a back-dodge)
                dashDirection = (escapePerp + attackAxis * 0.5f).normalized;

                // Realign the already-spawned VFX to match the corrected direction
                if (dashVFX != null)
                {
                    dashVFX.transform.localPosition = -(Vector3)dashDirection * 2f;
                    float vfxAngle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
                    dashVFX.transform.rotation = Quaternion.Euler(0, 0, vfxAngle);
                }

                vitalViewTriggered = true;
                break; // one trigger per dash
            }
        }
        if (!vitalViewTriggered)
            Debug.Log("[VitalView] No enemy in ready window — normal dash.");

        // Phase through enemy bodies for the entire dash — enemies must not block or cage the player.
        // Physics2D.IgnoreLayerCollision is global but immediately reversed at dash end / OnDisable.
        SetEnemyCollisionIgnored(true);

        // ---- DASH MOVEMENT (unscaled-time aware, wall-collision safe) ----
        // Cast ahead each step so the dash stops at solid walls instead of tunnelling through them.
        ContactFilter2D dashFilter = new ContactFilter2D();
        dashFilter.SetLayerMask(solidLayer);
        dashFilter.useTriggers = false;
        RaycastHit2D[] castHits = new RaycastHit2D[1];
        const float skinWidth = 0.05f;

        float startTime = Time.unscaledTime;
        while (Time.unscaledTime < startTime + dashDuration)
        {
            bool inBulletTime = GameManager.Instance != null && GameManager.Instance.IsVitalViewActive();
            float dt = inBulletTime ? Time.unscaledDeltaTime : Time.fixedDeltaTime;
            float stepDist = dashSpeed * dt;

            // Wall check: cast the player's collider forward by stepDist + skin
            if (solidLayer.value != 0 && col != null)
            {
                int hits = col.Cast(dashDirection, dashFilter, castHits, stepDist + skinWidth);
                if (hits > 0)
                {
                    // Move only up to the wall surface (minus skin gap), then stop
                    float safeDist = Mathf.Max(0f, castHits[0].distance - skinWidth);
                    if (safeDist > 0f)
                        rb.MovePosition(rb.position + dashDirection * safeDist);
                    break;
                }
            }

            rb.MovePosition(rb.position + dashDirection * stepDist);
            yield return new WaitForFixedUpdate();
        }

        SetEnemyCollisionIgnored(false);
        isDashing = false;
    }

    // 5 Attack method to trigger attack animation and cooldown
    private void Attack()
    {
        lastAttackTime = Time.unscaledTime;
        animator.SetTrigger("Attack");
        AudioManager.Instance.PlayRandomSFX(attackSwooshs);
    }

    // Parry attempt: press Space during an enemy's yellow (parryable) warning window
    private void AttemptParry()
    {
        EnemyCombat[] allEnemies = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None);
        foreach (EnemyCombat ec in allEnemies)
        {
            if (ec.IsInReadyWindow() && ec.IsParryable())
            {
                if (ec.TryParry())
                {
                    // Snap to the cardinal direction toward the attacking enemy
                    // so the correct directional Parry clip plays from the first frame
                    Vector2 raw = (ec.transform.position - transform.position).normalized;
                    Vector2 parryDir = Mathf.Abs(raw.x) >= Mathf.Abs(raw.y)
                        ? new Vector2(Mathf.Sign(raw.x), 0f)
                        : new Vector2(0f, Mathf.Sign(raw.y));

                    // Lock facing toward the attacker (mirrors how AttackStart works)
                    attackDir = parryDir;
                    lastMoveDir = parryDir;
                    facingLocked = true;

                    // Flip sprite immediately before the trigger fires
                    if (parryDir.x > 0f)
                        spriteRenderer.flipX = true;
                    else if (parryDir.x < 0f)
                        spriteRenderer.flipX = false;

                    // Push blend-tree params so the Parry state reads them from frame 1
                    animator.SetFloat("moveX", Mathf.Abs(parryDir.x));
                    animator.SetFloat("moveY", parryDir.y);
                    animator.SetTrigger("Parry");

                    // Fire SFX + hitstop at the clash impact frame, not at button-press.
                    // Tune parryImpactDelay in the Inspector to match your Parry animation.
                    // Once you add a ParryImpact() animation event at the clash frame,
                    // this coroutine becomes the fallback only.
                    if (parryHitstopCoroutine != null) StopCoroutine(parryHitstopCoroutine);
                    parryHitstopCoroutine = StartCoroutine(ParryHitstopCoroutine());

                    // Failsafe: release facing lock after 1s real-time in case
                    // ParryEnd animation event is not yet configured
                    if (parryFacingCoroutine != null) StopCoroutine(parryFacingCoroutine);
                    parryFacingCoroutine = StartCoroutine(ParryFacingFailsafe(1f));

                    Debug.Log($"[Parry] SUCCESS! Facing: {parryDir}");
                    return;
                }
            }
        }
        Debug.Log("[Parry] No parryable window open — whiff.");
    }

    // Delays hitstop+SFX until the clash impact frame (tunable via parryImpactDelay).
    // Superseded frame-perfectly once the ParryImpact animation event is wired up.
    private IEnumerator ParryHitstopCoroutine()
    {
        if (parryImpactDelay > 0f)
            yield return new WaitForSecondsRealtime(parryImpactDelay);
        AudioManager.Instance?.PlaySFX(SFXType.Clash);
        GameManager.Instance?.TriggerHitstop();
        parryHitstopCoroutine = null;
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

    /// <summary>
    /// Called by animation event at the clash / sword-contact frame of the Parry animation.
    /// Cancels the fallback delay coroutine and fires SFX + hitstop immediately (frame-perfect).
    /// Add this as an Animation Event in the Parry clip at the frame the sword makes contact.
    /// </summary>
    public void ParryImpact()
    {
        if (parryHitstopCoroutine != null)
        {
            StopCoroutine(parryHitstopCoroutine);
            parryHitstopCoroutine = null;
        }
        AudioManager.Instance?.PlaySFX(SFXType.Clash);

        // Spawn sparkle BEFORE hitstop so it appears on the exact freeze frame
        Vector3 spawnPos = transform.position + (Vector3)(attackDir.normalized * parrySparkOffset);
        ParrySparkEffect.Spawn(spawnPos, attackDir, spriteRenderer);

        GameManager.Instance?.TriggerHitstop();
    }

    // Called by animation event at the end of the Parry animation
    public void ParryEnd()
    {
        if (parryFacingCoroutine != null)
        {
            StopCoroutine(parryFacingCoroutine);
            parryFacingCoroutine = null;
        }
        facingLocked = false;
    }

    // Real-time failsafe so facing is never stuck if ParryEnd event isn't wired yet
    private IEnumerator ParryFacingFailsafe(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        facingLocked = false;
        parryFacingCoroutine = null;
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

    // ---- Vital View afterimage trail methods ----

    private IEnumerator AfterimageLoop()
    {
        while (GameManager.Instance != null && GameManager.Instance.IsVitalViewActive())
        {
            SpawnAfterimage();
            yield return new WaitForSecondsRealtime(0.05f); // ~20 ghosts/second in real time
        }
        afterimageCoroutine = null;
    }

    private void SpawnAfterimage()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject ghost = new GameObject("VV_Afterimage");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();
        ghostSR.sprite = spriteRenderer.sprite;
        ghostSR.flipX = spriteRenderer.flipX;
        ghostSR.flipY = spriteRenderer.flipY;
        ghostSR.sortingLayerName = spriteRenderer.sortingLayerName;
        ghostSR.sortingOrder = spriteRenderer.sortingOrder - 1; // always just behind player
        ghostSR.color = new Color(1f, 0.75f, 0.3f, 0.65f); // warm orange, semi-transparent

        StartCoroutine(FadeAfterimage(ghostSR, 0.22f));
    }

    private IEnumerator FadeAfterimage(SpriteRenderer sr, float duration)
    {
        float startAlpha = sr.color.a;
        Color c = sr.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (sr == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            sr.color = c;
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    // Helper function to rotate a vector by an angle
    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Pushes the player out of any solid collider they are overlapping.
    // Runs every FixedUpdate so it also recovers an already-stuck player.
    private void DepenetrateFromSolids()
    {
        if (solidLayer.value == 0 || col == null) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(solidLayer);
        filter.useTriggers = false;

        Collider2D[] overlaps = new Collider2D[8];
        int count = Physics2D.OverlapCollider(col, filter, overlaps);
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i] == null) continue;
            ColliderDistance2D dist = col.Distance(overlaps[i]);
            // dist.distance is negative when overlapping; dist.normal points from wall toward player.
            // Threshold 0.05 ignores normal wall-touching (≈0) which physics handles fine.
            // Only fire for real tunnelling penetration (dashing into a wall), so Move() and
            // DepenetrateFromSolids() never fight each other and cause the sticky-wall bug.
            if (dist.distance < -0.05f)
                rb.MovePosition(rb.position + dist.normal * -dist.distance);
        }
    }

    // Phases the player through all enemy-layer colliders.
    // Called at dash start (ignore=true) and dash end (ignore=false).
    // Physics2D.IgnoreLayerCollision is global but safely reversed — single-player, one dash at a time.
    private void SetEnemyCollisionIgnored(bool ignore)
    {
        int playerLayer = gameObject.layer;
        int maskValue = enemyLayer.value;
        for (int i = 0; i < 32; i++)
            if ((maskValue & (1 << i)) != 0)
                Physics2D.IgnoreLayerCollision(playerLayer, i, ignore);
    }
}
