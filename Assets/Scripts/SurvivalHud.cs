using UnityEngine;

// IMGUI keeps the project independent of additional UI packages.
public class SurvivalHud : MonoBehaviour
{
    private SurvivalGame game;
    private Font font;
    private GUIStyle text;
    private GUIStyle button;
    private readonly Color muted = new Color(0.52f, 0.64f, 0.74f);
    private readonly Color mint = new Color(0.3f, 0.95f, 0.77f);
    private readonly Color panel = new Color(0.045f, 0.075f, 0.115f, 0.98f);

    public void Initialize(SurvivalGame owner) { game = owner; }

    private void PrepareStyles()
    {
        if (text != null) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text = new GUIStyle(GUI.skin.label) { font = font, wordWrap = false };
        button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 22, fontStyle = FontStyle.Bold };
        button.normal.textColor = Color.white;
        button.hover.textColor = mint;
    }

    private void OnGUI()
    {
        if (game == null) return;
        PrepareStyles();
        Matrix4x4 original = GUI.matrix;
        Color originalColor = GUI.color;
        float scale = Mathf.Max(0.1f, Mathf.Min(Screen.width / 1440f, Screen.height / 900f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);

        Label(new Rect(42, 25, 310, 40), "CIRCLE RUN", 29, mint, FontStyle.Bold);
        Label(new Rect(43, 65, 300, 28), "АРЕНА ВЫЖИВАНИЯ", 14, muted);
        Label(new Rect(w / 2 - 160, 19, 320, 28), "ВРЕМЯ ВЫЖИВАНИЯ", 14, muted, align: TextAnchor.MiddleCenter);
        Label(new Rect(w / 2 - 170, 48, 340, 58), FormatTime(game.ElapsedTime), 44, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        Label(new Rect(w - 350, 28, 306, 28), "ЛУЧШИЙ РЕЗУЛЬТАТ", 14, muted, align: TextAnchor.MiddleRight);
        Label(new Rect(w - 350, 58, 306, 42), FormatTime(game.BestTime), 29, mint, FontStyle.Bold, TextAnchor.MiddleRight);
        Label(new Rect(43, 112, 420, 28), "ПРЕСЛЕДОВАТЕЛЕЙ  " + game.EnemyCount, 17, new Color(1f, 0.47f, 0.53f));
        float next = game.IsReady ? game.enemyInterval : game.NextEnemyIn;
        Label(new Rect(w - 440, 112, 396, 28), "НОВЫЙ ВРАГ ЧЕРЕЗ  " + Mathf.CeilToInt(next) + " СЕК", 17, muted, align: TextAnchor.MiddleRight);

        if (game.IsPlaying)
        {
            foreach (BonusPickup pickup in game.Bonuses)
            {
                if (pickup == null) continue;
                Vector3 p = game.view.WorldToScreenPoint(pickup.transform.position + Vector3.down * 0.53f);
                Label(new Rect(p.x / scale - 100, (Screen.height - p.y) / scale, 200, 26), BonusPickup.LabelFor(pickup.Kind), 12, BonusPickup.ColorFor(pickup.Kind), align: TextAnchor.MiddleCenter);
            }
            if (game.HasNotice)
                Label(new Rect(w / 2 - 330, 144, 660, 36), game.Notice, 21, game.NoticeColor, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        float bottom = h - 119;
        Box(new Rect(40, bottom, 310, 64), panel);
        float ready = 1f - Mathf.Clamp01(game.player.DashRemaining / game.player.dashCooldown);
        Label(new Rect(55, bottom + 8, 280, 26), ready >= 1f ? "ПРОБЕЛ  /  РЫВОК ГОТОВ" : "РЫВОК  /  " + game.player.DashRemaining.ToString("0.0") + " СЕК", 16, ready >= 1f ? mint : muted, FontStyle.Bold);
        Box(new Rect(56, bottom + 45, 278, 4), new Color(0.12f, 0.21f, 0.27f));
        Box(new Rect(56, bottom + 45, 278 * ready, 4), mint);
        string effects = game.player.HasShield ? "ЩИТ АКТИВЕН" : "ЩИТ — НЕТ";
        if (game.player.SpeedRemaining > 0) effects += "     СКОРОСТЬ " + game.player.SpeedRemaining.ToString("0.0") + " С";
        if (game.SlowRemaining > 0) effects += "     ЗАМЕДЛЕНИЕ " + game.SlowRemaining.ToString("0.0") + " С";
        Label(new Rect(375, bottom + 17, w - 415, 35), effects, 16, muted, align: TextAnchor.MiddleRight);
        Label(new Rect(40, h - 41, w - 80, 28), "WASD  движение     SHIFT  бег     ПРОБЕЛ  рывок     Q / E  поворот     R  новый раунд", 15, muted, align: TextAnchor.MiddleCenter);

        if (game.IsReady || game.IsOver) DrawDialog(w, h);
        GUI.matrix = original;
        GUI.color = originalColor;
    }

    private void DrawDialog(float w, float h)
    {
        Box(new Rect(0, 155, w, h - 300), new Color(0.015f, 0.025f, 0.045f, 0.72f));
        Rect r = new Rect(w / 2 - 345, h / 2 - 177, 690, 354);
        Box(r, panel);
        Box(new Rect(r.x, r.y, r.width, 3), game.IsReady ? mint : new Color(1f, 0.4f, 0.5f));
        Label(new Rect(r.x + 20, r.y + 24, 650, 45), game.IsReady ? "СКОЛЬКО ТЫ ПРОДЕРЖИШЬСЯ?" : "РАУНД ЗАВЕРШЁН", 29, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        if (game.IsReady)
        {
            Label(new Rect(r.x + 30, r.y + 85, 630, 32), "Убегай от красных кругов. Каждые 10 секунд их больше.", 18, muted, align: TextAnchor.MiddleCenter);
            Label(new Rect(r.x + 30, r.y + 126, 630, 32), "Рывок спасает от касания. Восстановление — 2,5 секунды.", 18, muted, align: TextAnchor.MiddleCenter);
            Label(new Rect(r.x + 25, r.y + 179, 210, 30), "●  ЩИТ · 1 УДАР", 16, BonusPickup.ColorFor(BonusKind.Shield), align: TextAnchor.MiddleCenter);
            Label(new Rect(r.x + 240, r.y + 179, 210, 30), "●  ЗАМЕДЛЕНИЕ · 6 С", 16, BonusPickup.ColorFor(BonusKind.Slow), align: TextAnchor.MiddleCenter);
            Label(new Rect(r.x + 455, r.y + 179, 210, 30), "●  СКОРОСТЬ · 6 С", 16, BonusPickup.ColorFor(BonusKind.Speed), align: TextAnchor.MiddleCenter);
        }
        else
        {
            Label(new Rect(r.x + 30, r.y + 77, 630, 75), FormatTime(game.ElapsedTime), 54, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(new Rect(r.x + 30, r.y + 165, 630, 35), game.IsNewRecord ? "НОВЫЙ ЛИЧНЫЙ РЕКОРД!" : "РЕКОРД  " + FormatTime(game.BestTime), 21, mint, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
        GUI.backgroundColor = new Color(0.15f, 0.46f, 0.39f);
        if (GUI.Button(new Rect(r.x + 150, r.y + 252, 390, 58), game.IsReady ? "НАЧАТЬ  ·  ENTER" : "ЕЩЁ РАЗ  ·  R", button)) game.BeginRound();
        GUI.backgroundColor = Color.white;
    }

    private void Label(Rect rect, string value, int size, Color color, FontStyle weight = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft)
    {
        text.fontSize = size;
        text.fontStyle = weight;
        text.alignment = align;
        text.normal.textColor = color;
        GUI.Label(rect, value, text);
    }

    private static void Box(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        return minutes.ToString("00") + ":" + (seconds % 60f).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
    }

}
