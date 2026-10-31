using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent iron, bronze and dark wood inventory ornament.</summary>
[AddComponentMenu("UI/Gothic Inventory Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GothicInventoryGraphic : MaskableGraphic
{
    public enum Surface { Panel, Slot, Hotbar }
    public Surface surface;
    [Range(0f, 1f)] public float highlight;

    private static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        if (r.width <= 0 || r.height <= 0) return;
        if (surface == Surface.Slot) DrawSlot(vh, r); else DrawPanel(vh, r);
    }

    private void DrawPanel(VertexHelper v, Rect r)
    {
        bool small = surface == Surface.Hotbar;
        Box(v, new Rect(r.x - 5, r.y - 8, r.width + 10, r.height + 10), new Color(0, 0, 0, .36f));
        Bevel(v, r, 3, C("101316"), C("72726B"), C("07090B"));
        Bevel(v, Inset(r, 4), 6, C("24282A"), C("474A46"), C("101315"));
        Bevel(v, Inset(r, 11), 1, C("17191A"), C("B79A66"), C("554630"));
        Rect inner = Inset(r, 14);
        Box(v, inner, C("17191B"));
        DrawWoodGrain(v, inner);
        Bevel(v, Inset(r, 17), 1, new Color(0, 0, 0, 0), C("46423A"), C("292A29"), false);
        DrawPanelCorners(v, r, small);
        if (!small) DrawPanelDividers(v, r);
    }

    // Vân gỗ cố định, không tạo texture hoặc đổi Random của gameplay.
    private void DrawWoodGrain(VertexHelper v, Rect inner)
    {
        for (int i = 0; i < 58; i++)
        {
            float y = Mathf.Lerp(inner.yMin, inner.yMax, i / 57f);
            float wave = Mathf.Sin(i * 2.73f) * 1.5f;
            Line(v, new Vector2(inner.xMin, y), new Vector2(inner.xMax, y + wave), .5f,
                new Color(.39f, .29f, .19f, i % 4 == 0 ? .12f : .045f));
        }
        for (int i = 1; i < 6; i++)
        {
            float x = Mathf.Lerp(inner.xMin, inner.xMax, i / 6f);
            Line(v, new Vector2(x, inner.yMin), new Vector2(x, inner.yMax), 1, new Color(0, 0, 0, .16f));
        }
    }

    private void DrawPanelCorners(VertexHelper v, Rect r, bool small)
    {
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sy = -1; sy <= 1; sy += 2)
        {
            Vector2 corner = new Vector2(sx < 0 ? r.xMin + 18 : r.xMax - 18, sy < 0 ? r.yMin + 18 : r.yMax - 18);
            Ornament(v, corner, new Vector2(-sx, -sy), small ? .58f : 1f);
            Rivet(v, corner, small ? 6 : 8);
        }
    }

    private void DrawPanelDividers(VertexHelper v, Rect r)
    {
        float y = r.yMax - 102;
        Line(v, new Vector2(r.xMin + 40, y), new Vector2(-18, y), 1, C("63533B"));
        Line(v, new Vector2(18, y), new Vector2(r.xMax - 40, y), 1, C("63533B"));
        Diamond(v, new Vector2(r.center.x, y), 5, 7, C("BA9B61"));
        Diamond(v, new Vector2(r.center.x, r.yMax - 31), 7, 11, C("866B42"));
        Diamond(v, new Vector2(r.center.x, r.yMax - 31), 3, 6, C("5F2630"));
        float bottom = r.yMin + 48;
        Line(v, new Vector2(r.xMin + 40, bottom), new Vector2(r.xMax - 40, bottom), .7f, C("494235"));
    }

    private void DrawSlot(VertexHelper v, Rect r)
    {
        Color glow = C("EACB8C");
        if (highlight > .001f)
        {
            for (int i = 6; i >= 1; i--)
                Ring(v, Inset(r, -i), 1.1f, new Color(glow.r, glow.g, glow.b, highlight * .035f * (7 - i)));
        }
        Box(v, new Rect(r.x + 1, r.y - 3, r.width, r.height), new Color(0, 0, 0, .5f));
        Bevel(v, r, 2, C("17191B"), Color.Lerp(C("958064"), C("F4DCA5"), highlight), C("080B0E"));
        Bevel(v, Inset(r, 3), 6, C("4A4133"), Color.Lerp(C("8B795C"), C("D5BA80"), highlight), C("28231E"));
        Bevel(v, Inset(r, 10), 2, C("121416"), C("1A1713"), C("A18C63"));
        DrawSlotLining(v, r);
        DrawSlotCorners(v, r);
        if (highlight > .001f) Ring(v, Inset(r, 11), 1.2f, new Color(glow.r, glow.g, glow.b, highlight * .85f));
    }

    private void DrawSlotLining(VertexHelper v, Rect r)
    {
        Rect well = Inset(r, 13);
        Box(v, well, C("22201E"));
        for (int i = 0; i < 23; i++)
        {
            float y = Mathf.Lerp(well.yMin + 1, well.yMax - 1, i / 22f);
            Line(v, new Vector2(well.xMin + 1, y), new Vector2(well.xMax - 1, y + Mathf.Sin(i * 4.3f)), .5f, new Color(.52f, .37f, .23f, .10f));
        }
        // Các vết xước nhỏ trên cạnh đồng.
        for (int i = 0; i < 14; i++)
        {
            float x = Mathf.Lerp(r.xMin + 14, r.xMax - 14, i / 13f);
            Line(v, new Vector2(x, r.yMax - 5), new Vector2(x + 5, r.yMax - 6), .45f, new Color(.85f, .78f, .61f, .23f));
        }
    }

    private void DrawSlotCorners(VertexHelper v, Rect r)
    {
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sy = -1; sy <= 1; sy += 2)
        {
            Vector2 p = new Vector2(sx < 0 ? r.xMin + 7 : r.xMax - 7, sy < 0 ? r.yMin + 7 : r.yMax - 7);
            Line(v, p + new Vector2(-sx * 2, 0), p + new Vector2(0, -sy * 6), 1, C("B19A70"));
            Rivet(v, p, 1.6f);
        }
    }

    private void Ornament(VertexHelper v, Vector2 origin, Vector2 direction, float scale)
    {
        for (int axis = 0; axis < 2; axis++)
        {
            DrawScroll(v, origin, direction, scale, axis);
            DrawLeaves(v, origin, direction, scale, axis);
        }
    }

    // Hai nét cuộn đối xứng tạo hoa văn góc.
    private void DrawScroll(VertexHelper v, Vector2 origin, Vector2 direction, float scale, int axis)
    {
        Vector2 previous = origin;
        for (int i = 0; i <= 30; i++)
        {
            float t = i / 30f;
            float a = t * Mathf.PI * 2.1f;
            float radius = (1 - t) * 13;
            Vector2 local = new Vector2(25 + Mathf.Cos(a) * radius, 11 + Mathf.Sin(a) * radius);
            if (axis == 1) local = new Vector2(local.y, local.x);
            Vector2 next = origin + Vector2.Scale(local * scale, direction);
            if (i > 0) Line(v, previous, next, 1.2f * scale, C("96856A"));
            previous = next;
        }
    }

    private void DrawLeaves(VertexHelper v, Vector2 origin, Vector2 direction, float scale, int axis)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 a = new Vector2(42 + 12 * i, 0);
            Vector2 b = a + new Vector2(-6, 7);
            Vector2 c = a + new Vector2(-10, 1);
            if (axis == 1) { a = new Vector2(a.y,a.x); b = new Vector2(b.y,b.x); c = new Vector2(c.y,c.x); }
            a = origin + Vector2.Scale(a * scale, direction);
            b = origin + Vector2.Scale(b * scale, direction);
            c = origin + Vector2.Scale(c * scale, direction);
            Line(v,a,b,1.1f*scale,C("96856A")); Line(v,b,c,1.1f*scale,C("96856A"));
        }
    }

    private void Rivet(VertexHelper v, Vector2 p, float radius)
    {
        Disc(v, p + Vector2.down, radius + 1.5f, C("080A0C"));
        Disc(v, p, radius, C("726D5F"));
        Disc(v, p + new Vector2(-.7f, .7f), radius * .7f, C("363A3A"));
        Disc(v, p + new Vector2(-radius * .3f, radius * .38f), radius * .22f, C("BDB5A0"));
    }

    private void Disc(VertexHelper v, Vector2 p, float radius, Color c)
    {
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8, b = (i + 1) * Mathf.PI / 8;
            Triangle(v, p, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                p + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, c);
        }
    }

    private void Diamond(VertexHelper v, Vector2 p, float w, float h, Color c)
    {
        Quad(v, p + Vector2.left*w, p + Vector2.up*h, p + Vector2.right*w, p + Vector2.down*h, c);
    }
    private static Rect Inset(Rect r, float n) { return new Rect(r.x+n,r.y+n,r.width-2*n,r.height-2*n); }
    private void Ring(VertexHelper v, Rect r, float n, Color c)
    {
        Box(v,new Rect(r.x,r.y,r.width,n),c); Box(v,new Rect(r.x,r.yMax-n,r.width,n),c);
        Box(v,new Rect(r.x,r.y+n,n,r.height-2*n),c); Box(v,new Rect(r.xMax-n,r.y+n,n,r.height-2*n),c);
    }
    private void Bevel(VertexHelper v, Rect r, float n, Color fill, Color light, Color dark, bool filled=true)
    {
        if(filled) Box(v,r,fill);
        Quad(v,new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax-n,r.yMax-n),new Vector2(r.xMin+n,r.yMax-n),light);
        Quad(v,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin+n,r.yMax-n),new Vector2(r.xMin+n,r.yMin+n),Color.Lerp(light,dark,.3f));
        Quad(v,new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax-n,r.yMin+n),new Vector2(r.xMax-n,r.yMax-n),dark);
        Quad(v,new Vector2(r.xMax,r.yMin),new Vector2(r.xMin,r.yMin),new Vector2(r.xMin+n,r.yMin+n),new Vector2(r.xMax-n,r.yMin+n),dark);
    }
    private void Box(VertexHelper v, Rect r, Color c) { Quad(v,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),c); }
    private void Line(VertexHelper v, Vector2 a, Vector2 b, float width, Color c)
    {
        Vector2 n = new Vector2(-(b-a).y,(b-a).x).normalized * width * .5f;
        Quad(v,a-n,a+n,b+n,b-n,c);
    }
    private void Quad(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int n=v.currentVertCount; Color final=tint*color;
        v.AddVert(a,final,Vector2.zero); v.AddVert(b,final,Vector2.zero); v.AddVert(c,final,Vector2.zero); v.AddVert(d,final,Vector2.zero);
        v.AddTriangle(n,n+1,n+2); v.AddTriangle(n,n+2,n+3);
    }
    private void Triangle(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int n=v.currentVertCount; Color final=tint*color;
        v.AddVert(a,final,Vector2.zero); v.AddVert(b,final,Vector2.zero); v.AddVert(c,final,Vector2.zero); v.AddTriangle(n,n+1,n+2);
    }
}
