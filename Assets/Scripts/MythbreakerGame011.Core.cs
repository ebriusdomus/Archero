using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-20000)]
public sealed partial class MythbreakerGame011 : MonoBehaviour
{
    enum State { Menu, Heroes, Settings, Play, Upgrade, Win, Lose }
    enum EType { Assassin, Gorgon, Hoplite, Minotaur }
    sealed class Enemy { public Vector2 p; public float hp,maxHp,speed,radius,phase,flash; public EType type; }
    sealed class Shot { public Vector2 p,v; public int pierce; }

    State state=State.Menu;
    readonly List<Enemy> enemies=new List<Enemy>();
    readonly List<Shot> shots=new List<Shot>();
    readonly Texture2D[] bg=new Texture2D[5];
    readonly string[] upName=new string[3], upDesc=new string[3];
    readonly int[] upId=new int[3];

    Texture2D circle,menu,perseo,assassin,gorgon,hoplite,minotaur;
    Vector2 hero=new Vector2(.5f,.73f),dragStart,dragNow,move;
    bool dragging,vibration=true,sound=true;
    float hp=520,maxHp=520,moveSpeed=.22f,damage=58,fireRate=.42f,nextShot,hurtCd;
    int multishot=1,pierce,level=1,kills,coins=224;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ if(FindFirstObjectByType<MythbreakerGame011>()==null) new GameObject("MYTHBREAKER 0.11").AddComponent<MythbreakerGame011>(); }

    void Awake()
    {
        Application.targetFrameRate=60; Screen.sleepTimeout=SleepTimeout.NeverSleep; Screen.orientation=ScreenOrientation.Portrait;
        circle=MakeCircle(64); menu=LoadB64("mythbreaker_menu_b64",false); perseo=LoadB64("perseo_sprite_b64",true); hoplite=LoadB64("hoplite_sprite_b64",true);
        assassin=LoadReal("MythbreakerSprites/assassin","assassin_sprite_b64"); gorgon=LoadReal("MythbreakerSprites/gorgon","gorgon_sprite_b64"); minotaur=LoadReal("MythbreakerSprites/minotaur","minotaur_sprite_b64");
        for(int i=0;i<5;i++) bg[i]=MythbreakerArt011.BuildArena(i+1);
    }

    void Start(){ MythbreakerBootstrap old=FindFirstObjectByType<MythbreakerBootstrap>(); if(old!=null) old.enabled=false; }

    Texture2D LoadReal(string path,string fallback){Texture2D t=Resources.Load<Texture2D>(path);return t!=null?t:LoadB64(fallback,true);}
    Texture2D LoadB64(string name,bool alpha)
    {
        TextAsset a=Resources.Load<TextAsset>(name); if(a==null)return null;
        try{byte[] b=Convert.FromBase64String(a.text.Replace("\r","").Replace("\n","").Replace(" ","").Trim());Texture2D t=new Texture2D(2,2,alpha?TextureFormat.RGBA32:TextureFormat.RGB24,false);if(!t.LoadImage(b,false))return null;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;return t;}catch{return null;}
    }
    Texture2D MakeCircle(int n){Texture2D t=new Texture2D(n,n,TextureFormat.RGBA32,false);Color[] p=new Color[n*n];float c=(n-1)*.5f;for(int y=0;y<n;y++)for(int x=0;x<n;x++){float a=Mathf.Clamp01(c-Vector2.Distance(new Vector2(x,y),new Vector2(c,c))+1.2f);p[y*n+x]=new Color(1,1,1,a);}t.SetPixels(p);t.Apply(false,false);return t;}

    void Update()
    {
        if(state!=State.Play)return;
        ReadInput();
        if(dragging&&move.sqrMagnitude>.001f){Vector2 n=hero+move*moveSpeed*Time.deltaTime;n.x=Mathf.Clamp(n.x,.12f,.88f);n.y=Mathf.Clamp(n.y,.22f,.80f);if(!Blocked(n))hero=n;}
        Combat();
    }

    void ReadInput()
    {
        if(Input.touchCount>0){Touch t=Input.GetTouch(0);Vector2 p=new Vector2(t.position.x,Screen.height-t.position.y);if(t.phase==TouchPhase.Began)Begin(p);else if(t.phase==TouchPhase.Moved||t.phase==TouchPhase.Stationary)Drag(p);else if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)End();return;}
        Vector2 m=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);if(Input.GetMouseButtonDown(0))Begin(m);else if(Input.GetMouseButton(0))Drag(m);else if(Input.GetMouseButtonUp(0))End();
    }
    void Begin(Vector2 p){if(!Arena().Contains(p))return;dragging=true;dragStart=dragNow=p;move=Vector2.zero;}
    void Drag(Vector2 p){if(!dragging)return;dragNow=p;float r=Mathf.Max(105,Screen.width*.18f);Vector2 raw=Vector2.ClampMagnitude(p-dragStart,r)/r;float m=raw.magnitude;move=m<.14f?Vector2.zero:raw.normalized*Mathf.Pow(Mathf.InverseLerp(.14f,1,m),1.72f);}
    void End(){dragging=false;move=Vector2.zero;}
    Rect Arena()=>new Rect(Screen.width*.025f,Screen.height*.108f,Screen.width*.95f,Screen.height*.765f);

    void NewRun(){hp=maxHp=520;moveSpeed=.22f;damage=58;fireRate=.42f;multishot=1;pierce=0;level=1;kills=0;coins=224;state=State.Play;Spawn();Haptic();}
    void Spawn()
    {
        enemies.Clear();shots.Clear();End();hero=new Vector2(.5f,.73f);nextShot=hurtCd=0;
        if(level==1){Add(EType.Assassin,.28f,.31f);Add(EType.Hoplite,.50f,.27f);Add(EType.Assassin,.72f,.31f);}
        else if(level==2){Add(EType.Gorgon,.27f,.30f);Add(EType.Gorgon,.73f,.30f);Add(EType.Assassin,.50f,.39f);}
        else if(level==3){Add(EType.Hoplite,.25f,.29f);Add(EType.Hoplite,.50f,.25f);Add(EType.Hoplite,.75f,.29f);Add(EType.Assassin,.50f,.40f);}
        else if(level==4){Add(EType.Gorgon,.24f,.28f);Add(EType.Gorgon,.50f,.24f);Add(EType.Gorgon,.76f,.28f);Add(EType.Hoplite,.35f,.42f);Add(EType.Hoplite,.65f,.42f);}
        else{Add(EType.Minotaur,.50f,.28f);Add(EType.Hoplite,.26f,.39f);Add(EType.Hoplite,.74f,.39f);}
    }
    void Add(EType t,float x,float y)
    {
        float eh=t==EType.Assassin?95+level*12:t==EType.Gorgon?125+level*14:t==EType.Hoplite?165+level*18:1050;
        float sp=t==EType.Assassin?.040f:t==EType.Gorgon?.033f:t==EType.Hoplite?.027f:.031f;
        float r=t==EType.Minotaur?.085f:t==EType.Hoplite?.047f:.041f;
        enemies.Add(new Enemy{p=new Vector2(x,y),hp=eh,maxHp=eh,speed=sp,radius=r,type=t,phase=enemies.Count*1.7f});
    }

    void Combat()
    {
        float dt=Time.deltaTime;
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Enemy e=enemies[i];e.flash=Mathf.Max(0,e.flash-dt*5);Vector2 d=hero-e.p;
            if(d.sqrMagnitude>.0001f){Vector2 dir=d.normalized;if(e.type==EType.Gorgon){Vector2 side=new Vector2(-dir.y,dir.x);dir=(dir+side*Mathf.Sin(Time.time*4+e.phase)*.26f).normalized;}float boost=e.type==EType.Minotaur&&Mathf.Sin(Time.time*2)>.78f?1.55f:1;Vector2 np=e.p+dir*e.speed*boost*dt;if(!Blocked(np))e.p=np;}
            if(Vector2.Distance(hero,e.p)<e.radius+.042f&&Time.time>=hurtCd){hp-=e.type==EType.Minotaur?60:e.type==EType.Hoplite?32:23;hurtCd=Time.time+.72f;Haptic();if(hp<=0){hp=0;state=State.Lose;End();return;}}
        }
        if(!dragging&&enemies.Count>0&&Time.time>=nextShot){nextShot=Time.time+fireRate;Fire();}
        for(int s=shots.Count-1;s>=0;s--)
        {
            Shot sh=shots[s];sh.p+=sh.v*dt;bool rm=sh.p.x<.03f||sh.p.x>.97f||sh.p.y<.10f||sh.p.y>.90f;
            if(!rm)for(int i=enemies.Count-1;i>=0;i--){Enemy e=enemies[i];if(Vector2.Distance(sh.p,e.p)<e.radius+.018f){e.hp-=damage;e.flash=1;if(e.hp<=0){enemies.RemoveAt(i);kills++;coins+=e.type==EType.Minotaur?50:5;}if(sh.pierce>0)sh.pierce--;else rm=true;break;}}
            if(rm)shots.RemoveAt(s);
        }
        if(enemies.Count==0){End();if(level>=5)state=State.Win;else{PrepareUpgrades();state=State.Upgrade;}}
    }
    void Fire()
    {
        Enemy n=enemies[0];float best=(n.p-hero).sqrMagnitude;for(int i=1;i<enemies.Count;i++){float d=(enemies[i].p-hero).sqrMagnitude;if(d<best){best=d;n=enemies[i];}}
        Vector2 dir=(n.p-hero).normalized;if(multishot==1)shots.Add(new Shot{p=hero,v=dir*.72f,pierce=pierce});else{float spread=multishot==2?7:11;for(int i=0;i<multishot;i++){float t=i/(float)(multishot-1),a=Mathf.Lerp(-spread,spread,t)*Mathf.Deg2Rad;float c=Mathf.Cos(a),s=Mathf.Sin(a);Vector2 v=new Vector2(dir.x*c-dir.y*s,dir.x*s+dir.y*c);shots.Add(new Shot{p=hero,v=v*.72f,pierce=pierce});}}
    }

    void PrepareUpgrades(){int seed=level*19+kills*3;for(int i=0;i<3;i++){int id=(seed+i*2)%6;while(i>0&&(id==upId[0]||(i>1&&id==upId[1])))id=(id+1)%6;upId[i]=id;if(id==0){upName[i]="TIRO RAPIDO";upDesc[i]="+15% velocità attacco";}else if(id==1){upName[i]="POTENZA";upDesc[i]="+18 danni";}else if(id==2){upName[i]="ERMES";upDesc[i]="+8% movimento";}else if(id==3){upName[i]="VITALITÀ";upDesc[i]="+80 HP e cura";}else if(id==4){upName[i]="MULTISHOT";upDesc[i]="+1 proiettile";}else{upName[i]="PERFORANTE";upDesc[i]="+1 bersaglio";}}}
    void Upgrade(int id){if(id==0)fireRate=Mathf.Max(.22f,fireRate*.85f);else if(id==1)damage+=18;else if(id==2)moveSpeed=Mathf.Min(.30f,moveSpeed*1.08f);else if(id==3){maxHp+=80;hp=Mathf.Min(maxHp,hp+110);}else if(id==4)multishot=Mathf.Min(3,multishot+1);else pierce=Mathf.Min(2,pierce+1);level++;state=State.Play;Spawn();Haptic();}

    bool Blocked(Vector2 p){Rect[] a=Obstacles();for(int i=0;i<a.Length;i++){Rect r=a[i];r.xMin-=.018f;r.xMax+=.018f;r.yMin-=.014f;r.yMax+=.014f;if(r.Contains(p))return true;}return false;}
    Rect[] Obstacles(){if(level==1)return new[]{new Rect(.18f,.50f,.14f,.085f),new Rect(.68f,.50f,.14f,.085f)};if(level==2)return new[]{new Rect(.16f,.50f,.13f,.085f),new Rect(.71f,.50f,.13f,.085f),new Rect(.44f,.44f,.12f,.075f)};if(level==3)return new[]{new Rect(.22f,.49f,.13f,.085f),new Rect(.65f,.49f,.13f,.085f),new Rect(.44f,.63f,.12f,.08f)};if(level==4)return new[]{new Rect(.14f,.46f,.12f,.09f),new Rect(.74f,.46f,.12f,.09f),new Rect(.31f,.62f,.12f,.08f),new Rect(.57f,.62f,.12f,.08f)};return new[]{new Rect(.15f,.58f,.13f,.085f),new Rect(.72f,.58f,.13f,.085f)};}
    void Haptic(){if(vibration)try{Handheld.Vibrate();}catch{}}
}
