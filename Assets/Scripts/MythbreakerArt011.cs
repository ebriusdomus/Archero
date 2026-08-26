using UnityEngine;

public static class MythbreakerArt011
{
    const int W=720, H=1280;

    public static Texture2D BuildArena(int level)
    {
        Texture2D t=new Texture2D(W,H,TextureFormat.RGB24,false);
        t.wrapMode=TextureWrapMode.Clamp;
        t.filterMode=FilterMode.Bilinear;

        Color ground,ground2,ground3,stone,stoneHi,stoneLo,water,accent,edge,foliage;
        if(level==1){ground=C("9ACD58");ground2=C("A6D767");ground3=C("88BB49");stone=C("E5D5AD");stoneHi=C("F5E8C9");stoneLo=C("9D8A66");water=C("48C2E5");accent=C("4DA7FF");edge=C("557E3D");foliage=C("3B8244");}
        else if(level==2){ground=C("7FC456");ground2=C("8DCE63");ground3=C("6FAE49");stone=C("DCCB9D");stoneHi=C("F0E3BE");stoneLo=C("8F805D");water=C("3ABBE0");accent=C("FF9D2D");edge=C("39713C");foliage=C("2F743D");}
        else if(level==3){ground=C("86BEAA");ground2=C("93C9B6");ground3=C("72A996");stone=C("DDD9C9");stoneHi=C("F2F0E5");stoneLo=C("8C9790");water=C("35B7E6");accent=C("50CFFF");edge=C("3C7169");foliage=C("34736A");}
        else if(level==4){ground=C("756887");ground2=C("7F7192");ground3=C("685B79");stone=C("B0A6B7");stoneHi=C("D6CDD9");stoneLo=C("695D70");water=C("5F5AB0");accent=C("B85DFF");edge=C("42394E");foliage=C("3B3446");}
        else{ground=C("B77B45");ground2=C("C58A50");ground3=C("9B6334");stone=C("D2A974");stoneHi=C("EBC58F");stoneLo=C("765239");water=C("D85D25");accent=C("FF6D24");edge=C("734229");foliage=C("62402A");}

        Fill(t,new RectInt(0,0,W,H),edge);
        RectInt field=new RectInt(18,0,W-36,H);
        Fill(t,field,ground);

        // Smaller, irregular paving. No large checkerboard squares.
        int tile=56;
        for(int y=0;y<H;y+=tile)
        {
            int offset=((y/tile)&1)==0?0:tile/2;
            for(int x=18-offset;x<W-18;x+=tile)
            {
                int hash=Mathf.Abs((x*31+y*17+level*53)%5);
                Color c=hash==0?ground2:hash==1?ground3:ground;
                RectInt r=new RectInt(x+2,y+2,tile-4,tile-4);
                Fill(t,r,c);
                if(hash==0) Fill(t,new RectInt(r.x+5,r.y+r.height-5,r.width-10,2),new Color(1f,1f,1f,.07f));
            }
        }

        // A worn Greek processional lane in the centre.
        Color lane=new Color(stone.r*.92f,stone.g*.90f,stone.b*.82f);
        Fill(t,new RectInt(270,0,180,H),new Color(lane.r,lane.g,lane.b,.32f));
        for(int y=40;y<H;y+=92)
        {
            RectInt slab=new RectInt(282+(y/92%2)*8,y,156,60);
            Fill(t,slab,new Color(lane.r,lane.g,lane.b,.45f));
            Fill(t,new RectInt(slab.x+7,slab.y+slab.height-6,slab.width-14,3),new Color(stoneHi.r,stoneHi.g,stoneHi.b,.16f));
        }

        if(level==2 || level==3)
        {
            Pool(t,new RectInt(32,350,112,245),water,stoneLo);
            Pool(t,new RectInt(W-144,350,112,245),water,stoneLo);
            Pool(t,new RectInt(32,790,112,200),water,stoneLo);
            Pool(t,new RectInt(W-144,790,112,200),water,stoneLo);
        }
        else if(level==5)
        {
            Pool(t,new RectInt(25,255,72,760),water,stoneLo);
            Pool(t,new RectInt(W-97,255,72,760),water,stoneLo);
        }

        // Irregular vegetation/rock clusters, not a row of identical circles.
        for(int i=0;i<13;i++)
        {
            int y=42+i*94+(i%3)*9;
            BushCluster(t,28,y,foliage,1f+(i%4)*.07f);
            BushCluster(t,W-33,y+38,foliage,.92f+((i+2)%4)*.06f);
        }

        // Top temple/fortress entrance. Texture coordinates are bottom-up, so draw it near H.
        TempleTopDown(t,H-205,stone,stoneHi,stoneLo,accent,level);

        // Central Greek mosaic instead of a technical target ring.
        GreekMosaic(t,W/2,650,stoneHi,stoneLo,level==4?C("C79BE5"):level==5?C("E9A15B"):C("F2E3B8"));

        // Render collision objects as low top-down ruins aligned to gameplay coordinates.
        Rect[] obs=Obstacles(level);
        for(int i=0;i<obs.Length;i++) LowRuin(t,ScreenRectToTexture(obs[i]),stone,stoneHi,stoneLo,i);

        // Loose stones, grass tufts, flowers.
        for(int i=0;i<34;i++)
        {
            int x=65+((i*137+level*17)%590), y=105+((i*179+level*41)%1010);
            if(x>255&&x<465&&i%2==0) x+=i%4==0?-170:170;
            if(i%5==0) Flower(t,x,y,level==4?C("C984E2"):level==5?C("F0B36D"):C("F4E9D4"));
            else if(i%3==0) Rock(t,x,y,stoneLo);
            else GrassTuft(t,x,y,ground3);
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

    static RectInt ScreenRectToTexture(Rect n)
    {
        float lx=(n.x-.025f)/.95f;
        float sy=(n.y-.108f)/.765f;
        float lw=n.width/.95f;
        float lh=n.height/.765f;
        int rw=Mathf.RoundToInt(lw*W), rh=Mathf.RoundToInt(lh*H);
        int rx=Mathf.RoundToInt(lx*W);
        int ry=H-Mathf.RoundToInt((sy+lh)*H);
        rx=Mathf.Clamp(rx,10,W-rw-10);
        ry=Mathf.Clamp(ry,40,H-rh-230);
        return new RectInt(rx,ry,rw,rh);
    }

    static void LowRuin(Texture2D t,RectInt r,Color stone,Color hi,Color lo,int variant)
    {
        if(r.width<30||r.height<20)return;
        Fill(t,new RectInt(r.x+8,r.y-8,r.width,r.height),new Color(.16f,.12f,.09f));
        Fill(t,r,stone);
        Fill(t,new RectInt(r.x+5,r.y+r.height-12,r.width-10,12),hi);
        Fill(t,new RectInt(r.x+5,r.y+5,r.width-10,6),lo);
        Stroke(t,r,lo,3);
        if((variant&1)==0)
        {
            Fill(t,new RectInt(r.x+r.width/2-3,r.y+8,6,r.height-16),new Color(lo.r,lo.g,lo.b,.52f));
        }
        else
        {
            Fill(t,new RectInt(r.x+8,r.y+r.height/2-3,r.width-16,6),new Color(lo.r,lo.g,lo.b,.48f));
        }
        // chips make the blocks read as ruins, not UI panels.
        Fill(t,new RectInt(r.x+2,r.y+r.height-8,14,8),groundChip(stone,lo));
        Fill(t,new RectInt(r.x+r.width-20,r.y+2,18,8),groundChip(stone,lo));
    }

    static Color groundChip(Color stone,Color lo){return new Color((stone.r+lo.r)*.5f,(stone.g+lo.g)*.5f,(stone.b+lo.b)*.5f);}

    static void TempleTopDown(Texture2D t,int y,Color stone,Color hi,Color lo,Color accent,int level)
    {
        // shadow toward the play field
        Fill(t,new RectInt(58,y-18,W-116,152),new Color(.12f,.10f,.09f));
        Fill(t,new RectInt(78,y,W-156,126),stone);
        Fill(t,new RectInt(95,y+94,W-190,28),hi);
        Fill(t,new RectInt(95,y+8,W-190,18),lo);

        // top-down doorway and stairs descending into arena
        Fill(t,new RectInt(W/2-88,y+56,176,66),level==5?C("5D2217"):C("222326"));
        for(int i=0;i<4;i++)
        {
            int sw=230-i*26;
            Fill(t,new RectInt(W/2-sw/2,y-8-i*16,sw,17),i%2==0?stone:hi);
        }

        ColumnTop(t,126,y+28,stone,hi,lo);
        ColumnTop(t,W-154,y+28,stone,hi,lo);
        Torch(t,92,y+32,accent);
        Torch(t,W-92,y+32,accent);

        // Greek pediment line and symbol
        Fill(t,new RectInt(160,y+112,W-320,7),lo);
        Ring(t,W/2,y+94,22,new Color(accent.r,accent.g,accent.b,.48f));
        Ring(t,W/2,y+94,12,new Color(hi.r,hi.g,hi.b,.55f));
    }

    static void ColumnTop(Texture2D t,int x,int y,Color stone,Color hi,Color lo)
    {
        Fill(t,new RectInt(x+6,y-7,38,92),new Color(.14f,.11f,.09f));
        Fill(t,new RectInt(x,y,38,92),stone);
        Fill(t,new RectInt(x+6,y+6,9,78),hi);
        Fill(t,new RectInt(x-7,y+78,52,14),hi);
        Fill(t,new RectInt(x-5,y,48,12),lo);
    }

    static void GreekMosaic(Texture2D t,int cx,int cy,Color hi,Color lo,Color accent)
    {
        Ring(t,cx,cy,92,new Color(lo.r,lo.g,lo.b,.28f));
        Ring(t,cx,cy,66,new Color(hi.r,hi.g,hi.b,.35f));
        Ring(t,cx,cy,34,new Color(accent.r,accent.g,accent.b,.30f));
        // simple meander cross
        Fill(t,new RectInt(cx-8,cy-52,16,104),new Color(accent.r,accent.g,accent.b,.16f));
        Fill(t,new RectInt(cx-52,cy-8,104,16),new Color(accent.r,accent.g,accent.b,.16f));
        Fill(t,new RectInt(cx-45,cy+34,28,8),new Color(hi.r,hi.g,hi.b,.30f));
        Fill(t,new RectInt(cx+17,cy-42,28,8),new Color(hi.r,hi.g,hi.b,.30f));
    }

    static void Pool(Texture2D t,RectInt r,Color water,Color border)
    {
        Fill(t,new RectInt(r.x-7,r.y-7,r.width+14,r.height+14),border);
        Fill(t,r,water);
        Fill(t,new RectInt(r.x+5,r.y+r.height-13,r.width-10,8),new Color(1f,1f,1f,.18f));
        for(int y=r.y+28;y<r.yMax-16;y+=46)
        {
            Fill(t,new RectInt(r.x+15,y,r.width-30,3),new Color(1f,1f,1f,.10f));
            if((y/46)%2==0) Circle(t,r.x+r.width/2+18,y+8,8,C("6BAF5B"));
        }
    }

    static void BushCluster(Texture2D t,int x,int y,Color c,float scale)
    {
        Color dark=new Color(c.r*.56f,c.g*.56f,c.b*.56f);
        Color light=new Color(Mathf.Min(1,c.r*1.15f),Mathf.Min(1,c.g*1.15f),Mathf.Min(1,c.b*1.15f));
        int r=Mathf.RoundToInt(27*scale);
        // small trunk glimpse gives depth
        Fill(t,new RectInt(x-4,y-18,8,28),C("6A4B30"));
        Circle(t,x+5,y-8,r,dark);
        Circle(t,x-6,y+5,r,c);
        Circle(t,x+15,y+8,Mathf.RoundToInt(r*.78f),light);
        Circle(t,x-14,y-3,Mathf.RoundToInt(r*.70f),c);
    }

    static void Torch(Texture2D t,int x,int y,Color flame)
    {
        Circle(t,x,y,24,new Color(flame.r,flame.g,flame.b,.25f));
        Circle(t,x,y,12,flame);
        Circle(t,x,y+4,5,C("FFF2A6"));
    }

    static void Flower(Texture2D t,int x,int y,Color c)
    {
        Circle(t,x-4,y,4,c);Circle(t,x+4,y,4,c);Circle(t,x,y-4,4,c);Circle(t,x,y+4,4,c);Circle(t,x,y,3,C("F6C94A"));
    }

    static void Rock(Texture2D t,int x,int y,Color c)
    {
        Fill(t,new RectInt(x-7,y-4,14,8),c);
        Fill(t,new RectInt(x-4,y+4,8,4),new Color(Mathf.Min(1,c.r*1.18f),Mathf.Min(1,c.g*1.18f),Mathf.Min(1,c.b*1.18f)));
    }

    static void GrassTuft(Texture2D t,int x,int y,Color c)
    {
        Fill(t,new RectInt(x-1,y,2,10),c);Fill(t,new RectInt(x-6,y+2,2,8),c);Fill(t,new RectInt(x+5,y+2,2,8),c);
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
    static Color C(string hex){Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);return c;}
}
