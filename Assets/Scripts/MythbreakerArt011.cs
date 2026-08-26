using UnityEngine;

public static class MythbreakerArt011
{
    const int W=720, H=1280;

    public static Texture2D BuildArena(int level)
    {
        Texture2D t=new Texture2D(W,H,TextureFormat.RGB24,false);
        t.wrapMode=TextureWrapMode.Clamp; t.filterMode=FilterMode.Bilinear;

        Color grass,grass2,stone,stoneHi,stoneLo,water,accent,banner,edge;
        if(level==1){grass=C("91C94C");grass2=C("99D154");stone=C("E7D6A9");stoneHi=C("F4E7BF");stoneLo=C("A8956C");water=C("47C3E8");accent=C("4DA7FF");banner=C("225A9C");edge=C("4B7938");}
        else if(level==2){grass=C("72B747");grass2=C("7FC34F");stone=C("D9C995");stoneHi=C("EFE2B6");stoneLo=C("92845D");water=C("39BCE1");accent=C("FF9B27");banner=C("245F92");edge=C("346D35");}
        else if(level==3){grass=C("79B8A2");grass2=C("83C2AD");stone=C("D8D5C2");stoneHi=C("F0EEE0");stoneLo=C("8C978F");water=C("32B6E7");accent=C("4FCBFF");banner=C("1C6D9C");edge=C("356C65");}
        else if(level==4){grass=C("66577A");grass2=C("706183");stone=C("A99EAF");stoneHi=C("CDC4D1");stoneLo=C("665A6C");water=C("5550A5");accent=C("B55CFF");banner=C("58277E");edge=C("383244");}
        else{grass=C("B8783E");grass2=C("C48346");stone=C("D0A56F");stoneHi=C("E6C08A");stoneLo=C("785438");water=C("D65A22");accent=C("FF6A20");banner=C("8B2A18");edge=C("744126");}

        Fill(t,new RectInt(0,0,W,H),edge);

        // Main play field
        int ax=22, ay=0, aw=W-44, ah=H;
        Fill(t,new RectInt(ax,ay,aw,ah),grass);

        // Soft checkerboard lawn/stone tiles: readable like a mobile action game, not a flat prototype.
        int cols=8, rows=15; float cw=aw/(float)cols, ch=ah/(float)rows;
        for(int y=0;y<rows;y++) for(int x=0;x<cols;x++)
        {
            Color c=((x+y)&1)==0?grass:grass2;
            RectInt r=new RectInt(Mathf.RoundToInt(ax+x*cw),Mathf.RoundToInt(y*ch),Mathf.CeilToInt(cw)+1,Mathf.CeilToInt(ch)+1);
            Fill(t,r,c);
        }

        // Water gives the arenas their own identity instead of copying Archero's generic green room.
        if(level==2 || level==3)
        {
            Pool(t,new RectInt(35,430,105,250),water);
            Pool(t,new RectInt(W-140,430,105,250),water);
            Pool(t,new RectInt(35,890,105,210),water);
            Pool(t,new RectInt(W-140,890,105,210),water);
        }
        if(level==5)
        {
            // Labyrinth side channels / lava.
            Pool(t,new RectInt(28,330,65,720),water);
            Pool(t,new RectInt(W-93,330,65,720),water);
        }

        // Vegetation / rocky borders.
        for(int y=50;y<H-40;y+=78)
        {
            Bush(t,36,y,level==4?C("40384C"):level==5?C("5E3B24"):C("2F783E"));
            Bush(t,W-36,y+26,level==4?C("40384C"):level==5?C("5E3B24"):C("2F783E"));
        }

        // Temple / gate at top.
        Temple(t,stone,stoneHi,stoneLo,banner,accent,level);

        // Greek medallion in the centre.
        Ring(t,W/2,625,100,new Color(stoneHi.r,stoneHi.g,stoneHi.b,.42f));
        Ring(t,W/2,625,74,new Color(stoneLo.r,stoneLo.g,stoneLo.b,.30f));
        Ring(t,W/2,625,38,new Color(stoneHi.r,stoneHi.g,stoneHi.b,.26f));

        // Collision blocks, deliberately drawn at the same logical positions used by gameplay.
        Rect[] obs=Obstacles(level);
        for(int i=0;i<obs.Length;i++) BlockFromScreenRect(t,obs[i],stone,stoneHi,stoneLo);

        // Small decorative stones / flowers.
        for(int i=0;i<26;i++)
        {
            int x=70+((i*113)%580), y=235+((i*173)%960);
            if(i%3==0) Circle(t,x,y,5,level==4?C("BA79D2"):level==5?C("E5A45C"):C("F3E9D2"));
            else Circle(t,x,y,4,new Color(stoneLo.r,stoneLo.g,stoneLo.b,.65f));
        }

        t.Apply(false,false);
        return t;
    }

    static Rect[] Obstacles(int l)
    {
        if(l==1)return new[]{new Rect(.18f,.50f,.14f,.085f),new Rect(.68f,.50f,.14f,.085f)};
        if(l==2)return new[]{new Rect(.16f,.50f,.13f,.085f),new Rect(.71f,.50f,.13f,.085f),new Rect(.44f,.44f,.12f,.075f)};
        if(l==3)return new[]{new Rect(.22f,.49f,.13f,.085f),new Rect(.65f,.49f,.13f,.085f),new Rect(.44f,.63f,.12f,.08f)};
        if(l==4)return new[]{new Rect(.14f,.46f,.12f,.09f),new Rect(.74f,.46f,.12f,.09f),new Rect(.31f,.62f,.12f,.08f),new Rect(.57f,.62f,.12f,.08f)};
        return new[]{new Rect(.15f,.58f,.13f,.085f),new Rect(.72f,.58f,.13f,.085f)};
    }

    // Gameplay coordinates are full-screen normalized; convert them to the arena texture local coordinates.
    static void BlockFromScreenRect(Texture2D t,Rect n,Color stone,Color hi,Color lo)
    {
        float lx=(n.x-.025f)/.95f, ly=(n.y-.108f)/.765f;
        float lw=n.width/.95f, lh=n.height/.765f;
        RectInt r=new RectInt(Mathf.RoundToInt(lx*W),Mathf.RoundToInt(ly*H),Mathf.RoundToInt(lw*W),Mathf.RoundToInt(lh*H));
        r.x=Mathf.Clamp(r.x,10,W-r.width-10);r.y=Mathf.Clamp(r.y,110,H-r.height-20);
        // shadow
        Fill(t,new RectInt(r.x+9,r.y+11,r.width,r.height),new Color(.16f,.12f,.08f));
        Fill(t,r,stone);
        Fill(t,new RectInt(r.x+5,r.y+5,r.width-10,Mathf.Max(7,r.height/5)),hi);
        Fill(t,new RectInt(r.x+7,r.y+r.height-12,r.width-14,8),lo);
        Stroke(t,r,lo,3);
        // block seams
        if(r.width>80){int mx=r.x+r.width/2;Fill(t,new RectInt(mx-2,r.y+7,4,r.height-14),new Color(lo.r,lo.g,lo.b,.55f));}
    }

    static void Temple(Texture2D t,Color stone,Color hi,Color lo,Color banner,Color flame,int level)
    {
        Fill(t,new RectInt(48,30,W-96,155),lo);
        Fill(t,new RectInt(70,18,W-140,130),stone);
        Fill(t,new RectInt(88,14,W-176,28),hi);
        // doorway
        Fill(t,new RectInt(W/2-92,56,184,115),C("171716"));
        Fill(t,new RectInt(W/2-78,66,156,100),level==5?C("5C2416"):C("222425"));
        // columns
        Column(t,126,48,stone,hi,lo); Column(t,W-154,48,stone,hi,lo);
        // banners
        Fill(t,new RectInt(180,58,48,90),banner); Fill(t,new RectInt(W-228,58,48,90),banner);
        // steps
        for(int i=0;i<4;i++) Fill(t,new RectInt(W/2-130+i*12,166+i*16,260-i*24,18),i%2==0?stone:hi);
        Torch(t,92,165,flame); Torch(t,W-92,165,flame);
    }

    static void Column(Texture2D t,int x,int y,Color stone,Color hi,Color lo)
    {
        Fill(t,new RectInt(x,y,34,110),stone);Fill(t,new RectInt(x+6,y,8,110),hi);
        Fill(t,new RectInt(x-8,y,50,14),hi);Fill(t,new RectInt(x-6,y+98,46,15),lo);
    }

    static void Torch(Texture2D t,int x,int y,Color flame)
    {
        Circle(t,x,y,23,new Color(flame.r,flame.g,flame.b,.28f));
        Circle(t,x,y,12,flame);Circle(t,x,y-4,5,C("FFF2A6"));
    }

    static void Pool(Texture2D t,RectInt r,Color water)
    {
        Fill(t,new RectInt(r.x-6,r.y-6,r.width+12,r.height+12),C("5C7948"));
        Fill(t,r,water);
        Fill(t,new RectInt(r.x+6,r.y+6,r.width-12,10),new Color(1f,1f,1f,.15f));
        for(int y=r.y+24;y<r.yMax-10;y+=38) Fill(t,new RectInt(r.x+14,y,r.width-28,3),new Color(1f,1f,1f,.10f));
    }

    static void Bush(Texture2D t,int x,int y,Color c)
    {
        Circle(t,x+4,y+8,30,new Color(c.r*.58f,c.g*.58f,c.b*.58f));
        Circle(t,x,y,28,c);Circle(t,x+18,y+5,22,new Color(Mathf.Min(1,c.r*1.12f),Mathf.Min(1,c.g*1.12f),Mathf.Min(1,c.b*1.12f)));
    }

    static void Ring(Texture2D t,int cx,int cy,int r,Color c)
    {
        int rr=r*r, inner=(r-5)*(r-5);
        for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++){int d=x*x+y*y;if(d<=rr&&d>=inner)Set(t,cx+x,cy+y,c);}
    }

    static void Circle(Texture2D t,int cx,int cy,int r,Color c)
    {
        int rr=r*r;for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(x*x+y*y<=rr)Set(t,cx+x,cy+y,c);
    }

    static void Fill(Texture2D t,RectInt r,Color c)
    {
        int x0=Mathf.Max(0,r.x),y0=Mathf.Max(0,r.y),x1=Mathf.Min(W,r.xMax),y1=Mathf.Min(H,r.yMax);
        Color32 cc=c;for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)t.SetPixel(x,y,cc);
    }

    static void Stroke(Texture2D t,RectInt r,Color c,int s)
    {
        Fill(t,new RectInt(r.x,r.y,r.width,s),c);Fill(t,new RectInt(r.x,r.yMax-s,r.width,s),c);Fill(t,new RectInt(r.x,r.y,s,r.height),c);Fill(t,new RectInt(r.xMax-s,r.y,s,r.height),c);
    }

    static void Set(Texture2D t,int x,int y,Color c){if(x>=0&&x<W&&y>=0&&y<H)t.SetPixel(x,y,c);}

    static Color C(string hex)
    {
        Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);return c;
    }
}
