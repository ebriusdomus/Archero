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
        public float hp, maxHp, speed, radius, phase;
        public EnemyType type;
        public float hitFlash;
    }

    sealed class Shot
    {
        public Vector2 p, v;
        public int pierce;
    }

    AppState state = AppState.Menu;
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();
    Texture2D circle, menu, perseo, assassin, gorgon, hoplite, minotaur;
    Vector2 hero = new Vector2(.50f, .78f), dragStart, dragNow, moveInput;
    bool dragging, vibration = true, sound = true;
    float heroHp = 520f, heroMaxHp = 520f, moveSpeed = .27f, shotDamage = 58f, fireInterval = .42f;
    int multishot = 1, pierce, level = 1, kills, coins = 224;
    float nextShot, hurtCooldown;
    readonly string[] upName = new string[3];
    readonly string[] upDesc = new string[3];
    readonly int[] upId = new int[3];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBoot()
    {
        if (FindFirstObjectByType<MythbreakerBootstrap>() == null)
            new GameObject("MYTHBREAKER BOOT 0.9").AddComponent<MythbreakerBootstrap>();
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
        if (t == null) return null;
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
        catch { return null; }
    }

    Texture2D MakeCircle(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color[] px = new Color[n * n];
        float c = (n - 1) * .5f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float a = Mathf.Clamp01(c - Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) + 1.25f);
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
            candidate.x = Mathf.Clamp(candidate.x, .13f, .87f);
            candidate.y = Mathf.Clamp(candidate.y, .24f, .82f);
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
        dragging = true; dragStart = dragNow = p; moveInput = Vector2.zero;
    }

    void ContinueDrag(Vector2 p)
    {
        if (!dragging) return;
        dragNow = p;
        float r = Mathf.Max(90f, Screen.width * .16f);
        Vector2 raw = Vector2.ClampMagnitude(dragNow - dragStart, r) / r;
        float mag = raw.magnitude;
        if (mag < .10f) moveInput = Vector2.zero;
        else moveInput = raw.normalized * Mathf.Pow(Mathf.InverseLerp(.10f, 1f, mag), 1.45f);
    }

    void EndDrag() { dragging = false; moveInput = Vector2.zero; }
    Rect ArenaRect() => new Rect(Screen.width * .035f, Screen.height * .125f, Screen.width * .93f, Screen.height * .725f);

    void StartRun()
    {
        heroHp = heroMaxHp = 520f; moveSpeed = .27f; shotDamage = 58f; fireInterval = .42f;
        multishot = 1; pierce = 0; level = 1; kills = 0; coins = 224;
        state = AppState.Playing; SpawnLevel(); Haptic();
    }

    void SpawnLevel()
    {
        enemies.Clear(); shots.Clear(); EndDrag(); hero = new Vector2(.50f, .78f); nextShot = hurtCooldown = 0f;
        if (level == 1) { AddEnemy(EnemyType.Assassin,.29f,.31f); AddEnemy(EnemyType.Hoplite,.50f,.26f); AddEnemy(EnemyType.Assassin,.71f,.31f); }
        else if (level == 2) { AddEnemy(EnemyType.Gorgon,.28f,.30f); AddEnemy(EnemyType.Gorgon,.72f,.30f); AddEnemy(EnemyType.Assassin,.50f,.38f); }
        else if (level == 3) { AddEnemy(EnemyType.Hoplite,.26f,.29f); AddEnemy(EnemyType.Hoplite,.50f,.25f); AddEnemy(EnemyType.Hoplite,.74f,.29f); AddEnemy(EnemyType.Assassin,.50f,.40f); }
        else if (level == 4) { AddEnemy(EnemyType.Gorgon,.25f,.28f); AddEnemy(EnemyType.Gorgon,.50f,.24f); AddEnemy(EnemyType.Gorgon,.75f,.28f); AddEnemy(EnemyType.Hoplite,.36f,.42f); AddEnemy(EnemyType.Hoplite,.64f,.42f); }
        else { AddEnemy(EnemyType.Minotaur,.50f,.29f); AddEnemy(EnemyType.Hoplite,.27f,.39f); AddEnemy(EnemyType.Hoplite,.73f,.39f); }
    }

    void AddEnemy(EnemyType type, float x, float y)
    {
        float hp = type == EnemyType.Assassin ? 95f + level*12f : type == EnemyType.Gorgon ? 125f + level*14f : type == EnemyType.Hoplite ? 165f + level*18f : 1050f;
        float sp = type == EnemyType.Assassin ? .043f : type == EnemyType.Gorgon ? .035f : type == EnemyType.Hoplite ? .029f : .034f;
        float r = type == EnemyType.Minotaur ? .082f : type == EnemyType.Hoplite ? .047f : .041f;
        enemies.Add(new Enemy { p=new Vector2(x,y), hp=hp, maxHp=hp, speed=sp, radius=r, type=type, phase=enemies.Count*1.7f });
    }

    void UpdateCombat()
    {
        float dt = Time.deltaTime;
        for (int i=enemies.Count-1;i>=0;i--)
        {
            Enemy e=enemies[i]; e.hitFlash=Mathf.Max(0f,e.hitFlash-dt*5f);
            Vector2 toHero=hero-e.p;
            if (toHero.sqrMagnitude>.0001f)
            {
                Vector2 dir=toHero.normalized;
                if (e.type==EnemyType.Gorgon) { Vector2 side=new Vector2(-dir.y,dir.x); dir=(dir+side*Mathf.Sin(Time.time*4f+e.phase)*.26f).normalized; }
                float boost=e.type==EnemyType.Minotaur && Mathf.Sin(Time.time*2f)>.78f ? 1.65f : 1f;
                Vector2 np=e.p+dir*e.speed*boost*dt; if(!PointBlocked(np)) e.p=np;
            }
            if (Vector2.Distance(hero,e.p)<e.radius+.040f && Time.time>=hurtCooldown)
            {
                heroHp -= e.type==EnemyType.Minotaur ? 60f : e.type==EnemyType.Hoplite ? 32f : 23f;
                hurtCooldown=Time.time+.72f; Haptic();
                if(heroHp<=0f){heroHp=0f;state=AppState.GameOver;EndDrag();return;}
            }
        }

        if(!dragging && enemies.Count>0 && Time.time>=nextShot){nextShot=Time.time+fireInterval;FireAtNearest();}
        for(int s=shots.Count-1;s>=0;s--)
        {
            Shot sh=shots[s]; sh.p+=sh.v*dt;
            bool remove=sh.p.x<.04f||sh.p.x>.96f||sh.p.y<.12f||sh.p.y>.90f;
            if(!remove)
            {
                for(int i=enemies.Count-1;i>=0;i--)
                {
                    Enemy e=enemies[i];
                    if(Vector2.Distance(sh.p,e.p)<e.radius+.018f)
                    {
                        e.hp-=shotDamage;e.hitFlash=1f;
                        if(e.hp<=0f){enemies.RemoveAt(i);kills++;coins+=e.type==EnemyType.Minotaur?50:5;}
                        if(sh.pierce>0)sh.pierce--;else remove=true;break;
                    }
                }
            }
            if(remove)shots.RemoveAt(s);
        }
        if(enemies.Count==0){EndDrag();if(level>=5)state=AppState.Victory;else{PrepareUpgrades();state=AppState.Upgrade;}}
    }

    void FireAtNearest()
    {
        Enemy nearest=enemies[0];float best=(nearest.p-hero).sqrMagnitude;
        for(int i=1;i<enemies.Count;i++){float d=(enemies[i].p-hero).sqrMagnitude;if(d<best){best=d;nearest=enemies[i];}}
        Vector2 dir=(nearest.p-hero).normalized;
        if(multishot==1)shots.Add(new Shot{p=hero,v=dir*.72f,pierce=pierce});
        else
        {
            float spread=multishot==2?7f:11f;
            for(int i=0;i<multishot;i++){float t=i/(float)(multishot-1);shots.Add(new Shot{p=hero,v=Rotate(dir,Mathf.Lerp(-spread,spread,t)*Mathf.Deg2Rad)*.72f,pierce=pierce});}
        }
    }

    Vector2 Rotate(Vector2 v,float a){float c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(v.x*c-v.y*s,v.x*s+v.y*c);}

    void PrepareUpgrades()
    {
        int seed=level*19+kills*3;
        for(int i=0;i<3;i++)
        {
            int id=(seed+i*2)%6;while(i>0&&(id==upId[0]||(i>1&&id==upId[1])))id=(id+1)%6;upId[i]=id;
            if(id==0){upName[i]="TIRO RAPIDO";upDesc[i]="+15% velocità di attacco";}
            else if(id==1){upName[i]="POTENZA";upDesc[i]="+18 danni";}
            else if(id==2){upName[i]="ERMES";upDesc[i]="+8% velocità movimento";}
            else if(id==3){upName[i]="VITALITÀ";upDesc[i]="+80 HP e cura";}
            else if(id==4){upName[i]="MULTISHOT";upDesc[i]="+1 proiettile";}
            else{upName[i]="PERFORANTE";upDesc[i]="+1 bersaglio attraversato";}
        }
    }

    void ApplyUpgrade(int id)
    {
        if(id==0)fireInterval=Mathf.Max(.22f,fireInterval*.85f);else if(id==1)shotDamage+=18f;else if(id==2)moveSpeed=Mathf.Min(.34f,moveSpeed*1.08f);
        else if(id==3){heroMaxHp+=80f;heroHp=Mathf.Min(heroMaxHp,heroHp+110f);}else if(id==4)multishot=Mathf.Min(3,multishot+1);else pierce=Mathf.Min(2,pierce+1);
        level++;state=AppState.Playing;SpawnLevel();Haptic();
    }

    bool PointBlocked(Vector2 p)
    {
        Rect[] rr=Obstacles(level);
        for(int i=0;i<rr.Length;i++){Rect x=rr[i];x.xMin-=.018f;x.xMax+=.018f;x.yMin-=.014f;x.yMax+=.014f;if(x.Contains(p))return true;}return false;
    }

    Rect[] Obstacles(int l)
    {
        if(l==1)return new[]{new Rect(.18f,.50f,.14f,.085f),new Rect(.68f,.50f,.14f,.085f)};
        if(l==2)return new[]{new Rect(.16f,.50f,.13f,.085f),new Rect(.71f,.50f,.13f,.085f),new Rect(.44f,.44f,.12f,.075f)};
        if(l==3)return new[]{new Rect(.22f,.49f,.13f,.085f),new Rect(.65f,.49f,.13f,.085f),new Rect(.44f,.63f,.12f,.08f)};
        if(l==4)return new[]{new Rect(.14f,.46f,.12f,.09f),new Rect(.74f,.46f,.12f,.09f),new Rect(.31f,.62f,.12f,.08f),new Rect(.57f,.62f,.12f,.08f)};
        return new[]{new Rect(.15f,.58f,.13f,.085f),new Rect(.72f,.58f,.13f,.085f)};
    }

    void Haptic(){if(vibration)try{Handheld.Vibrate();}catch{}}

    void OnGUI()
    {
        int w=Screen.width,h=Screen.height;float s=Mathf.Clamp(w/720f,.72f,1.8f);
        if(state==AppState.Menu)DrawMenu(w,h,s);else if(state==AppState.Heroes)DrawHeroes(w,h,s);else if(state==AppState.Settings)DrawSettings(w,h,s);else if(state==AppState.Playing)DrawGame(w,h,s);else if(state==AppState.Upgrade)DrawUpgrade(w,h,s);else DrawEnd(w,h,s,state==AppState.Victory);
    }

    void DrawMenu(int w,int h,float s)
    {
        Fill(new Rect(0,0,w,h),new Color(.004f,.01f,.035f));
        if(menu!=null){float img=menu.width/(float)menu.height,scr=w/(float)h;Rect r;if(scr>img){float hh=w/img;r=new Rect(0,(h-hh)*.5f,w,hh);}else{float ww=h*img;r=new Rect((w-ww)*.5f,0,ww,h);}GUI.color=Color.white;GUI.DrawTexture(r,menu,ScaleMode.ScaleAndCrop);}
        GUIStyle invisible=TransparentButton();if(GUI.Button(new Rect(w*.12f,h*.765f,w*.76f,h*.12f),"",invisible))StartRun();
        if(GUI.Button(new Rect(0,h*.895f,w*.49f,h*.10f),"",invisible)){state=AppState.Heroes;Haptic();}if(GUI.Button(new Rect(w*.51f,h*.895f,w*.49f,h*.10f),"",invisible)){state=AppState.Settings;Haptic();}
        GUI.color=new Color(1,1,1,.90f);GUI.Label(new Rect(w*.77f,h*.012f,w*.19f,28*s),"v0.9",Right(Mathf.RoundToInt(13*s)));GUI.color=Color.white;
    }

    void DrawHeroes(int w,int h,float s)
    {
        DrawPanel(w,h);GUI.color=Gold();GUI.Label(new Rect(0,h*.07f,w,70*s),"EROI",Center(Mathf.RoundToInt(40*s)));GUI.color=Color.white;GUI.Label(new Rect(0,h*.13f,w,42*s),"Campioni della Grecia",Center(Mathf.RoundToInt(18*s)));
        Rect card=new Rect(w*.09f,h*.23f,w*.82f,h*.30f);Fill(card,new Color(.04f,.09f,.19f,.98f));Stroke(card,Gold(),3);DrawTextureShadowed(perseo,new Vector2(w*.28f,h*.375f),w*.28f,w*.34f,0f);
        GUI.color=Gold();GUI.Label(new Rect(w*.43f,h*.27f,w*.42f,42*s),"PERSEO",Left(Mathf.RoundToInt(28*s)));GUI.color=Color.white;GUI.Label(new Rect(w*.43f,h*.33f,w*.42f,115*s),"Arma: Lancia divina\nVeloce • preciso\nEroe iniziale",Left(Mathf.RoundToInt(17*s)));
        GUI.color=new Color(.72f,.78f,.88f);GUI.Label(new Rect(w*.10f,h*.59f,w*.80f,100*s),"ERACLE • ATALANTA • ACHILLE\nsi sbloccano proseguendo l'avventura",Center(Mathf.RoundToInt(17*s)));GUI.color=Color.white;
        if(GUI.Button(new Rect(w*.20f,h*.82f,w*.60f,68*s),"INDIETRO",Button(Mathf.RoundToInt(21*s)))){state=AppState.Menu;Haptic();}
    }

    void DrawSettings(int w,int h,float s)
    {
        DrawPanel(w,h);GUI.color=Gold();GUI.Label(new Rect(0,h*.07f,w,70*s),"IMPOSTAZIONI",Center(Mathf.RoundToInt(36*s)));GUI.color=Color.white;Setting(w,h,s,.27f,"VIBRAZIONE",ref vibration);Setting(w,h,s,.41f,"AUDIO",ref sound);
        GUI.Label(new Rect(w*.10f,h*.58f,w*.80f,110*s),"60 FPS • verticale\nMovimento morbido a un dito\nAuto-attacco da fermo",Center(Mathf.RoundToInt(18*s)));if(GUI.Button(new Rect(w*.20f,h*.82f,w*.60f,68*s),"INDIETRO",Button(Mathf.RoundToInt(21*s)))){state=AppState.Menu;Haptic();}
    }

    void Setting(int w,int h,float s,float y,string label,ref bool value)
    {
        Rect r=new Rect(w*.11f,h*y,w*.78f,72*s);Fill(r,new Color(.03f,.07f,.15f,.98f));Stroke(r,new Color(.30f,.42f,.60f),2);GUI.Label(new Rect(r.x+20*s,r.y,r.width*.60f,r.height),label,Left(Mathf.RoundToInt(20*s)));
        GUI.color=value?new Color(.16f,.82f,.42f):new Color(.56f,.58f,.64f);GUI.Label(new Rect(r.x+r.width*.63f,r.y,r.width*.30f,r.height),value?"ON":"OFF",Right(Mathf.RoundToInt(20*s)));GUI.color=Color.white;if(GUI.Button(r,"",TransparentButton())){value=!value;Haptic();}
    }

    void DrawGame(int w,int h,float s)
    {
        DrawArena(w,h,s);DrawHud(w,h,s);DrawTextureShadowed(perseo,new Vector2(hero.x*w,hero.y*h),w*.175f,w*.205f,-w*.003f);
        for(int i=0;i<enemies.Count;i++)DrawEnemy(enemies[i],w,h,s);for(int i=0;i<shots.Count;i++)DrawProjectile(shots[i].p,w,h);
        if(dragging){float r=Mathf.Max(90,w*.16f);DrawCircle(dragStart,r,new Color(.01f,.04f,.12f,.28f));StrokeCircle(dragStart,r,new Color(.85f,.75f,.50f,.34f),3f);Vector2 k=dragStart+Vector2.ClampMagnitude(dragNow-dragStart,r);DrawCircle(k,r*.33f,new Color(.08f,.45f,1f,.80f));DrawCircle(k,r*.12f,new Color(.72f,.90f,1f,.95f));}
    }

    void DrawArena(int w,int h,float s)
    {
        Color outer,grass,path,stone,accent;
        if(level==1){outer=new Color(.055f,.075f,.055f);grass=new Color(.28f,.38f,.18f);path=new Color(.60f,.52f,.36f);stone=new Color(.55f,.49f,.39f);accent=new Color(1f,.48f,.08f);}
        else if(level==2){outer=new Color(.025f,.095f,.045f);grass=new Color(.34f,.50f,.18f);path=new Color(.64f,.58f,.37f);stone=new Color(.62f,.56f,.43f);accent=new Color(1f,.50f,.07f);}
        else if(level==3){outer=new Color(.025f,.06f,.09f);grass=new Color(.14f,.24f,.24f);path=new Color(.34f,.40f,.42f);stone=new Color(.38f,.43f,.44f);accent=new Color(.08f,.58f,1f);}
        else if(level==4){outer=new Color(.055f,.025f,.085f);grass=new Color(.15f,.12f,.18f);path=new Color(.30f,.25f,.33f);stone=new Color(.34f,.29f,.39f);accent=new Color(.58f,.18f,1f);}
        else{outer=new Color(.10f,.025f,.015f);grass=new Color(.18f,.08f,.035f);path=new Color(.36f,.21f,.13f);stone=new Color(.39f,.27f,.20f);accent=new Color(1f,.24f,.035f);}
        Fill(new Rect(0,0,w,h),outer);Rect a=ArenaRect();Fill(a,grass);Rect lane=new Rect(a.x+a.width*.18f,a.y,a.width*.64f,a.height);Fill(lane,path);
        for(int i=0;i<11;i++){float px=lane.x+lane.width*(.10f+((i*37)%80)/100f),py=a.y+a.height*(.05f+i*.085f),sw=w*(.065f+(i%3)*.012f),sh=h*(.026f+(i%2)*.010f);Fill(new Rect(px-sw*.5f,py-sh*.5f,sw,sh),new Color(stone.r,stone.g,stone.b,.52f));}
        for(int i=0;i<18;i++){float yy=a.y+a.height*(.035f+i*.054f),rr=w*(.030f+(i%4)*.005f);Color leaf=i%2==0?new Color(grass.r*.65f,grass.g*.82f,grass.b*.55f):new Color(grass.r*.50f,grass.g*.68f,grass.b*.45f);DrawCircle(new Vector2(a.x+rr*.75f,yy),rr,leaf);DrawCircle(new Vector2(a.xMax-rr*.75f,yy),rr,leaf);}
        DrawTempleTop(a,w,h,stone,accent);Rect[] obs=Obstacles(level);for(int i=0;i<obs.Length;i++)DrawObstacle3D(obs[i],w,h,stone);
        Vector2 med=new Vector2(w*.50f,h*.46f);DrawCircle(med,w*.19f,new Color(.18f,.12f,.06f,.18f));StrokeCircle(med,w*.19f,new Color(.90f,.69f,.32f,.24f),2f);StrokeCircle(med,w*.13f,new Color(.90f,.69f,.32f,.18f),2f);
        Stroke(a,Gold(),2f);GUI.color=Gold();GUI.Label(new Rect(0,h*.850f,w,34*s),LevelName(),Center(Mathf.RoundToInt(15*s)));GUI.color=Color.white;
    }

    string LevelName(){if(level==1)return"ROVINE DI ATENE";if(level==2)return"FORESTA SACRA";if(level==3)return"COSTA DI SALAMINA";if(level==4)return"FORTEZZA DI MEGARA";return"LABIRINTO DEL MINOTAURO";}

    void DrawTempleTop(Rect a,int w,int h,Color stone,Color accent)
    {
        float topH=a.height*.10f;Fill(new Rect(a.x,a.y,a.width,topH),new Color(stone.r*.62f,stone.g*.62f,stone.b*.62f));Rect gate=new Rect(w*.39f,a.y+h*.008f,w*.22f,topH*.78f);Fill(gate,new Color(.055f,.045f,.035f));Stroke(gate,new Color(stone.r*.95f,stone.g*.92f,stone.b*.82f),3f);
        for(int side=0;side<2;side++){float x=side==0?a.x+w*.075f:a.xMax-w*.075f;DrawColumn3D(new Rect(x-w*.025f,a.y+h*.008f,w*.05f,topH*.90f),stone);DrawTorch(new Vector2(x+(side==0?w*.055f:-w*.055f),a.y+topH*.60f),w,accent);}
    }

    void DrawColumn3D(Rect r,Color stone)
    {
        Fill(new Rect(r.x+4,r.y+7,r.width,r.height),new Color(0,0,0,.24f));Fill(r,stone);Fill(new Rect(r.x+r.width*.18f,r.y,r.width*.18f,r.height),new Color(1,1,1,.10f));Fill(new Rect(r.x-r.width*.14f,r.y,r.width*1.28f,r.height*.12f),new Color(stone.r*1.08f,stone.g*1.08f,stone.b*1.08f));Fill(new Rect(r.x-r.width*.12f,r.y+r.height*.86f,r.width*1.24f,r.height*.14f),new Color(stone.r*.82f,stone.g*.82f,stone.b*.82f));
    }

    void DrawTorch(Vector2 p,int w,Color flame){DrawCircle(p,w*.028f,new Color(flame.r,flame.g,flame.b,.18f));DrawCircle(p,w*.015f,flame);DrawCircle(new Vector2(p.x,p.y-w*.006f),w*.006f,new Color(1f,.90f,.45f));}

    void DrawObstacle3D(Rect n,int w,int h,Color stone)
    {
        Rect r=new Rect(n.x*w,n.y*h,n.width*w,n.height*h);Fill(new Rect(r.x+7,r.y+10,r.width,r.height),new Color(0,0,0,.28f));Fill(r,stone);Fill(new Rect(r.x,r.y,r.width,r.height*.18f),new Color(1,1,1,.10f));Stroke(r,new Color(.78f,.68f,.48f),2f);Stroke(new Rect(r.x+r.width*.18f,r.y+r.height*.18f,r.width*.64f,r.height*.64f),new Color(.25f,.18f,.10f,.35f),2f);
    }

    void DrawHud(int w,int h,float s)
    {
        Fill(new Rect(0,0,w,h*.115f),new Color(.004f,.010f,.028f,.98f));Fill(new Rect(0,h*.112f,w,2),Gold());GUI.color=Color.white;GUI.Label(new Rect(w*.035f,h*.010f,w*.25f,36*s),"PERSEO",Left(Mathf.RoundToInt(19*s)));GUI.color=Gold();GUI.Label(new Rect(w*.28f,h*.010f,w*.50f,36*s),"ATTICA • LIVELLO "+level,Center(Mathf.RoundToInt(18*s)));GUI.color=Color.white;GUI.Label(new Rect(w*.79f,h*.010f,w*.17f,36*s),"◆ "+coins,Right(Mathf.RoundToInt(17*s)));
        Rect hp=new Rect(w*.07f,h*.064f,w*.70f,12*s);Fill(hp,new Color(.07f,.05f,.04f));Fill(new Rect(hp.x,hp.y,hp.width*Mathf.Clamp01(heroHp/heroMaxHp),hp.height),new Color(.08f,.78f,.25f));Stroke(hp,new Color(.83f,.69f,.38f),1.5f);GUI.Label(new Rect(w*.76f,h*.051f,w*.20f,30*s),Mathf.CeilToInt(heroHp)+"/"+Mathf.CeilToInt(heroMaxHp),Right(Mathf.RoundToInt(13*s)));
    }

    void DrawEnemy(Enemy e,int w,int h,float s)
    {
        Texture2D t=e.type==EnemyType.Assassin?assassin:e.type==EnemyType.Gorgon?gorgon:e.type==EnemyType.Hoplite?hoplite:minotaur;Vector2 c=new Vector2(e.p.x*w,e.p.y*h);
        float ww=e.type==EnemyType.Minotaur?w*.29f:e.type==EnemyType.Gorgon?w*.18f:e.type==EnemyType.Hoplite?w*.17f:w*.15f;float hh=e.type==EnemyType.Minotaur?w*.32f:e.type==EnemyType.Gorgon?w*.21f:e.type==EnemyType.Hoplite?w*.20f:w*.18f;
        DrawTextureShadowed(t,c,ww,hh,e.hitFlash);float barW=e.type==EnemyType.Minotaur?w*.28f:ww*.76f;Rect bar=new Rect(c.x-barW*.5f,c.y-hh*.55f,barW,Mathf.Max(4,6*s));Fill(bar,new Color(.07f,.02f,.015f));Fill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(e.hp/e.maxHp),bar.height),new Color(.88f,.10f,.045f));Stroke(bar,new Color(.22f,.06f,.025f),1f);
    }

    void DrawProjectile(Vector2 p,int w,int h)
    {
        Vector2 c=new Vector2(p.x*w,p.y*h);float r=w*.010f;DrawCircle(new Vector2(c.x,c.y+r*2.5f),r*1.5f,new Color(.02f,.25f,1f,.20f));DrawCircle(new Vector2(c.x,c.y+r*1.3f),r*1.8f,new Color(.04f,.44f,1f,.25f));DrawCircle(c,r*2.2f,new Color(.05f,.50f,1f,.24f));DrawCircle(c,r*1.15f,new Color(.18f,.74f,1f,.92f));DrawCircle(c,r*.48f,Color.white);
    }

    void DrawUpgrade(int w,int h,float s)
    {
        DrawGame(w,h,s);Fill(new Rect(0,0,w,h),new Color(.002f,.008f,.025f,.88f));GUI.color=Gold();GUI.Label(new Rect(0,h*.11f,w,60*s),"SCEGLI UN POTENZIAMENTO",Center(Mathf.RoundToInt(27*s)));GUI.color=Color.white;GUI.Label(new Rect(0,h*.16f,w,40*s),"Prima del livello "+(level+1),Center(Mathf.RoundToInt(17*s)));
        for(int i=0;i<3;i++){Rect r=new Rect(w*.075f,h*(.27f+i*.19f),w*.85f,h*.145f);Color border=i==0?new Color(.18f,.82f,.42f):i==1?new Color(1f,.52f,.10f):new Color(.60f,.26f,.92f);Fill(r,new Color(.025f,.055f,.12f,.99f));Stroke(r,border,3);GUI.color=Gold();GUI.Label(new Rect(r.x+20*s,r.y+9*s,r.width-40*s,42*s),upName[i],Left(Mathf.RoundToInt(21*s)));GUI.color=Color.white;GUI.Label(new Rect(r.x+20*s,r.y+50*s,r.width-40*s,45*s),upDesc[i],Left(Mathf.RoundToInt(16*s)));if(GUI.Button(r,"",TransparentButton()))ApplyUpgrade(upId[i]);}
    }

    void DrawEnd(int w,int h,float s,bool win)
    {
        DrawPanel(w,h);GUI.color=Gold();GUI.Label(new Rect(0,h*.19f,w,90*s),win?"ATTICA LIBERATA":"SCONFITTA",Center(Mathf.RoundToInt(44*s)));GUI.color=Color.white;GUI.Label(new Rect(0,h*.34f,w,70*s),win?"Hai completato i primi 5 livelli":"Il mito continua",Center(Mathf.RoundToInt(21*s)));GUI.Label(new Rect(0,h*.42f,w,50*s),"Nemici sconfitti: "+kills+"   ◆ "+coins,Center(Mathf.RoundToInt(17*s)));
        if(GUI.Button(new Rect(w*.15f,h*.59f,w*.70f,76*s),"RIPROVA",Button(Mathf.RoundToInt(23*s))))StartRun();if(GUI.Button(new Rect(w*.22f,h*.72f,w*.56f,64*s),"MENU",Button(Mathf.RoundToInt(20*s)))){state=AppState.Menu;Haptic();}
    }

    void DrawPanel(int w,int h){Fill(new Rect(0,0,w,h),new Color(.004f,.015f,.050f));for(int i=0;i<8;i++)Fill(new Rect(0,h*i/8f,w,h/8f+1),new Color(.02f,.10f,.22f,.04f+i*.008f));Fill(new Rect(0,h*.025f,w,3),Gold());Fill(new Rect(0,h*.965f,w,3),Gold());}

    void DrawTextureShadowed(Texture2D tex,Vector2 center,float width,float height,float flash)
    {
        DrawCircle(new Vector2(center.x+width*.02f,center.y+height*.30f),width*.30f,new Color(0,0,0,.24f));if(tex==null){DrawCircle(center,width*.26f,new Color(.12f,.24f,.52f));return;}Color old=GUI.color;GUI.color=flash>0f?Color.Lerp(Color.white,new Color(1f,.45f,.35f),Mathf.Clamp01(flash)):Color.white;GUI.DrawTexture(new Rect(center.x-width*.5f,center.y-height*.5f,width,height),tex,ScaleMode.ScaleToFit,true);GUI.color=old;
    }

    void DrawCircle(Vector2 p,float r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(p.x-r,p.y-r,r*2,r*2),circle);GUI.color=old;}
    void StrokeCircle(Vector2 p,float r,Color c,float t){for(int i=0;i<18;i++){float a0=i*Mathf.PI*2f/18f;Vector2 q=p+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*r;DrawCircle(q,t,c);}}
    void Fill(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    void Stroke(Rect r,Color c,float t){Fill(new Rect(r.x,r.y,r.width,t),c);Fill(new Rect(r.x,r.yMax-t,r.width,t),c);Fill(new Rect(r.x,r.y,t,r.height),c);Fill(new Rect(r.xMax-t,r.y,t,r.height),c);}
    Color Gold()=>new Color(.94f,.67f,.16f);
    GUIStyle TransparentButton(){GUIStyle s=new GUIStyle(GUI.skin.button);s.normal.background=null;s.hover.background=null;s.active.background=null;s.normal.textColor=Color.clear;s.hover.textColor=Color.clear;s.active.textColor=Color.clear;s.border=new RectOffset(0,0,0,0);return s;}
    GUIStyle Center(int z){GUIStyle s=new GUIStyle(GUI.skin.label);s.alignment=TextAnchor.MiddleCenter;s.fontSize=z;s.fontStyle=FontStyle.Bold;s.wordWrap=true;s.normal.textColor=Color.white;return s;}
    GUIStyle Left(int z){GUIStyle s=Center(z);s.alignment=TextAnchor.MiddleLeft;return s;}
    GUIStyle Right(int z){GUIStyle s=Center(z);s.alignment=TextAnchor.MiddleRight;return s;}
    GUIStyle Button(int z){GUIStyle s=new GUIStyle(GUI.skin.button);s.fontSize=z;s.fontStyle=FontStyle.Bold;s.alignment=TextAnchor.MiddleCenter;s.normal.textColor=Color.white;s.hover.textColor=Color.white;s.active.textColor=Gold();return s;}
}
