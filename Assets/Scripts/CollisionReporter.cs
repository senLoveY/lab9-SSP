using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Report(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // A contact that outlasts dash/shield protection must still be dangerous.
        Report(collision);
    }

    private void Report(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && player != null)
            player.ReceiveEnemyHit();
    }
}
