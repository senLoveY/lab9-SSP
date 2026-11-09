using System.Collections.Generic;
using UnityEngine;

public class SurvivalGame : MonoBehaviour
{
    public const string RecordKey = "CircleRun.BestSeconds.v1";

    [Header("Scene")]
    public PlayerController player;
    public EnemyFollow firstEnemy;
    public Camera view;

    [Header("Arena")]
    public float halfWidth = 8.3f;
    public float halfHeight = 3.4f;

    [Header("Difficulty and bonuses")]
    [Min(1f)] public float enemyInterval = 10f;
    [Min(1f)] public float bonusInterval = 7f;
    public float bonusDuration = 6f;

    private enum RoundState { Ready, Playing, Over }
    private RoundState state;
    private readonly List<EnemyFollow> enemies = new List<EnemyFollow>();
    private readonly List<BonusPickup> bonuses = new List<BonusPickup>();
    private Transform arenaRoot;
    private Transform enemyRoot;
    private Transform bonusRoot;
    private Vector2 playerStart;
    private Vector2 enemyStart;
    private Animator pulse;
    private Sprite square;
    private Sprite circle;
    private Texture2D squareTexture;
    private Texture2D circleTexture;
    private PhysicsMaterial2D wallMaterial;
    private float nextEnemyAt;
    private float nextBonusAt;
    private float slowEndsAt;
    private float noticeEndsAt;
    private int wave;

    public bool IsPlaying => state == RoundState.Playing;
    public bool IsReady => state == RoundState.Ready;
    public bool IsOver => state == RoundState.Over;
    public float ElapsedTime { get; private set; }
    public float BestTime { get; private set; }
    public bool IsNewRecord { get; private set; }
    public int EnemyCount => enemies.Count;
    public IReadOnlyList<BonusPickup> Bonuses => bonuses;
    public float NextEnemyIn => Mathf.Max(0f, nextEnemyAt - ElapsedTime);
    public float SlowRemaining => Mathf.Max(0f, slowEndsAt - Time.time);
    public float EnemySpeedMultiplier => SlowRemaining > 0f ? 0.45f : 1f;
    public string Notice { get; private set; }
    public Color NoticeColor { get; private set; }
    public bool HasNotice => Time.time < noticeEndsAt;

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (firstEnemy == null) firstEnemy = FindAnyObjectByType<EnemyFollow>();
        if (view == null) view = Camera.main;
        if (player == null || firstEnemy == null || view == null)
        {
            Debug.LogError("Circle Run: assign Player, Enemy and Main Camera.", this);
            enabled = false;
            return;
        }

        BestTime = PlayerPrefs.GetFloat(RecordKey, 0f);
        if (float.IsNaN(BestTime) || float.IsInfinity(BestTime) || BestTime < 0f) BestTime = 0f;
        playerStart = player.transform.position;
        enemyStart = firstEnemy.transform.position;
        pulse = player.GetComponent<Animator>();
        CreateSprites();
        BuildArena();
        enemyRoot = new GameObject("Pursuers").transform;
        bonusRoot = new GameObject("Bonuses").transform;
        enemyRoot.SetParent(transform);
        bonusRoot.SetParent(transform);
        enemies.Add(firstEnemy);
        firstEnemy.Configure(this, player.transform, 2f);
        player.Prepare(this, playerStart);
        if (pulse != null) pulse.speed = 0f;
        gameObject.AddComponent<SurvivalHud>().Initialize(this);
        UpdateCamera();
    }

    private void Update()
    {
        UpdateCamera();
        if (IsReady && Input.GetKeyDown(KeyCode.Return)) BeginRound();
        else if (!IsReady && Input.GetKeyDown(KeyCode.R)) BeginRound();
        if (!IsPlaying) return;

        ElapsedTime += Time.deltaTime;
        while (ElapsedTime >= nextEnemyAt)
        {
            wave++;
            SpawnEnemy();
            nextEnemyAt += enemyInterval;
            ShowNotice("НОВЫЙ ПРЕСЛЕДОВАТЕЛЬ", new Color(1f, 0.4f, 0.5f));
        }
        if (ElapsedTime >= nextBonusAt)
        {
            if (bonuses.Count < 3) SpawnBonus((BonusKind)Random.Range(0, 3), RandomBonusPosition());
            nextBonusAt += bonusInterval;
        }
    }

    private void UpdateCamera()
    {
        if (view == null) return;
        view.orthographic = true;
        view.orthographicSize = Mathf.Max(5.6f, (halfWidth + 1f) / Mathf.Max(0.2f, view.aspect));
        view.backgroundColor = new Color(0.025f, 0.035f, 0.06f);
    }

    public void BeginRound()
    {
        state = RoundState.Ready;
        foreach (BonusPickup pickup in bonuses)
        {
            if (pickup == null) continue;
            pickup.gameObject.SetActive(false);
            Destroy(pickup.gameObject);
        }
        bonuses.Clear();
        for (int i = 1; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            enemies[i].gameObject.SetActive(false);
            Destroy(enemies[i].gameObject);
        }
        enemies.Clear();
        enemies.Add(firstEnemy);
        ElapsedTime = 0f;
        wave = 0;
        nextEnemyAt = enemyInterval;
        nextBonusAt = bonusInterval;
        slowEndsAt = noticeEndsAt = 0f;
        IsNewRecord = false;
        player.Prepare(this, playerStart);
        if (pulse != null)
        {
            pulse.Rebind();
            pulse.Update(0f);
            pulse.speed = 1f;
        }
        firstEnemy.enabled = true;
        firstEnemy.Body.simulated = true;
        firstEnemy.Body.position = enemyStart;
        firstEnemy.Body.linearVelocity = Vector2.zero;
        firstEnemy.Body.angularVelocity = 0f;
        firstEnemy.Configure(this, player.transform, 2f);
        state = RoundState.Playing;
        SpawnBonus(BonusKind.Shield, playerStart + Vector2.up * 1.6f);
        SpawnBonus(BonusKind.Slow, new Vector2(-5f, -1.8f));
        SpawnBonus(BonusKind.Speed, new Vector2(4.8f, -1.6f));
    }

    public void EndRound()
    {
        if (!IsPlaying) return;
        state = RoundState.Over;
        IsNewRecord = ElapsedTime > BestTime;
        SaveRecord();
        player.Body.linearVelocity = Vector2.zero;
        player.Body.angularVelocity = 0f;
        player.Body.simulated = false;
        foreach (EnemyFollow enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.Body.linearVelocity = Vector2.zero;
            enemy.Body.angularVelocity = 0f;
            enemy.Body.simulated = false;
        }
        if (pulse != null) pulse.speed = 0f;
    }

    private void SaveRecord()
    {
        if (ElapsedTime <= BestTime) return;
        BestTime = ElapsedTime;
        PlayerPrefs.SetFloat(RecordKey, BestTime);
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit() { if (IsPlaying) SaveRecord(); }

    public EnemyFollow SpawnEnemy()
    {
        EnemyFollow enemy = Instantiate(firstEnemy, SafeEdgePosition(), Quaternion.identity, enemyRoot);
        enemy.name = "Enemy " + (enemies.Count + 1);
        enemy.enabled = true;
        enemy.Body.simulated = true;
        enemy.Body.linearVelocity = Vector2.zero;
        enemies.Add(enemy);
        float speed = Mathf.Min(4.2f, 2f + wave * 0.12f);
        foreach (EnemyFollow pursuer in enemies) pursuer.Configure(this, player.transform, speed);
        return enemy;
    }

    private Vector2 SafeEdgePosition()
    {
        Vector2 point = Vector2.zero;
        for (int attempt = 0; attempt < 24; attempt++)
        {
            int side = Random.Range(0, 4);
            float x = Random.Range(-halfWidth + 0.8f, halfWidth - 0.8f);
            float y = Random.Range(-halfHeight + 0.8f, halfHeight - 0.8f);
            point = side < 2 ? new Vector2(side == 0 ? -halfWidth + 0.8f : halfWidth - 0.8f, y)
                             : new Vector2(x, side == 2 ? -halfHeight + 0.8f : halfHeight - 0.8f);
            if (Vector2.Distance(point, player.Body.position) > 3.5f && IsSpawnFree(point)) return point;
        }
        // Choose the farthest corner as a deterministic safe fallback.
        return new Vector2(player.Body.position.x > 0f ? -halfWidth + 0.8f : halfWidth - 0.8f,
                           player.Body.position.y > 0f ? -halfHeight + 0.8f : halfHeight - 0.8f);
    }

    private bool IsSpawnFree(Vector2 point)
    {
        foreach (EnemyFollow enemy in enemies)
            if (enemy != null && Vector2.Distance(enemy.Body.position, point) < 1.1f) return false;
        return true;
    }

    private Vector2 RandomBonusPosition()
    {
        Vector2 point = Vector2.zero;
        for (int attempt = 0; attempt < 24; attempt++)
        {
            point = new Vector2(Random.Range(-halfWidth + 1f, halfWidth - 1f),
                                Random.Range(-halfHeight + 1f, halfHeight - 1f));
            bool free = Vector2.Distance(point, player.Body.position) > 1.5f && IsSpawnFree(point);
            foreach (BonusPickup bonus in bonuses)
                if (bonus != null && Vector2.Distance(point, bonus.transform.position) < 1.2f) free = false;
            if (free) return point;
        }
        return point;
    }

    public BonusPickup SpawnBonus(BonusKind kind, Vector2 position)
    {
        GameObject item = new GameObject("Bonus " + kind);
        item.transform.SetParent(bonusRoot);
        item.transform.position = ClampToArena(position, Vector2.one * 0.5f);
        SpriteRenderer sprite = item.AddComponent<SpriteRenderer>();
        sprite.sprite = circle;
        sprite.sortingOrder = 2;
        sprite.color = BonusPickup.ColorFor(kind);
        CircleCollider2D trigger = item.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.68f;
        BonusPickup pickup = item.AddComponent<BonusPickup>();
        pickup.Initialize(this, kind, 14f);
        bonuses.Add(pickup);
        return pickup;
    }

    public void ApplyBonus(BonusKind kind)
    {
        if (!IsPlaying) return;
        switch (kind)
        {
            case BonusKind.Shield: player.GrantShield(); break;
            case BonusKind.Slow: slowEndsAt = Time.time + bonusDuration; break;
            case BonusKind.Speed: player.GrantSpeed(bonusDuration); break;
        }
        ShowNotice(BonusPickup.LabelFor(kind) + (kind == BonusKind.Shield ? " АКТИВЕН" : " · " + bonusDuration.ToString("0.#") + " СЕК"), BonusPickup.ColorFor(kind));
    }

    public void ForgetBonus(BonusPickup bonus) { bonuses.Remove(bonus); }

    public void RepelEnemies(Vector2 origin)
    {
        foreach (EnemyFollow enemy in enemies)
        {
            if (enemy == null || Vector2.Distance(origin, enemy.Body.position) > 3f) continue;
            Vector2 direction = enemy.Body.position - origin;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
            enemy.Body.position = ClampToArena(origin + direction.normalized * 2.7f, Vector2.one * 0.6f);
            enemy.Body.linearVelocity = Vector2.zero;
        }
    }

    public Vector2 ClampToArena(Vector2 point, Vector2 margin)
    {
        return new Vector2(Mathf.Clamp(point.x, -halfWidth + margin.x, halfWidth - margin.x),
                           Mathf.Clamp(point.y, -halfHeight + margin.y, halfHeight - margin.y));
    }

    public void ShowNotice(string message, Color color)
    {
        Notice = message;
        NoticeColor = color;
        noticeEndsAt = Time.time + 1.8f;
    }

    private void CreateSprites()
    {
        squareTexture = new Texture2D(1, 1);
        squareTexture.SetPixel(0, 0, Color.white);
        squareTexture.Apply();
        square = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32));
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32f - distance));
            }
        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
        circle = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
        wallMaterial = new PhysicsMaterial2D("Arena frictionless") { friction = 0f, bounciness = 0f };
    }

    private void BuildArena()
    {
        arenaRoot = new GameObject("Arena").transform;
        arenaRoot.SetParent(transform);
        Decoration("Floor", Vector2.zero, new Vector2(halfWidth * 2, halfHeight * 2), new Color(0.035f, 0.06f, 0.09f), -50);
        Color grid = new Color(0.065f, 0.105f, 0.14f);
        for (int x = -(int)halfWidth; x <= (int)halfWidth; x++)
            Decoration("Grid X", new Vector2(x, 0), new Vector2(0.012f, halfHeight * 2), grid, -49);
        for (int y = -(int)halfHeight; y <= (int)halfHeight; y++)
            Decoration("Grid Y", new Vector2(0, y), new Vector2(halfWidth * 2, 0.012f), grid, -49);
        Wall("North", new Vector2(0, halfHeight + 0.15f), new Vector2(halfWidth * 2 + 0.6f, 0.3f));
        Wall("South", new Vector2(0, -halfHeight - 0.15f), new Vector2(halfWidth * 2 + 0.6f, 0.3f));
        Wall("West", new Vector2(-halfWidth - 0.15f, 0), new Vector2(0.3f, halfHeight * 2));
        Wall("East", new Vector2(halfWidth + 0.15f, 0), new Vector2(0.3f, halfHeight * 2));
    }

    private GameObject Decoration(string label, Vector2 position, Vector2 size, Color color, int order)
    {
        GameObject item = new GameObject(label);
        item.transform.SetParent(arenaRoot);
        item.transform.position = position;
        item.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sprite = item.AddComponent<SpriteRenderer>();
        sprite.sprite = square;
        sprite.color = color;
        sprite.sortingOrder = order;
        return item;
    }

    private void Wall(string label, Vector2 position, Vector2 size)
    {
        GameObject wall = Decoration(label + " wall", position, size, new Color(0.16f, 0.28f, 0.34f), -40);
        wall.AddComponent<BoxCollider2D>().sharedMaterial = wallMaterial;
    }

    private void OnDestroy()
    {
        if (square != null) Destroy(square);
        if (circle != null) Destroy(circle);
        if (squareTexture != null) Destroy(squareTexture);
        if (circleTexture != null) Destroy(circleTexture);
        if (wallMaterial != null) Destroy(wallMaterial);
    }
}
