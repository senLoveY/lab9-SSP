using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color defaultColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && spriteRenderer != null)
            spriteRenderer.color = Color.red;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && spriteRenderer != null)
            spriteRenderer.color = defaultColor;
    }
}
