using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float jumpSpeed = 12f;

    [Header("Feel")]
    [SerializeField] float fallMultiplier = 2.5f;   // тяжёлое падение
    [SerializeField] float lowJumpMultiplier = 2f;  // отпустил Space — короткий прыжок
    [SerializeField] float coyoteTime = 0.1f;       // прыжок сразу после схода с края
    [SerializeField] float jumpBuffer = 0.1f;       // нажал Space за миг до приземления — засчитается

    [Header("Ground Check")]
    [Tooltip("Опционально. Если не задан (или указан не дочерний объект кота) — точка проверки берётся от нижней грани коллайдера кота.")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.15f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Collider Polish")]
    [SerializeField] float colliderEdgeRadius = 0.02f;

    Rigidbody2D rb;
    int facing = 1;
    float inputX;
    float coyoteTimer;
    float jumpBufferTimer;
    bool jumpHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Без трения — кот не «липнет» к боковинам платформ при движении к стене в воздухе.
        rb.sharedMaterial = new PhysicsMaterial2D("CatFrictionless") { friction = 0f, bounciness = 0f };

        // Скруглённые углы коллайдера — не цепляется за края платформ.
        var box = GetComponent<BoxCollider2D>();
        if (box != null && colliderEdgeRadius > 0f) box.edgeRadius = colliderEdgeRadius;

        facing = transform.localScale.x >= 0f ? 1 : -1;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        inputX = 0f;
        if (kb.aKey.isPressed) inputX -= 1f;
        if (kb.dKey.isPressed) inputX += 1f;

        if (inputX > 0f && facing != 1) SetFacing(1);
        else if (inputX < 0f && facing != -1) SetFacing(-1);

        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) jumpBufferTimer = jumpBuffer;
        jumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (IsGrounded() && rb.linearVelocity.y <= 0.01f) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;

        jumpBufferTimer -= dt;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * dt;
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * dt;
    }

    // --- Состояние для анимации ---
    public bool IsOnGround => IsGrounded();
    public float HorizontalSpeed => rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

    bool IsGrounded()
    {
        Vector2 point = GetGroundCheckPoint();
        // Запросы не должны хватать собственный коллайдер кота — иначе IsGrounded == true в воздухе.
        Collider2D hit = Physics2D.OverlapCircle(point, groundCheckRadius, groundMask);
        if (hit == null) return false;
        return hit.transform != transform && !hit.transform.IsChildOf(transform);
    }

    Vector2 GetGroundCheckPoint()
    {
        // Используем groundCheck только если он указывает на дочерний объект кота —
        // иначе точка проверки «прилипает» к чужому объекту и кот считается заземлённым где попало.
        if (groundCheck != null && groundCheck.IsChildOf(transform))
            return groundCheck.position;

        var col = GetComponent<Collider2D>();
        if (col != null)
            return new Vector2(col.bounds.center.x, col.bounds.min.y);

        return transform.position;
    }

    void SetFacing(int dir)
    {
        facing = dir;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetGroundCheckPoint(), groundCheckRadius);
    }
}
