using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 5f;
    [Min(0f)] public float sprintMultiplier = 2f;
    [Min(0f)] public float rotateSpeed = 180f;

    [Header("Dash")]
    [Min(0.01f)] public float dashDuration = 0.18f;
    [Min(0.1f)] public float dashCooldown = 2.5f;
    [Min(0f)] public float dashSpeed = 16f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float rotationInput;
    private bool isSprinting;
    private Vector2 lastDirection = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndsAt;
    private float dashReadyAt;
    private float protectionEndsAt;
    private float speedEndsAt;
    private SpriteRenderer visual;
    private Collider2D bodyCollider;
    private Color baseColor;
    private SurvivalGame game;

    public bool HasShield { get; private set; }
    public bool IsDashing => Time.time < dashEndsAt;
    public bool IsProtected => IsDashing || Time.time < protectionEndsAt;
    public float DashRemaining => Mathf.Max(0f, dashReadyAt - Time.time);
    public float SpeedRemaining => Mathf.Max(0f, speedEndsAt - Time.time);
    public Rigidbody2D Body => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        bodyCollider = GetComponent<Collider2D>();
        visual = GetComponent<SpriteRenderer>();
        baseColor = visual != null ? visual.color : Color.cyan;
    }

    public void Prepare(SurvivalGame owner, Vector2 position)
    {
        game = owner;
        rb.simulated = true;
        rb.position = position;
        rb.rotation = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        movement = Vector2.zero;
        lastDirection = Vector2.right;
        rotationInput = 0f;
        isSprinting = false;
        dashEndsAt = dashReadyAt = protectionEndsAt = speedEndsAt = 0f;
        HasShield = false;
        if (visual != null) visual.color = baseColor;
    }

    private void Update()
    {
        if (game != null && !game.IsPlaying) return;
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (movement.sqrMagnitude > 0f) lastDirection = movement;

        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput += 1f;
        if (Input.GetKey(KeyCode.E)) rotationInput -= 1f;
        if (Input.GetKeyDown(KeyCode.Space)) TryDash(lastDirection);

        if (visual != null)
        {
            Color color = HasShield ? new Color(0.35f, 1f, 0.8f) : baseColor;
            if (SpeedRemaining > 0f) color = new Color(1f, 0.8f, 0.25f);
            if (IsProtected) color = Color.Lerp(color, Color.white, 0.65f);
            visual.color = color;
        }
    }

    private void FixedUpdate()
    {
        if (game != null && !game.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        float currentSpeed = moveSpeed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        if (SpeedRemaining > 0f) currentSpeed *= 1.45f;

        rb.linearVelocity = IsDashing ? dashDirection * dashSpeed : movement * currentSpeed;
        rb.MoveRotation(rb.rotation + rotationInput * rotateSpeed * Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        if (game == null || !game.IsPlaying) return;
        // Animated scaling can grow the collider into a wall; keep the full rotated box inside.
        Bounds bounds = bodyCollider.bounds;
        Vector2 margin = new Vector2(bounds.extents.x, bounds.extents.y);
        Vector2 safe = game.ClampToArena(rb.position, margin);
        if ((safe - rb.position).sqrMagnitude > 0.001f) rb.position = safe;
    }

    public bool TryDash(Vector2 direction)
    {
        if (game == null || !game.IsPlaying || DashRemaining > 0f) return false;
        dashDirection = direction.sqrMagnitude > 0f ? direction.normalized : lastDirection;
        dashEndsAt = Time.time + dashDuration;
        dashReadyAt = Time.time + dashCooldown;
        return true;
    }

    public void GrantShield() { HasShield = true; }
    public void GrantSpeed(float seconds) { speedEndsAt = Time.time + seconds; }

    public void ReceiveEnemyHit()
    {
        if (game == null || !game.IsPlaying || IsProtected) return;
        if (HasShield)
        {
            HasShield = false;
            protectionEndsAt = Time.time + 1f;
            game.RepelEnemies(rb.position);
            game.ShowNotice("ЩИТ ПОГЛОТИЛ УДАР", new Color(0.35f, 1f, 0.8f));
            return;
        }
        if (visual != null) visual.color = new Color(1f, 0.2f, 0.3f);
        game.EndRound();
    }
}
