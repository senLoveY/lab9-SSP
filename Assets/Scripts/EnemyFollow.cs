using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFollow : MonoBehaviour
{
    public Transform target;
    [Min(0f)] public float followSpeed = 2f;

    private Rigidbody2D rb;
    private SurvivalGame game;
    public Rigidbody2D Body => rb;

    public void Configure(SurvivalGame owner, Transform player, float speed)
    {
        game = owner;
        target = player;
        followSpeed = speed;
    }

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
        if (target == null || (game != null && !game.IsPlaying))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speed = followSpeed * (game != null ? game.EnemySpeedMultiplier : 1f);
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, target.position, speed * Time.fixedDeltaTime);
        // Динамическое тело движется со скоростью и участвует в столкновениях.
        rb.linearVelocity = (nextPosition - rb.position) / Time.fixedDeltaTime;
    }
}
