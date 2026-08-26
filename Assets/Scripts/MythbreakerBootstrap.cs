using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class MythbreakerBootstrap : MonoBehaviour
{
    enum AppState { Menu, Heroes, Settings, Playing, Upgrade, Victory, GameOver }
    enum EnemyType { Satyr, Snake, Hoplite }

    sealed class Enemy
    {
        public Vector2 p;
        public float hp;
        public float maxHp;
        public float speed;
        public float radius;
        public float phase;
        public EnemyType type;
    }

    sealed class Shot
    {
        public Vector2 p;
        public Vector2 v;
        public int pierce;
    }

    AppState state = AppState.Menu;
    Texture2D menuTexture;
    Texture2D circleTexture;
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();

    Vector2 hero = new Vector2(0.50f, 0.79f);
    Vector2 dragStart;
    Vector2 dragNow;
    Vector2 moveInput;
    bool dragging;

    int selectedHero;
    bool vibration = true;
    bool sound = true;

    float heroHp;
    float heroMaxHp;
    float moveSpeed;
    float shotDamage;
    float fireInterval;
    int multiShot;
    int pierce;

    float nextShot;
    float hurtCooldown;
    int wave = 1;
    int kills;
    string diagnostic = "MYTHBREAKER 0.7";

    readonly string[] upgradeNames = new string[3];
    readonly string[] upgradeDescriptions = new string[3];
    readonly int[] upgradeIds = new int[3];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBoot()
    {
        if (FindFirstObjectByType<MythbreakerBootstrap>() == null)
            new GameObject("MYTHBREAKER BOOT 0.7").AddComponent<MythbreakerBootstrap>();
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        circleTexture = MakeCircle(64);
        LoadMenu();
        ApplyHeroBaseStats();
    }

    void LoadMenu()
    {
        TextAsset encoded = Resources.Load<TextAsset>("mythbreaker_menu_b64");
        if (encoded == null)
        {
            diagnostic = "0.7 • MENU SOURCE MISSING";
            return;
        }

        try
        {
            string clean = encoded.text.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
            byte[] bytes = Convert.FromBase64String(clean);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!tex.LoadImage(bytes, false))
            {
                Destroy(tex);
                diagnostic = "0.7 • MENU IMAGE LOAD FAILED";
                return;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            menuTexture = tex;
        }
        catch (Exception e)
        {
            diagnostic = "0.7 • MENU " + e.GetType().Name;
        }
    }

    Texture2D MakeCircle(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float r = c - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float a = Mathf.Clamp01(r + 1.5f - d);
            px[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px);
        t.Apply(false, false);
        return t;
    }

    void Update()
    {
        if (state != AppState.Playing) return;
        ReadPointer();

        if (dragging && moveInput.sqrMagnitude > 0.001f)
        {
            hero += moveInput * moveSpeed * Time.deltaTime;
            hero.x = Mathf.Clamp(hero.x, 0.13f, 0.87f);
            hero.y = Mathf.Clamp(hero.y, 0.20f, 0.84f);
        }

        UpdateCombat();
    }

    void ReadPointer()
    {
        try
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                Vector2 guiPos = new Vector2(t.position.x, Screen.height - t.position.y);
                if (t.phase == TouchPhase.Began) BeginDrag(guiPos);
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) ContinueDrag(guiPos);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) EndDrag();
                return;
            }

            if (Input.GetMouseButtonDown(0)) BeginDrag(InputToGui(Input.mousePosition));
            else if (Input.GetMouseButton(0)) ContinueDrag(InputToGui(Input.mousePosition));
            else if (Input.GetMouseButtonUp(0)) EndDrag();
        }
        catch (Exception e)
        {
            diagnostic = "0.7 • INPUT " + e.GetType().Name;
            EndDrag();
        }
    }

    Vector2 InputToGui(Vector3 p) => new Vector2(p.x, Screen.height - p.y);

    void BeginDrag(Vector2 p)
    {
        if (!ArenaRect().Contains(p)) return;
        dragging = true;
        dragStart = p;
        dragNow = p;
        moveInput = Vector2.zero;
    }

    void ContinueDrag(Vector2 p)
    {
        if (!dragging) return;
        dragNow = p;
        float radius = Mathf.Max(76f, Screen.width * 0.14f);
        Vector2 delta = Vector2.ClampMagnitude(dragNow - dragStart, radius);
        moveInput = delta / radius;
    }

    void EndDrag()
    {
        dragging = false;
        moveInput = Vector2.zero;
    }

    Rect ArenaRect() => new Rect(Screen.width * 0.055f, Screen.height * 0.115f, Screen.width * 0.89f, Screen.height * 0.755f);

    void ApplyHeroBaseStats()
    {
        if (selectedHero == 0)
        {
            heroMaxHp = 100f;
            moveSpeed = 0.46f;
            shotDamage = 24f;
            fireInterval = 0.43f;
        }
        else
        {
            heroMaxHp = 135f;
            moveSpeed = 0.38f;
            shotDamage = 34f;
            fireInterval = 0.58f;
        }
        multiShot = 1;
        pierce = 0;
    }

    void StartRun()
    {
        ApplyHeroBaseStats();
        state = AppState.Playing;
        hero = new Vector2(0.50f, 0.79f);
        heroHp = heroMaxHp;
        wave = 1;
        kills = 0;
        nextShot = 0f;
        hurtCooldown = 0f;
        EndDrag();
        enemies.Clear();
        shots.Clear();
        SpawnWave();
        Haptic();
    }

    void SpawnWave()
    {
        enemies.Clear();
        shots.Clear();
        int count = 2 + wave;
        for (int i = 0; i < count; i++)
        {
            EnemyType type = (EnemyType)((i + wave - 1) % 3);
            float x = 0.16f + 0.68f * ((i + 1f) / (count + 1f));
            float y = 0.22f + (i % 2) * 0.055f;
            float hp = type == EnemyType.Hoplite ? 62f + wave * 11f : 36f + wave * 8f;
            float speed = type == EnemyType.Snake ? 0.070f : type == EnemyType.Hoplite ? 0.034f : 0.050f;
            enemies.Add(new Enemy
            {
                p = new Vector2(x, y),
                hp = hp,
                maxHp = hp,
                speed = speed + wave * 0.0035f,
                radius = type == EnemyType.Hoplite ? 0.038f : 0.032f,
                phase = i * 1.7f,
                type = type
            });
        }
    }

    void UpdateCombat()
    {
        float dt = Time.deltaTime;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            Vector2 d = hero - e.p;
            if (d.sqrMagnitude > 0.0001f)
            {
                Vector2 dir = d.normalized;
                if (e.type == EnemyType.Snake)
                {
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    dir = (dir + side * Mathf.Sin(Time.time * 5f + e.phase) * 0.32f).normalized;
                }
                e.p += dir * e.speed * dt;
            }

            if (Vector2.Distance(hero, e.p) < e.radius + 0.037f && Time.time >= hurtCooldown)
            {
                heroHp -= e.type == EnemyType.Hoplite ? 17f : 11f;
                hurtCooldown = Time.time + 0.65f;
                Haptic();
                if (d.sqrMagnitude > 0.001f) e.p -= d.normalized * 0.055f;
                if (heroHp <= 0f)
                {
                    heroHp = 0f;
                    EndDrag();
                    state = AppState.GameOver;
                    return;
                }
            }
        }

        if (!dragging && enemies.Count > 0 && Time.time >= nextShot)
        {
            nextShot = Time.time + fireInterval;
            FireAtNearest();
        }

        for (int s = shots.Count - 1; s >= 0; s--)
        {
            Shot shot = shots[s];
            shot.p += shot.v * dt;
            bool remove = shot.p.x < 0.04f || shot.p.x > 0.96f || shot.p.y < 0.10f || shot.p.y > 0.91f;

            if (!remove)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy e = enemies[i];
                    if (Vector2.Distance(shot.p, e.p) < e.radius + 0.015f)
                    {
                        e.hp -= shotDamage;
                        if (e.hp <= 0f)
                        {
                            enemies.RemoveAt(i);
                            kills++;
                        }

                        if (shot.pierce > 0) shot.pierce--;
                        else remove = true;
                        break;
                    }
                }
            }
            if (remove) shots.RemoveAt(s);
        }

        if (enemies.Count == 0)
        {
            EndDrag();
            if (wave >= 5) state = AppState.Victory;
            else
            {
                PrepareUpgradeChoices();
                state = AppState.Upgrade;
            }
        }
    }

    void FireAtNearest()
    {
        Enemy nearest = enemies[0];
        float best = (nearest.p - hero).sqrMagnitude;
        for (int i = 1; i < enemies.Count; i++)
        {
            float d = (enemies[i].p - hero).sqrMagnitude;
            if (d < best) { best = d; nearest = enemies[i]; }
        }

        Vector2 dir = (nearest.p - hero).normalized;
        if (multiShot <= 1)
        {
            shots.Add(new Shot { p = hero, v = dir * 0.82f, pierce = pierce });
            return;
        }

        float spread = multiShot == 2 ? 7f : 10f;
        for (int i = 0; i < multiShot; i++)
        {
            float t = multiShot == 1 ? 0f : i / (float)(multiShot - 1);
            float angle = Mathf.Lerp(-spread, spread, t);
            Vector2 d = Rotate(dir, angle * Mathf.Deg2Rad);
            shots.Add(new Shot { p = hero, v = d * 0.82f, pierce = pierce });
        }
    }

    Vector2 Rotate(Vector2 v, float a)
    {
        float c = Mathf.Cos(a);
        float s = Mathf.Sin(a);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void PrepareUpgradeChoices()
    {
        int seed = wave * 17 + kills * 3;
        for (int i = 0; i < 3; i++)
        {
            int id = (seed + i * 2) % 6;
            while (i > 0 && (id == upgradeIds[0] || (i > 1 && id == upgradeIds[1]))) id = (id + 1) % 6;
            upgradeIds[i] = id;
            DescribeUpgrade(id, out upgradeNames[i], out upgradeDescriptions[i]);
        }
    }

    void DescribeUpgrade(int id, out string name, out string description)
    {
        switch (id)
        {
            case 0: name = "TIRO RAPIDO"; description = "+14% velocità d'attacco"; break;
            case 1: name = "POTENZA DIVINA"; description = "+7 danni per colpo"; break;
            case 2: name = "PASSO DI ERMES"; description = "+10% velocità movimento"; break;
            case 3: name = "VITALITÀ"; description = "+25 vita massima e cura"; break;
            case 4: name = "COLPO MULTIPLO"; description = "Aggiunge un proiettile"; break;
            default: name = "FRECCIA PERFORANTE"; description = "+1 bersaglio attraversato"; break;
        }
    }

    void ApplyUpgrade(int id)
    {
        switch (id)
        {
            case 0: fireInterval = Mathf.Max(0.19f, fireInterval * 0.86f); break;
            case 1: shotDamage += 7f; break;
            case 2: moveSpeed *= 1.10f; break;
            case 3: heroMaxHp += 25f; heroHp = Mathf.Min(heroMaxHp, heroHp + 35f); break;
            case 4: multiShot = Mathf.Min(3, multiShot + 1); break;
            case 5: pierce = Mathf.Min(2, pierce + 1); break;
        }
        wave++;
        state = AppState.Playing;
        SpawnWave();
        Haptic();
    }

    void Haptic()
    {
        if (!vibration) return;
        try { Handheld.Vibrate(); } catch { }
    }

    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;
        float s = Mathf.Clamp(w / 720f, 0.72f, 1.8f);

        if (state == AppState.Menu) DrawMenu(w, h, s);
        else if (state == AppState.Heroes) DrawHeroes(w, h, s);
        else if (state == AppState.Settings) DrawSettings(w, h, s);
        else if (state == AppState.Playing) DrawGame(w, h, s);
        else if (state == AppState.Upgrade) DrawUpgrade(w, h, s);
        else DrawEnd(w, h, s, state == AppState.Victory);
    }

    void DrawMenu(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.004f, 0.010f, 0.035f));
        if (menuTexture != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), menuTexture, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.color = new Color(0.96f, 0.72f, 0.18f);
            GUI.Label(new Rect(0, h * 0.18f, w, 80f * s), "lello's game", Center(Mathf.RoundToInt(28f * s)));
            GUI.Label(new Rect(0, h * 0.34f, w, 110f * s), "MYTHBREAKER", Center(Mathf.RoundToInt(52f * s)));
        }

        GUIStyle transparent = TransparentButton();
        if (GUI.Button(new Rect(w * 0.10f, h * 0.765f, w * 0.80f, h * 0.13f), "START", transparent)) StartRun();
        if (GUI.Button(new Rect(0, h * 0.895f, w * 0.49f, h * 0.095f), "HEROES", transparent)) { state = AppState.Heroes; Haptic(); }
        if (GUI.Button(new Rect(w * 0.51f, h * 0.895f, w * 0.49f, h * 0.095f), "SETTINGS", transparent)) { state = AppState.Settings; Haptic(); }

        GUI.color = new Color(1f, 1f, 1f, 0.72f);
        GUI.Label(new Rect(w * 0.72f, h * 0.018f, w * 0.25f, 28f * s), "v0.7", Right(Mathf.RoundToInt(13f * s)));
        GUI.color = Color.white;
    }

    void DrawHeroes(int w, int h, float s)
    {
        DrawPanelBackground(w, h);
        GUI.color = Gold();
        GUI.Label(new Rect(0, h * 0.07f, w, 70f * s), "EROI", Center(Mathf.RoundToInt(42f * s)));
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.125f, w, 42f * s), "Scegli il tuo campione", Center(Mathf.RoundToInt(18f * s)));

        DrawHeroCard(new Rect(w * 0.07f, h * 0.22f, w * 0.86f, h * 0.22f), 0, "PERSEO", "Mythbow", "Veloce • preciso • tiro rapido", s);
        DrawHeroCard(new Rect(w * 0.07f, h * 0.48f, w * 0.86f, h * 0.22f), 1, "ERACLE", "Clava di Nemea", "Più vita • più danno • più lento", s);

        if (GUI.Button(new Rect(w * 0.20f, h * 0.82f, w * 0.60f, 68f * s), "INDIETRO", Button(Mathf.RoundToInt(21f * s)))) { state = AppState.Menu; Haptic(); }
    }

    void DrawHeroCard(Rect r, int id, string name, string weapon, string detail, float s)
    {
        bool selected = selectedHero == id;
        Fill(r, selected ? new Color(0.07f, 0.16f, 0.32f, 0.97f) : new Color(0.025f, 0.055f, 0.13f, 0.96f));
        Stroke(r, selected ? Gold() : new Color(0.30f, 0.38f, 0.52f), selected ? 4f : 2f);

        Vector2 portrait = new Vector2((r.x + r.height * 0.38f) / Screen.width, (r.y + r.height * 0.50f) / Screen.height);
        DrawHeroSilhouette(portrait, id == 0 ? 0.050f : 0.055f);

        GUI.color = selected ? Gold() : Color.white;
        GUI.Label(new Rect(r.x + r.height * 0.72f, r.y + 15f * s, r.width - r.height * 0.78f, 42f * s), (selected ? "✓ " : "") + name, Left(Mathf.RoundToInt(25f * s)));
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + r.height * 0.72f, r.y + 58f * s, r.width - r.height * 0.78f, 34f * s), weapon, Left(Mathf.RoundToInt(17f * s)));
        GUI.color = new Color(0.82f, 0.87f, 0.96f);
        GUI.Label(new Rect(r.x + r.height * 0.72f, r.y + 96f * s, r.width - r.height * 0.78f, 70f * s), detail, Left(Mathf.RoundToInt(15f * s)));
        GUI.color = Color.white;

        if (GUI.Button(r, "", TransparentButton())) { selectedHero = id; ApplyHeroBaseStats(); Haptic(); }
    }

    void DrawSettings(int w, int h, float s)
    {
        DrawPanelBackground(w, h);
        GUI.color = Gold();
        GUI.Label(new Rect(0, h * 0.07f, w, 70f * s), "IMPOSTAZIONI", Center(Mathf.RoundToInt(38f * s)));
        GUI.color = Color.white;

        DrawSettingRow(w, h, s, 0.26f, "VIBRAZIONE", vibration, () => { vibration = !vibration; Haptic(); });
        DrawSettingRow(w, h, s, 0.39f, "AUDIO", sound, () => { sound = !sound; Haptic(); });

        GUI.color = new Color(0.74f, 0.80f, 0.92f);
        GUI.Label(new Rect(w * 0.10f, h * 0.57f, w * 0.80f, 120f * s), "60 FPS\nControllo a un dito\nAuto-attacco da fermo\nBuild stabile 0.7", Center(Mathf.RoundToInt(18f * s)));
        GUI.color = Color.white;

        if (GUI.Button(new Rect(w * 0.20f, h * 0.82f, w * 0.60f, 68f * s), "INDIETRO", Button(Mathf.RoundToInt(21f * s)))) { state = AppState.Menu; Haptic(); }
    }

    void DrawSettingRow(int w, int h, float s, float y, string label, bool enabled, Action action)
    {
        Rect r = new Rect(w * 0.10f, h * y, w * 0.80f, 72f * s);
        Fill(r, new Color(0.035f, 0.075f, 0.16f, 0.96f));
        Stroke(r, new Color(0.26f, 0.38f, 0.58f), 2f);
        GUI.Label(new Rect(r.x + 22f * s, r.y, r.width * 0.58f, r.height), label, Left(Mathf.RoundToInt(20f * s)));
        GUI.color = enabled ? new Color(0.16f, 0.82f, 0.42f) : new Color(0.55f, 0.58f, 0.64f);
        GUI.Label(new Rect(r.x + r.width * 0.62f, r.y, r.width * 0.32f, r.height), enabled ? "ON" : "OFF", Right(Mathf.RoundToInt(20f * s)));
        GUI.color = Color.white;
        if (GUI.Button(r, "", TransparentButton())) action();
    }

    void DrawGame(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.004f, 0.012f, 0.035f));
        Rect arena = ArenaRect();
        DrawArena(arena, w, h);
        DrawHud(w, h, s);

        DrawHeroSilhouette(hero, selectedHero == 0 ? 0.044f : 0.050f);
        for (int i = 0; i < enemies.Count; i++) DrawEnemy(enemies[i], w, h, s);
        for (int i = 0; i < shots.Count; i++) DrawProjectile(shots[i].p);

        if (dragging)
        {
            float radius = Mathf.Max(76f, w * 0.14f);
            DrawScreenCircle(dragStart, radius, new Color(0.02f, 0.07f, 0.18f, 0.40f));
            Vector2 knob = dragStart + Vector2.ClampMagnitude(dragNow - dragStart, radius);
            DrawScreenCircle(knob, radius * 0.40f, new Color(0.12f, 0.55f, 1f, 0.76f));
            DrawScreenCircle(knob, radius * 0.22f, new Color(0.96f, 0.72f, 0.18f, 0.90f));
        }

        GUI.color = new Color(0.86f, 0.90f, 0.98f, 0.92f);
        GUI.Label(new Rect(0, h * 0.885f, w, 36f * s), dragging ? "MUOVITI • RILASCIA PER ATTACCARE" : "TRASCINA PER MUOVERTI • AUTO-ATTACCO", Center(Mathf.RoundToInt(14f * s)));
        GUI.color = Color.white;
    }

    void DrawArena(Rect arena, int w, int h)
    {
        Fill(arena, new Color(0.12f, 0.16f, 0.22f));
        for (int i = 0; i < 9; i++)
        {
            float y = arena.y + arena.height * i / 9f;
            Fill(new Rect(arena.x, y, arena.width, 1.5f), new Color(0.24f, 0.30f, 0.37f, 0.52f));
        }
        for (int i = 0; i < 5; i++)
        {
            float x = arena.x + arena.width * i / 5f;
            Fill(new Rect(x, arena.y, 1.5f, arena.height), new Color(0.20f, 0.26f, 0.34f, 0.38f));
        }

        Stroke(arena, Gold(), 4f);
        for (int i = 0; i < 5; i++)
        {
            float y = arena.y + arena.height * (0.10f + i * 0.19f);
            DrawColumn(new Rect(arena.x + 5f, y, w * 0.035f, h * 0.075f));
            DrawColumn(new Rect(arena.xMax - w * 0.035f - 5f, y, w * 0.035f, h * 0.075f));
        }

        Fill(new Rect(arena.x + arena.width * 0.28f, arena.y + arena.height * 0.47f, arena.width * 0.44f, 2f), new Color(0.80f, 0.58f, 0.18f, 0.35f));
    }

    void DrawColumn(Rect r)
    {
        Color stone = new Color(0.74f, 0.71f, 0.60f);
        Fill(new Rect(r.x - r.width * 0.16f, r.y, r.width * 1.32f, r.height * 0.10f), stone);
        Fill(new Rect(r.x, r.y + r.height * 0.08f, r.width, r.height * 0.80f), new Color(0.63f, 0.61f, 0.52f));
        Fill(new Rect(r.x - r.width * 0.12f, r.y + r.height * 0.87f, r.width * 1.24f, r.height * 0.13f), stone);
    }

    void DrawHud(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h * 0.105f), new Color(0.006f, 0.020f, 0.060f, 0.98f));
        Fill(new Rect(0, h * 0.102f, w, 3f), Gold());

        GUI.color = Color.white;
        GUI.Label(new Rect(w * 0.04f, h * 0.012f, w * 0.38f, 38f * s), selectedHero == 0 ? "PERSEO" : "ERACLE", Left(Mathf.RoundToInt(19f * s)));
        GUI.color = Gold();
        GUI.Label(new Rect(w * 0.42f, h * 0.012f, w * 0.54f, 38f * s), "ATTICA • " + wave + "/5", Right(Mathf.RoundToInt(18f * s)));

        Rect hp = new Rect(w * 0.075f, h * 0.064f, w * 0.66f, 11f * s);
        Fill(hp, new Color(0.12f, 0.12f, 0.14f));
        Fill(new Rect(hp.x, hp.y, hp.width * Mathf.Clamp01(heroHp / heroMaxHp), hp.height), new Color(0.15f, 0.78f, 0.33f));
        Stroke(hp, new Color(0.70f, 0.76f, 0.82f), 1f);

        GUI.color = Color.white;
        GUI.Label(new Rect(w * 0.76f, h * 0.052f, w * 0.20f, 30f * s), "KO " + kills, Right(Mathf.RoundToInt(14f * s)));
    }

    void DrawHeroSilhouette(Vector2 p, float r)
    {
        float w = Screen.width;
        float h = Screen.height;
        Vector2 center = new Vector2(p.x * w, p.y * h);
        float size = r * w;

        DrawScreenCircle(new Vector2(center.x, center.y - size * 0.55f), size * 0.36f, new Color(0.92f, 0.68f, 0.42f));
        Fill(new Rect(center.x - size * 0.48f, center.y - size * 0.20f, size * 0.96f, size * 1.10f), new Color(0.035f, 0.18f, 0.52f));
        Fill(new Rect(center.x - size * 0.62f, center.y - size * 0.05f, size * 0.22f, size * 0.95f), new Color(0.06f, 0.10f, 0.22f));
        Fill(new Rect(center.x + size * 0.43f, center.y - size * 0.10f, size * 0.12f, size * 1.20f), Gold());
        DrawScreenCircle(new Vector2(center.x, center.y + size * 0.10f), size * 0.14f, new Color(0.12f, 0.62f, 1f));
    }

    void DrawEnemy(Enemy e, int w, int h, float s)
    {
        Vector2 c = new Vector2(e.p.x * w, e.p.y * h);
        float r = e.radius * w;

        if (e.type == EnemyType.Satyr)
        {
            DrawScreenCircle(c, r, new Color(0.58f, 0.12f, 0.065f));
            Fill(new Rect(c.x - r * 0.78f, c.y - r * 1.28f, r * 0.28f, r * 0.70f), new Color(0.86f, 0.62f, 0.20f));
            Fill(new Rect(c.x + r * 0.50f, c.y - r * 1.28f, r * 0.28f, r * 0.70f), new Color(0.86f, 0.62f, 0.20f));
        }
        else if (e.type == EnemyType.Snake)
        {
            for (int i = 0; i < 4; i++)
                DrawScreenCircle(new Vector2(c.x, c.y + i * r * 0.55f), r * (0.78f - i * 0.10f), new Color(0.10f, 0.54f, 0.28f));
            DrawScreenCircle(new Vector2(c.x - r * 0.25f, c.y - r * 0.18f), r * 0.10f, Gold());
            DrawScreenCircle(new Vector2(c.x + r * 0.25f, c.y - r * 0.18f), r * 0.10f, Gold());
        }
        else
        {
            DrawScreenCircle(c, r, new Color(0.52f, 0.23f, 0.10f));
            DrawScreenCircle(new Vector2(c.x - r * 0.45f, c.y), r * 0.70f, new Color(0.82f, 0.58f, 0.18f));
            DrawScreenCircle(new Vector2(c.x - r * 0.45f, c.y), r * 0.47f, new Color(0.18f, 0.24f, 0.34f));
            Fill(new Rect(c.x + r * 0.38f, c.y - r * 1.45f, r * 0.15f, r * 2.15f), new Color(0.72f, 0.70f, 0.62f));
        }

        Rect bar = new Rect(c.x - r, c.y - r * 1.65f, r * 2f, Mathf.Max(3f, 5f * s));
        Fill(bar, new Color(0.10f, 0.08f, 0.08f));
        Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(e.hp / e.maxHp), bar.height), new Color(0.82f, 0.16f, 0.08f));
    }

    void DrawProjectile(Vector2 p)
    {
        Vector2 c = new Vector2(p.x * Screen.width, p.y * Screen.height);
        float r = Screen.width * 0.011f;
        DrawScreenCircle(c, r * 1.75f, new Color(0.08f, 0.52f, 1f, 0.28f));
        DrawScreenCircle(c, r, new Color(0.22f, 0.78f, 1f));
        DrawScreenCircle(c, r * 0.43f, Gold());
    }

    void DrawUpgrade(int w, int h, float s)
    {
        DrawGame(w, h, s);
        Fill(new Rect(0, 0, w, h), new Color(0.005f, 0.012f, 0.035f, 0.84f));
        GUI.color = Gold();
        GUI.Label(new Rect(0, h * 0.12f, w, 70f * s), "BENEDIZIONE DEGLI DEI", Center(Mathf.RoundToInt(31f * s)));
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.17f, w, 45f * s), "Scegli un potere", Center(Mathf.RoundToInt(18f * s)));

        for (int i = 0; i < 3; i++)
        {
            Rect r = new Rect(w * 0.08f, h * (0.28f + i * 0.19f), w * 0.84f, h * 0.145f);
            Fill(r, new Color(0.035f, 0.085f, 0.18f, 0.98f));
            Stroke(r, i == 0 ? Gold() : new Color(0.24f, 0.42f, 0.68f), 3f);
            GUI.color = Gold();
            GUI.Label(new Rect(r.x + 20f * s, r.y + 10f * s, r.width - 40f * s, 42f * s), upgradeNames[i], Left(Mathf.RoundToInt(22f * s)));
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 20f * s, r.y + 52f * s, r.width - 40f * s, 45f * s), upgradeDescriptions[i], Left(Mathf.RoundToInt(16f * s)));
            if (GUI.Button(r, "", TransparentButton())) ApplyUpgrade(upgradeIds[i]);
        }
    }

    void DrawEnd(int w, int h, float s, bool win)
    {
        DrawPanelBackground(w, h);
        GUI.color = Gold();
        GUI.Label(new Rect(0, h * 0.20f, w, 95f * s), win ? "VITTORIA" : "SCONFITTA", Center(Mathf.RoundToInt(50f * s)));
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.34f, w, 70f * s), win ? "Attica è stata liberata" : "Il mito continua", Center(Mathf.RoundToInt(21f * s)));
        GUI.Label(new Rect(0, h * 0.41f, w, 55f * s), "Nemici sconfitti: " + kills, Center(Mathf.RoundToInt(18f * s)));

        if (GUI.Button(new Rect(w * 0.15f, h * 0.59f, w * 0.70f, 76f * s), "RIPROVA", Button(Mathf.RoundToInt(23f * s)))) StartRun();
        if (GUI.Button(new Rect(w * 0.22f, h * 0.72f, w * 0.56f, 64f * s), "MENU", Button(Mathf.RoundToInt(20f * s)))) { state = AppState.Menu; Haptic(); }
    }

    void DrawPanelBackground(int w, int h)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.004f, 0.015f, 0.050f));
        for (int i = 0; i < 8; i++)
        {
            float a = 0.04f + i * 0.008f;
            Fill(new Rect(0, h * i / 8f, w, h / 8f + 1f), new Color(0.02f, 0.10f, 0.22f, a));
        }
        Fill(new Rect(0, h * 0.025f, w, 3f), Gold());
        Fill(new Rect(0, h * 0.965f, w, 3f), Gold());
    }

    Color Gold() => new Color(0.94f, 0.67f, 0.16f);

    void DrawCircle(Vector2 p, float r, Color color)
    {
        float size = r * Screen.width * 2f;
        Vector2 c = new Vector2(p.x * Screen.width, p.y * Screen.height);
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), circleTexture);
        GUI.color = old;
    }

    void DrawScreenCircle(Vector2 p, float r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), circleTexture);
        GUI.color = old;
    }

    void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void Stroke(Rect r, Color c, float t)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    GUIStyle TransparentButton()
    {
        GUIStyle st = new GUIStyle(GUI.skin.button);
        st.normal.background = null;
        st.hover.background = null;
        st.active.background = null;
        st.normal.textColor = Color.clear;
        st.hover.textColor = Color.clear;
        st.active.textColor = Color.clear;
        st.border = new RectOffset(0, 0, 0, 0);
        return st;
    }

    GUIStyle Center(int size)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.alignment = TextAnchor.MiddleCenter;
        st.fontSize = size;
        st.fontStyle = FontStyle.Bold;
        st.wordWrap = true;
        st.normal.textColor = Color.white;
        return st;
    }

    GUIStyle Left(int size)
    {
        GUIStyle st = Center(size);
        st.alignment = TextAnchor.MiddleLeft;
        return st;
    }

    GUIStyle Right(int size)
    {
        GUIStyle st = Center(size);
        st.alignment = TextAnchor.MiddleRight;
        return st;
    }

    GUIStyle Button(int size)
    {
        GUIStyle st = new GUIStyle(GUI.skin.button);
        st.fontSize = size;
        st.fontStyle = FontStyle.Bold;
        st.alignment = TextAnchor.MiddleCenter;
        st.normal.textColor = Color.white;
        st.hover.textColor = Color.white;
        st.active.textColor = Gold();
        return st;
    }
}
