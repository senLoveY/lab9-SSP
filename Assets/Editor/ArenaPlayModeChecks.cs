using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Runs against the real scene, physics triggers and elapsed game time.
[InitializeOnLoad]
public static class ArenaPlayModeChecks
{
    private const string Pending = "CircleRun.RunChecks";
    private static SurvivalGame game;
    private static int step;
    private static double due;
    private static double deadline;
    private static bool hadRecord;
    private static float oldRecord;
    private static float stoppedTime;
    private static string results;

    static ArenaPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += OnMode;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Circle Run/Run gameplay checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnMode(PlayModeStateChange mode)
    {
        if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            step = 0;
            game = null;
            results = "Unity " + Application.unityVersion + "\n";
            due = EditorApplication.timeSinceStartup + 0.3;
            deadline = due + 25;
            hadRecord = PlayerPrefs.HasKey(SurvivalGame.RecordKey);
            oldRecord = PlayerPrefs.GetFloat(SurvivalGame.RecordKey, 0);
        }
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        results += "PASS " + description + "\n";
    }

    private static void PickUp(BonusKind kind)
    {
        foreach (BonusPickup bonus in game.Bonuses)
            if (bonus.Kind == kind)
            {
                game.player.Body.position = bonus.transform.position;
                game.player.Body.linearVelocity = Vector2.zero;
                return;
            }
        throw new Exception("Missing pickup " + kind);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        double now = EditorApplication.timeSinceStartup;
        if (now < due) return;
        try
        {
            if (now > deadline) throw new Exception("Gameplay check timed out");
            switch (step)
            {
                case 0:
                    game = UnityEngine.Object.FindAnyObjectByType<SurvivalGame>();
                    Check(game != null && game.IsReady, "Scene starts at the ready screen");
                    game.BeginRound();
                    game.firstEnemy.enabled = false;
                    game.firstEnemy.Body.position = new Vector2(6, 2);
                    game.firstEnemy.Body.linearVelocity = Vector2.zero;
                    Check(game.EnemyCount == 1 && game.Bonuses.Count == 3, "Round starts with one enemy and three bonuses");
                    PickUp(BonusKind.Shield);
                    due = now + 0.25;
                    break;
                case 1:
                    Check(game.player.HasShield && game.Bonuses.Count == 2, "Shield is collected through a physics trigger");
                    game.player.ReceiveEnemyHit();
                    Check(game.IsPlaying && !game.player.HasShield && game.player.IsProtected, "Shield absorbs exactly one hit and grants escape time");
                    game.player.ReceiveEnemyHit();
                    Check(game.IsPlaying, "Repeated contact during protection is harmless");
                    PickUp(BonusKind.Slow);
                    due = now + 0.25;
                    break;
                case 2:
                    Check(game.EnemySpeedMultiplier < 0.5f && game.Bonuses.Count == 1, "Slow pickup reduces enemy speed");
                    PickUp(BonusKind.Speed);
                    due = now + 0.25;
                    break;
                case 3:
                    Check(game.player.SpeedRemaining > 5f && game.Bonuses.Count == 0, "Speed pickup activates its timed effect");
                    game.player.Body.position = new Vector2(game.halfWidth - 0.9f, 0);
                    Check(game.player.TryDash(Vector2.right), "Dash activates when ready");
                    Check(!game.player.TryDash(Vector2.right), "Dash cannot bypass cooldown");
                    due = now + 0.45;
                    break;
                case 4:
                    Check(game.player.GetComponent<Collider2D>().bounds.max.x <= game.halfWidth + 0.1f, "Dash cannot cross the arena wall");
                    due = now + 2.4;
                    break;
                case 5:
                    Check(game.player.DashRemaining <= 0f && game.player.TryDash(Vector2.left), "Dash recharges and can be reused");
                    due = now + 0.4;
                    break;
                case 6:
                    if (game.ElapsedTime < 10.1f) { due = now + 0.1; return; }
                    Check(game.EnemyCount == 2 && game.NextEnemyIn > 9f, "A new pursuer appears after ten seconds");
                    foreach (EnemyFollow enemy in UnityEngine.Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None))
                        if (enemy != game.firstEnemy) Check(enemy.enabled && enemy.Body.linearVelocity.sqrMagnitude > 0.01f, "Spawned pursuer actively follows the player");
                    Check(game.player.SpeedRemaining == 0f && game.SlowRemaining == 0f, "Temporary bonuses expire");
                    Check(game.IsPlaying, "Survival timer keeps the round active");
                    // Force real collider contact with the original pursuer.
                    game.firstEnemy.Body.position = game.player.Body.position;
                    game.firstEnemy.Body.linearVelocity = Vector2.zero;
                    due = now + 0.25;
                    break;
                case 7:
                    Check(game.IsOver && !game.player.Body.simulated, "Enemy collision ends and freezes the round");
                    Check(PlayerPrefs.GetFloat(SurvivalGame.RecordKey, 0) >= game.ElapsedTime, "Best result persists in PlayerPrefs");
                    stoppedTime = game.ElapsedTime;
                    due = now + 0.25;
                    break;
                case 8:
                    Check(Mathf.Approximately(game.ElapsedTime, stoppedTime), "Timer stops after game over");
                    game.BeginRound();
                    Check(game.IsPlaying && game.EnemyCount == 1 && game.Bonuses.Count == 3 && game.ElapsedTime == 0f,
                          "Restart resets timer, enemies and pickups");
                    Check(!game.player.HasShield && game.player.SpeedRemaining == 0 && game.SlowRemaining == 0,
                          "Restart clears active effects");
                    Finish(null);
                    return;
            }
            step++;
        }
        catch (Exception exception) { Finish(exception); }
    }

    private static void Finish(Exception error)
    {
        if (game != null) game.EndRound();
        if (hadRecord) PlayerPrefs.SetFloat(SurvivalGame.RecordKey, oldRecord);
        else PlayerPrefs.DeleteKey(SurvivalGame.RecordKey);
        PlayerPrefs.Save();
        results += error == null ? "ALL GAMEPLAY CHECKS PASSED\n" : "FAIL " + error + "\n";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/ArenaChecks.txt", results);
        if (error == null) Debug.Log(results); else Debug.LogError(results);
        SessionState.SetBool(Pending, false);
        EditorApplication.isPlaying = false;
    }
}
