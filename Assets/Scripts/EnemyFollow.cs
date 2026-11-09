using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFollow : MonoBehaviour
{
    public Transform target;
    [Min(0f)] public float followSpeed = 2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(rb.position, target.position, followSpeed * Time.fixedDeltaTime);
        // Динамическое тело движется со скоростью и участвует в столкновениях.
        rb.linearVelocity = (nextPosition - rb.position) / Time.fixedDeltaTime;
    }
}
