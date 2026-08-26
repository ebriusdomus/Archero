using System;
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
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();

    Vector2 hero = new Vector2(0.5f, 0.78f);
    float heroHp = 100f;
    float nextShot;
    float hurtCooldown;
    int wave = 1;
    int selectedHero;
    string status = "BUILD 0.4 • MAIN • SAFE RUNTIME";

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        LoadMenu();
    }

    void LoadMenu()
    {
        menu = Resources.Load<Texture2D>("mythbreaker_menu");
        if (menu != null) return;

        try
        {
            TextAsset encoded = Resources.Load<TextAsset>("mythbreaker_menu_b64");
            if (encoded == null || string.IsNullOrWhiteSpace(encoded.text))
            {
                status = "BUILD 0.4 • MENU FALLBACK";
                return;
            }

            byte[] bytes = Convert.FromBase64String(encoded.text.Trim());
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(bytes, false)) menu = tex;
            else Destroy(tex);
        }
        catch (Exception e)
        {
            status = "MENU ERROR • " + e.GetType().Name;
        }
    }

    void Update()
    {
        if (state != AppState.Playing) return;

        HandleMovement();
        UpdateCombat();
    }

    void HandleMovement()
    {
        Vector2 target = hero;
        bool moving = false;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled)
            {
                target = new Vector2(t.position.x / Screen.width, 1f - t.position.y / Screen.height);
                moving = true;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 m = Input.mousePosition;
            target = new Vector2(m.x / Screen.width, 1f - m.y / Screen.height);
            moving = true;
        }

        if (moving)
        {
            target.x = Mathf.Clamp(target.x, 0.10f, 0.90f);
            target.y = Mathf.Clamp(target.y, 0.20f, 0.88f);
            hero = Vector2.Lerp(hero, target, Mathf.Clamp01(Time.deltaTime * 12f));
        }
    }

    void UpdateCombat()
    {
        float dt = Time.deltaTime;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            Vector2 d = hero - e.p;
            if (d.sqrMagnitude > 0.0001f) e.p += d.normalized * e.speed * dt;

            if (Vector2.Distance(hero, e.p) < e.radius + 0.035f && Time.time >= hurtCooldown)
            {
                heroHp -= 12f;
                hurtCooldown = Time.time + 0.65f;
                e.p -= d.normalized * 0.08f;
                if (heroHp <= 0f)
                {
                    heroHp = 0f;
                    state = AppState.GameOver;
                    return;
                }
            }
        }

        if (enemies.Count > 0 && Time.time >= nextShot)
        {
            nextShot = Time.time + (selectedHero == 1 ? 0.60f : 0.42f);
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
            bool remove = shot.p.x < 0f || shot.p.x > 1f || shot.p.y < 0f || shot.p.y > 1f;

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
            else
            {
                wave++;
                SpawnWave();
            }
        }
    }

    void StartGame()
    {
        state = AppState.Playing;
        hero = new Vector2(0.5f, 0.78f);
        heroHp = selectedHero == 1 ? 135f : 100f;
        nextShot = 0f;
        hurtCooldown = 0f;
        wave = 1;
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
            float x = 0.16f + (0.68f * ((i + 1f) / (count + 1f)));
            float y = 0.22f + 0.055f * (i % 2);
            enemies.Add(new Enemy
            {
                p = new Vector2(x, y),
                hp = 34f + wave * 9f,
                speed = 0.055f + wave * 0.006f,
                radius = 0.032f + (wave == 5 && i == count - 1 ? 0.025f : 0f)
            });
        }
    }

    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;
        float s = Mathf.Clamp(w / 720f, 0.72f, 1.8f);

        if (state == AppState.Playing) DrawGame(w, h, s);
        else if (state == AppState.Heroes) DrawHeroes(w, h, s);
        else if (state == AppState.Settings) DrawSettings(w, h, s);
        else if (state == AppState.GameOver) DrawEnd(w, h, s, false);
        else if (state == AppState.Victory) DrawEnd(w, h, s, true);
        else DrawMenu(w, h, s);

        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, h - 32f * s, w, 32f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h - 32f * s, w, 30f * s), status, Center(Mathf.RoundToInt(15f * s)));
    }

    void DrawMenu(int w, int h, float s)
    {
        if (menu != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), menu, ScaleMode.ScaleAndCrop);
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        }
        else
        {
            Fill(new Rect(0, 0, w, h), new Color(0.015f, 0.04f, 0.13f));
            Fill(new Rect(w * 0.07f, h * 0.13f, w * 0.86f, h * 0.34f), new Color(0.03f, 0.12f, 0.30f));
            GUI.Label(new Rect(0, h * 0.08f, w, 50f * s), "lello's game", Center(Mathf.RoundToInt(27f * s)));
            GUI.Label(new Rect(0, h * 0.23f, w, 90f * s), "MYTHBREAKER", Center(Mathf.RoundToInt(52f * s)));
            GUI.Label(new Rect(0, h * 0.34f, w, 60f * s), "GREEK LEGENDS", Center(Mathf.RoundToInt(22f * s)));
        }

        float bw = w * 0.76f;
        float bh = 70f * s;
        float x = (w - bw) * 0.5f;
        GUIStyle button = Button(Mathf.RoundToInt(23f * s));

        if (GUI.Button(new Rect(x, h * 0.60f, bw, bh), "NUOVA PARTITA", button)) StartGame();
        if (GUI.Button(new Rect(x, h * 0.69f, bw, bh), "EROI", button)) state = AppState.Heroes;
        if (GUI.Button(new Rect(x, h * 0.78f, bw, bh), "IMPOSTAZIONI", button)) state = AppState.Settings;
    }

    void DrawHeroes(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.018f, 0.05f, 0.14f));
        GUI.Label(new Rect(0, h * 0.07f, w, 70f * s), "SCEGLI L'EROE", Center(Mathf.RoundToInt(40f * s)));

        GUIStyle b = Button(Mathf.RoundToInt(22f * s));
        string p = selectedHero == 0 ? "✓ PERSEO • Mythbow" : "PERSEO • Mythbow";
        string e = selectedHero == 1 ? "✓ ERACLE • Clava di Nemea" : "ERACLE • Clava di Nemea";
        if (GUI.Button(new Rect(w * 0.10f, h * 0.27f, w * 0.80f, 90f * s), p, b)) selectedHero = 0;
        if (GUI.Button(new Rect(w * 0.10f, h * 0.41f, w * 0.80f, 90f * s), e, b)) selectedHero = 1;

        GUI.Label(new Rect(w * 0.10f, h * 0.56f, w * 0.80f, 120f * s),
            selectedHero == 0 ? "Perseo: rapido, attacco automatico più veloce." : "Eracle: più vita e colpi più pesanti.",
            Center(Mathf.RoundToInt(20f * s)));

        if (GUI.Button(new Rect(w * 0.15f, h * 0.75f, w * 0.70f, 72f * s), "GIOCA", b)) StartGame();
        if (GUI.Button(new Rect(w * 0.25f, h * 0.86f, w * 0.50f, 60f * s), "INDIETRO", b)) state = AppState.Menu;
    }

    void DrawSettings(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.018f, 0.05f, 0.14f));
        GUI.Label(new Rect(0, h * 0.10f, w, 70f * s), "IMPOSTAZIONI", Center(Mathf.RoundToInt(40f * s)));
        GUI.Label(new Rect(w * 0.10f, h * 0.28f, w * 0.80f, 250f * s),
            "60 FPS\nSchermo verticale\nControllo a un dito\nAuto-attacco da fermo\nBuild stabile 0.4",
            Center(Mathf.RoundToInt(22f * s)));
        if (GUI.Button(new Rect(w * 0.22f, h * 0.76f, w * 0.56f, 70f * s), "INDIETRO", Button(Mathf.RoundToInt(22f * s)))) state = AppState.Menu;
    }

    void DrawGame(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.055f, 0.075f, 0.11f));
        Fill(new Rect(w * 0.07f, h * 0.12f, w * 0.86f, h * 0.78f), new Color(0.73f, 0.70f, 0.58f));

        for (int i = 0; i < 5; i++)
        {
            Fill(new Rect(w * 0.075f, h * (0.18f + i * 0.14f), w * 0.035f, h * 0.09f), new Color(0.88f, 0.86f, 0.76f));
            Fill(new Rect(w * 0.89f, h * (0.18f + i * 0.14f), w * 0.035f, h * 0.09f), new Color(0.88f, 0.86f, 0.76f));
        }

        Fill(new Rect(0, 0, w, h * 0.105f), new Color(0.01f, 0.025f, 0.08f));
        GUI.Label(new Rect(w * 0.04f, h * 0.015f, w * 0.40f, 45f * s), selectedHero == 0 ? "PERSEO" : "ERACLE", Left(Mathf.RoundToInt(19f * s)));
        GUI.Label(new Rect(w * 0.40f, h * 0.015f, w * 0.56f, 45f * s), "ATTICA • ONDATA " + wave + "/5", Right(Mathf.RoundToInt(19f * s)));

        Fill(new Rect(w * 0.08f, h * 0.065f, w * 0.84f, 12f * s), new Color(0.18f, 0.16f, 0.15f));
        Fill(new Rect(w * 0.08f, h * 0.065f, w * 0.84f * Mathf.Clamp01(heroHp / (selectedHero == 1 ? 135f : 100f)), 12f * s), new Color(0.12f, 0.75f, 0.24f));

        DrawCircle(w, h, hero, 0.037f, new Color(0.06f, 0.23f, 0.65f));
        DrawCircle(w, h, hero + new Vector2(0f, -0.025f), 0.018f, new Color(0.98f, 0.69f, 0.14f));

        for (int i = 0; i < enemies.Count; i++)
            DrawCircle(w, h, enemies[i].p, enemies[i].radius, i == enemies.Count - 1 && wave == 5 ? new Color(0.38f, 0.08f, 0.04f) : new Color(0.68f, 0.15f, 0.08f));

        for (int i = 0; i < shots.Count; i++)
            DrawCircle(w, h, shots[i].p, 0.010f, new Color(0.10f, 0.72f, 1f));

        GUI.Label(new Rect(0, h * 0.91f, w, 40f * s), "TOCCA E TRASCINA PER MUOVERTI • ATTACCO AUTOMATICO", Center(Mathf.RoundToInt(15f * s)));
    }

    void DrawEnd(int w, int h, float s, bool win)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.015f, 0.04f, 0.12f));
        GUI.Label(new Rect(0, h * 0.24f, w, 100f * s), win ? "VITTORIA" : "SCONFITTA", Center(Mathf.RoundToInt(50f * s)));
        GUI.Label(new Rect(0, h * 0.37f, w, 80f * s), win ? "Il primo frammento del mito è tuo." : "Gli dei ti attendono di nuovo.", Center(Mathf.RoundToInt(22f * s)));
        if (GUI.Button(new Rect(w * 0.15f, h * 0.58f, w * 0.70f, 78f * s), "RIPROVA", Button(Mathf.RoundToInt(23f * s)))) StartGame();
        if (GUI.Button(new Rect(w * 0.22f, h * 0.72f, w * 0.56f, 64f * s), "MENU", Button(Mathf.RoundToInt(21f * s)))) state = AppState.Menu;
    }

    void DrawCircle(int w, int h, Vector2 p, float r, Color color)
    {
        float px = p.x * w;
        float py = p.y * h;
        float size = r * w * 2f;
        Fill(new Rect(px - size * 0.5f, py - size * 0.5f, size, size), color);
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
        st.wordWrap = true;
        return st;
    }
}
