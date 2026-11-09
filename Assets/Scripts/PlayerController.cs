using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 5f;
    [Min(0f)] public float sprintMultiplier = 2f;
    [Min(0f)] public float rotateSpeed = 180f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private float rotationInput;
    private bool isSprinting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput += 1f;
        if (Input.GetKey(KeyCode.E)) rotationInput -= 1f;
    }

    private void FixedUpdate()
    {
        float currentSpeed = moveSpeed;
        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        rb.linearVelocity = movement * currentSpeed;
        rb.MoveRotation(rb.rotation + rotationInput * rotateSpeed * Time.fixedDeltaTime);
    }
}
