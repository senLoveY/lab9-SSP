using UnityEngine;

public enum BonusKind { Shield, Slow, Speed }

public class BonusPickup : MonoBehaviour
{
    public BonusKind Kind { get; private set; }
    private SurvivalGame game;
    private float expiresAt;
    private bool collected;

    public void Initialize(SurvivalGame owner, BonusKind kind, float lifetime)
    {
        game = owner;
        Kind = kind;
        expiresAt = Time.time + lifetime;
        transform.localScale = Vector3.one * 0.55f;
    }

    private void Update()
    {
        if (game == null || !game.IsPlaying) return;
        transform.localScale = Vector3.one * (0.55f + Mathf.Sin(Time.time * 4f) * 0.035f);
        if (Time.time >= expiresAt) Remove();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || game == null || !game.IsPlaying) return;
        if (!other.TryGetComponent<PlayerController>(out _)) return;
        game.ApplyBonus(Kind);
        Remove();
    }

    private void Remove()
    {
        if (collected) return;
        collected = true;
        game.ForgetBonus(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public static string LabelFor(BonusKind kind)
    {
        switch (kind)
        {
            case BonusKind.Shield: return "ЩИТ";
            case BonusKind.Slow: return "ЗАМЕДЛЕНИЕ";
            default: return "СКОРОСТЬ";
        }
    }

    public static Color ColorFor(BonusKind kind)
    {
        switch (kind)
        {
            case BonusKind.Shield: return new Color(0.25f, 0.95f, 0.73f);
            case BonusKind.Slow: return new Color(0.65f, 0.5f, 1f);
            default: return new Color(1f, 0.77f, 0.25f);
        }
    }
}
