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

    [Header("Audio")]
    public AudioClip dashSFX;       // assign your MP3 here in Inspector
    public AudioClip[] footstepClips;

    public float footstepInterval = 0.8f; // time between steps
    private int lastFootstepIndex = -1;
    private float footstepTimer;

    private AudioSource audioSource; // will grab AudioSource component

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    private bool isOnGrass;
    private Vector2 lastPosition;

    [Header("Attack")]
    public float attackCooldown = 0.25f;
    private float lastAttackTime = -Mathf.Infinity;
    private bool isAttacking = false;
    private bool facingLocked = false;
    private bool attackFacingRight;
    private Vector2 attackDir;

    private Vector2 movementInput;
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogError("AudioSource missing on Player!");

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

        // Update lastMoveDir only when input is not zero
        if (!isAttacking && movementInput != Vector2.zero)
        {
            lastMoveDir = movementInput;
        }

        // Base sprite faces LEFT; flipX when moving right
        if (facingLocked)
        {
            // Force stored facing direction
            spriteRenderer.flipX = attackFacingRight;
        }
        else
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

        if (isAttacking)
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
        if (dashSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(dashSFX);
        }

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
    }

    // Called by animation event at the start of the attack animation
    public void AttackStart()
    {
        isAttacking = true;
        facingLocked = true;

        // Store facing direction ONCE
        attackFacingRight = spriteRenderer.flipX;

        // Store FULL attack direction (up/down/left/right)
        attackDir = lastMoveDir;
    }

    public void AttackEnd()
    {
        isAttacking = false;
        facingLocked = false;
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
                if (isOnGrass) PlayFootstepGrass();

                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f; // reset when idle
        }
    }

    private void PlayFootstepGrass()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        } while (index == lastFootstepIndex && footstepClips.Length > 1);

        lastFootstepIndex = index;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(footstepClips[index], 0.6f);
        audioSource.pitch = 1f;
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
}
