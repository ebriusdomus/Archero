using System;
using System.Collections.Generic;
using UnityEngine;

public class MythbreakerGame : MonoBehaviour
{
    public static MythbreakerGame I { get; private set; }

    enum GameState { MainMenu, Heroes, Playing, Upgrade, RoomClear, Victory, GameOver, Paused, Settings }
    enum UpgradeType
    {
        Multishot,
        RapidFire,
        DivinePower,
        Vitality,
        Hermes,
        Piercing,
        ZeusChain,
        AthenaShield,
        ArtemisCrit,
        PoseidonForce
    }

    const int RoomTotal = 5;
    const int DemoLevel = 1;

    GameState state = GameState.MainMenu;
    GameState stateBeforePause = GameState.Playing;

    readonly List<MBEnemy> enemies = new List<MBEnemy>();
    readonly List<MBProjectile> projectiles = new List<MBProjectile>();

    Transform player;
    Camera cam;
    Texture2D menuTexture;

    float maxHp;
    float hp;
    float moveSpeed;
    float attackDamage;
    float attackCooldown;
    float nextAttack;
    float lastMovementTime;
    float invulnerableUntil;
    float damageFlashUntil;
    float cameraShakeUntil;
    float cameraShakePower;

    int multishot;
    int pierce;
    int chainLightning;
    int shieldCharges;
    int xp;
    int heroLevel;
    int xpNeeded;
    int room;
    int runCoins;
    int totalCoins;
    int highestLevel;

    float critChance;
    float critMultiplier;
    float knockbackPower;

    HeroCatalog.HeroId selectedHero = HeroCatalog.HeroId.Perseus;
    readonly UpgradeType[] choices = new UpgradeType[3];

    bool pointerHeld;
    Vector2 pointerStart;
    Vector2 pointerNow;
    Vector2 moveInput;

    Material blue;
    Material darkBlue;
    Material gold;
    Material marble;
    Material enemyRed;
    Material enemyGreen;
    Material enemyBronze;
    Material enemyPurple;
    Material projectileMat;
    Material critProjectileMat;

    Vector3 baseCameraPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<MythbreakerGame>() == null)
        {
            GameObject go = new GameObject("MYTHBREAKER");
            DontDestroyOnLoad(go);
            go.AddComponent<MythbreakerGame>();
        }
    }

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;

        totalCoins = PlayerPrefs.GetInt("mythbreaker.totalCoins", 0);
        highestLevel = Mathf.Max(1, PlayerPrefs.GetInt("mythbreaker.highestLevel", 1));
        menuTexture = Resources.Load<Texture2D>("mythbreaker_menu");

        CreateMaterials();
        BuildWorld();
        SetWorldVisible(false);
    }

    void CreateMaterials()
    {
        blue = Mat(new Color(0.025f, 0.16f, 0.48f));
        darkBlue = Mat(new Color(0.015f, 0.07f, 0.20f));
        gold = Mat(new Color(0.95f, 0.61f, 0.08f));
        marble = Mat(new Color(0.78f, 0.79f, 0.74f));
        enemyRed = Mat(new Color(0.50f, 0.10f, 0.06f));
        enemyGreen = Mat(new Color(0.05f, 0.40f, 0.20f));
        enemyBronze = Mat(new Color(0.46f, 0.28f, 0.07f));
        enemyPurple = Mat(new Color(0.32f, 0.10f, 0.48f));
        projectileMat = Mat(new Color(0.20f, 0.72f, 1.00f));
        critProjectileMat = Mat(new Color(0.95f, 0.68f, 0.10f));
    }

    Material Mat(Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = c;
        return m;
    }

    void BuildWorld()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject c = new GameObject("Main Camera");
            c.tag = "MainCamera";
            cam = c.AddComponent<Camera>();
        }

        baseCameraPosition = new Vector3(0f, 11.7f, -10.2f);
        cam.transform.position = baseCameraPosition;
        cam.transform.LookAt(new Vector3(0f, 0f, 0.4f));
        cam.fieldOfView = 47f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.025f, 0.07f);

        if (FindFirstObjectByType<Light>() == null)
        {
            Light l = new GameObject("Olympus Light").AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.35f;
            l.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Arena Floor";
        floor.transform.localScale = new Vector3(1.05f, 1f, 1.45f);
        floor.GetComponent<Renderer>().material = marble;
        floor.tag = "Arena";

        BuildGreekDecor();
        CreatePlayer();
    }

    void BuildGreekDecor()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -5; z <= 7; z += 3)
            {
                GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = "Greek Column";
                col.transform.position = new Vector3(side * 4.7f, 1.2f, z);
                col.transform.localScale = new Vector3(0.42f, 1.2f, 0.42f);
                col.GetComponent<Renderer>().material = marble;
                Destroy(col.GetComponent<Collider>());

                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Olympic Flame";
                flame.transform.position = new Vector3(side * 4.65f, 2.55f, z);
                flame.transform.localScale = Vector3.one * 0.18f;
                flame.GetComponent<Renderer>().material = gold;
                Destroy(flame.GetComponent<Collider>());
            }
        }

        GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        altar.name = "Temple Altar";
        altar.transform.position = new Vector3(0f, 0.3f, 6.25f);
        altar.transform.localScale = new Vector3(3.8f, 0.6f, 1.1f);
        altar.GetComponent<Renderer>().material = gold;
        Destroy(altar.GetComponent<Collider>());

        GameObject emblem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        emblem.name = "Olympus Emblem";
        emblem.transform.position = new Vector3(0f, 0.05f, 0.6f);
        emblem.transform.localScale = new Vector3(1.8f, 0.03f, 1.8f);
        emblem.GetComponent<Renderer>().material = darkBlue;
        Destroy(emblem.GetComponent<Collider>());
    }

    void CreatePlayer()
    {
        GameObject root = new GameObject("Perseus");
        player = root.transform;
        player.position = new Vector3(0f, 0f, -4.4f);

        GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        legs.name = "Hero Legs";
        legs.transform.SetParent(player);
        legs.transform.localPosition = new Vector3(0f, 0.37f, 0f);
        legs.transform.localScale = new Vector3(0.44f, 0.36f, 0.44f);
        legs.GetComponent<Renderer>().material = darkBlue;
        Destroy(legs.GetComponent<Collider>());

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Hero Body";
        body.transform.SetParent(player);
        body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        body.transform.localScale = new Vector3(0.62f, 0.72f, 0.62f);
        body.GetComponent<Renderer>().material = blue;
        Destroy(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Hero Head";
        head.transform.SetParent(player);
        head.transform.localPosition = new Vector3(0f, 1.62f, 0.02f);
        head.transform.localScale = Vector3.one * 0.42f;
        head.GetComponent<Renderer>().material = gold;
        Destroy(head.GetComponent<Collider>());

        GameObject cape = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cape.name = "Blue Cape";
        cape.transform.SetParent(player);
        cape.transform.localPosition = new Vector3(0f, 0.90f, -0.30f);
        cape.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        cape.transform.localScale = new Vector3(0.62f, 0.72f, 0.07f);
        cape.GetComponent<Renderer>().material = blue;
        Destroy(cape.GetComponent<Collider>());

        GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weapon.name = "Mythbow";
        weapon.transform.SetParent(player);
        weapon.transform.localPosition = new Vector3(0.55f, 0.98f, 0.12f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
        weapon.transform.localScale = new Vector3(0.11f, 1.0f, 0.11f);
        weapon.GetComponent<Renderer>().material = gold;
        Destroy(weapon.GetComponent<Collider>());
    }

    void SetWorldVisible(bool visible)
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = visible;
        if (player != null) player.gameObject.SetActive(visible);
    }

    void Update()
    {
        UpdateCamera();
        if (state != GameState.Playing) return;
        HandleMovement();
        AutoAttack();
    }

    void UpdateCamera()
    {
        if (cam == null) return;
        Vector3 desired = baseCameraPosition;
        if (player != null && state == GameState.Playing)
        {
            desired.x += player.position.x * 0.08f;
            desired.z += player.position.z * 0.04f;
        }

        if (Time.time < cameraShakeUntil)
        {
            desired += new Vector3(UnityEngine.Random.Range(-cameraShakePower, cameraShakePower),
                UnityEngine.Random.Range(-cameraShakePower, cameraShakePower) * 0.45f,
                UnityEngine.Random.Range(-cameraShakePower, cameraShakePower));
        }

        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, Time.unscaledDeltaTime * 8f);
        cam.transform.LookAt(new Vector3(0f, 0f, 0.4f));
    }

    void HandleMovement()
    {
        bool hasPointer = false;
        bool pointerEnded = false;
        Vector2 screenPosition = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            screenPosition = t.position;
            hasPointer = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
            pointerEnded = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

            if (t.phase == TouchPhase.Began)
            {
                pointerHeld = true;
                pointerStart = t.position;
                pointerNow = t.position;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                pointerHeld = true;
                pointerStart = Input.mousePosition;
                pointerNow = pointerStart;
            }

            if (Input.GetMouseButton(0))
            {
                hasPointer = true;
                screenPosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0)) pointerEnded = true;
        }

        if (pointerHeld && hasPointer)
        {
            pointerNow = screenPosition;
            float radius = Mathf.Max(75f, Screen.width * 0.13f);
            Vector2 delta = Vector2.ClampMagnitude(pointerNow - pointerStart, radius);
            moveInput = delta / radius;

            if (moveInput.sqrMagnitude > 0.015f)
            {
                Vector3 worldMove = new Vector3(moveInput.x, 0f, moveInput.y);
                player.position += worldMove * moveSpeed * Time.deltaTime;
                player.position = new Vector3(
                    Mathf.Clamp(player.position.x, -4.0f, 4.0f),
                    0f,
                    Mathf.Clamp(player.position.z, -5.65f, 5.25f));
                lastMovementTime = Time.time;

                if (worldMove.sqrMagnitude > 0.02f)
                    player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(worldMove), Time.deltaTime * 12f);
            }
        }
        else moveInput = Vector2.zero;

        if (pointerEnded)
        {
            pointerHeld = false;
            moveInput = Vector2.zero;
        }
    }

    void AutoAttack()
    {
        if (Time.time < nextAttack) return;
        if (Time.time - lastMovementTime < 0.09f) return;
        if (moveInput.sqrMagnitude > 0.015f) return;

        MBEnemy target = NearestEnemy();
        if (target == null) return;

        nextAttack = Time.time + attackCooldown;
        Vector3 dir = target.transform.position - player.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        float spread = 11f;
        for (int i = 0; i < multishot; i++)
        {
            float angle = (i - (multishot - 1) * 0.5f) * spread;
            bool critical = UnityEngine.Random.value < critChance;
            SpawnPlayerProjectile(Quaternion.Euler(0f, angle, 0f) * dir, critical);
        }

        player.rotation = Quaternion.LookRotation(dir);
    }

    void SpawnPlayerProjectile(Vector3 dir, bool critical)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = critical ? "Critical Divine Shot" : "Divine Shot";
        o.transform.position = player.position + new Vector3(0f, 0.78f, 0f) + dir * 0.62f;
        o.transform.localScale = Vector3.one * (critical ? 0.27f : 0.21f);
        o.GetComponent<Renderer>().material = critical ? critProjectileMat : projectileMat;
        Destroy(o.GetComponent<Collider>());

        MBProjectile p = o.AddComponent<MBProjectile>();
        float damage = attackDamage * (critical ? critMultiplier : 1f);
        p.Setup(dir, 13.5f, damage, true, pierce, knockbackPower);
        projectiles.Add(p);
    }

    public void SpawnEnemyProjectile(Vector3 from, Vector3 dir, float damage, float speed = 7f)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = "Enemy Shot";
        o.transform.position = from;
        o.transform.localScale = Vector3.one * 0.26f;
        o.GetComponent<Renderer>().material = enemyRed;
        Destroy(o.GetComponent<Collider>());

        MBProjectile p = o.AddComponent<MBProjectile>();
        p.Setup(dir, speed, damage, false, 0, 0f);
        projectiles.Add(p);
    }

    public void SpawnRadialEnemyShots(Vector3 from, int count, float damage, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector3 d = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            SpawnEnemyProjectile(from, d, damage, speed);
        }
    }

    public void ProjectileGone(MBProjectile p)
    {
        projectiles.Remove(p);
    }

    MBEnemy NearestEnemy()
    {
        MBEnemy best = null;
        float dist = float.MaxValue;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            MBEnemy e = enemies[i];
            if (e == null || !e.Alive)
            {
                enemies.RemoveAt(i);
                continue;
            }

            float d = (e.transform.position - player.position).sqrMagnitude;
            if (d < dist)
            {
                dist = d;
                best = e;
            }
        }
        return best;
    }

    MBEnemy NearestOtherEnemy(MBEnemy origin)
    {
        MBEnemy best = null;
        float dist = float.MaxValue;
        for (int i = 0; i < enemies.Count; i++)
        {
            MBEnemy e = enemies[i];
            if (e == null || !e.Alive || e == origin) continue;
            float d = (e.transform.position - origin.transform.position).sqrMagnitude;
            if (d < dist)
            {
                dist = d;
                best = e;
            }
        }
        return best;
    }

    public Transform Player => player;
    public bool IsPlaying => state == GameState.Playing;

    public void DamagePlayer(float damage)
    {
        if (state != GameState.Playing) return;
        if (Time.time < invulnerableUntil) return;

        if (shieldCharges > 0)
        {
            shieldCharges--;
            invulnerableUntil = Time.time + 0.45f;
            damageFlashUntil = Time.time + 0.12f;
            ShakeCamera(0.08f, 0.14f);
            return;
        }

        hp -= damage;
        invulnerableUntil = Time.time + 0.52f;
        damageFlashUntil = Time.time + 0.16f;
        ShakeCamera(0.14f, 0.18f);

        if (hp <= 0f)
        {
            hp = 0f;
            state = GameState.GameOver;
        }
    }

    void ShakeCamera(float power, float duration)
    {
        cameraShakePower = Mathf.Max(cameraShakePower, power);
        cameraShakeUntil = Time.time + duration;
    }

    public void EnemyKilled(MBEnemy e, int coinReward, int xpReward)
    {
        enemies.Remove(e);
        runCoins += coinReward;
        xp += xpReward;

        if (xp >= xpNeeded && state == GameState.Playing)
        {
            xp -= xpNeeded;
            heroLevel++;
            xpNeeded = 3 + heroLevel * 2;
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
            MBEnemy e = enemies[i];
            if (e == null || !e.Alive) continue;

            float radius = e.IsBoss ? 1.15f : 0.68f;
            if ((e.transform.position - shot.transform.position).sqrMagnitude < radius * radius)
            {
                e.TakeDamage(shot.Damage, shot.KnockbackDirection * shot.Knockback);

                if (chainLightning > 0)
                {
                    MBEnemy chained = NearestOtherEnemy(e);
                    if (chained != null && (chained.transform.position - e.transform.position).sqrMagnitude < 13f)
                    {
                        float chainDamage = attackDamage * (0.28f + 0.10f * chainLightning);
                        chained.TakeDamage(chainDamage, Vector3.zero);
                    }
                }

                shot.OnHit();
                return;
            }
        }
    }

    void CheckRoomClear()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null && enemies[i].Alive) return;

        if (state != GameState.Playing) return;
        state = room >= RoomTotal ? GameState.Victory : GameState.RoomClear;

        if (state == GameState.Victory)
        {
            totalCoins += runCoins;
            highestLevel = Mathf.Max(highestLevel, DemoLevel + 1);
            PlayerPrefs.SetInt("mythbreaker.totalCoins", totalCoins);
            PlayerPrefs.SetInt("mythbreaker.highestLevel", highestLevel);
            PlayerPrefs.Save();
        }
    }

    void StartRun()
    {
        ClearCombatObjects();

        HeroCatalog.Hero hero = HeroCatalog.Get(selectedHero);
        maxHp = hero.hp;
        hp = maxHp;
        moveSpeed = hero.moveSpeed;
        attackDamage = hero.damage;
        attackCooldown = hero.attackCooldown;

        multishot = 1;
        pierce = 0;
        chainLightning = 0;
        shieldCharges = 0;
        critChance = 0.08f;
        critMultiplier = 1.75f;
        knockbackPower = 0f;
        xp = 0;
        heroLevel = 1;
        xpNeeded = 5;
        room = 1;
        runCoins = 0;
        nextAttack = 0f;
        lastMovementTime = -10f;
        invulnerableUntil = 0f;

        player.gameObject.name = hero.displayName;
        player.position = new Vector3(0f, 0f, -4.4f);
        SetWorldVisible(true);
        state = GameState.Playing;
        Time.timeScale = 1f;
        SpawnRoom();
    }

    void ClearCombatObjects()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null) Destroy(enemies[i].gameObject);
        for (int i = 0; i < projectiles.Count; i++)
            if (projectiles[i] != null) Destroy(projectiles[i].gameObject);
        enemies.Clear();
        projectiles.Clear();
    }

    void SpawnRoom()
    {
        ClearCombatObjects();
        pointerHeld = false;
        moveInput = Vector2.zero;
        player.position = new Vector3(0f, 0f, -4.4f);

        switch (room)
        {
            case 1:
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(-2.2f, 0f, 2.0f));
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(2.2f, 0f, 2.4f));
                SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(0f, 0f, 4.3f));
                break;

            case 2:
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(-2.8f, 0f, 0.8f));
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(2.8f, 0f, 0.8f));
                SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(-2.2f, 0f, 4.3f));
                SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(2.2f, 0f, 4.3f));
                break;

            case 3:
                SpawnEnemy(MBEnemy.Kind.Harpy, new Vector3(-2.8f, 0f, 2.7f));
                SpawnEnemy(MBEnemy.Kind.Harpy, new Vector3(2.8f, 0f, 2.7f));
                SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(0f, 0f, 4.6f));
                SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(0f, 0f, 1.6f));
                break;

            case 4:
                SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(-2.6f, 0f, 2.1f));
                SpawnEnemy(MBEnemy.Kind.Hoplite, new Vector3(2.6f, 0f, 2.1f));
                SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(-2.4f, 0f, 4.7f));
                SpawnEnemy(MBEnemy.Kind.Serpent, new Vector3(2.4f, 0f, 4.7f));
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(0f, 0f, 3.4f));
                break;

            default:
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(-3.0f, 0f, 2.0f));
                SpawnEnemy(MBEnemy.Kind.Satyr, new Vector3(3.0f, 0f, 2.0f));
                SpawnEnemy(MBEnemy.Kind.Cyclops, new Vector3(0f, 0f, 4.2f));
                break;
        }
    }

    void SpawnEnemy(MBEnemy.Kind kind, Vector3 pos)
    {
        PrimitiveType primitive = PrimitiveType.Capsule;
        if (kind == MBEnemy.Kind.Serpent) primitive = PrimitiveType.Cylinder;
        if (kind == MBEnemy.Kind.Harpy) primitive = PrimitiveType.Sphere;

        GameObject o = GameObject.CreatePrimitive(primitive);
        o.name = kind.ToString();
        bool boss = kind == MBEnemy.Kind.Cyclops;
        o.transform.position = pos + Vector3.up * (boss ? 1.05f : 0.65f);
        o.transform.localScale = boss ? new Vector3(1.55f, 1.55f, 1.55f) : new Vector3(0.82f, 0.82f, 0.82f);

        Renderer renderer = o.GetComponent<Renderer>();
        if (kind == MBEnemy.Kind.Serpent) renderer.material = enemyGreen;
        else if (kind == MBEnemy.Kind.Hoplite) renderer.material = enemyBronze;
        else if (kind == MBEnemy.Kind.Harpy) renderer.material = enemyPurple;
        else renderer.material = enemyRed;

        Destroy(o.GetComponent<Collider>());
        MBEnemy e = o.AddComponent<MBEnemy>();
        e.Setup(kind);
        enemies.Add(e);
    }

    void RollUpgradeChoices()
    {
        List<UpgradeType> pool = new List<UpgradeType>((UpgradeType[])Enum.GetValues(typeof(UpgradeType)));
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
            case UpgradeType.Multishot:
                multishot = Mathf.Min(5, multishot + 1);
                break;
            case UpgradeType.RapidFire:
                attackCooldown = Mathf.Max(0.24f, attackCooldown * 0.84f);
                break;
            case UpgradeType.DivinePower:
                attackDamage += 11f;
                break;
            case UpgradeType.Vitality:
                maxHp += 30f;
                hp = Mathf.Min(maxHp, hp + 30f);
                break;
            case UpgradeType.Hermes:
                moveSpeed += 0.75f;
                break;
            case UpgradeType.Piercing:
                pierce = Mathf.Min(3, pierce + 1);
                break;
            case UpgradeType.ZeusChain:
                chainLightning = Mathf.Min(3, chainLightning + 1);
                break;
            case UpgradeType.AthenaShield:
                shieldCharges = Mathf.Min(3, shieldCharges + 1);
                break;
            case UpgradeType.ArtemisCrit:
                critChance = Mathf.Min(0.45f, critChance + 0.10f);
                critMultiplier += 0.12f;
                break;
            case UpgradeType.PoseidonForce:
                knockbackPower = Mathf.Min(2.2f, knockbackPower + 0.65f);
                break;
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
            case UpgradeType.RapidFire: return "ARES\nVelocità attacco +16%";
            case UpgradeType.DivinePower: return "POTERE DI ZEUS\n+11 danni";
            case UpgradeType.Vitality: return "AMBROSIA\n+30 vita massima";
            case UpgradeType.Hermes: return "SANDALI DI HERMES\nVelocità movimento";
            case UpgradeType.Piercing: return "LANCIA SACRA\nPerfora un nemico";
            case UpgradeType.ZeusChain: return "FULMINE DI ZEUS\nDanno a catena";
            case UpgradeType.AthenaShield: return "EGIDA DI ATENA\nBlocca un colpo";
            case UpgradeType.ArtemisCrit: return "OCCHIO DI ARTEMIDE\nCritico +10%";
            default: return "ONDA DI POSEIDONE\nRespinge i nemici";
        }
    }

    void OnGUI()
    {
        int w = Screen.width;
        int h = Screen.height;
        float s = Mathf.Clamp(w / 720f, 0.68f, 1.8f);

        GUI.skin.button.fontSize = Mathf.RoundToInt(26f * s);
        GUI.skin.label.fontSize = Mathf.RoundToInt(24f * s);

        if (state == GameState.MainMenu || state == GameState.Heroes || state == GameState.Settings)
        {
            DrawMenuBackground(w, h);
            if (state == GameState.MainMenu) DrawMainMenu(w, h, s);
            else if (state == GameState.Heroes) DrawHeroes(w, h, s);
            else DrawSettings(w, h, s);
            return;
        }

        DrawHud(w, h, s);
        if (state == GameState.Playing) DrawJoystick(w, h, s);

        if (state == GameState.Upgrade) DrawUpgrade(w, h, s);
        else if (state == GameState.RoomClear) DrawRoomClear(w, h, s);
        else if (state == GameState.Victory) DrawVictory(w, h, s);
        else if (state == GameState.GameOver) DrawGameOver(w, h, s);
        else if (state == GameState.Paused) DrawPause(w, h, s);

        if (Time.time < damageFlashUntil)
        {
            GUI.color = new Color(1f, 0.05f, 0.02f, 0.16f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    void DrawMenuBackground(int w, int h)
    {
        GUI.color = Color.white;
        if (menuTexture != null)
            GUI.DrawTexture(new Rect(0, 0, w, h), menuTexture, ScaleMode.ScaleAndCrop);
        else
        {
            GUI.color = new Color(0.02f, 0.06f, 0.18f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        }

        GUI.color = new Color(0f, 0f, 0f, 0.30f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    GUIStyle CenterStyle(int size, FontStyle fontStyle = FontStyle.Bold)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = size;
        style.fontStyle = fontStyle;
        style.normal.textColor = Color.white;
        return style;
    }

    void DrawMainMenu(int w, int h, float s)
    {
        GUI.Label(new Rect(0, h * 0.045f, w, 58f * s), "lello's game", CenterStyle(Mathf.RoundToInt(30f * s)));
        GUI.Label(new Rect(0, h * 0.17f, w, 100f * s), "MYTHBREAKER", CenterStyle(Mathf.RoundToInt(54f * s)));

        float bw = w * 0.72f;
        float bh = 72f * s;
        float x = (w - bw) * 0.5f;

        if (GUI.Button(new Rect(x, h * 0.60f, bw, bh), "NUOVA PARTITA")) state = GameState.Heroes;
        if (GUI.Button(new Rect(x, h * 0.69f, bw, bh), "CONTINUA • LIVELLO " + highestLevel)) state = GameState.Heroes;
        if (GUI.Button(new Rect(x, h * 0.78f, bw * 0.48f, bh), "EROI")) state = GameState.Heroes;
        if (GUI.Button(new Rect(x + bw * 0.52f, h * 0.78f, bw * 0.48f, bh), "IMPOSTAZIONI")) state = GameState.Settings;

        GUI.Label(new Rect(0, h - 62f * s, w, 28f * s), "MONETE  " + totalCoins, CenterStyle(Mathf.RoundToInt(18f * s), FontStyle.Normal));
        GUI.Label(new Rect(0, h - 36f * s, w, 26f * s), "Build 0.2 • Grecia", CenterStyle(Mathf.RoundToInt(16f * s), FontStyle.Normal));
    }

    void DrawHeroes(int w, int h, float s)
    {
        GUI.Label(new Rect(0, h * 0.06f, w, 64f * s), "SCEGLI IL TUO EROE", CenterStyle(Mathf.RoundToInt(38f * s)));

        float x = w * 0.075f;
        float bw = w * 0.85f;
        float bh = 68f * s;
        float startY = h * 0.17f;
        float spacing = 0.078f * h;

        HeroCatalog.Hero[] heroes = HeroCatalog.Heroes;
        int shown = Mathf.Min(heroes.Length, 8);
        for (int i = 0; i < shown; i++)
        {
            HeroCatalog.Hero hero = heroes[i];
            bool unlocked = hero.unlockLevel <= highestLevel;
            GUI.enabled = unlocked;
            string suffix = unlocked ? "DISPONIBILE" : "SBLOCCO LIV. " + hero.unlockLevel;
            if (GUI.Button(new Rect(x, startY + spacing * i, bw, bh), hero.displayName.ToUpperInvariant() + " • " + suffix + "\n" + hero.weapon + " • " + hero.title))
                selectedHero = hero.id;
            GUI.enabled = true;
        }

        HeroCatalog.Hero selected = HeroCatalog.Get(selectedHero);
        GUI.Label(new Rect(w * 0.08f, h * 0.81f, w * 0.84f, 62f * s), selected.passive, CenterStyle(Mathf.RoundToInt(17f * s), FontStyle.Normal));

        if (GUI.Button(new Rect(w * 0.08f, h * 0.89f, w * 0.55f, 64f * s), "GIOCA CON " + selected.displayName.ToUpperInvariant())) StartRun();
        if (GUI.Button(new Rect(w * 0.66f, h * 0.89f, w * 0.26f, 64f * s), "INDIETRO")) state = GameState.MainMenu;
    }

    void DrawSettings(int w, int h, float s)
    {
        GUI.Label(new Rect(0, h * 0.12f, w, 70f * s), "IMPOSTAZIONI", CenterStyle(Mathf.RoundToInt(40f * s)));
        GUI.Label(new Rect(w * 0.10f, h * 0.30f, w * 0.80f, 220f * s),
            "60 FPS target\nSchermo verticale\nJoystick dinamico a un dito\nAuto-attacco da fermo\nAudio e vibrazione: prossima iterazione",
            CenterStyle(Mathf.RoundToInt(23f * s), FontStyle.Normal));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.75f, w * 0.64f, 75f * s), "INDIETRO")) state = GameState.MainMenu;
    }

    void DrawHud(int w, int h, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, w, 102f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;

        HeroCatalog.Hero hero = HeroCatalog.Get(selectedHero);
        GUI.Label(new Rect(10f * s, 6f * s, w * 0.31f, 32f * s), hero.displayName.ToUpperInvariant() + " Lv." + heroLevel, CenterStyle(Mathf.RoundToInt(18f * s)));
        GUI.Label(new Rect(w * 0.32f, 6f * s, w * 0.40f, 32f * s), "ATTICA • " + room + "/" + RoomTotal, CenterStyle(Mathf.RoundToInt(18f * s)));
        GUI.Label(new Rect(w * 0.73f, 6f * s, w * 0.20f, 32f * s), runCoins.ToString(), CenterStyle(Mathf.RoundToInt(18f * s)));

        GUI.color = new Color(0.15f, 0.15f, 0.15f);
        GUI.DrawTexture(new Rect(w * 0.07f, 47f * s, w * 0.62f, 18f * s), Texture2D.whiteTexture);
        GUI.color = new Color(0.18f, 0.86f, 0.26f);
        GUI.DrawTexture(new Rect(w * 0.07f, 47f * s, w * 0.62f * Mathf.Clamp01(hp / maxHp), 18f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(w * 0.07f, 40f * s, w * 0.62f, 31f * s), Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(maxHp), CenterStyle(Mathf.RoundToInt(16f * s)));

        GUI.color = new Color(0.12f, 0.12f, 0.12f);
        GUI.DrawTexture(new Rect(w * 0.07f, 74f * s, w * 0.45f, 10f * s), Texture2D.whiteTexture);
        GUI.color = new Color(0.22f, 0.62f, 1f);
        GUI.DrawTexture(new Rect(w * 0.07f, 74f * s, w * 0.45f * Mathf.Clamp01((float)xp / xpNeeded), 10f * s), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (shieldCharges > 0)
            GUI.Label(new Rect(w * 0.53f, 66f * s, w * 0.16f, 28f * s), "SCUDO x" + shieldCharges, CenterStyle(Mathf.RoundToInt(13f * s)));

        MBEnemy boss = GetBoss();
        if (boss != null)
        {
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            GUI.DrawTexture(new Rect(w * 0.16f, 108f * s, w * 0.68f, 28f * s), Texture2D.whiteTexture);
            GUI.color = new Color(0.85f, 0.12f, 0.06f);
            GUI.DrawTexture(new Rect(w * 0.16f, 108f * s, w * 0.68f * boss.HpNormalized, 28f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(w * 0.16f, 101f * s, w * 0.68f, 38f * s), boss.DisplayName, CenterStyle(Mathf.RoundToInt(16f * s)));
        }

        if (state == GameState.Playing && GUI.Button(new Rect(w - 72f * s, 43f * s, 55f * s, 42f * s), "Ⅱ"))
        {
            stateBeforePause = state;
            state = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    MBEnemy GetBoss()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null && enemies[i].Alive && enemies[i].IsBoss) return enemies[i];
        return null;
    }

    void DrawJoystick(int w, int h, float s)
    {
        if (!pointerHeld) return;

        float radius = Mathf.Max(75f, w * 0.13f);
        Vector2 delta = Vector2.ClampMagnitude(pointerNow - pointerStart, radius);
        Vector2 knob = pointerStart + delta;

        GUI.color = new Color(0.05f, 0.12f, 0.25f, 0.38f);
        GUI.DrawTexture(new Rect(pointerStart.x - radius, h - pointerStart.y - radius, radius * 2f, radius * 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0.95f, 0.65f, 0.12f, 0.58f);
        float k = radius * 0.72f;
        GUI.DrawTexture(new Rect(knob.x - k * 0.5f, h - knob.y - k * 0.5f, k, k), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawUpgrade(int w, int h, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, h * 0.20f, w, 70f * s), "POTERE DEGLI DEI", CenterStyle(Mathf.RoundToInt(39f * s)));
        GUI.Label(new Rect(0, h * 0.27f, w, 42f * s), "Scegli una benedizione", CenterStyle(Mathf.RoundToInt(21f * s), FontStyle.Normal));

        float gap = w * 0.025f;
        float cardW = (w - gap * 4f) / 3f;
        float y = h * 0.40f;
        float ch = 230f * s;
        for (int i = 0; i < 3; i++)
            if (GUI.Button(new Rect(gap + i * (cardW + gap), y, cardW, ch), UpgradeName(choices[i]))) ApplyUpgrade(choices[i]);
    }

    void DrawRoomClear(int w, int h, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.74f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.31f, w, 80f * s), "STANZA LIBERATA", CenterStyle(Mathf.RoundToInt(41f * s)));
        GUI.Label(new Rect(0, h * 0.40f, w, 48f * s), "Il cammino verso il tempio continua", CenterStyle(Mathf.RoundToInt(20f * s), FontStyle.Normal));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.53f, w * 0.64f, 82f * s), "ENTRA NELLA STANZA " + (room + 1)))
        {
            room++;
            state = GameState.Playing;
            SpawnRoom();
        }
    }

    void DrawVictory(int w, int h, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.80f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, h * 0.22f, w, 90f * s), "VITTORIA", CenterStyle(Mathf.RoundToInt(49f * s)));
        GUI.Label(new Rect(w * 0.08f, h * 0.34f, w * 0.84f, 140f * s),
            "Livello 1 completato\nIl sentiero spezzato\n+" + runCoins + " monete",
            CenterStyle(Mathf.RoundToInt(27f * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.60f, w * 0.64f, 82f * s), "TORNA AL TEMPIO")) ReturnToMenu();
    }

    void DrawGameOver(int w, int h, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        HeroCatalog.Hero hero = HeroCatalog.Get(selectedHero);
        GUI.Label(new Rect(0, h * 0.27f, w, 90f * s), hero.displayName.ToUpperInvariant() + " È CADUTO", CenterStyle(Mathf.RoundToInt(41f * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.49f, w * 0.64f, 82f * s), "RIPROVA")) StartRun();
        if (GUI.Button(new Rect(w * 0.18f, h * 0.61f, w * 0.64f, 82f * s), "MENU")) ReturnToMenu();
    }

    void DrawPause(int w, int h, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.84f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, h * 0.29f, w, 80f * s), "PAUSA", CenterStyle(Mathf.RoundToInt(48f * s)));
        if (GUI.Button(new Rect(w * 0.18f, h * 0.48f, w * 0.64f, 82f * s), "CONTINUA"))
        {
            Time.timeScale = 1f;
            state = stateBeforePause;
        }
        if (GUI.Button(new Rect(w * 0.18f, h * 0.60f, w * 0.64f, 82f * s), "ESCI AL MENU")) ReturnToMenu();
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        pointerHeld = false;
        ClearCombatObjects();
        SetWorldVisible(false);
        state = GameState.MainMenu;
    }
}

public class MBEnemy : MonoBehaviour
{
    public enum Kind { Satyr, Serpent, Hoplite, Harpy, Cyclops }

    public bool Alive { get; private set; } = true;
    public bool IsBoss => kind == Kind.Cyclops;
    public float HpNormalized => maxHp <= 0f ? 0f : Mathf.Clamp01(hp / maxHp);
    public string DisplayName => kind == Kind.Cyclops ? "CICLOPE GUARDIANO" : kind.ToString().ToUpperInvariant();

    Kind kind;
    float maxHp;
    float hp;
    float speed;
    float contactDamage;
    float nextHit;
    float nextShot;
    float nextSpecial;
    float chargeUntil;
    float hitPulseUntil;
    int coinReward;
    int xpReward;
    Renderer bodyRenderer;
    Vector3 baseScale;

    public void Setup(Kind k)
    {
        kind = k;
        bodyRenderer = GetComponent<Renderer>();
        baseScale = transform.localScale;

        switch (k)
        {
            case Kind.Satyr:
                maxHp = 52f; speed = 2.45f; contactDamage = 13f; coinReward = 2; xpReward = 2;
                break;
            case Kind.Serpent:
                maxHp = 42f; speed = 1.35f; contactDamage = 9f; coinReward = 2; xpReward = 2;
                break;
            case Kind.Hoplite:
                maxHp = 82f; speed = 1.52f; contactDamage = 18f; coinReward = 3; xpReward = 3;
                break;
            case Kind.Harpy:
                maxHp = 48f; speed = 2.05f; contactDamage = 10f; coinReward = 3; xpReward = 3;
                break;
            default:
                maxHp = 330f; speed = 1.18f; contactDamage = 28f; coinReward = 14; xpReward = 8;
                break;
        }

        hp = maxHp;
        nextSpecial = Time.time + UnityEngine.Random.Range(1.8f, 3.0f);
        nextShot = Time.time + UnityEngine.Random.Range(0.6f, 1.3f);
    }

    void Update()
    {
        if (!Alive || MythbreakerGame.I == null || !MythbreakerGame.I.IsPlaying || MythbreakerGame.I.Player == null) return;

        Transform p = MythbreakerGame.I.Player;
        Vector3 d = p.position - transform.position;
        d.y = 0f;
        float dist = d.magnitude;
        if (dist > 0.01f) d /= dist;

        switch (kind)
        {
            case Kind.Serpent:
                UpdateSerpent(d, dist);
                break;
            case Kind.Hoplite:
                UpdateHoplite(d, dist);
                break;
            case Kind.Harpy:
                UpdateHarpy(d, dist);
                break;
            case Kind.Cyclops:
                UpdateCyclops(d, dist);
                break;
            default:
                UpdateMelee(d, dist, 0.78f, 0.85f);
                break;
        }

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, -4.15f, 4.15f), transform.position.y, Mathf.Clamp(transform.position.z, -5.6f, 5.3f));
        if (d.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 8f);

        float pulse = Time.time < hitPulseUntil ? 1.08f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * pulse, Time.deltaTime * 15f);
    }

    void UpdateMelee(Vector3 d, float dist, float stopDistance, float hitCooldown)
    {
        if (dist > stopDistance) transform.position += d * speed * Time.deltaTime;
        else if (Time.time >= nextHit)
        {
            nextHit = Time.time + hitCooldown;
            MythbreakerGame.I.DamagePlayer(contactDamage);
        }
    }

    void UpdateSerpent(Vector3 d, float dist)
    {
        if (dist > 4.6f) transform.position += d * speed * Time.deltaTime;
        else if (dist < 3.2f) transform.position -= d * speed * 0.65f * Time.deltaTime;

        if (Time.time >= nextShot && dist < 8f)
        {
            nextShot = Time.time + 1.65f;
            MythbreakerGame.I.SpawnEnemyProjectile(transform.position + Vector3.up * 0.35f, d, 11f, 7.4f);
        }
    }

    void UpdateHoplite(Vector3 d, float dist)
    {
        if (Time.time >= nextSpecial)
        {
            nextSpecial = Time.time + 3.1f;
            chargeUntil = Time.time + 0.58f;
        }

        float currentSpeed = Time.time < chargeUntil ? speed * 3.05f : speed;
        if (dist > 0.80f) transform.position += d * currentSpeed * Time.deltaTime;
        else if (Time.time >= nextHit)
        {
            nextHit = Time.time + 1.0f;
            MythbreakerGame.I.DamagePlayer(contactDamage + (Time.time < chargeUntil ? 8f : 0f));
        }
    }

    void UpdateHarpy(Vector3 d, float dist)
    {
        Vector3 tangent = new Vector3(-d.z, 0f, d.x);
        float orbitSign = Mathf.Sin(Time.time * 1.7f + GetInstanceID()) >= 0f ? 1f : -1f;
        Vector3 movement = tangent * orbitSign;
        if (dist > 4.5f) movement += d * 0.85f;
        if (dist < 2.8f) movement -= d * 0.65f;
        if (movement.sqrMagnitude > 0.01f) movement.Normalize();
        transform.position += movement * speed * Time.deltaTime;

        if (Time.time >= nextShot && dist < 7.5f)
        {
            nextShot = Time.time + 1.35f;
            MythbreakerGame.I.SpawnEnemyProjectile(transform.position, d, 9f, 8.3f);
        }
    }

    void UpdateCyclops(Vector3 d, float dist)
    {
        if (Time.time >= nextSpecial)
        {
            nextSpecial = Time.time + 3.2f;
            MythbreakerGame.I.SpawnRadialEnemyShots(transform.position + Vector3.up * 0.45f, 8, 12f, 6.3f);
        }

        if (dist > 1.15f) transform.position += d * speed * Time.deltaTime;
        else if (Time.time >= nextHit)
        {
            nextHit = Time.time + 1.35f;
            MythbreakerGame.I.DamagePlayer(contactDamage);
        }
    }

    public void TakeDamage(float damage, Vector3 knockback)
    {
        if (!Alive) return;

        hp -= damage;
        hitPulseUntil = Time.time + 0.09f;

        if (!IsBoss && knockback.sqrMagnitude > 0.001f)
            transform.position += knockback;

        if (hp <= 0f)
        {
            Alive = false;
            MythbreakerGame.I.EnemyKilled(this, coinReward, xpReward);
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
    public float Knockback { get; private set; }
    public Vector3 KnockbackDirection => dir;

    public void Setup(Vector3 direction, float projectileSpeed, float damage, bool isFriendly, int pierce, float knockback)
    {
        dir = direction.normalized;
        speed = projectileSpeed;
        Damage = damage;
        friendly = isFriendly;
        remainingPierce = pierce;
        Knockback = knockback;
    }

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Remove();
            return;
        }

        if (MythbreakerGame.I == null) return;

        if (friendly)
            MythbreakerGame.I.ProjectileHitEnemy(this);
        else if (MythbreakerGame.I.Player != null && (MythbreakerGame.I.Player.position - transform.position).sqrMagnitude < 0.44f)
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
