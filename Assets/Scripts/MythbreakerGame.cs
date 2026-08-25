using System;
using System.Collections.Generic;
using UnityEngine;

public class MythbreakerGame : MonoBehaviour
{
    public static MythbreakerGame I { get; private set; }

    enum GameState { MainMenu, Heroes, Playing, Upgrade, RoomClear, Victory, GameOver, Paused, Settings }
    enum UpgradeType { Multishot, RapidFire, DivinePower, Vitality, Hermes, Piercing }

    GameState state = GameState.MainMenu;
    GameState stateBeforePause = GameState.Playing;

    readonly List<MBEnemy> enemies = new List<MBEnemy>();
    readonly List<MBProjectile> projectiles = new List<MBProjectile>();

    Transform player;
    Camera cam;
    Texture2D menuTexture;

    float maxHp = 120f;
    float hp = 120f;
    float moveSpeed = 7f;
    float attackDamage = 28f;
    float attackCooldown = 0.62f;
    float nextAttack;
    float lastMovementTime;
    int multishot = 1;
    int pierce = 0;
    int xp = 0;
    int heroLevel = 1;
    int xpNeeded = 3;
    int room = 1;
    int runCoins = 0;
    int selectedHero = 0;

    Vector2 lastMouse;
    bool mouseWasDown;
    readonly UpgradeType[] choices = new UpgradeType[3];

    Material blue;
    Material gold;
    Material marble;
    Material enemyRed;
    Material enemyGreen;
    Material enemyBronze;
    Material projectileMat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<MythbreakerGame>() == null)
        {
            var go = new GameObject("MYTHBREAKER");
            DontDestroyOnLoad(go);
            go.AddComponent<MythbreakerGame>();
        }
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        menuTexture = Resources.Load<Texture2D>("mythbreaker_menu");
        CreateMaterials();
        BuildWorld();
        SetWorldVisible(false);
    }

    void CreateMaterials()
    {
        blue = Mat(new Color(0.03f, 0.18f, 0.48f));
        gold = Mat(new Color(0.92f, 0.62f, 0.10f));
        marble = Mat(new Color(0.78f, 0.78f, 0.72f));
        enemyRed = Mat(new Color(0.48f, 0.12f, 0.08f));
        enemyGreen = Mat(new Color(0.08f, 0.38f, 0.22f));
        enemyBronze = Mat(new Color(0.42f, 0.27f, 0.08f));
        projectileMat = Mat(new Color(0.25f, 0.72f, 1f));
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = c;
        return m;
    }

    void BuildWorld()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var c = new GameObject("Main Camera");
            c.tag = "MainCamera";
            cam = c.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 11.5f, -9.8f);
        cam.transform.LookAt(new Vector3(0, 0, 0.3f));
        cam.fieldOfView = 48f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.015f, 0.035f, 0.08f);

        if (FindFirstObjectByType<Light>() == null)
        {
            var l = new GameObject("Olympus Light").AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.35f;
            l.transform.rotation = Quaternion.Euler(48, -28, 0);
        }

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Arena Floor";
        floor.transform.localScale = new Vector3(1.05f, 1f, 1.45f);
        floor.GetComponent<Renderer>().material = marble;
        floor.tag = "Arena";

        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -5; z <= 6; z += 3)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = "Greek Column";
                col.transform.position = new Vector3(side * 4.7f, 1.2f, z);
                col.transform.localScale = new Vector3(0.42f, 1.2f, 0.42f);
                col.GetComponent<Renderer>().material = marble;
                Destroy(col.GetComponent<Collider>());
            }
        }

        var altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        altar.name = "Temple Altar";
        altar.transform.position = new Vector3(0, 0.3f, 6.2f);
        altar.transform.localScale = new Vector3(3.6f, 0.6f, 1.1f);
        altar.GetComponent<Renderer>().material = gold;
        Destroy(altar.GetComponent<Collider>());

        CreatePlayer();
    }

    void CreatePlayer()
    {
        var root = new GameObject("Perseus");
        player = root.transform;
        player.position = new Vector3(0, 0, -4.4f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Perseus Body";
        body.transform.SetParent(player);
        body.transform.localPosition = new Vector3(0, 0.75f, 0);
        body.transform.localScale = new Vector3(0.65f, 0.8f, 0.65f);
        body.GetComponent<Renderer>().material = blue;
        Destroy(body.GetComponent<Collider>());

        var crest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crest.name = "Divine Crest";
        crest.transform.SetParent(player);
        crest.transform.localPosition = new Vector3(0, 1.65f, 0.05f);
        crest.transform.localScale = Vector3.one * 0.42f;
        crest.GetComponent<Renderer>().material = gold;
        Destroy(crest.GetComponent<Collider>());

        var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weapon.name = "Mythbow";
        weapon.transform.SetParent(player);
        weapon.transform.localPosition = new Vector3(0.55f, 0.9f, 0.1f);
        weapon.transform.localRotation = Quaternion.Euler(0, 0, 35);
        weapon.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);
        weapon.GetComponent<Renderer>().material = gold;
        Destroy(weapon.GetComponent<Collider>());
    }

    void SetWorldVisible(bool visible)
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            r.enabled = visible;
        if (player != null) player.gameObject.SetActive(visible);
    }

    void Update()
    {
        if (state != GameState.Playing) return;
        HandleMovement();
        AutoAttack();
    }

    void HandleMovement()
    {
        Vector3 delta = Vector3.zero;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                Vector2 d = t.deltaPosition / Mathf.Max(480f, Screen.height);
                delta = new Vector3(d.x, 0, d.y) * moveSpeed * 16f;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 m = Input.mousePosition;
            if (mouseWasDown)
            {
                Vector2 d = (m - lastMouse) / Mathf.Max(480f, Screen.height);
                delta = new Vector3(d.x, 0, d.y) * moveSpeed * 16f;
            }
            lastMouse = m;
            mouseWasDown = true;
        }
        else mouseWasDown = false;

        if (delta.sqrMagnitude > 0.00001f)
        {
            player.position += delta;
            player.position = new Vector3(Mathf.Clamp(player.position.x, -4.0f, 4.0f), 0, Mathf.Clamp(player.position.z, -5.7f, 5.4f));
            lastMovementTime = Time.time;
        }
    }

    void AutoAttack()
    {
        if (Time.time < nextAttack || Time.time - lastMovementTime < 0.12f) return;
        MBEnemy target = NearestEnemy();
        if (target == null) return;
        nextAttack = Time.time + attackCooldown;

        Vector3 dir = (target.transform.position - player.position);
        dir.y = 0;
        dir.Normalize();

        float spread = 12f;
        for (int i = 0; i < multishot; i++)
        {
            float a = (i - (multishot - 1) * 0.5f) * spread;
            SpawnPlayerProjectile(Quaternion.Euler(0, a, 0) * dir);
        }
        player.rotation = Quaternion.LookRotation(dir);
    }

    void SpawnPlayerProjectile(Vector3 dir)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = "Divine Shot";
        o.transform.position = player.position + new Vector3(0, 0.75f, 0) + dir * 0.6f;
        o.transform.localScale = Vector3.one * 0.22f;
        o.GetComponent<Renderer>().material = projectileMat;
        Destroy(o.GetComponent<Collider>());
        var p = o.AddComponent<MBProjectile>();
        p.Setup(dir, 13f, attackDamage, true, pierce);
        projectiles.Add(p);
    }

    public void SpawnEnemyProjectile(Vector3 from, Vector3 dir, float damage)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = "Enemy Shot";
        o.transform.position = from;
        o.transform.localScale = Vector3.one * 0.28f;
        o.GetComponent<Renderer>().material = enemyRed;
        Destroy(o.GetComponent<Collider>());
        var p = o.AddComponent<MBProjectile>();
        p.Setup(dir, 7f, damage, false, 0);
        projectiles.Add(p);
    }

    public void ProjectileGone(MBProjectile p) => projectiles.Remove(p);

    MBEnemy NearestEnemy()
    {
        MBEnemy best = null;
        float dist = float.MaxValue;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null || !enemies[i].Alive) { enemies.RemoveAt(i); continue; }
            float d = (enemies[i].transform.position - player.position).sqrMagnitude;
            if (d < dist) { dist = d; best = enemies[i]; }
        }
        return best;
    }

    public Transform Player => player;
    public bool IsPlaying => state == GameState.Playing;

    public void DamagePlayer(float damage)
    {
        if (state != GameState.Playing) return;
        hp -= damage;
        if (hp <= 0)
        {
            hp = 0;
            state = GameState.GameOver;
        }
    }

    public void EnemyKilled(MBEnemy e, int reward)
    {
        enemies.Remove(e);
        runCoins += reward;
        xp++;
        if (xp >= xpNeeded && state == GameState.Playing)
        {
            xp -= xpNeeded;
            heroLevel++;
            xpNeeded = 2 + heroLevel;
            RollUpgradeChoices();
            state = GameState.Upgrade;
            Time.timeScale = 0f;
            return;
        }
        CheckRoomClear();
    }

    public void ProjectileHitEnemy(MBProjectile shot)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null || !e.Alive) continue;
            if ((e.transform.position - shot.transform.position).sqrMagnitude < 0.62f)
            {
                e.TakeDamage(shot.Damage);
                shot.OnHit();
                return;
            }
        }
    }

    void CheckRoomClear()
    {
        for (int i = 0; i < enemies.Count; i++) if (enemies[i] != null && enemies[i].Alive) return;
        if (state != GameState.Playing) return;
        state = room >= 3 ? GameState.Victory : GameState.RoomClear;
    }

    void StartRun()
    {
        ClearCombatObjects();
        maxHp = 120f; hp = maxHp; moveSpeed = 7f; attackDamage = 28f; attackCooldown = 0.62f;
        multishot = 1; pierce = 0; xp = 0; heroLevel = 1; xpNeeded = 3; room = 1; runCoins = 0;
        player.position = new Vector3(0, 0, -4.4f);
        SetWorldVisible(true);
        state = GameState.Playing;
        Time.timeScale = 1f;
        SpawnRoom();
    }

    void ClearCombatObjects()
    {
        foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
        foreach (var p in projectiles) if (p != null) Destroy(p.gameObject);
        enemies.Clear(); projectiles.Clear();
    }

    void SpawnRoom()
    {
        ClearCombatObjects();
        player.position = new Vector3(0, 0, -4.4f);

        if (room == 1)
        {
            SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(-2.2f, 0, 2.0f));
            SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(2.2f, 0, 2.4f));
            SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(0, 0, 4.3f));
        }
        else if (room == 2)
        {
            SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(-2.7f, 0, 1.0f));
            SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(2.7f, 0, 1.0f));
            SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(-2.2f, 0, 4.5f));
            SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(2.2f, 0, 4.5f));
        }
        else
        {
            SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(-2.5f, 0, 2.4f));
            SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(2.5f, 0, 2.4f));
            SpawnEnemy(MBEnemy.Kind.Cyclops, new Vector3(0, 0, 4.3f));
        }
    }

    void SpawnEnemy(MBEnemy.Kind kind, Vector3 pos)
    {
        PrimitiveType primitive = kind == MBEnemy.Kind.Serpent ? PrimitiveType.Cylinder : PrimitiveType.Capsule;
        var o = GameObject.CreatePrimitive(primitive);
        o.name = kind.ToString();
        o.transform.position = pos + Vector3.up * (kind == MBEnemy.Kind.Cyclops ? 1.05f : 0.65f);
        o.transform.localScale = kind == MBEnemy.Kind.Cyclops ? new Vector3(1.35f, 1.35f, 1.35f) : new Vector3(0.8f, 0.8f, 0.8f);
        o.GetComponent<Renderer>().material = kind == MBEnemy.Kind.Serpent ? enemyGreen : (kind == MBEnemy.Kind.Hoplite ? enemyBronze : enemyRed);
        Destroy(o.GetComponent<Collider>());
        var e = o.AddComponent<MBEnemy>();
        e.Setup(kind);
        enemies.Add(e);
    }

    void RollUpgradeChoices()
    {
        var pool = new List<UpgradeType>((UpgradeType[])Enum.GetValues(typeof(UpgradeType)));
        for (int i = 0; i < 3; i++)
        {
            int r = UnityEngine.Random.Range(0, pool.Count);
            choices[i] = pool[r];
            pool.RemoveAt(r);
        }
    }

    void ApplyUpgrade(UpgradeType u)
    {
        switch (u)
        {
            case UpgradeType.Multishot: multishot = Mathf.Min(5, multishot + 1); break;
            case UpgradeType.RapidFire: attackCooldown = Mathf.Max(0.24f, attackCooldown * 0.84f); break;
            case UpgradeType.DivinePower: attackDamage += 11f; break;
            case UpgradeType.Vitality: maxHp += 30f; hp = Mathf.Min(maxHp, hp + 30f); break;
            case UpgradeType.Hermes: moveSpeed += 0.8f; break;
            case UpgradeType.Piercing: pierce = Mathf.Min(3, pierce + 1); break;
        }
        Time.timeScale = 1f;
        state = GameState.Playing;
        CheckRoomClear();
    }

    string UpgradeName(UpgradeType u)
    {
        switch (u)
        {
            case UpgradeType.Multishot: return "MULTISHOT\n+1 colpo divino";
            case UpgradeType.RapidFire: return "ARES\nAttacco +16%";
            case UpgradeType.DivinePower: return "ZEUS\n+11 danni";
            case UpgradeType.Vitality: return "AMBROSIA\n+30 vita";
            case UpgradeType.Hermes: return "HERMES\nPiù velocità";
            default: return "LANCIA SACRA\nPerfora nemici";
        }
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;
        float s = Mathf.Clamp(w / 720f, 0.7f, 1.8f);
        GUI.skin.button.fontSize = Mathf.RoundToInt(28 * s);
        GUI.skin.label.fontSize = Mathf.RoundToInt(25 * s);

        if (state == GameState.MainMenu || state == GameState.Heroes || state == GameState.Settings)
        {
            DrawMenuBackground(w, h);
            if (state == GameState.MainMenu) DrawMainMenu(w, h, s);
            else if (state == GameState.Heroes) DrawHeroes(w, h, s);
            else DrawSettings(w, h, s);
            return;
        }

        DrawHud(w, h, s);

        if (state == GameState.Upgrade) DrawUpgrade(w, h, s);
        else if (state == GameState.RoomClear) DrawRoomClear(w, h, s);
        else if (state == GameState.Victory) DrawVictory(w, h, s);
        else if (state == GameState.GameOver) DrawGameOver(w, h, s);
        else if (state == GameState.Paused) DrawPause(w, h, s);
    }

    void DrawMenuBackground(int w, int h)
    {
        GUI.color = Color.white;
        if (menuTexture != null) GUI.DrawTexture(new Rect(0, 0, w, h), menuTexture, ScaleMode.ScaleAndCrop);
        else
        {
            GUI.color = new Color(0.02f, 0.06f, 0.18f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        }
        GUI.color = new Color(0, 0, 0, 0.32f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    GUIStyle CenterStyle(int size, FontStyle style = FontStyle.Bold)
    {
        return new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = size, fontStyle = style, normal = { textColor = Color.white } };
    }

    void DrawMainMenu(int w, int h, float s)
    {
        GUI.Label(new Rect(0, h * 0.05f, w, 60 * s), "lello's game", CenterStyle(Mathf.RoundToInt(31 * s)));
        GUI.Label(new Rect(0, h * 0.18f, w, 100 * s), "MYTHBREAKER", CenterStyle(Mathf.RoundToInt(55 * s)));
        float bw = w * 0.72f, bh = 72 * s, x = (w - bw) / 2f;
        if (GUI.Button(new Rect(x, h * 0.61f, bw, bh), "NUOVA PARTITA")) { state = GameState.Heroes; }
        if (GUI.Button(new Rect(x, h * 0.70f, bw, bh), "CONTINUA  •  PROSSIMAMENTE")) { }
        if (GUI.Button(new Rect(x, h * 0.79f, bw * 0.48f, bh), "EROI")) state = GameState.Heroes;
        if (GUI.Button(new Rect(x + bw * 0.52f, h * 0.79f, bw * 0.48f, bh), "IMPOSTAZIONI")) state = GameState.Settings;
        GUI.Label(new Rect(0, h - 50 * s, w, 38 * s), "Build 0.1 • Grecia", CenterStyle(Mathf.RoundToInt(18 * s), FontStyle.Normal));
    }

    void DrawHeroes(int w, int h, float s)
    {
        GUI.Label(new Rect(0, h * 0.08f, w, 70 * s), "SCEGLI IL TUO EROE", CenterStyle(Mathf.RoundToInt(40 * s)));
        float x = w * 0.10f, bw = w * 0.80f, bh = 88 * s;
        if (GUI.Button(new Rect(x, h * 0.27f, bw, bh), "PERSEO  •  DISPONIBILE\nMythbow • Critico e precisione")) selectedHero = 0;
        GUI.enabled = false;
        GUI.Button(new Rect(x, h * 0.40f, bw, bh), "ERACLE  •  BLOCCATO\nForza • Colpi ad area");
        GUI.Button(new Rect(x, h * 0.53f, bw, bh), "ATALANTA  •  BLOCCATA\nVelocità • Frecce multiple");
        GUI.Button(new Rect(x, h * 0.66f, bw, bh), "ACHILLE  •  BLOCCATO\nLancia • Assalto");
        GUI.enabled = true;
        if (GUI.Button(new Rect(x, h * 0.82f, bw * 0.58f, bh * 0.9f), "GIOCA CON PERSEO")) StartRun();
        if (GUI.Button(new Rect(x + bw * 0.62f, h * 0.82f, bw * 0.38f, bh * 0.9f), "INDIETRO")) state = GameState.MainMenu;
    }

    void DrawSettings(int w, int h, float s)
    {
        GUI.Label(new Rect(0, h * 0.12f, w, 70 * s), "IMPOSTAZIONI", CenterStyle(Mathf.RoundToInt(40 * s)));
        GUI.Label(new Rect(w * 0.12f, h * 0.30f, w * 0.76f, 180 * s), "• 60 FPS target\n• Schermo verticale\n• Controllo a un dito\n• Audio: in arrivo nella 0.2", CenterStyle(Mathf.RoundToInt(24 * s), FontStyle.Normal));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.75f, w * 0.64f, 75 * s), "INDIETRO")) state = GameState.MainMenu;
    }

    void DrawHud(int w, int h, float s)
    {
        GUI.color = new Color(0, 0, 0, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, w, 92 * s), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(18 * s, 8 * s, w * 0.40f, 35 * s), "PERSEO  Lv." + heroLevel, CenterStyle(Mathf.RoundToInt(20 * s)));
        GUI.Label(new Rect(w * 0.38f, 8 * s, w * 0.35f, 35 * s), "ATTICA  •  " + room + "/3", CenterStyle(Mathf.RoundToInt(20 * s)));
        GUI.Label(new Rect(w * 0.72f, 8 * s, w * 0.24f, 35 * s), "🪙 " + runCoins, CenterStyle(Mathf.RoundToInt(20 * s)));

        GUI.color = new Color(0.18f, 0.18f, 0.18f);
        GUI.DrawTexture(new Rect(w * 0.08f, 52 * s, w * 0.62f, 18 * s), Texture2D.whiteTexture);
        GUI.color = new Color(0.18f, 0.86f, 0.26f);
        GUI.DrawTexture(new Rect(w * 0.08f, 52 * s, w * 0.62f * (hp / maxHp), 18 * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(w * 0.08f, 45 * s, w * 0.62f, 32 * s), Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(maxHp), CenterStyle(Mathf.RoundToInt(17 * s)));

        if (state == GameState.Playing && GUI.Button(new Rect(w - 74 * s, 42 * s, 56 * s, 42 * s), "Ⅱ"))
        {
            stateBeforePause = state;
            state = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    void DrawUpgrade(int w, int h, float s)
    {
        GUI.color = new Color(0, 0, 0, 0.80f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.22f, w, 70 * s), "POTERE DEGLI DEI", CenterStyle(Mathf.RoundToInt(40 * s)));
        float gap = w * 0.025f, cardW = (w - gap * 4) / 3f, y = h * 0.40f, ch = 210 * s;
        for (int i = 0; i < 3; i++) if (GUI.Button(new Rect(gap + i * (cardW + gap), y, cardW, ch), UpgradeName(choices[i]))) ApplyUpgrade(choices[i]);
    }

    void DrawRoomClear(int w, int h, float s)
    {
        GUI.color = new Color(0, 0, 0, 0.72f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.32f, w, 80 * s), "STANZA LIBERATA", CenterStyle(Mathf.RoundToInt(42 * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.52f, w * 0.64f, 82 * s), "ENTRA NELLA STANZA " + (room + 1))) { room++; state = GameState.Playing; SpawnRoom(); }
    }

    void DrawVictory(int w, int h, float s)
    {
        GUI.color = new Color(0, 0, 0, 0.78f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.26f, w, 90 * s), "VITTORIA", CenterStyle(Mathf.RoundToInt(50 * s)));
        GUI.Label(new Rect(w * 0.1f, h * 0.38f, w * 0.8f, 100 * s), "Livello 1 completato\nRovine dell'Attica", CenterStyle(Mathf.RoundToInt(28 * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.58f, w * 0.64f, 82 * s), "TORNA AL TEMPIO")) ReturnToMenu();
    }

    void DrawGameOver(int w, int h, float s)
    {
        GUI.color = new Color(0, 0, 0, 0.80f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.28f, w, 90 * s), "PERSEO È CADUTO", CenterStyle(Mathf.RoundToInt(44 * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.49f, w * 0.64f, 82 * s), "RIPROVA")) StartRun();
        if (GUI.Button(new Rect(w * 0.18f, h * 0.61f, w * 0.64f, 82 * s), "MENU")) ReturnToMenu();
    }

    void DrawPause(int w, int h, float s)
    {
        GUI.color = new Color(0, 0, 0, 0.82f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.30f, w, 80 * s), "PAUSA", CenterStyle(Mathf.RoundToInt(48 * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.48f, w * 0.64f, 82 * s), "CONTINUA")) { Time.timeScale = 1f; state = stateBeforePause; }
        if (GUI.Button(new Rect(w * 0.18f, h * 0.60f, w * 0.64f, 82 * s), "ESCI AL MENU")) ReturnToMenu();
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        ClearCombatObjects();
        SetWorldVisible(false);
        state = GameState.MainMenu;
    }
}

public class MBEnemy : MonoBehaviour
{
    public enum Kind { Satyr, Serpent, Hoplite, Cyclops }
    public bool Alive { get; private set; } = true;
    Kind kind;
    float hp;
    float speed;
    float contactDamage;
    float nextHit;
    float nextShot;
    int reward;

    public void Setup(Kind k)
    {
        kind = k;
        switch (k)
        {
            case Kind.Satyr: hp = 52; speed = 2.4f; contactDamage = 13; reward = 2; break;
            case Kind.Serpent: hp = 40; speed = 1.35f; contactDamage = 9; reward = 2; break;
            case Kind.Hoplite: hp = 78; speed = 1.55f; contactDamage = 18; reward = 3; break;
            default: hp = 240; speed = 1.18f; contactDamage = 28; reward = 10; break;
        }
    }

    void Update()
    {
        if (!Alive || MythbreakerGame.I == null || !MythbreakerGame.I.IsPlaying || MythbreakerGame.I.Player == null) return;
        Transform p = MythbreakerGame.I.Player;
        Vector3 d = p.position - transform.position; d.y = 0;
        float dist = d.magnitude;
        if (dist > 0.01f) d /= dist;

        if (kind == Kind.Serpent)
        {
            if (dist > 3.8f) transform.position += d * speed * Time.deltaTime;
            if (Time.time >= nextShot && dist < 7.5f)
            {
                nextShot = Time.time + 1.75f;
                MythbreakerGame.I.SpawnEnemyProjectile(transform.position + Vector3.up * 0.4f, d, 11f);
            }
        }
        else
        {
            if (dist > 0.75f) transform.position += d * speed * Time.deltaTime;
            else if (Time.time >= nextHit)
            {
                nextHit = Time.time + (kind == Kind.Cyclops ? 1.25f : 0.85f);
                MythbreakerGame.I.DamagePlayer(contactDamage);
            }
        }
        if (d.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(d);
    }

    public void TakeDamage(float damage)
    {
        if (!Alive) return;
        hp -= damage;
        transform.localScale *= 0.97f;
        if (hp <= 0)
        {
            Alive = false;
            MythbreakerGame.I.EnemyKilled(this, reward);
            Destroy(gameObject);
        }
    }
}

public class MBProjectile : MonoBehaviour
{
    Vector3 dir;
    float speed;
    float life = 4f;
    bool friendly;
    int remainingPierce;
    public float Damage { get; private set; }

    public void Setup(Vector3 direction, float projectileSpeed, float damage, bool isFriendly, int pierce)
    {
        dir = direction.normalized;
        speed = projectileSpeed;
        Damage = damage;
        friendly = isFriendly;
        remainingPierce = pierce;
    }

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0) { Remove(); return; }

        if (friendly) MythbreakerGame.I.ProjectileHitEnemy(this);
        else if (MythbreakerGame.I.Player != null && (MythbreakerGame.I.Player.position - transform.position).sqrMagnitude < 0.42f)
        {
            MythbreakerGame.I.DamagePlayer(Damage);
            Remove();
        }
    }

    public void OnHit()
    {
        if (remainingPierce > 0) remainingPierce--;
        else Remove();
    }

    void Remove()
    {
        if (MythbreakerGame.I != null) MythbreakerGame.I.ProjectileGone(this);
        Destroy(gameObject);
    }
}
