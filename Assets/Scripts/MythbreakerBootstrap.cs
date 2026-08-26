using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class MythbreakerBootstrap : MonoBehaviour
{
    enum AppState { Menu, Playing, Victory, GameOver }

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
    Texture2D menuTexture;
    Texture2D circleTexture;
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();

    Vector2 hero = new Vector2(0.50f, 0.79f);
    Vector2 dragStart;
    Vector2 dragNow;
    Vector2 moveInput;
    bool dragging;

    float heroHp = 100f;
    float nextShot;
    float hurtCooldown;
    int wave = 1;
    string diagnostic = "MYTHBREAKER 0.6 • CLEAN BASE";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBoot()
    {
        if (FindFirstObjectByType<MythbreakerBootstrap>() == null)
            new GameObject("MYTHBREAKER CLEAN BOOT 0.6").AddComponent<MythbreakerBootstrap>();
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        circleTexture = MakeCircle(64);
        LoadMenuFromCommittedTextAsset();
    }

    void LoadMenuFromCommittedTextAsset()
    {
        TextAsset encoded = Resources.Load<TextAsset>("mythbreaker_menu_b64");
        if (encoded == null)
        {
            diagnostic = "0.6 • MENU SOURCE MISSING";
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
                diagnostic = "0.6 • MENU IMAGE LOAD FAILED";
                return;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            menuTexture = tex;
        }
        catch (Exception e)
        {
            diagnostic = "0.6 • MENU " + e.GetType().Name;
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
        ReadTouchAndMouse();

        if (dragging && moveInput.sqrMagnitude > 0.001f)
        {
            hero += moveInput * 0.43f * Time.deltaTime;
            hero.x = Mathf.Clamp(hero.x, 0.12f, 0.88f);
            hero.y = Mathf.Clamp(hero.y, 0.22f, 0.84f);
        }

        UpdateCombat();
    }

    void ReadTouchAndMouse()
    {
        try
        {
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                Vector2 guiPos = new Vector2(t.position.x, Screen.height - t.position.y);

                if (t.phase == TouchPhase.Began)
                    BeginDrag(guiPos);
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    ContinueDrag(guiPos);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    EndDrag();
                return;
            }

            if (Input.GetMouseButtonDown(0)) BeginDrag(InputToGui(Input.mousePosition));
            else if (Input.GetMouseButton(0)) ContinueDrag(InputToGui(Input.mousePosition));
            else if (Input.GetMouseButtonUp(0)) EndDrag();
        }
        catch (Exception e)
        {
            diagnostic = "0.6 • INPUT " + e.GetType().Name;
            EndDrag();
        }
    }

    Vector2 InputToGui(Vector3 p) => new Vector2(p.x, Screen.height - p.y);

    void BeginDrag(Vector2 p)
    {
        Rect arena = ArenaRect();
        if (!arena.Contains(p)) return;
        dragging = true;
        dragStart = p;
        dragNow = p;
        moveInput = Vector2.zero;
    }

    void ContinueDrag(Vector2 p)
    {
        if (!dragging) return;
        dragNow = p;
        float radius = Mathf.Max(72f, Screen.width * 0.13f);
        Vector2 delta = Vector2.ClampMagnitude(dragNow - dragStart, radius);
        moveInput = delta / radius;
    }

    void EndDrag()
    {
        dragging = false;
        moveInput = Vector2.zero;
    }

    Rect ArenaRect()
    {
        return new Rect(Screen.width * 0.065f, Screen.height * 0.12f, Screen.width * 0.87f, Screen.height * 0.76f);
    }

    void StartRun()
    {
        state = AppState.Playing;
        hero = new Vector2(0.50f, 0.79f);
        heroHp = 100f;
        wave = 1;
        nextShot = 0f;
        hurtCooldown = 0f;
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
            float x = 0.18f + 0.64f * ((i + 1f) / (count + 1f));
            float y = 0.23f + (i % 2) * 0.055f;
            enemies.Add(new Enemy
            {
                p = new Vector2(x, y),
                hp = 30f + wave * 8f,
                speed = 0.045f + wave * 0.004f,
                radius = 0.031f
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
            if (d.sqrMagnitude > 0.0001f) e.p += d.normalized * e.speed * dt;

            if (Vector2.Distance(hero, e.p) < e.radius + 0.038f && Time.time >= hurtCooldown)
            {
                heroHp -= 12f;
                hurtCooldown = Time.time + 0.65f;
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
            nextShot = Time.time + 0.43f;
            Enemy nearest = enemies[0];
            float best = (nearest.p - hero).sqrMagnitude;
            for (int i = 1; i < enemies.Count; i++)
            {
                float d = (enemies[i].p - hero).sqrMagnitude;
                if (d < best) { best = d; nearest = enemies[i]; }
            }
            Vector2 dir = (nearest.p - hero).normalized;
            shots.Add(new Shot { p = hero, v = dir * 0.78f });
        }

        for (int s = shots.Count - 1; s >= 0; s--)
        {
            Shot shot = shots[s];
            shot.p += shot.v * dt;
            bool remove = shot.p.x < 0.05f || shot.p.x > 0.95f || shot.p.y < 0.12f || shot.p.y > 0.90f;

            if (!remove)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy e = enemies[i];
                    if (Vector2.Distance(shot.p, e.p) < e.radius + 0.018f)
                    {
                        e.hp -= 22f;
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
            if (wave >= 3) state = AppState.Victory;
            else { wave++; SpawnWave(); }
        }
    }

    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;
        float s = Mathf.Clamp(w / 720f, 0.72f, 1.8f);

        if (state == AppState.Menu) DrawMenu(w, h, s);
        else if (state == AppState.Playing) DrawGame(w, h, s);
        else DrawEnd(w, h, s, state == AppState.Victory);

        GUI.color = new Color(0f, 0f, 0f, 0.76f);
        GUI.DrawTexture(new Rect(0, h - 30f * s, w, 30f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h - 30f * s, w, 28f * s), diagnostic, Center(Mathf.RoundToInt(13f * s)));
    }

    void DrawMenu(int w, int h, float s)
    {
        if (menuTexture != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, w, h), menuTexture, ScaleMode.ScaleAndCrop);
            GUIStyle invisible = new GUIStyle(GUI.skin.button);
            invisible.normal.background = null;
            invisible.hover.background = null;
            invisible.active.background = null;
            invisible.normal.textColor = Color.clear;
            invisible.hover.textColor = Color.clear;
            invisible.active.textColor = Color.clear;
            if (GUI.Button(new Rect(w * 0.10f, h * 0.61f, w * 0.80f, h * 0.23f), "START", invisible)) StartRun();
        }
        else
        {
            Fill(new Rect(0, 0, w, h), new Color(0.008f, 0.025f, 0.075f));
            GUI.color = new Color(0.94f, 0.68f, 0.14f);
            GUI.Label(new Rect(0, h * 0.12f, w, 70f * s), "lello's game", Center(Mathf.RoundToInt(28f * s)));
            GUI.Label(new Rect(0, h * 0.28f, w, 110f * s), "MYTHBREAKER", Center(Mathf.RoundToInt(52f * s)));
            GUI.color = Color.white;
            if (GUI.Button(new Rect(w * 0.14f, h * 0.64f, w * 0.72f, 78f * s), "NUOVA PARTITA", Button(Mathf.RoundToInt(24f * s)))) StartRun();
        }
    }

    void DrawGame(int w, int h, float s)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.010f, 0.020f, 0.050f));
        Rect arena = ArenaRect();
        Fill(arena, new Color(0.62f, 0.58f, 0.43f));

        Fill(new Rect(0, 0, w, h * 0.105f), new Color(0.005f, 0.016f, 0.055f));
        GUI.Label(new Rect(w * 0.04f, h * 0.015f, w * 0.38f, 42f * s), "PERSEO", Left(Mathf.RoundToInt(19f * s)));
        GUI.Label(new Rect(w * 0.42f, h * 0.015f, w * 0.54f, 42f * s), "ATTICA • " + wave + "/3", Right(Mathf.RoundToInt(19f * s)));

        Fill(new Rect(w * 0.08f, h * 0.067f, w * 0.84f, 11f * s), new Color(0.18f, 0.15f, 0.13f));
        Fill(new Rect(w * 0.08f, h * 0.067f, w * 0.84f * Mathf.Clamp01(heroHp / 100f), 11f * s), new Color(0.10f, 0.76f, 0.23f));

        for (int i = 0; i < 5; i++)
        {
            float y = h * (0.18f + i * 0.135f);
            Fill(new Rect(arena.x + 3, y, w * 0.026f, h * 0.073f), new Color(0.88f, 0.84f, 0.67f));
            Fill(new Rect(arena.xMax - w * 0.026f - 3, y, w * 0.026f, h * 0.073f), new Color(0.88f, 0.84f, 0.67f));
        }

        DrawCircle(hero, 0.047f, new Color(0.04f, 0.18f, 0.62f));
        DrawCircle(hero + new Vector2(0f, -0.024f), 0.021f, new Color(0.96f, 0.66f, 0.10f));

        for (int i = 0; i < enemies.Count; i++) DrawCircle(enemies[i].p, enemies[i].radius, new Color(0.67f, 0.12f, 0.05f));
        for (int i = 0; i < shots.Count; i++) DrawCircle(shots[i].p, 0.011f, new Color(0.05f, 0.72f, 1f));

        if (dragging)
        {
            float radius = Mathf.Max(72f, w * 0.13f);
            DrawScreenCircle(dragStart, radius, new Color(0.02f, 0.06f, 0.16f, 0.34f));
            Vector2 knob = dragStart + Vector2.ClampMagnitude(dragNow - dragStart, radius);
            DrawScreenCircle(knob, radius * 0.42f, new Color(0.96f, 0.66f, 0.10f, 0.80f));
        }

        GUI.Label(new Rect(0, h * 0.90f, w, 44f * s), dragging ? "MUOVITI • RILASCIA PER ATTACCARE" : "TRASCINA PER MUOVERTI • AUTO-ATTACCO", Center(Mathf.RoundToInt(15f * s)));
    }

    void DrawEnd(int w, int h, float s, bool win)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.008f, 0.025f, 0.075f));
        GUI.color = new Color(0.94f, 0.68f, 0.14f);
        GUI.Label(new Rect(0, h * 0.25f, w, 100f * s), win ? "VITTORIA" : "SCONFITTA", Center(Mathf.RoundToInt(50f * s)));
        GUI.color = Color.white;
        if (GUI.Button(new Rect(w * 0.15f, h * 0.57f, w * 0.70f, 78f * s), "RIPROVA", Button(Mathf.RoundToInt(23f * s)))) StartRun();
        if (GUI.Button(new Rect(w * 0.22f, h * 0.71f, w * 0.56f, 66f * s), "MENU", Button(Mathf.RoundToInt(21f * s)))) state = AppState.Menu;
    }

    void DrawCircle(Vector2 p, float r, Color c)
    {
        float size = r * Screen.width * 2f;
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(p.x * Screen.width - size * 0.5f, p.y * Screen.height - size * 0.5f, size, size), circleTexture);
        GUI.color = old;
    }

    void DrawScreenCircle(Vector2 p, float r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
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
        return st;
    }
}