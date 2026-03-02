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

        ReadInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!isDashing) Move(); // Normal movement only when not dashing
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
        if (movementInput != Vector2.zero)
        {
            lastMoveDir = movementInput;
        }

        // Base sprite faces LEFT; flipX when moving right
        if (movementInput.x > 0) spriteRenderer.flipX = true;
        else if (movementInput.x < 0) spriteRenderer.flipX = false;
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

        if (isMoving)
        {
            animator.SetFloat("moveX", Mathf.Abs(movementInput.x));
            animator.SetFloat("moveY", movementInput.y);
        }
    }

    // 4️ Dash coroutine
    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        // Dash direction = current input
        Vector2 dashDirection = movementInput;

        // Use last movement direction if input is zero
        if (dashDirection == Vector2.zero)
            dashDirection = lastMoveDir;

        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
    }
}
