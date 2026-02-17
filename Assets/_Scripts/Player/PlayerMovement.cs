using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControllerHK : MonoBehaviour
{
    [Header("Горизонталь")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 14f;
    [SerializeField] private float frictionAmount = 0.5f;

    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float fallMultiplier = 4.5f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("Планирование (Плащ)")]
    [SerializeField] private float glideGravityScale = 0.8f;

    [Header("Зацеп за уступ (Climb)")]
    [SerializeField] private Transform wallCheck; // Точка на уровне груди
    [SerializeField] private Transform ledgeCheck; // Точка чуть выше головы
    [SerializeField] private float wallCheckDistance = 0.4f;
    private bool isTouchingWall;
    private bool isTouchingLedge;
    private bool isHanging;

    [Header("Проверки")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.25f;
    [SerializeField] private LayerMask whatIsGround;

    private Rigidbody2D rb;
    private Animator anim;

    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool isJumping;
    private bool isGliding;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // 1. Сбор ввода
        moveInput = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput = 1;

        // 2. Таймеры и проверки
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        // Проверка уступа: стена перед нами есть, а над головой — пусто
        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
        isTouchingLedge = Physics2D.Raycast(ledgeCheck.position, transform.right, wallCheckDistance, whatIsGround);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isHanging = false; // На земле не висим
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Логика зацепа
        if (isTouchingWall && !isTouchingLedge && !isGrounded && rb.linearVelocity.y < 0.1f)
        {
            if (!isHanging) EnterHanging();
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // 3. Логика Планирования
        if (!isGrounded && !isHanging && rb.linearVelocity.y < 0 && Keyboard.current.spaceKey.isPressed)
            isGliding = true;
        else
            isGliding = false;

        // 4. Логика Прыжка (из любого состояния: с земли или с уступа)
        if (jumpBufferCounter > 0f)
        {
            if (coyoteTimeCounter > 0f && !isJumping)
            {
                StartJump();
            }
            else if (isHanging) // Прыжок с уступа
            {
                ExitHanging();
                StartJump();
            }
        }

        // Прыжок короче при отпускании кнопки
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0 && isJumping)
        {
            rb.AddForce(Vector2.down * rb.linearVelocity.y * (1 - jumpCutMultiplier), ForceMode2D.Impulse);
            jumpBufferCounter = 0;
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        UpdateAnimations();
    }

    private void EnterHanging()
    {
        isHanging = true;
        isJumping = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0; // "Прилипаем" к уступу
    }

    private void ExitHanging()
    {
        isHanging = false;
        rb.gravityScale = 3f;
    }

    private void StartJump()
    {
        float force = jumpForce;
        if (rb.linearVelocity.y < 0) force -= rb.linearVelocity.y;

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        jumpBufferCounter = 0;
        coyoteTimeCounter = 0;
        isJumping = true;
    }

    void FixedUpdate()
    {
        if (!isHanging) // Если висим, не двигаемся по горизонтали физикой
        {
            ApplyMovement();
            ApplyGravity();
        }
    }

    private void ApplyMovement()
    {
        float targetSpeed = moveInput * maxSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, 0.9f) * Mathf.Sign(speedDif);
        rb.AddForce(movement * Vector2.right);

        if (isGrounded && Mathf.Abs(moveInput) < 0.01f)
        {
            float amount = Mathf.Min(Mathf.Abs(rb.linearVelocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.linearVelocity.x);
            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }

    private void ApplyGravity()
    {
        if (isGliding) rb.gravityScale = glideGravityScale;
        else if (rb.linearVelocity.y < 0) rb.gravityScale = fallMultiplier;
        else rb.gravityScale = 3f;

        if (isGrounded && rb.linearVelocity.y <= 0) isJumping = false;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("verticalVe", rb.linearVelocity.y);
        anim.SetBool("isGliding", isGliding);
        anim.SetBool("isHanging", isHanging); // Новое состояние для аниматора
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        // Рисуем лучи проверки уступа
        if (wallCheck != null && ledgeCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + transform.right * wallCheckDistance);
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + transform.right * wallCheckDistance);
        }
    }
}