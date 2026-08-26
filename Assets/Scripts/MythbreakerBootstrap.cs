using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public sealed class MythbreakerBootstrap : MonoBehaviour
{
    enum AppState { Menu, Heroes, Settings, Playing, Upgrade, Victory, GameOver }
    enum EnemyType { Assassin, Gorgon, Hoplite, Minotaur }

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
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();

    Texture2D circle;
    Texture2D menu;
    Texture2D perseo;
    Texture2D assassin;
    Texture2D gorgon;
    Texture2D hoplite;
    Texture2D minotaur;

    Vector2 hero = new Vector2(.50f, .79f);
    Vector2 dragStart;
    Vector2 dragNow;
    Vector2 moveInput;
    bool dragging;

    bool vibration = true;
    bool sound = true;
    float heroHp = 520f;
    float heroMaxHp = 520f;
    float moveSpeed = .45f;
    float shotDamage = 58f;
    float fireInterval = .42f;
    int multishot = 1;
    int pierce;
    int level = 1;
    int kills;
    int coins = 199;
    float nextShot;
    float hurtCooldown;
    string diagnostic = "MYTHBREAKER 0.8 • ATTICA";

    readonly string[] upName = new string[3];
    readonly string[] upDesc = new string[3];
    readonly int[] upId = new int[3];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBoot()
    {
        if (FindFirstObjectByType<MythbreakerBootstrap>() == null)
            new GameObject("MYTHBREAKER BOOT 0.8").AddComponent<MythbreakerBootstrap>();
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;
        circle = MakeCircle(64);
        menu = LoadB64("mythbreaker_menu_b64", false);
        perseo = LoadB64("perseo_sprite_b64", true);
        assassin = LoadB64("assassin_sprite_b64", true);
        gorgon = LoadB64("gorgon_sprite_b64", true);
        hoplite = LoadB64("hoplite_sprite_b64", true);
        minotaur = LoadB64("minotaur_sprite_b64", true);
    }

    Texture2D LoadB64(string name, bool alpha)
    {
        TextAsset t = Resources.Load<TextAsset>(name);
        if (t == null) { diagnostic = "0.8 • MISSING " + name; return null; }
        try
        {
            string s = t.text.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();
            byte[] b = Convert.FromBase64String(s);
            Texture2D x = new Texture2D(2, 2, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            if (!x.LoadImage(b, false)) { Destroy(x); return null; }
            x.wrapMode = TextureWrapMode.Clamp;
            x.filterMode = FilterMode.Bilinear;
            return x;
        }
        catch (Exception e) { diagnostic = "0.8 • " + e.GetType().Name; return null; }
    }

    Texture2D MakeCircle(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color[] px = new Color[n * n];
        float c = (n - 1) * .5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float a = Mathf.Clamp01(c - Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) + 1.2f);
            px[y * n + x] = new Color(1, 1, 1, a);
        }
        t.SetPixels(px); t.Apply(false, false); return t;
    }

    void Update()
    {
        if (state != AppState.Playing) return;
        ReadPointer();
        if (dragging && moveInput.sqrMagnitude > .001f)
        {
            Vector2 candidate = hero + moveInput * moveSpeed * Time.deltaTime;
            candidate.x = Mathf.Clamp(candidate.x, .12f, .88f);
            candidate.y = Mathf.Clamp(candidate.y, .20f, .84f);
            if (!PointBlocked(candidate)) hero = candidate;
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
                Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y);
                if (t.phase == TouchPhase.Began) BeginDrag(p);
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) ContinueDrag(p);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) EndDrag();
                return;
            }
            Vector3 m = Input.mousePosition;
            Vector2 mp = new Vector2(m.x, Screen.height - m.y);
            if (Input.GetMouseButtonDown(0)) BeginDrag(mp);
            else if (Input.GetMouseButton(0)) ContinueDrag(mp);
            else if (Input.GetMouseButtonUp(0)) EndDrag();
        }
        catch { EndDrag(); }
    }

    void BeginDrag(Vector2 p)
    {
        if (!ArenaRect().Contains(p)) return;
        dragging = true; dragStart = p; dragNow = p; moveInput = Vector2.zero;
    }

    void ContinueDrag(Vector2 p)
    {
        if (!dragging) return;
        dragNow = p;
        float r = Mathf.Max(72f, Screen.width * .13f);
        moveInput = Vector2.ClampMagnitude(dragNow - dragStart, r) / r;
    }

    void EndDrag() { dragging = false; moveInput = Vector2.zero; }

    Rect ArenaRect() => new Rect(Screen.width * .045f, Screen.height * .105f, Screen.width * .91f, Screen.height * .775f);

    void StartRun()
    {
        heroHp = heroMaxHp = 520f;
        moveSpeed = .45f;
        shotDamage = 58f;
        fireInterval = .42f;
        multishot = 1; pierce = 0; level = 1; kills = 0; coins = 199;
        state = AppState.Playing;
        SpawnLevel();
        Haptic();
    }

    void SpawnLevel()
    {
        enemies.Clear(); shots.Clear(); EndDrag();
        hero = new Vector2(.50f, .79f);
        nextShot = 0; hurtCooldown = 0;
        if (level == 1)
        {
            AddEnemy(EnemyType.Assassin, .30f, .31f); AddEnemy(EnemyType.Hoplite, .50f, .27f); AddEnemy(EnemyType.Assassin, .70f, .31f);
        }
        else if (level == 2)
        {
            AddEnemy(EnemyType.Gorgon, .27f, .29f); AddEnemy(EnemyType.Gorgon, .73f, .29f); AddEnemy(EnemyType.Assassin, .38f, .39f); AddEnemy(EnemyType.Assassin, .62f, .39f);
        }
        else if (level == 3)
        {
            AddEnemy(EnemyType.Hoplite, .24f, .27f); AddEnemy(EnemyType.Hoplite, .50f, .24f); AddEnemy(EnemyType.Hoplite, .76f, .27f); AddEnemy(EnemyType.Assassin, .35f, .41f); AddEnemy(EnemyType.Assassin, .65f, .41f);
        }
        else if (level == 4)
        {
            AddEnemy(EnemyType.Gorgon, .25f, .27f); AddEnemy(EnemyType.Gorgon, .50f, .23f); AddEnemy(EnemyType.Gorgon, .75f, .27f); AddEnemy(EnemyType.Hoplite, .34f, .41f); AddEnemy(EnemyType.Hoplite, .66f, .41f);
        }
        else
        {
            AddEnemy(EnemyType.Minotaur, .50f, .29f); AddEnemy(EnemyType.Hoplite, .25f, .37f); AddEnemy(EnemyType.Hoplite, .75f, .37f);
        }
    }

    void AddEnemy(EnemyType type, float x, float y)
    {
        float hp = type == EnemyType.Assassin ? 95f + level * 12f : type == EnemyType.Gorgon ? 125f + level * 14f : type == EnemyType.Hoplite ? 165f + level * 18f : 1050f;
        float sp = type == EnemyType.Assassin ? .058f : type == EnemyType.Gorgon ? .046f : type == EnemyType.Hoplite ? .036f : .043f;
        float r = type == EnemyType.Minotaur ? .075f : type == EnemyType.Hoplite ? .040f : .034f;
        enemies.Add(new Enemy { p = new Vector2(x, y), hp = hp, maxHp = hp, speed = sp, radius = r, type = type, phase = enemies.Count * 1.7f });
    }

    void UpdateCombat()
    {
        float dt = Time.deltaTime;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy e = enemies[i];
            Vector2 toHero = hero - e.p;
            if (toHero.sqrMagnitude > .0001f)
            {
                Vector2 dir = toHero.normalized;
                if (e.type == EnemyType.Gorgon)
                {
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    dir = (dir + side * Mathf.Sin(Time.time * 4.5f + e.phase) * .34f).normalized;
                }
                float boost = e.type == EnemyType.Minotaur && Mathf.Sin(Time.time * 2.2f) > .72f ? 2.1f : 1f;
                Vector2 np = e.p + dir * e.speed * boost * dt;
                if (!PointBlocked(np)) e.p = np;
            }
            if (Vector2.Distance(hero, e.p) < e.radius + .035f && Time.time >= hurtCooldown)
            {
                float dmg = e.type == EnemyType.Minotaur ? 65f : e.type == EnemyType.Hoplite ? 34f : 24f;
                heroHp -= dmg; hurtCooldown = Time.time + .65f; Haptic();
                if (heroHp <= 0) { heroHp = 0; state = AppState.GameOver; EndDrag(); return; }
            }
        }

        if (!dragging && enemies.Count > 0 && Time.time >= nextShot)
        {
            nextShot = Time.time + fireInterval;
            FireAtNearest();
        }

        for (int s = shots.Count - 1; s >= 0; s--)
        {
            Shot sh = shots[s]; sh.p += sh.v * dt;
            bool remove = sh.p.x < .04f || sh.p.x > .96f || sh.p.y < .10f || sh.p.y > .91f;
            if (!remove)
            {
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Enemy e = enemies[i];
                    if (Vector2.Distance(sh.p, e.p) < e.radius + .014f)
                    {
                        e.hp -= shotDamage;
                        if (e.hp <= 0) { enemies.RemoveAt(i); kills++; coins += e.type == EnemyType.Minotaur ? 50 : 5; }
                        if (sh.pierce > 0) sh.pierce--; else remove = true;
                        break;
                    }
                }
            }
            if (remove) shots.RemoveAt(s);
        }

        if (enemies.Count == 0)
        {
            EndDrag();
            if (level >= 5) state = AppState.Victory;
            else { PrepareUpgrades(); state = AppState.Upgrade; }
        }
    }

    void FireAtNearest()
    {
        Enemy nearest = enemies[0]; float best = (nearest.p - hero).sqrMagnitude;
        for (int i = 1; i < enemies.Count; i++)
        {
            float d = (enemies[i].p - hero).sqrMagnitude;
            if (d < best) { best = d; nearest = enemies[i]; }
        }
        Vector2 dir = (nearest.p - hero).normalized;
        if (multishot == 1) shots.Add(new Shot { p = hero, v = dir * .82f, pierce = pierce });
        else
        {
            float spread = multishot == 2 ? 7f : 11f;
            for (int i = 0; i < multishot; i++)
            {
                float t = i / (float)(multishot - 1);
                shots.Add(new Shot { p = hero, v = Rotate(dir, Mathf.Lerp(-spread, spread, t) * Mathf.Deg2Rad) * .82f, pierce = pierce });
            }
        }
    }

    Vector2 Rotate(Vector2 v, float a)
    {
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void PrepareUpgrades()
    {
        int seed = level * 19 + kills * 3;
        for (int i = 0; i < 3; i++)
        {
            int id = (seed + i * 2) % 6;
            while (i > 0 && (id == upId[0] || (i > 1 && id == upId[1]))) id = (id + 1) % 6;
            upId[i] = id;
            if (id == 0) { upName[i] = "TIRO RAPIDO"; upDesc[i] = "+15% velocità di attacco"; }
            else if (id == 1) { upName[i] = "POTENZA"; upDesc[i] = "+18 danni"; }
            else if (id == 2) { upName[i] = "ERMES"; upDesc[i] = "+10% velocità movimento"; }
            else if (id == 3) { upName[i] = "VITALITÀ"; upDesc[i] = "+80 HP e cura"; }
            else if (id == 4) { upName[i] = "MULTISHOT"; upDesc[i] = "+1 proiettile"; }
            else { upName[i] = "PERFORANTE"; upDesc[i] = "+1 bersaglio attraversato"; }
        }
    }

    void ApplyUpgrade(int id)
    {
        if (id == 0) fireInterval = Mathf.Max(.22f, fireInterval * .85f);
        else if (id == 1) shotDamage += 18f;
        else if (id == 2) moveSpeed *= 1.10f;
        else if (id == 3) { heroMaxHp += 80f; heroHp = Mathf.Min(heroMaxHp, heroHp + 110f); }
        else if (id == 4) multishot = Mathf.Min(3, multishot + 1);
        else pierce = Mathf.Min(2, pierce + 1);
        level++; state = AppState.Playing; SpawnLevel(); Haptic();
    }

    bool PointBlocked(Vector2 p)
    {
        Rect[] rr = Obstacles(level);
        for (int i = 0; i < rr.Length; i++) if (rr[i].Contains(p)) return true;
        return false;
    }

    Rect[] Obstacles(int l)
    {
        if (l == 1) return new[] { new Rect(.17f,.48f,.13f,.10f), new Rect(.70f,.48f,.13f,.10f), new Rect(.17f,.67f,.13f,.10f), new Rect(.70f,.67f,.13f,.10f) };
        if (l == 2) return new[] { new Rect(.12f,.52f,.14f,.09f), new Rect(.74f,.52f,.14f,.09f), new Rect(.43f,.44f,.14f,.09f) };
        if (l == 3) return new[] { new Rect(.22f,.49f,.12f,.10f), new Rect(.66f,.49f,.12f,.10f), new Rect(.44f,.63f,.12f,.10f) };
        if (l == 4) return new[] { new Rect(.12f,.46f,.12f,.11f), new Rect(.76f,.46f,.12f,.11f), new Rect(.31f,.62f,.12f,.10f), new Rect(.57f,.62f,.12f,.10f) };
        return new[] { new Rect(.14f,.57f,.13f,.10f), new Rect(.73f,.57f,.13f,.10f) };
    }

    void Haptic() { if (vibration) try { Handheld.Vibrate(); } catch { } }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;
        float s = Mathf.Clamp(w / 720f, .72f, 1.8f);
        if (state == AppState.Menu) DrawMenu(w, h, s);
        else if (state == AppState.Heroes) DrawHeroes(w, h, s);
        else if (state == AppState.Settings) DrawSettings(w, h, s);
        else if (state == AppState.Playing) DrawGame(w, h, s);
        else if (state == AppState.Upgrade) DrawUpgrade(w, h, s);
        else DrawEnd(w, h, s, state == AppState.Victory);
    }

    void DrawMenu(int w, int h, float s)
    {
        Fill(new Rect(0,0,w,h), new Color(.004f,.01f,.035f));
        if (menu != null)
        {
            float img = menu.width / (float)menu.height;
            float scr = w / (float)h;
            Rect r;
            if (scr > img) { float hh = w / img; r = new Rect(0,(h-hh)*.5f,w,hh); }
            else { float ww = h * img; r = new Rect((w-ww)*.5f,0,ww,h); }
            GUI.color = Color.white; GUI.DrawTexture(r, menu, ScaleMode.ScaleAndCrop);
        }
        GUIStyle invisible = TransparentButton();
        if (GUI.Button(new Rect(w*.12f,h*.765f,w*.76f,h*.12f),"",invisible)) StartRun();
        if (GUI.Button(new Rect(0,h*.895f,w*.49f,h*.10f),"",invisible)) { state = AppState.Heroes; Haptic(); }
        if (GUI.Button(new Rect(w*.51f,h*.895f,w*.49f,h*.10f),"",invisible)) { state = AppState.Settings; Haptic(); }
        GUI.color = new Color(1,1,1,.85f); GUI.Label(new Rect(w*.78f,h*.012f,w*.18f,28*s),"v0.8",Right(Mathf.RoundToInt(13*s))); GUI.color = Color.white;
    }

    void DrawHeroes(int w, int h, float s)
    {
        DrawPanel(w,h); GUI.color = Gold();
        GUI.Label(new Rect(0,h*.07f,w,70*s),"EROI",Center(Mathf.RoundToInt(40*s))); GUI.color = Color.white;
        GUI.Label(new Rect(0,h*.13f,w,42*s),"Campioni della Grecia",Center(Mathf.RoundToInt(18*s)));
        Rect card = new Rect(w*.09f,h*.23f,w*.82f,h*.30f); Fill(card,new Color(.04f,.09f,.19f,.98f)); Stroke(card,Gold(),3);
        DrawTextureCentered(perseo,new Vector2(w*.28f,h*.375f),w*.24f,w*.30f);
        GUI.color = Gold(); GUI.Label(new Rect(w*.43f,h*.27f,w*.42f,42*s),"PERSEO",Left(Mathf.RoundToInt(28*s))); GUI.color = Color.white;
        GUI.Label(new Rect(w*.43f,h*.33f,w*.42f,115*s),"Arma: Lancia divina\nVeloce • preciso\nEroe iniziale",Left(Mathf.RoundToInt(17*s)));
        GUI.color = new Color(.72f,.78f,.88f); GUI.Label(new Rect(w*.10f,h*.59f,w*.80f,100*s),"ERACLE • ATALANTA • ACHILLE\nsi sbloccano proseguendo l'avventura",Center(Mathf.RoundToInt(17*s))); GUI.color = Color.white;
        if (GUI.Button(new Rect(w*.20f,h*.82f,w*.60f,68*s),"INDIETRO",Button(Mathf.RoundToInt(21*s)))) { state=AppState.Menu; Haptic(); }
    }

    void DrawSettings(int w, int h, float s)
    {
        DrawPanel(w,h); GUI.color=Gold(); GUI.Label(new Rect(0,h*.07f,w,70*s),"IMPOSTAZIONI",Center(Mathf.RoundToInt(36*s))); GUI.color=Color.white;
        Setting(w,h,s,.27f,"VIBRAZIONE",ref vibration); Setting(w,h,s,.41f,"AUDIO",ref sound);
        GUI.Label(new Rect(w*.10f,h*.58f,w*.80f,110*s),"60 FPS • verticale\nMovimento a un dito\nAuto-attacco da fermo",Center(Mathf.RoundToInt(18*s)));
        if (GUI.Button(new Rect(w*.20f,h*.82f,w*.60f,68*s),"INDIETRO",Button(Mathf.RoundToInt(21*s)))) { state=AppState.Menu; Haptic(); }
    }

    void Setting(int w,int h,float s,float y,string label,ref bool value)
    {
        Rect r=new Rect(w*.11f,h*y,w*.78f,72*s); Fill(r,new Color(.03f,.07f,.15f,.98f)); Stroke(r,new Color(.30f,.42f,.60f),2);
        GUI.Label(new Rect(r.x+20*s,r.y,r.width*.60f,r.height),label,Left(Mathf.RoundToInt(20*s)));
        GUI.color=value?new Color(.16f,.82f,.42f):new Color(.56f,.58f,.64f); GUI.Label(new Rect(r.x+r.width*.63f,r.y,r.width*.30f,r.height),value?"ON":"OFF",Right(Mathf.RoundToInt(20*s))); GUI.color=Color.white;
        if(GUI.Button(r,"",TransparentButton())) { value=!value; Haptic(); }
    }

    void DrawGame(int w,int h,float s)
    {
        DrawArena(w,h,s); DrawHud(w,h,s);
        DrawTextureCentered(perseo,new Vector2(hero.x*w,hero.y*h),w*.125f,w*.155f);
        for(int i=0;i<enemies.Count;i++) DrawEnemy(enemies[i],w,h,s);
        for(int i=0;i<shots.Count;i++) DrawProjectile(shots[i].p,w,h);
        if(dragging)
        {
            float r=Mathf.Max(72,w*.13f); DrawCircle(dragStart,r,new Color(.01f,.04f,.12f,.38f));
            Vector2 k=dragStart+Vector2.ClampMagnitude(dragNow-dragStart,r); DrawCircle(k,r*.39f,new Color(.08f,.45f,1f,.72f));
        }
    }

    void DrawArena(int w,int h,float s)
    {
        Color floor = level==1?new Color(.56f,.45f,.31f):level==2?new Color(.30f,.39f,.25f):level==3?new Color(.30f,.34f,.39f):level==4?new Color(.25f,.20f,.32f):new Color(.34f,.22f,.16f);
        Color dark = level==1?new Color(.12f,.09f,.06f):level==2?new Color(.06f,.12f,.07f):level==3?new Color(.06f,.09f,.13f):level==4?new Color(.09f,.05f,.13f):new Color(.13f,.05f,.025f);
        Fill(new Rect(0,0,w,h),dark);
        Rect a=ArenaRect(); Fill(a,floor); Stroke(a,Gold(),3);
        int cols=7, rows=12; float cw=a.width/cols, rh=a.height/rows;
        for(int y=0;y<rows;y++) for(int x=0;x<cols;x++)
        {
            float v=((x+y)&1)==0?.035f:-.025f; Color c=new Color(Mathf.Clamp01(floor.r+v),Mathf.Clamp01(floor.g+v),Mathf.Clamp01(floor.b+v));
            Stroke(new Rect(a.x+x*cw,a.y+y*rh,cw,rh),new Color(c.r*.72f,c.g*.72f,c.b*.72f,.62f),1);
        }
        Rect med=new Rect(w*.29f,h*.40f,w*.42f,w*.42f); DrawCircle(new Vector2(med.center.x,med.center.y),med.width*.5f,new Color(.15f,.11f,.07f,.23f)); DrawCircle(new Vector2(med.center.x,med.center.y),med.width*.36f,new Color(.84f,.64f,.28f,.10f));
        Rect[] obs=Obstacles(level); for(int i=0;i<obs.Length;i++) DrawObstacle(obs[i],w,h,level);
        DrawWallDecor(a,w,h,level);
        GUI.color=Gold(); GUI.Label(new Rect(0,h*.865f,w,34*s),level==1?"TEMPIO DI ATENA":level==2?"FORESTA SACRA":level==3?"ROVINE DI DEDALO":level==4?"CAVERNA OSCURA":"ARENA DEL MINOTAURO",Center(Mathf.RoundToInt(14*s))); GUI.color=Color.white;
    }

    void DrawObstacle(Rect n,int w,int h,int theme)
    {
        Rect r=new Rect(n.x*w,n.y*h,n.width*w,n.height*h);
        Color baseC=theme==2?new Color(.24f,.31f,.18f):theme==4?new Color(.28f,.18f,.34f):theme==5?new Color(.32f,.18f,.10f):new Color(.47f,.42f,.33f);
        Fill(new Rect(r.x+4,r.y+7,r.width,r.height),new Color(0,0,0,.25f)); Fill(r,baseC); Stroke(r,new Color(.72f,.62f,.40f),2);
        for(int i=1;i<3;i++) Fill(new Rect(r.x,r.y+r.height*i/3f,r.width,1.5f),new Color(.12f,.10f,.08f,.45f));
    }

    void DrawWallDecor(Rect a,int w,int h,int theme)
    {
        float bw=w*.045f;
        for(int i=0;i<5;i++)
        {
            float y=a.y+a.height*(.11f+i*.19f);
            Fill(new Rect(a.x+4,y,bw,h*.07f),new Color(.43f,.39f,.31f)); Fill(new Rect(a.xMax-bw-4,y,bw,h*.07f),new Color(.43f,.39f,.31f));
        }
        Color flame=theme==4?new Color(.55f,.18f,1f):theme==3?new Color(.12f,.58f,1f):new Color(1f,.48f,.06f);
        for(int side=0;side<2;side++) for(int i=0;i<3;i++)
        {
            float x=side==0?a.x+w*.055f:a.xMax-w*.055f; float y=a.y+a.height*(.18f+i*.28f);
            DrawCircle(new Vector2(x,y),w*.024f,new Color(flame.r,flame.g,flame.b,.24f)); DrawCircle(new Vector2(x,y),w*.012f,flame);
        }
    }

    void DrawHud(int w,int h,float s)
    {
        Fill(new Rect(0,0,w,h*.103f),new Color(.005f,.014f,.043f,.98f)); Fill(new Rect(0,h*.100f,w,3),Gold());
        GUI.color=Color.white; GUI.Label(new Rect(w*.04f,h*.010f,w*.28f,38*s),"PERSEO",Left(Mathf.RoundToInt(19*s)));
        GUI.color=Gold(); GUI.Label(new Rect(w*.30f,h*.010f,w*.48f,38*s),"ATTICA • LIVELLO "+level,Center(Mathf.RoundToInt(18*s))); GUI.color=Color.white;
        GUI.Label(new Rect(w*.78f,h*.010f,w*.18f,38*s),"◆ "+coins,Right(Mathf.RoundToInt(17*s)));
        Rect hp=new Rect(w*.075f,h*.063f,w*.66f,11*s); Fill(hp,new Color(.08f,.06f,.06f)); Fill(new Rect(hp.x,hp.y,hp.width*Mathf.Clamp01(heroHp/heroMaxHp),hp.height),new Color(.11f,.76f,.24f)); Stroke(hp,new Color(.80f,.70f,.42f),1);
        GUI.Label(new Rect(w*.74f,h*.050f,w*.22f,30*s),Mathf.CeilToInt(heroHp)+"/"+Mathf.CeilToInt(heroMaxHp),Right(Mathf.RoundToInt(13*s)));
    }

    void DrawEnemy(Enemy e,int w,int h,float s)
    {
        Texture2D t=e.type==EnemyType.Assassin?assassin:e.type==EnemyType.Gorgon?gorgon:e.type==EnemyType.Hoplite?hoplite:minotaur;
        Vector2 c=new Vector2(e.p.x*w,e.p.y*h);
        float ww=e.type==EnemyType.Minotaur?w*.22f:e.type==EnemyType.Gorgon?w*.135f:e.type==EnemyType.Hoplite?w*.125f:w*.105f;
        float hh=e.type==EnemyType.Minotaur?w*.25f:e.type==EnemyType.Gorgon?w*.16f:e.type==EnemyType.Hoplite?w*.15f:w*.12f;
        DrawTextureCentered(t,c,ww,hh);
        float barW=e.type==EnemyType.Minotaur?w*.25f:ww*.82f; Rect bar=new Rect(c.x-barW*.5f,c.y-hh*.58f,barW,Mathf.Max(4,5*s)); Fill(bar,new Color(.08f,.03f,.02f)); Fill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(e.hp/e.maxHp),bar.height),e.type==EnemyType.Minotaur?new Color(.96f,.20f,.03f):new Color(.80f,.08f,.04f)); Stroke(bar,new Color(.22f,.08f,.03f),1);
    }

    void DrawProjectile(Vector2 p,int w,int h)
    {
        Vector2 c=new Vector2(p.x*w,p.y*h); float r=w*.009f; DrawCircle(c,r*2.4f,new Color(.05f,.35f,1f,.18f)); DrawCircle(c,r*1.25f,new Color(.10f,.60f,1f,.75f)); DrawCircle(c,r*.52f,Color.white);
    }

    void DrawUpgrade(int w,int h,float s)
    {
        DrawGame(w,h,s); Fill(new Rect(0,0,w,h),new Color(.002f,.008f,.025f,.88f));
        GUI.color=Gold(); GUI.Label(new Rect(0,h*.11f,w,60*s),"SCEGLI UN POTENZIAMENTO",Center(Mathf.RoundToInt(27*s))); GUI.color=Color.white;
        GUI.Label(new Rect(0,h*.16f,w,40*s),"Prima del livello "+(level+1),Center(Mathf.RoundToInt(17*s)));
        for(int i=0;i<3;i++)
        {
            Rect r=new Rect(w*.075f,h*(.27f+i*.19f),w*.85f,h*.145f); Color border=i==0?new Color(.18f,.82f,.42f):i==1?new Color(1f,.52f,.10f):new Color(.60f,.26f,.92f);
            Fill(r,new Color(.025f,.055f,.12f,.99f)); Stroke(r,border,3); GUI.color=Gold(); GUI.Label(new Rect(r.x+20*s,r.y+9*s,r.width-40*s,42*s),upName[i],Left(Mathf.RoundToInt(21*s))); GUI.color=Color.white; GUI.Label(new Rect(r.x+20*s,r.y+50*s,r.width-40*s,45*s),upDesc[i],Left(Mathf.RoundToInt(16*s)));
            if(GUI.Button(r,"",TransparentButton())) ApplyUpgrade(upId[i]);
        }
    }

    void DrawEnd(int w,int h,float s,bool win)
    {
        DrawPanel(w,h); GUI.color=Gold(); GUI.Label(new Rect(0,h*.19f,w,90*s),win?"ATTICA LIBERATA":"SCONFITTA",Center(Mathf.RoundToInt(44*s))); GUI.color=Color.white;
        GUI.Label(new Rect(0,h*.34f,w,70*s),win?"Hai completato i primi 5 livelli":"Il mito continua",Center(Mathf.RoundToInt(21*s))); GUI.Label(new Rect(0,h*.42f,w,50*s),"Nemici sconfitti: "+kills+"   ◆ "+coins,Center(Mathf.RoundToInt(17*s)));
        if(GUI.Button(new Rect(w*.15f,h*.59f,w*.70f,76*s),"RIPROVA",Button(Mathf.RoundToInt(23*s)))) StartRun();
        if(GUI.Button(new Rect(w*.22f,h*.72f,w*.56f,64*s),"MENU",Button(Mathf.RoundToInt(20*s)))) { state=AppState.Menu; Haptic(); }
    }

    void DrawPanel(int w,int h)
    {
        Fill(new Rect(0,0,w,h),new Color(.004f,.015f,.050f));
        for(int i=0;i<8;i++) Fill(new Rect(0,h*i/8f,w,h/8f+1),new Color(.02f,.10f,.22f,.04f+i*.008f));
        Fill(new Rect(0,h*.025f,w,3),Gold()); Fill(new Rect(0,h*.965f,w,3),Gold());
    }

    void DrawTextureCentered(Texture2D tex,Vector2 center,float width,float height)
    {
        if(tex==null) { DrawCircle(center,width*.30f,new Color(.12f,.24f,.52f)); return; }
        Color old=GUI.color; GUI.color=Color.white; GUI.DrawTexture(new Rect(center.x-width*.5f,center.y-height*.5f,width,height),tex,ScaleMode.ScaleToFit,true); GUI.color=old;
    }

    void DrawCircle(Vector2 p,float r,Color c)
    {
        Color old=GUI.color; GUI.color=c; GUI.DrawTexture(new Rect(p.x-r,p.y-r,r*2,r*2),circle); GUI.color=old;
    }

    void Fill(Rect r,Color c) { Color old=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=old; }
    void Stroke(Rect r,Color c,float t) { Fill(new Rect(r.x,r.y,r.width,t),c); Fill(new Rect(r.x,r.yMax-t,r.width,t),c); Fill(new Rect(r.x,r.y,t,r.height),c); Fill(new Rect(r.xMax-t,r.y,t,r.height),c); }
    Color Gold() => new Color(.94f,.67f,.16f);

    GUIStyle TransparentButton()
    {
        GUIStyle s=new GUIStyle(GUI.skin.button); s.normal.background=null; s.hover.background=null; s.active.background=null; s.normal.textColor=Color.clear; s.hover.textColor=Color.clear; s.active.textColor=Color.clear; s.border=new RectOffset(0,0,0,0); return s;
    }
    GUIStyle Center(int z) { GUIStyle s=new GUIStyle(GUI.skin.label); s.alignment=TextAnchor.MiddleCenter; s.fontSize=z; s.fontStyle=FontStyle.Bold; s.wordWrap=true; s.normal.textColor=Color.white; return s; }
    GUIStyle Left(int z) { GUIStyle s=Center(z); s.alignment=TextAnchor.MiddleLeft; return s; }
    GUIStyle Right(int z) { GUIStyle s=Center(z); s.alignment=TextAnchor.MiddleRight; return s; }
    GUIStyle Button(int z) { GUIStyle s=new GUIStyle(GUI.skin.button); s.fontSize=z; s.fontStyle=FontStyle.Bold; s.alignment=TextAnchor.MiddleCenter; s.normal.textColor=Color.white; s.hover.textColor=Color.white; s.active.textColor=Gold(); return s; }
}
