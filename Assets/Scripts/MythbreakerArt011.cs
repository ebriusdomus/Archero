using UnityEngine;

public static class MythbreakerArt011
{
    public static Texture2D BuildArena(int theme, int w = 360, int h = 640)
    {
        Color[] px = new Color[w*h];
        Color baseC = theme==1 ? new Color(.55f,.39f,.22f) : theme==2 ? new Color(.18f,.33f,.13f) : theme==3 ? new Color(.30f,.36f,.34f) : theme==4 ? new Color(.20f,.17f,.28f) : new Color(.25f,.10f,.055f);
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float nx=(x-w*.5f)/(w*.5f), ny=(y-h*.48f)/(h*.52f);
            float vig=Mathf.Clamp01(1f-.22f*(nx*nx+ny*ny));
            float n=(Hash01(x,y,theme)-.5f)*.07f;
            Color c=baseC*(vig+n); c.a=1f; px[y*w+x]=c;
        }

        if(theme==2) GrassPath(px,w,h); else StoneFloor(px,w,h,theme);
        if(theme==1) Attica(px,w,h);
        else if(theme==2) Forest(px,w,h);
        else if(theme==3) Coast(px,w,h);
        else if(theme==4) Fortress(px,w,h);
        else Labyrinth(px,w,h);

        Texture2D t=new Texture2D(w,h,TextureFormat.RGBA32,false);
        t.SetPixels(px); t.Apply(false,false); t.wrapMode=TextureWrapMode.Clamp; t.filterMode=FilterMode.Bilinear; return t;
    }

    static float Hash01(int x,int y,int seed)
    {
        unchecked { int n=x*374761393+y*668265263+seed*1442695041; n=(n^(n>>13))*1274126177; n^=n>>16; return (n&0x7fffffff)/2147483647f; }
    }

    static void StoneFloor(Color[] p,int w,int h,int theme)
    {
        Color s=theme==1?new Color(.72f,.54f,.34f):theme==3?new Color(.46f,.51f,.48f):theme==4?new Color(.34f,.30f,.42f):new Color(.36f,.20f,.12f);
        int x0=(int)(w*.14f),x1=(int)(w*.86f),y0=(int)(h*.06f),y1=(int)(h*.96f),tw=64,th=45;
        for(int y=y0,row=0;y<y1;y+=th,row++) for(int x=x0-(row%2)*32;x<x1;x+=tw)
        {
            float v=(Hash01(x,y,theme)-.5f)*.10f; Color c=new Color(Mathf.Clamp01(s.r+v),Mathf.Clamp01(s.g+v),Mathf.Clamp01(s.b+v),1);
            Rounded(p,w,h,x+2,y+2,Mathf.Min(x+tw-3,x1),Mathf.Min(y+th-3,y1),c,5); Outline(p,w,h,x+2,y+2,Mathf.Min(x+tw-3,x1),Mathf.Min(y+th-3,y1),new Color(.15f,.10f,.06f,.42f),1);
            if(Hash01(x+5,y+7,theme)>.76f){int cx=x+tw/2,cy=y+th/2;Line(p,w,h,cx-9,cy-4,cx+2,cy+4,new Color(.16f,.10f,.07f,.32f),1);}
        }
    }

    static void GrassPath(Color[] p,int w,int h)
    {
        Dots(p,w,h,new Color(.31f,.50f,.18f,.62f),3000,91);
        int cx=w/2;
        for(int y=(int)(h*.05f),i=0;y<h*.96f;y+=40,i++)
        {
            int ww=88+(i%3)*12, x=cx-ww/2+((i%4)-2)*8;
            Rounded(p,w,h,x,y,x+ww,y+50,new Color(.58f,.52f,.36f),8); Outline(p,w,h,x,y,x+ww,y+50,new Color(.27f,.23f,.15f,.70f),2);
        }
    }

    static void Attica(Color[] p,int w,int h)
    {
        Rect(p,w,h,0,0,w,(int)(h*.11f),new Color(.15f,.085f,.04f)); Rect(p,w,h,(int)(w*.29f),8,(int)(w*.71f),(int)(h*.10f),new Color(.045f,.03f,.02f)); Outline(p,w,h,(int)(w*.29f),8,(int)(w*.71f),(int)(h*.10f),new Color(.78f,.58f,.24f),3);
        Column(p,w,h,36,82,new Color(.58f,.49f,.36f)); Column(p,w,h,w-56,82,new Color(.58f,.49f,.36f)); Brazier(p,w,h,52,128,new Color(1f,.42f,.05f)); Brazier(p,w,h,w-52,128,new Color(1f,.42f,.05f));
        Medallion(p,w,h,w/2,(int)(h*.47f),70,new Color(.74f,.55f,.25f,.48f)); Rubble(p,w,h,new Color(.27f,.18f,.11f),1);
    }

    static void Forest(Color[] p,int w,int h)
    {
        Bushes(p,w,h,new Color(.055f,.18f,.055f),new Color(.14f,.35f,.09f)); Column(p,w,h,38,70,new Color(.46f,.43f,.33f)); Column(p,w,h,w-58,70,new Color(.46f,.43f,.33f));
        Column(p,w,h,44,(int)(h*.52f),new Color(.38f,.36f,.28f)); Column(p,w,h,w-64,(int)(h*.52f),new Color(.38f,.36f,.28f)); Brazier(p,w,h,56,108,new Color(1f,.45f,.06f)); Brazier(p,w,h,w-56,108,new Color(1f,.45f,.06f));
        Medallion(p,w,h,w/2,(int)(h*.45f),62,new Color(.72f,.57f,.25f,.30f)); Dots(p,w,h,new Color(.80f,.86f,.35f,.45f),900,131);
    }

    static void Coast(Color[] p,int w,int h)
    {
        int water=(int)(w*.13f); Rect(p,w,h,0,(int)(h*.10f),water,(int)(h*.95f),new Color(.02f,.31f,.43f)); Rect(p,w,h,w-water,(int)(h*.10f),w,(int)(h*.95f),new Color(.02f,.31f,.43f));
        for(int y=80;y<h-40;y+=45){Line(p,w,h,5,y,water-7,y+9,new Color(.45f,.86f,.93f,.50f),2);Line(p,w,h,w-water+7,y+14,w-5,y+4,new Color(.45f,.86f,.93f,.45f),2);}
        Column(p,w,h,water+8,74,new Color(.50f,.52f,.48f)); Column(p,w,h,w-water-28,74,new Color(.50f,.52f,.48f)); Medallion(p,w,h,w/2,(int)(h*.47f),68,new Color(.64f,.57f,.36f,.42f));
    }

    static void Fortress(Color[] p,int w,int h)
    {
        Rect(p,w,h,0,0,w,(int)(h*.10f),new Color(.09f,.07f,.15f)); Column(p,w,h,32,68,new Color(.35f,.31f,.42f)); Column(p,w,h,w-52,68,new Color(.35f,.31f,.42f)); Brazier(p,w,h,46,116,new Color(.58f,.16f,1f)); Brazier(p,w,h,w-46,116,new Color(.58f,.16f,1f));
        Block(p,w,h,46,245);Block(p,w,h,w-106,245);Block(p,w,h,46,425);Block(p,w,h,w-106,425);Medallion(p,w,h,w/2,(int)(h*.47f),66,new Color(.62f,.48f,.72f,.30f));
    }

    static void Labyrinth(Color[] p,int w,int h)
    {
        Rubble(p,w,h,new Color(.12f,.05f,.025f),5); Color wall=new Color(.20f,.065f,.03f), glow=new Color(.98f,.27f,.03f,.48f);
        Maze(p,w,h,40,175,145,175,145,270,wall,glow);Maze(p,w,h,w-40,175,w-145,175,w-145,270,wall,glow);Maze(p,w,h,40,420,150,420,150,530,wall,glow);Maze(p,w,h,w-40,420,w-150,420,w-150,530,wall,glow);
        Brazier(p,w,h,44,108,new Color(1f,.18f,.02f));Brazier(p,w,h,w-44,108,new Color(1f,.18f,.02f));Brazier(p,w,h,44,h-68,new Color(1f,.18f,.02f));Brazier(p,w,h,w-44,h-68,new Color(1f,.18f,.02f));
        Medallion(p,w,h,w/2,(int)(h*.46f),76,new Color(.94f,.31f,.07f,.28f));
    }

    static void Maze(Color[] p,int w,int h,int x1,int y1,int x2,int y2,int x3,int y3,Color wall,Color glow){Line(p,w,h,x1,y1,x2,y2,wall,12);Line(p,w,h,x2,y2,x3,y3,wall,12);Line(p,w,h,x1,y1,x2,y2,glow,2);Line(p,w,h,x2,y2,x3,y3,glow,2);}
    static void Block(Color[] p,int w,int h,int x,int y){Rounded(p,w,h,x,y,x+60,y+78,new Color(.18f,.15f,.25f),7);Outline(p,w,h,x,y,x+60,y+78,new Color(.76f,.61f,.34f,.75f),2);Outline(p,w,h,x+8,y+9,x+52,y+69,new Color(.34f,.30f,.42f,.85f),1);}
    static void Column(Color[] p,int w,int h,int x,int y,Color c){Rect(p,w,h,x,y,x+20,y+74,c);Rect(p,w,h,x-5,y-5,x+25,y+7,c*1.07f);Rect(p,w,h,x-5,y+67,x+25,y+79,c*.72f);Rect(p,w,h,x+4,y+4,x+7,y+67,new Color(1,1,1,.12f));Rect(p,w,h,x+15,y+4,x+19,y+67,new Color(0,0,0,.15f));}
    static void Brazier(Color[] p,int w,int h,int cx,int cy,Color f){Circle(p,w,h,cx,cy,18,new Color(f.r,f.g,f.b,.15f));Circle(p,w,h,cx,cy,10,new Color(.14f,.10f,.06f,1));Circle(p,w,h,cx,cy-2,6,new Color(f.r,f.g,f.b,.95f));Circle(p,w,h,cx,cy-5,3,new Color(1f,.88f,.32f,1));}
    static void Medallion(Color[] p,int w,int h,int cx,int cy,int r,Color c){Ring(p,w,h,cx,cy,r,r-3,c);Ring(p,w,h,cx,cy,r-12,r-14,new Color(c.r,c.g,c.b,c.a*.8f));for(int a=0;a<360;a+=45){float q=a*Mathf.Deg2Rad;Line(p,w,h,cx+(int)(Mathf.Cos(q)*(r-22)),cy+(int)(Mathf.Sin(q)*(r-22)),cx+(int)(Mathf.Cos(q)*(r-7)),cy+(int)(Mathf.Sin(q)*(r-7)),new Color(c.r,c.g,c.b,c.a*.62f),2);}}
    static void Rubble(Color[] p,int w,int h,Color c,int seed){for(int i=0;i<22;i++){int y=68+i*25,sz=10+(i*7+seed*3)%13;Rounded(p,w,h,4,y,4+sz,y+sz,c,3);Rounded(p,w,h,w-4-sz,y+8,w-4,y+8+sz,c*.90f,3);}}
    static void Bushes(Color[] p,int w,int h,Color d,Color l){for(int y=52;y<h-20;y+=28){int r=17+(y/28)%7;Circle(p,w,h,10,y,r,d);Circle(p,w,h,22,y+5,r-5,l);Circle(p,w,h,w-10,y,r,d);Circle(p,w,h,w-22,y+5,r-5,l);}}
    static void Dots(Color[] p,int w,int h,Color c,int count,int seed){for(int i=0;i<count;i++){int x=(int)(Hash01(i,seed,3)*w),y=(int)(Hash01(seed,i,7)*h);Blend(p,w,h,x,y,c);}}
    static void Rect(Color[] p,int w,int h,int x0,int y0,int x1,int y1,Color c){x0=Mathf.Clamp(x0,0,w);x1=Mathf.Clamp(x1,0,w);y0=Mathf.Clamp(y0,0,h);y1=Mathf.Clamp(y1,0,h);for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)Blend(p,w,h,x,y,c);}
    static void Rounded(Color[] p,int w,int h,int x0,int y0,int x1,int y1,Color c,int r){x0=Mathf.Clamp(x0,0,w);x1=Mathf.Clamp(x1,0,w);y0=Mathf.Clamp(y0,0,h);y1=Mathf.Clamp(y1,0,h);for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){int dx=Mathf.Max(Mathf.Max(x0+r-x,0),x-(x1-r-1));int dy=Mathf.Max(Mathf.Max(y0+r-y,0),y-(y1-r-1));if(dx*dx+dy*dy<=r*r)Blend(p,w,h,x,y,c);}}
    static void Outline(Color[] p,int w,int h,int x0,int y0,int x1,int y1,Color c,int t){Rect(p,w,h,x0,y0,x1,y0+t,c);Rect(p,w,h,x0,y1-t,x1,y1,c);Rect(p,w,h,x0,y0,x0+t,y1,c);Rect(p,w,h,x1-t,y0,x1,y1,c);}
    static void Circle(Color[] p,int w,int h,int cx,int cy,int r,Color c){int rr=r*r;for(int y=Mathf.Max(0,cy-r);y<Mathf.Min(h,cy+r+1);y++)for(int x=Mathf.Max(0,cx-r);x<Mathf.Min(w,cx+r+1);x++)if((x-cx)*(x-cx)+(y-cy)*(y-cy)<=rr)Blend(p,w,h,x,y,c);}
    static void Ring(Color[] p,int w,int h,int cx,int cy,int o,int inn,Color c){int oo=o*o,ii=inn*inn;for(int y=Mathf.Max(0,cy-o);y<Mathf.Min(h,cy+o+1);y++)for(int x=Mathf.Max(0,cx-o);x<Mathf.Min(w,cx+o+1);x++){int d=(x-cx)*(x-cx)+(y-cy)*(y-cy);if(d<=oo&&d>=ii)Blend(p,w,h,x,y,c);}}
    static void Line(Color[] p,int w,int h,int x0,int y0,int x1,int y1,Color c,int t){int dx=Mathf.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Mathf.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;while(true){Circle(p,w,h,x0,y0,Mathf.Max(1,t/2),c);if(x0==x1&&y0==y1)break;int e2=2*err;if(e2>=dy){err+=dy;x0+=sx;}if(e2<=dx){err+=dx;y0+=sy;}}}
    static void Blend(Color[] p,int w,int h,int x,int y,Color c){if(x<0||x>=w||y<0||y>=h)return;int i=(h-1-y)*w+x;float a=Mathf.Clamp01(c.a);Color d=p[i];p[i]=new Color(d.r*(1-a)+c.r*a,d.g*(1-a)+c.g*a,d.b*(1-a)+c.b*a,1);}
}
