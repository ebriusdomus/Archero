using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class MythbreakerBootstrap : MonoBehaviour
{
    enum AppState { Menu, Heroes, Settings, Playing, GameOver, Victory }

    sealed class Enemy
    {
        public Vector2 p;
        public float hp;
        public float speed;
        public float radius;
    }

    sealed class Shot
    {
        public Vector2 p;
        public Vector2 v;
    }

    AppState state = AppState.Menu;
    Texture2D menu;
    Texture2D circle;
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();

    Vector2 hero = new Vector2(0.5f, 0.79f);
    Vector2 moveInput;
    Vector2 dragStart;
    Vector2 dragNow;
    bool dragging;

    float heroHp = 100f;
    float nextShot;
    float hurtCooldown;
    int wave = 1;
    int selectedHero;
    string status = "MYTHBREAKER 0.5 • TOUCH FIX";

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        menu = Resources.Load<Texture2D>("mythbreaker_menu");
        if (menu == null) status = "MYTHBREAKER 0.5 • MENU ASSET MISSING";
        circle = MakeCircle(64);
    }

    Texture2D MakeCircle(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        float c = (size - 1) * 0.5f;
        float r = c - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float a = Mathf.Clamp01(r + 1.2f - d);
            px[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px);
        t.Apply(false, false);
        return t;
    }

    void Update()
    {
        if (state != AppState.Playing) return;

        if (dragging && moveInput.sqrMagnitude > 0.0025f)
        {
            float speed = selectedHero == 1 ? 0.36f : 0.43f;
            hero += moveInput * speed * Time.deltaTime;
            hero.x = Mathf.Clamp(hero.x, 0.12f, 0.88f);
            hero.y = Mathf.Clamp(hero.y, 0.18f, 0.86f);
        }

        UpdateCombat();
    }

    void UpdateCombat()
    {
        float dt = Time.deltaTime;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            Vector2 d = hero - e.p;
            if (d.sqrMagnitude > 0.0001f) e.p += d.normalized * e.speed * dt;

            if (Vector2.Distance(hero, e.p) < e.radius + 0.038f && Time.time >= hurtCooldown)
            {
                heroHp -= 12f;
                hurtCooldown = Time.time + 0.65f;
                if (d.sqrMagnitude > 0.001f) e.p -= d.normalized * 0.07f;
                if (heroHp <= 0f)
                {
                    heroHp = 0f;
                    dragging = false;
                    moveInput = Vector2.zero;
                    state = AppState.GameOver;
                    return;
                }
            }
        }

        // Archero rule: attack automatically only while the player is stopped.
        if (!dragging && enemies.Count > 0 && Time.time >= nextShot)
        {
            nextShot = Time.time + (selectedHero == 1 ? 0.62f : 0.43f);
            Enemy nearest = enemies[0];
            float best = (nearest.p - hero).sqrMagnitude;
            for (int i = 1; i < enemies.Count; i++)
            {
                float dist = (enemies[i].p - hero).sqrMagnitude;
                if (dist < best) { best = dist; nearest = enemies[i]; }
            }
            Vector2 dir = (nearest.p - hero).normalized;
            shots.Add(new Shot { p = hero, v = dir * 0.82f });
        }

        for (int s = shots.Count - 1; s >= 0; s--)
        {
            Shot shot = shots[s];
            shot.p += shot.v * dt;
            bool remove = shot.p.x < 0.04f || shot.p.x > 0.96f || shot.p.y < 0.10f || shot.p.y > 0.92f;

            if (!remove)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy e = enemies[i];
                    if (Vector2.Distance(shot.p, e.p) < e.radius + 0.018f)
                    {
                        e.hp -= selectedHero == 1 ? 34f : 24f;
                        remove = true;
                        if (e.hp <= 0f) enemies.RemoveAt(i);
                        break;
                    }
                }
            }
            if (remove) shots.RemoveAt(s);
        }

        if (enemies.Count == 0)
        {
            if (wave >= 5) state = AppState.Victory;
            else { wave++; SpawnWave(); }
        }
    }

    void StartGame()
    {
        state = AppState.Playing;
        hero = new Vector2(0.5f, 0.79f);
        heroHp = selectedHero == 1 ? 135f : 100f;
        nextShot = 0f;
        hurtCooldown = 0f;
        wave = 1;
        dragging = false;
        moveInput = Vector2.zero;
        enemies.Clear();
        shots.Clear();
        SpawnWave();
    }

    void SpawnWave()
    {
        enemies.Clear();
        shots.Clear();
        int count = 2 + wave;
        for (int i = 0; i < count; i++)
        {
            float x = 0.16f + 0.68f * ((i + 1f) / (count + 1f));
            float y = 0.22f + 0.055f * (i % 2);
            enemies.Add(new Enemy
            {
                p = new Vector2(x, y),
                hp = 34f + wave * 9f,
                speed = 0.050f + wave * 0.0055f,
                radius = 0.033f + (wave == 5 && i == count - 1 ? 0.028f : 0f)
            });
        }
    }

    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;
        float s = Mathf.Clamp(w / 720f, 0.72f, 1.8f);

        if (state == AppState.Playing)
        {
            HandleGuiMovement(Event.current, w, h);
            DrawGame(w, h, s);
        }
        else if (state == AppState.Heroes) DrawHeroes(w, h, s);
        else if (state == AppState.Settings) DrawSettings(w, h, s);
        else if (state == AppState.GameOver) DrawEnd(w, h, s, false);
        else if (state == AppState.Victory) DrawEnd(w, h, s, true);
        else DrawMenu(w, h, s);
    }

    void HandleGuiMovement(Event e, int w, int h)
    {
        Rect arena = new Rect(w * 0.07f, h * 0.12f, w * 0.86f, h * 0.78f);
        if (e == null) return;

        if (e.type == EventType.MouseDown && arena.Contains(e.mousePosition))
        {
            dragging = true;
            dragStart = e.mousePosition;
            dragNow = dragStart;
            moveInput = Vector2.zero;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && dragging)
        {
            dragNow = e.mousePosition;
            Vector2 delta = dragNow - dragStart;
            float radius = Mathf.Max(70f, w * 0.12f);
            delta = Vector2.ClampMagnitude(delta, radius);
            moveInput = delta / radius;
            e.Use();
        }
        else if (e.type == EventType.MouseUp && dragging)
        {
            dragging = false;
            moveInput = Vector2.zero;
            e.Use();
        }
    }

    void DrawMenu(int w, int h, float s)
    {
        if (menu != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), menu, ScaleMode.ScaleAndCrop);
            GUI.color = Color.white;

            GUIStyle invisible = new GUIStyle(GUI.skin.button);
            invisible.normal.background = null;
            invisible.hover.background = null;
            invisible.active.background = null;
            invisible.normal.textColor = new Color(0f, 0f, 0f, 0f);
            invisible.hover.textColor = new Color(0f, 0f, 0f, 0f);
            invisible.active.textColor = new Color(0f, 0f, 0f, 0f);

            if (GUI.Button(new Rect(w * 0.12f, h * 0.68f, w * 0.76f, h * 0.11f), "START", invisible)) StartGame();
            if (GUI.Button(new Rect(w * 0.03f, h * 0.79f, w * 0.46f, h * 0.11f), "HEROES", invisible)) state = AppState.Heroes;
            if (GUI.Button(new Rect(w * 0.51f, h * 0.79f, w * 0.46f, h * 0.11f), "SETTINGS", invisible)) state = AppState.Settings;
            return;
        }

        Fill(new Rect(0, 0, w, h), new Color(0.008f, 0.025f, 0.08f));
        GUI.color = new Color(0.95f, 0.68f, 0.12f);
        GUI.Label(new Rect(0, h * 0.13f, w, 70f * s), "lello's game", Center(Mathf.RoundToInt(27f * s)));
        GUI.Label(new Rect(0, h * 0.28f, w, 100f * s), "MYTHBREAKER", Center(Mathf.RoundToInt(52f * s)));
        GUI.color = Color.white;
        if (GUI.Button(new Rect(w * 0.12f, h * 0.62f, w * 0.76f, 72f * s), "NUOVA PARTITA", Button(Mathf.RoundToInt(23f * s)))) StartGame();
        if (GUI.Button(new Rect(w * 0.12f, h * 0.72f, w * 0.76f, 72f * s), "EROI", Button(Mathf.RoundToInt(23f * s)))) state = AppState.Heroes;
        if (GUI.Button(new Rect(w * 0.12f, h * 0.82f, w * 0.76f, 72f * s), "IMPOSTAZIONI", Button(Mathf.RoundToInt(23f * s)))) state = AppState.Settings;
    }

    void DrawHeroes(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.008f, 0.025f, 0.08f));
        GUI.color = new Color(0.95f, 0.68f, 0.12f);
        GUI.Label(new Rect(0, h * 0.07f, w, 70f * s), "SCEGLI L'EROE", Center(Mathf.RoundToInt(40f * s)));
        GUI.color = Color.white;
        GUIStyle b = Button(Mathf.RoundToInt(22f * s));
        string p = selectedHero == 0 ? "✓ PERSEO • Mythbow" : "PERSEO • Mythbow";
        string er = selectedHero == 1 ? "✓ ERACLE • Clava di Nemea" : "ERACLE • Clava di Nemea";
        if (GUI.Button(new Rect(w * 0.10f, h * 0.27f, w * 0.80f, 90f * s), p, b)) selectedHero = 0;
        if (GUI.Button(new Rect(w * 0.10f, h * 0.41f, w * 0.80f, 90f * s), er, b)) selectedHero = 1;
        GUI.Label(new Rect(w * 0.10f, h * 0.56f, w * 0.80f, 120f * s), selectedHero == 0 ? "Perseo • veloce e preciso" : "Eracle • più vita e più danno", Center(Mathf.RoundToInt(20f * s)));
        if (GUI.Button(new Rect(w * 0.15f, h * 0.75f, w * 0.70f, 72f * s), "GIOCA", b)) StartGame();
        if (GUI.Button(new Rect(w * 0.25f, h * 0.86f, w * 0.50f, 60f * s), "INDIETRO", b)) state = AppState.Menu;
    }

    void DrawSettings(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.008f, 0.025f, 0.08f));
        GUI.color = new Color(0.95f, 0.68f, 0.12f);
        GUI.Label(new Rect(0, h * 0.10f, w, 70f * s), "IMPOSTAZIONI", Center(Mathf.RoundToInt(40f * s)));
        GUI.color = Color.white;
        GUI.Label(new Rect(w * 0.10f, h * 0.28f, w * 0.80f, 250f * s), "60 FPS\nVerticale\nJoystick dinamico a un dito\nAttacco automatico da fermo\nBuild 0.5", Center(Mathf.RoundToInt(22f * s)));
        if (GUI.Button(new Rect(w * 0.22f, h * 0.76f, w * 0.56f, 70f * s), "INDIETRO", Button(Mathf.RoundToInt(22f * s)))) state = AppState.Menu;
    }

    void DrawGame(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.015f, 0.025f, 0.055f));
        Rect arena = new Rect(w * 0.07f, h * 0.12f, w * 0.86f, h * 0.78f);
        Fill(arena, new Color(0.68f, 0.64f, 0.49f));
        Fill(new Rect(arena.x, arena.y, arena.width, h * 0.015f), new Color(0.73f, 0.52f, 0.13f));
        Fill(new Rect(arena.x, arena.yMax - h * 0.015f, arena.width, h * 0.015f), new Color(0.73f, 0.52f, 0.13f));

        for (int i = 0; i < 5; i++)
        {
            float y = h * (0.18f + i * 0.14f);
            Fill(new Rect(w * 0.073f, y, w * 0.035f, h * 0.085f), new Color(0.90f, 0.87f, 0.72f));
            Fill(new Rect(w * 0.892f, y, w * 0.035f, h * 0.085f), new Color(0.90f, 0.87f, 0.72f));
        }

        Fill(new Rect(0, 0, w, h * 0.105f), new Color(0.005f, 0.018f, 0.060f));
        GUI.Label(new Rect(w * 0.04f, h * 0.015f, w * 0.40f, 45f * s), selectedHero == 0 ? "PERSEO" : "ERACLE", Left(Mathf.RoundToInt(19f * s)));
        GUI.Label(new Rect(w * 0.40f, h * 0.015f, w * 0.56f, 45f * s), "ATTICA • ONDATA " + wave + "/5", Right(Mathf.RoundToInt(19f * s)));
        Fill(new Rect(w * 0.08f, h * 0.065f, w * 0.84f, 12f * s), new Color(0.16f, 0.14f, 0.13f));
        Fill(new Rect(w * 0.08f, h * 0.065f, w * 0.84f * Mathf.Clamp01(heroHp / (selectedHero == 1 ? 135f : 100f)), 12f * s), new Color(0.10f, 0.77f, 0.23f));

        DrawCircle(w, h, hero, 0.045f, new Color(0.02f, 0.16f, 0.55f));
        DrawCircle(w, h, hero + new Vector2(0f, -0.025f), 0.022f, new Color(0.98f, 0.68f, 0.10f));
        for (int i = 0; i < enemies.Count; i++)
            DrawCircle(w, h, enemies[i].p, enemies[i].radius, i == enemies.Count - 1 && wave == 5 ? new Color(0.32f, 0.035f, 0.02f) : new Color(0.66f, 0.10f, 0.045f));
        for (int i = 0; i < shots.Count; i++)
            DrawCircle(w, h, shots[i].p, 0.011f, new Color(0.08f, 0.72f, 1f));

        if (dragging)
        {
            float radius = Mathf.Max(70f, w * 0.12f);
            DrawScreenCircle(dragStart, radius, new Color(0.02f, 0.08f, 0.20f, 0.32f));
            Vector2 knob = dragStart + Vector2.ClampMagnitude(dragNow - dragStart, radius);
            DrawScreenCircle(knob, radius * 0.42f, new Color(0.95f, 0.65f, 0.10f, 0.70f));
        }

        GUI.Label(new Rect(0, h * 0.91f, w, 40f * s), dragging ? "MUOVITI • RILASCIA PER ATTACCARE" : "TRASCINA PER MUOVERTI • ATTACCO AUTOMATICO", Center(Mathf.RoundToInt(15f * s)));
        GUI.Label(new Rect(0, h - 27f * s, w, 25f * s), "MYTHBREAKER 0.5", Center(Mathf.RoundToInt(13f * s)));
    }

    void DrawEnd(int w, int h, float s, bool win)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.008f, 0.025f, 0.08f));
        GUI.color = new Color(0.95f, 0.68f, 0.12f);
        GUI.Label(new Rect(0, h * 0.24f, w, 100f * s), win ? "VITTORIA" : "SCONFITTA", Center(Mathf.RoundToInt(50f * s)));
        GUI.color = Color.white;
        if (GUI.Button(new Rect(w * 0.15f, h * 0.58f, w * 0.70f, 78f * s), "RIPROVA", Button(Mathf.RoundToInt(23f * s)))) StartGame();
        if (GUI.Button(new Rect(w * 0.22f, h * 0.72f, w * 0.56f, 64f * s), "MENU", Button(Mathf.RoundToInt(21f * s)))) state = AppState.Menu;
    }

    void DrawCircle(int w, int h, Vector2 p, float r, Color color)
    {
        float size = r * w * 2f;
        Rect rect = new Rect(p.x * w - size * 0.5f, p.y * h - size * 0.5f, size, size);
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, circle);
        GUI.color = old;
    }

    void DrawScreenCircle(Vector2 p, float r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), circle);
        GUI.color = old;
    }

    void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
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

    GUIStyle Left(int size) { GUIStyle st = Center(size); st.alignment = TextAnchor.MiddleLeft; return st; }
    GUIStyle Right(int size) { GUIStyle st = Center(size); st.alignment = TextAnchor.MiddleRight; return st; }
    GUIStyle Button(int size) { GUIStyle st = new GUIStyle(GUI.skin.button); st.fontSize = size; st.fontStyle = FontStyle.Bold; st.wordWrap = true; return st; }
}
