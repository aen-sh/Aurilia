using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControllerHK : MonoBehaviour
{
    [Header("Горизонталь")]
    [SerializeField] private float maxSpeed = 10f;       // Макс. скорость (попробуй 9-11)
    [SerializeField] private float acceleration = 12f;  // Сила разгона (чем выше, тем быстрее отклик)
    [SerializeField] private float deceleration = 14f;  // Сила торможения
    [SerializeField] private float frictionAmount = 0.5f; // Трение при остановке

    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 16f;      // Сила прыжка
    [SerializeField] private float jumpCutMultiplier = 0.5f; // Насколько сильно режется прыжок при отпускании
    [SerializeField] private float fallMultiplier = 4.5f;    // Быстрое падение (тяжесть)
    [SerializeField] private float jumpBufferTime = 0.15f;   // Буфер нажатия
    [SerializeField] private float coyoteTime = 0.1f;       // Койот-тайм

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Важные настройки физики для веса
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // 1. Сбор ввода (New Input System)
        moveInput = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput = 1;

        // 2. Таймеры и проверки
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // 3. Логика Прыжка
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isJumping)
        {
            StartJump();
        }

        // Variable Jump Height: если отпустили кнопку в полете вверх
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && rb.linearVelocity.y > 0 && isJumping)
        {
            rb.AddForce(Vector2.down * rb.linearVelocity.y * (1 - jumpCutMultiplier), ForceMode2D.Impulse);
            jumpBufferCounter = 0;
        }

        // Поворот спрайта
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        UpdateAnimations();
    }

    private void StartJump()
    {
        float force = jumpForce;
        if (rb.linearVelocity.y < 0) force -= rb.linearVelocity.y; // Компенсация падения

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        jumpBufferCounter = 0;
        coyoteTimeCounter = 0;
        isJumping = true;
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravity();
    }

    private void ApplyMovement()
    {
        // Вычисляем целевую скорость
        float targetSpeed = moveInput * maxSpeed;

        // Плавное нарастание скорости (убирает "дешевый" моментальный переход)
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Применяем силу (квадратичное ускорение для сочности)
        float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, 0.9f) * Mathf.Sign(speedDif);
        rb.AddForce(movement * Vector2.right);

        // Трение на земле для мгновенной остановки (как в HK)
        if (isGrounded && Mathf.Abs(moveInput) < 0.01f)
        {
            float amount = Mathf.Min(Mathf.Abs(rb.linearVelocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.linearVelocity.x);
            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }

    private void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0) // Если падаем
        {
            rb.gravityScale = fallMultiplier;
        }
        else
        {
            rb.gravityScale = 3f; // Обычный вес прыжка
        }

        if (isGrounded && rb.linearVelocity.y <= 0)
        {
            isJumping = false;
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetBool("IsMoving", Mathf.Abs(moveInput) > 0.01f);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }
}