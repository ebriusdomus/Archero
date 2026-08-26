using UnityEngine;

public sealed partial class MythbreakerGame011 : MonoBehaviour
{
    void OnGUI()
    {
        int w=Screen.width,h=Screen.height;float s=Mathf.Clamp(w/720f,.72f,1.8f);
        if(state==State.Menu)Menu(w,h,s);else if(state==State.Heroes)Heroes(w,h,s);else if(state==State.Settings)Settings(w,h,s);else if(state==State.Play)Game(w,h,s);else if(state==State.Upgrade)UpgradeScreen(w,h,s);else EndScreen(w,h,s,state==State.Win);
    }

    void Menu(int w,int h,float s)
    {
        Fill(new Rect(0,0,w,h),new Color(.005f,.01f,.03f));if(menu!=null){GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,w,h),menu,ScaleMode.ScaleAndCrop,true);}Fill(new Rect(0,0,w,h*.10f),new Color(0,0,0,.15f));GUI.color=Color.white;GUI.Label(new Rect(w*.79f,h*.012f,w*.17f,28*s),"v0.12",Right((int)(13*s)));
        GUIStyle inv=Invisible();if(GUI.Button(new Rect(w*.10f,h*.72f,w*.80f,h*.17f),"",inv))NewRun();if(GUI.Button(new Rect(0,h*.89f,w*.49f,h*.11f),"",inv)){state=State.Heroes;Haptic();}if(GUI.Button(new Rect(w*.51f,h*.89f,w*.49f,h*.11f),"",inv)){state=State.Settings;Haptic();}
    }
    void Heroes(int w,int h,float s){Panel(w,h);GUI.color=Gold();GUI.Label(new Rect(0,h*.06f,w,60*s),"EROI",Center((int)(38*s)));GUI.color=Color.white;Rect c=new Rect(w*.08f,h*.20f,w*.84f,h*.36f);Fill(c,new Color(.03f,.08f,.16f,.98f));Stroke(c,Gold(),3);Tex(perseo,new Vector2(w*.29f,h*.39f),w*.30f,w*.38f);GUI.color=Gold();GUI.Label(new Rect(w*.46f,h*.27f,w*.40f,44*s),"PERSEO",Left((int)(27*s)));GUI.color=Color.white;GUI.Label(new Rect(w*.46f,h*.33f,w*.40f,120*s),"Lancia divina\nPrecisione • mobilità\nEroe iniziale",Left((int)(17*s)));GUI.Label(new Rect(w*.08f,h*.61f,w*.84f,90*s),"ERACLE • ATALANTA • ACHILLE\nprossimamente",Center((int)(17*s)));if(GUI.Button(new Rect(w*.20f,h*.82f,w*.60f,68*s),"INDIETRO",Btn((int)(21*s))))state=State.Menu;}
    void Settings(int w,int h,float s){Panel(w,h);GUI.color=Gold();GUI.Label(new Rect(0,h*.06f,w,60*s),"IMPOSTAZIONI",Center((int)(34*s)));GUI.color=Color.white;Toggle(w,h,s,.27f,"VIBRAZIONE",ref vibration);Toggle(w,h,s,.41f,"AUDIO",ref sound);GUI.Label(new Rect(w*.08f,h*.58f,w*.84f,110*s),"60 FPS • verticale\nMovimento analogico progressivo\nAuto-attacco da fermo",Center((int)(18*s)));if(GUI.Button(new Rect(w*.20f,h*.82f,w*.60f,68*s),"INDIETRO",Btn((int)(21*s))))state=State.Menu;}
    void Toggle(int w,int h,float s,float y,string name,ref bool v){Rect r=new Rect(w*.11f,h*y,w*.78f,72*s);Fill(r,new Color(.03f,.07f,.15f,.98f));Stroke(r,new Color(.30f,.42f,.60f),2);GUI.Label(new Rect(r.x+20*s,r.y,r.width*.60f,r.height),name,Left((int)(20*s)));GUI.color=v?new Color(.16f,.82f,.42f):new Color(.56f,.58f,.64f);GUI.Label(new Rect(r.x+r.width*.63f,r.y,r.width*.30f,r.height),v?"ON":"OFF",Right((int)(20*s)));GUI.color=Color.white;if(GUI.Button(r,"",Invisible())){v=!v;Haptic();}}

    void Game(int w,int h,float s)
    {
        Fill(new Rect(0,0,w,h),Outside());
        Rect a=Arena();
        GUI.color=Color.white;
        if(bg[Mathf.Clamp(level-1,0,4)]!=null)GUI.DrawTexture(a,bg[Mathf.Clamp(level-1,0,4)],ScaleMode.ScaleAndCrop,true);
        else Fill(a,new Color(.30f,.46f,.20f));
        if(level==5)Fill(a,new Color(.12f,.025f,.005f,.06f));
        Stroke(a,Gold(),2);

        Hud(w,h,s);

        if(!dragging)
        {
            Vector2 idle=new Vector2(w*.50f,h*.815f);
            Circle(idle,w*.105f,new Color(.02f,.09f,.16f,.13f));
            Circle(idle,w*.058f,new Color(.03f,.34f,.76f,.20f));
            Circle(idle,w*.027f,new Color(.08f,.42f,.92f,.30f));
        }

        Vector2 hc=new Vector2(hero.x*w,hero.y*h);
        Shadow(hc,w*.082f,w*.038f,.30f);
        Tex(perseo,hc,w*.19f,w*.235f);
        MiniHp(hc,hp/maxHp,w*.13f,s,new Color(.10f,.86f,.23f));

        for(int i=0;i<enemies.Count;i++)EnemyDraw(enemies[i],w,h,s);
        for(int i=0;i<shots.Count;i++)ShotDraw(shots[i],w,h);

        if(dragging)
        {
            float r=Mathf.Max(105,w*.18f);
            Circle(dragStart,r,new Color(.01f,.04f,.10f,.26f));
            Circle(dragStart,r*.72f,new Color(.05f,.30f,.55f,.12f));
            Vector2 k=dragStart+Vector2.ClampMagnitude(dragNow-dragStart,r);
            Circle(k,r*.35f,new Color(.06f,.46f,1f,.72f));
            Circle(k,r*.17f,new Color(.46f,.82f,1f,.86f));
        }

        GUI.color=Gold();GUI.Label(new Rect(0,h*.878f,w,32*s),LevelName(),Center((int)(14*s)));GUI.color=Color.white;
    }

    Color Outside(){return level==1?new Color(.035f,.025f,.018f):level==2?new Color(.018f,.05f,.022f):level==3?new Color(.015f,.04f,.06f):level==4?new Color(.025f,.02f,.04f):new Color(.055f,.012f,.006f);}
    string LevelName(){return level==1?"ROVINE DI ATENE":level==2?"FORESTA SACRA":level==3?"COSTA DI SALAMINA":level==4?"FORTEZZA DI MEGARA":"LABIRINTO DEL MINOTAURO";}

    void Hud(int w,int h,float s)
    {
        Fill(new Rect(0,0,w,h*.106f),new Color(.004f,.010f,.028f,.98f));Fill(new Rect(0,h*.103f,w,2),Gold());
        GUI.color=Color.white;GUI.Label(new Rect(w*.035f,h*.008f,w*.28f,38*s),"PERSEO",Left((int)(19*s)));
        GUI.color=Gold();GUI.Label(new Rect(w*.28f,h*.008f,w*.50f,38*s),"ATTICA • LIVELLO "+level,Center((int)(18*s)));
        GUI.color=Color.white;GUI.Label(new Rect(w*.78f,h*.008f,w*.18f,38*s),"◆ "+coins,Right((int)(17*s)));
        Rect r=new Rect(w*.07f,h*.064f,w*.67f,12*s);Fill(r,new Color(.06f,.04f,.04f));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(hp/maxHp),r.height),new Color(.10f,.82f,.23f));Stroke(r,new Color(.88f,.74f,.40f),1);GUI.Label(new Rect(w*.74f,h*.050f,w*.22f,30*s),Mathf.CeilToInt(hp)+"/"+Mathf.CeilToInt(maxHp),Right((int)(13*s)));
    }

    void EnemyDraw(Enemy e,int w,int h,float s)
    {
        Texture2D t=e.type==EType.Assassin?assassin:e.type==EType.Gorgon?gorgon:e.type==EType.Hoplite?hoplite:minotaur;
        Vector2 c=new Vector2(e.p.x*w,e.p.y*h);
        float ww=e.type==EType.Minotaur?w*.35f:e.type==EType.Gorgon?w*.21f:e.type==EType.Hoplite?w*.19f:w*.17f;
        float hh=e.type==EType.Minotaur?w*.39f:e.type==EType.Gorgon?w*.245f:e.type==EType.Hoplite?w*.225f:w*.20f;
        Shadow(c,ww*.42f,ww*.19f,e.type==EType.Minotaur?.42f:.29f);
        Color old=GUI.color;if(e.flash>0)GUI.color=Color.Lerp(Color.white,new Color(1f,.35f,.20f),e.flash*.55f);Tex(t,c,ww,hh);GUI.color=old;
        float bw=e.type==EType.Minotaur?w*.30f:ww*.78f;Rect bar=new Rect(c.x-bw*.5f,c.y-hh*.57f,bw,Mathf.Max(5,6*s));Fill(bar,new Color(.07f,.015f,.01f));Fill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(e.hp/e.maxHp),bar.height),e.type==EType.Minotaur?new Color(.98f,.18f,.025f):new Color(.84f,.075f,.035f));Stroke(bar,new Color(.20f,.04f,.02f),1);
    }

    void ShotDraw(Shot q,int w,int h){Vector2 c=new Vector2(q.p.x*w,q.p.y*h),dir=q.v.sqrMagnitude>.0001f?q.v.normalized:Vector2.up,tail=c-new Vector2(dir.x*w*.030f,dir.y*h*.018f);Line(tail,c,new Color(.10f,.52f,1f,.24f),w*.019f);Line((tail+c)*.5f,c,new Color(.40f,.82f,1f,.65f),w*.010f);float r=w*.010f;Circle(c,r*2.5f,new Color(.05f,.35f,1f,.16f));Circle(c,r*1.2f,new Color(.12f,.62f,1f,.78f));Circle(c,r*.48f,Color.white);}
    void MiniHp(Vector2 c,float ratio,float width,float s,Color col){Rect r=new Rect(c.x-width*.5f,c.y-width*.52f,width,Mathf.Max(5,6*s));Fill(r,new Color(.02f,.03f,.02f,.86f));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(ratio),r.height),col);Stroke(r,new Color(.06f,.13f,.05f),1);}

    void UpgradeScreen(int w,int h,float s){Game(w,h,s);Fill(new Rect(0,0,w,h),new Color(.002f,.008f,.025f,.88f));GUI.color=Gold();GUI.Label(new Rect(0,h*.11f,w,60*s),"SCEGLI UN POTENZIAMENTO",Center((int)(27*s)));GUI.color=Color.white;GUI.Label(new Rect(0,h*.16f,w,40*s),"Prima del livello "+(level+1),Center((int)(17*s)));for(int i=0;i<3;i++){Rect r=new Rect(w*.075f,h*(.27f+i*.19f),w*.85f,h*.145f);Color b=i==0?new Color(.18f,.82f,.42f):i==1?new Color(1f,.52f,.10f):new Color(.60f,.26f,.92f);Fill(r,new Color(.025f,.055f,.12f,.99f));Stroke(r,b,3);GUI.color=Gold();GUI.Label(new Rect(r.x+20*s,r.y+9*s,r.width-40*s,42*s),upName[i],Left((int)(21*s)));GUI.color=Color.white;GUI.Label(new Rect(r.x+20*s,r.y+50*s,r.width-40*s,45*s),upDesc[i],Left((int)(16*s)));if(GUI.Button(r,"",Invisible()))Upgrade(upId[i]);}}
    void EndScreen(int w,int h,float s,bool win){Panel(w,h);GUI.color=Gold();GUI.Label(new Rect(0,h*.19f,w,90*s),win?"ATTICA LIBERATA":"SCONFITTA",Center((int)(44*s)));GUI.color=Color.white;GUI.Label(new Rect(0,h*.34f,w,70*s),win?"Primi 5 livelli completati":"Il mito continua",Center((int)(21*s)));GUI.Label(new Rect(0,h*.42f,w,50*s),"Nemici: "+kills+"   ◆ "+coins,Center((int)(17*s)));if(GUI.Button(new Rect(w*.15f,h*.59f,w*.70f,76*s),"RIPROVA",Btn((int)(23*s))))NewRun();if(GUI.Button(new Rect(w*.22f,h*.72f,w*.56f,64*s),"MENU",Btn((int)(20*s))))state=State.Menu;}

    void Panel(int w,int h){Fill(new Rect(0,0,w,h),new Color(.004f,.015f,.05f));for(int i=0;i<8;i++)Fill(new Rect(0,h*i/8f,w,h/8f+1),new Color(.02f,.10f,.22f,.04f+i*.008f));Fill(new Rect(0,h*.025f,w,3),Gold());Fill(new Rect(0,h*.965f,w,3),Gold());}
    void Tex(Texture2D t,Vector2 c,float w,float h){if(t==null){Circle(c,w*.3f,new Color(.12f,.24f,.52f));return;}GUI.DrawTexture(new Rect(c.x-w*.5f,c.y-h*.5f,w,h),t,ScaleMode.ScaleToFit,true);}
    void Shadow(Vector2 c,float rx,float ry,float a){Color o=GUI.color;GUI.color=new Color(0,0,0,a);GUI.DrawTexture(new Rect(c.x-rx,c.y-ry*.1f,rx*2,ry*2),circle);GUI.color=o;}
    void Circle(Vector2 p,float r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(p.x-r,p.y-r,r*2,r*2),circle);GUI.color=o;}
    void Line(Vector2 a,Vector2 b,Color c,float width){Matrix4x4 m=GUI.matrix;Color o=GUI.color;Vector2 d=b-a;GUI.color=c;GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,d.magnitude,width),Texture2D.whiteTexture);GUI.matrix=m;GUI.color=o;}
    void Fill(Rect r,Color c){Color o=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=o;}
    void Stroke(Rect r,Color c,float t){Fill(new Rect(r.x,r.y,r.width,t),c);Fill(new Rect(r.x,r.yMax-t,r.width,t),c);Fill(new Rect(r.x,r.y,t,r.height),c);Fill(new Rect(r.xMax-t,r.y,t,r.height),c);}
    Color Gold()=>new Color(.94f,.67f,.16f);
    GUIStyle Invisible(){GUIStyle s=new GUIStyle(GUI.skin.button);s.normal.background=null;s.hover.background=null;s.active.background=null;s.normal.textColor=Color.clear;s.hover.textColor=Color.clear;s.active.textColor=Color.clear;return s;}
    GUIStyle Center(int z){GUIStyle s=new GUIStyle(GUI.skin.label);s.alignment=TextAnchor.MiddleCenter;s.fontSize=z;s.fontStyle=FontStyle.Bold;s.wordWrap=true;s.normal.textColor=Color.white;return s;}
    GUIStyle Left(int z){GUIStyle s=Center(z);s.alignment=TextAnchor.MiddleLeft;return s;} GUIStyle Right(int z){GUIStyle s=Center(z);s.alignment=TextAnchor.MiddleRight;return s;}
    GUIStyle Btn(int z){GUIStyle s=new GUIStyle(GUI.skin.button);s.fontSize=z;s.fontStyle=FontStyle.Bold;s.alignment=TextAnchor.MiddleCenter;s.normal.textColor=Color.white;s.hover.textColor=Color.white;s.active.textColor=Gold();return s;}
}
