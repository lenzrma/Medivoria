using UnityEngine;
using UnityEngine.UI;

/// <summary>Декоративная рамка в духе средневекового фэнтези: бронза, патина, зубцы стены, заклёпки, розетки.</summary>
public static class ProceduralBoardFrame
{
    static Sprite _white;

    static Sprite WhiteSprite
    {
        get
        {
            if (_white == null)
            {
                var t = Texture2D.whiteTexture;
                _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            }
            return _white;
        }
    }

    public static void BuildOrReplace(
        Transform uiParent,
        RectTransform gridRt,
        Vector2 boardSize,
        Vector3 worldCenter,
        bool frameOverTiles,
        float outsetExtra,
        Color shadow,
        Color bandDark,
        Color bandGold,
        Color bandGoldBright,
        Color bandInner,
        float thick1,
        float thick2,
        float thick3,
        float thick4,
        int notchesPerLongEdge,
        float innerPadThickness,
        Color innerPadColor)
    {
        const string rootName = "ProceduralBoardFrame";
        Transform existing = uiParent.Find(rootName);
        if (existing != null)
            Object.Destroy(existing.gameObject);

        float T = Mathf.Max(4f, thick1 + thick2 + thick3 + thick4 + Mathf.Max(0f, outsetExtra));
        float ow = boardSize.x + 2f * T;
        float oh = boardSize.y + 2f * T;

        var rootGo = new GameObject(rootName, typeof(RectTransform));
        rootGo.transform.SetParent(uiParent, false);
        var root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(ow, oh);
        root.localScale = Vector3.one;
        root.localRotation = Quaternion.identity;
        root.position = worldCenter;

        AddSoftVolumetricShadow(root, ow, oh, shadow);
        AddGroundContactShadow(root, ow, oh);

        float d = 0f;
        AddBeveledLayer(root, ow, oh, d, thick1, bandDark, "A", bevelStrength: 0.85f, addDropShadow: true);
        d += thick1;
        AddBeveledLayer(root, ow, oh, d, thick2, bandGold, "B", bevelStrength: 0.75f, addDropShadow: false);
        d += thick2;
        AddBeveledLayer(root, ow, oh, d, thick3, bandGoldBright, "C", bevelStrength: 0.9f, addDropShadow: false);
        d += thick3;
        AddBeveledLayer(root, ow, oh, d, thick4, bandInner, "D", bevelStrength: 0.7f, addDropShadow: false);
        d += thick4;

        AddDirectionalRimHighlights(root, ow, oh, thick1, bandGoldBright);
        AddSpecularOnGoldBands(root, ow, oh, thick1, thick2, thick3, bandGold, bandGoldBright);
        AddInnerOpeningRim(root, ow, oh, d, MulAlpha(Lighten(bandGoldBright, 0.5f), 0.55f));
        AddInnerAmbientOcclusion(root, ow, oh, d, bandInner);

        float corner = Mathf.Clamp(thick1 + thick2, 11f, 20f);
        AddCornerRosettes(root, ow, oh, d, corner, bandDark, bandGold, bandGoldBright);

        AddEdgeNotchesSoft(root, ow, oh, d, notchesPerLongEdge, bandDark, bandGoldBright);

        Color fantasyBurgundy = new Color(0.4f, 0.1f, 0.15f, 1f);
        AddVerdigrisPatina(root, ow, oh, thick1, bandDark);
        AddCastleMerlons(root, ow, oh, thick1, bandDark);
        AddIronRivets(root, ow, oh, thick1, bandDark, bandGoldBright);
        AddInnerColonetteFrieze(root, ow, oh, d, bandInner, bandGoldBright);
        AddTopCabochon(root, ow, oh, thick1, fantasyBurgundy, bandGoldBright);

        // Поверх декора — иначе AO/кромка перекрывают цвет зазора и кажется, что оттенок «не меняется».
        AddInnerPadRing(root, boardSize, innerPadThickness, innerPadColor);

        PlaceRelativeToGrid(gridRt, root, frameOverTiles);
    }

    static void AddDropShadow(GameObject go, Vector2 distance, Color effectColor)
    {
        var sh = go.GetComponent<Shadow>();
        if (sh == null) sh = go.AddComponent<Shadow>();
        sh.effectColor = effectColor;
        sh.effectDistance = distance;
        sh.useGraphicAlpha = true;
    }

    /// <summary>Мягкая объёмная тень: несколько слоёв с разным смещением и альфой (имитация рассеянного света).</summary>
    static void AddSoftVolumetricShadow(RectTransform root, float ow, float oh, Color baseShadow)
    {
        var holder = new GameObject("SoftShadowGroup", typeof(RectTransform));
        holder.transform.SetParent(root, false);
        var hrt = holder.GetComponent<RectTransform>();
        StretchFull(hrt);
        holder.transform.SetAsFirstSibling();

        // offsetMin = (left, bottom), offsetMax = (-right, -top) при stretch-якорях.
        // Раньше оба X были положительными — получались «лучи» по углам (серый крест).
        const int layers = 8;
        float maxSpread = Mathf.Clamp(Mathf.Max(ow, oh) * 0.028f, 10f, 26f);
        float baseA = Mathf.Clamp01(baseShadow.a * 0.65f);

        for (int i = 0; i < layers; i++)
        {
            float t = i / (float)Mathf.Max(1, layers - 1);
            float spread = Mathf.Lerp(maxSpread, 2f, t);
            float falloff = Mathf.SmoothStep(0f, 1f, t);
            float a = Mathf.Lerp(0.012f, 0.045f, 1f - falloff) * baseA;
            float left = spread * 0.55f;
            float right = spread * 0.95f;
            float bottom = spread * 0.9f;
            float top = spread * 0.5f;
            var layer = CreateImage(hrt, $"VShadow_{i}", new Color(0f, 0f, 0f, a));
            StretchFull(layer);
            layer.offsetMin = new Vector2(left, bottom);
            layer.offsetMax = new Vector2(-right, -top);
        }

        var core = CreateImage(hrt, "VShadow_Core", new Color(0f, 0f, 0f, 0.06f * baseA));
        StretchFull(core);
        float inset = Mathf.Clamp(Mathf.Min(ow, oh) * 0.012f, 3f, 8f);
        core.offsetMin = new Vector2(inset, inset);
        core.offsetMax = new Vector2(-inset, -inset);
        AddDropShadow(core.gameObject, new Vector2(2f, -2.5f), new Color(0f, 0f, 0f, 0.18f));
    }

    /// <summary>Контактное затемнение у «пола» — чуть плотнее снизу.</summary>
    static void AddGroundContactShadow(RectTransform root, float ow, float oh)
    {
        var holder = new GameObject("GroundShadow", typeof(RectTransform));
        holder.transform.SetParent(root, false);
        var hrt = holder.GetComponent<RectTransform>();
        StretchFull(hrt);
        holder.transform.SetSiblingIndex(1);

        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            float a = Mathf.Lerp(0.06f, 0.01f, t);
            var g = CreateImage(hrt, $"GroundSh_{i}", new Color(0f, 0f, 0f, a));
            StretchFull(g);
            float drop = Mathf.Lerp(6f, 22f, t);
            float insetX = Mathf.Lerp(18f, 36f, t);
            g.offsetMin = new Vector2(insetX, -drop);
            g.offsetMax = new Vector2(-insetX, -oh * Mathf.Lerp(0.58f, 0.72f, t));
        }
    }

    /// <summary>Свет сверху-слева: тонкий рассеянный кант по наружным рёбрам.</summary>
    static void AddDirectionalRimHighlights(RectTransform root, float ow, float oh, float t1, Color hi)
    {
        Color c = MulAlpha(Lighten(hi, 0.42f), 0.22f);
        float inset = Mathf.Max(2f, t1 * 0.35f);

        var top = CreateImage(root, "SunRim_T", c);
        top.anchorMin = top.anchorMax = new Vector2(0.5f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(ow - inset * 2f, 1.4f);
        top.anchoredPosition = new Vector2(inset * 0.35f, -inset * 0.25f);

        var le = CreateImage(root, "SunRim_L", MulAlpha(c, 0.9f));
        le.anchorMin = le.anchorMax = new Vector2(0f, 0.5f);
        le.pivot = new Vector2(0f, 0.5f);
        le.sizeDelta = new Vector2(1.4f, oh - inset * 2f);
        le.anchoredPosition = new Vector2(inset * 0.25f, inset * 0.15f);
    }

    /// <summary>Локальное затемнение у внутреннего проёма — имитация рассеянной окклюзии.</summary>
    static void AddInnerAmbientOcclusion(RectTransform root, float ow, float oh, float depth, Color inner)
    {
        float inset = depth + 4f;
        float w = Mathf.Max(8f, ow - 2f * inset);
        float h = Mathf.Max(8f, oh - 2f * inset);
        Color cBase = MulAlpha(Darken(inner, 0.1f), 1f);

        void EdgeStack(string prefix, bool isHorizontal, float signY, float signX)
        {
            int steps = 6;
            for (int k = 0; k < steps; k++)
            {
                float u = k / (float)(steps - 1);
                float fade = (1f - u) * (1f - u);
                Color c = MulAlpha(cBase, 0.22f * fade);
                var s = CreateImage(root, $"{prefix}_{k}", c);
                s.anchorMin = s.anchorMax = new Vector2(0.5f, 0.5f);
                if (isHorizontal)
                {
                    s.sizeDelta = new Vector2(w - u * 10f, Mathf.Lerp(5f, 0.8f, u));
                    s.anchoredPosition = new Vector2(0f, signY * (h * 0.5f - 1.2f - u * 5f));
                }
                else
                {
                    s.sizeDelta = new Vector2(Mathf.Lerp(5f, 0.8f, u), h - u * 10f);
                    s.anchoredPosition = new Vector2(signX * (w * 0.5f - 1.2f - u * 5f), 0f);
                }
            }
        }

        EdgeStack("AO_T", true, 1f, 0f);
        EdgeStack("AO_B", true, -1f, 0f);
        EdgeStack("AO_L", false, 0f, -1f);
        EdgeStack("AO_R", false, 0f, 1f);
    }

    static void AddBeveledLayer(
        RectTransform root,
        float ow,
        float oh,
        float depth,
        float thick,
        Color col,
        string tag,
        float bevelStrength,
        bool addDropShadow)
    {
        float wIn = Mathf.Max(1f, ow - 2f * depth);
        float vH = Mathf.Max(1f, oh - 2f * (depth + thick));
        float rim = Mathf.Clamp(thick * 0.22f, 1.2f, 4f);
        float rimInX = Mathf.Max(0f, wIn - rim * 2.5f);

        Color outer = Darken(col, 0.38f * bevelStrength);
        Color inner = Lighten(col, 0.28f * bevelStrength);

        var top = CreateImage(root, $"Top_{tag}", col);
        top.anchorMin = top.anchorMax = new Vector2(0.5f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(wIn, thick);
        top.anchoredPosition = new Vector2(0f, -depth);
        if (addDropShadow) AddDropShadow(top.gameObject, new Vector2(1.5f, -2f), MulAlpha(Color.black, 0.35f));

        var topO = CreateImage(root, $"Top_{tag}_o", outer);
        topO.anchorMin = topO.anchorMax = new Vector2(0.5f, 1f);
        topO.pivot = new Vector2(0.5f, 1f);
        topO.sizeDelta = new Vector2(wIn, rim);
        topO.anchoredPosition = new Vector2(0f, -depth);

        float hi = Mathf.Min(rim + 0.5f, thick * 0.45f);
        var topI = CreateImage(root, $"Top_{tag}_i", inner);
        topI.anchorMin = topI.anchorMax = new Vector2(0.5f, 1f);
        topI.pivot = new Vector2(0.5f, 1f);
        topI.sizeDelta = new Vector2(rimInX, hi);
        topI.anchoredPosition = new Vector2(0f, -(depth + thick) + hi);

        var bot = CreateImage(root, $"Bot_{tag}", col);
        bot.anchorMin = bot.anchorMax = new Vector2(0.5f, 0f);
        bot.pivot = new Vector2(0.5f, 0f);
        bot.sizeDelta = new Vector2(wIn, thick);
        bot.anchoredPosition = new Vector2(0f, depth);

        var botO = CreateImage(root, $"Bot_{tag}_o", outer);
        botO.anchorMin = botO.anchorMax = new Vector2(0.5f, 0f);
        botO.pivot = new Vector2(0.5f, 0f);
        botO.sizeDelta = new Vector2(wIn, rim);
        botO.anchoredPosition = new Vector2(0f, depth);

        var botI = CreateImage(root, $"Bot_{tag}_i", inner);
        botI.anchorMin = botI.anchorMax = new Vector2(0.5f, 0f);
        botI.pivot = new Vector2(0.5f, 0f);
        botI.sizeDelta = new Vector2(rimInX, hi);
        botI.anchoredPosition = new Vector2(0f, depth + thick - hi);

        float vInH = Mathf.Max(1f, vH - rim * 2f);

        var le = CreateImage(root, $"Left_{tag}", col);
        le.anchorMin = le.anchorMax = new Vector2(0f, 0.5f);
        le.pivot = new Vector2(0f, 0.5f);
        le.sizeDelta = new Vector2(thick, vH);
        le.anchoredPosition = new Vector2(depth, 0f);

        var leO = CreateImage(root, $"Left_{tag}_o", outer);
        leO.anchorMin = leO.anchorMax = new Vector2(0f, 0.5f);
        leO.pivot = new Vector2(0f, 0.5f);
        leO.sizeDelta = new Vector2(rim, vInH);
        leO.anchoredPosition = new Vector2(depth, 0f);

        float wi = Mathf.Min(rim + 0.5f, thick * 0.45f);
        var leI = CreateImage(root, $"Left_{tag}_i", inner);
        leI.anchorMin = leI.anchorMax = new Vector2(0f, 0.5f);
        leI.pivot = new Vector2(1f, 0.5f);
        leI.sizeDelta = new Vector2(wi, Mathf.Max(1f, vInH - 2f));
        leI.anchoredPosition = new Vector2(depth + thick, 0f);

        var ri = CreateImage(root, $"Right_{tag}", col);
        ri.anchorMin = ri.anchorMax = new Vector2(1f, 0.5f);
        ri.pivot = new Vector2(1f, 0.5f);
        ri.sizeDelta = new Vector2(thick, vH);
        ri.anchoredPosition = new Vector2(-depth, 0f);

        var riO = CreateImage(root, $"Right_{tag}_o", outer);
        riO.anchorMin = riO.anchorMax = new Vector2(1f, 0.5f);
        riO.pivot = new Vector2(1f, 0.5f);
        riO.sizeDelta = new Vector2(rim, vInH);
        riO.anchoredPosition = new Vector2(-depth, 0f);

        var riI = CreateImage(root, $"Right_{tag}_i", inner);
        riI.anchorMin = riI.anchorMax = new Vector2(1f, 0.5f);
        riI.pivot = new Vector2(0f, 0.5f);
        riI.sizeDelta = new Vector2(wi, Mathf.Max(1f, vInH - 2f));
        riI.anchoredPosition = new Vector2(-depth - thick, 0f);
    }

    static void AddSpecularOnGoldBands(
        RectTransform root,
        float ow,
        float oh,
        float t1,
        float t2,
        float t3,
        Color gold,
        Color goldHi)
    {
        var spec = CreateImage(root, "SpecGold", MulAlpha(Lighten(goldHi, 0.55f), 0.22f));
        spec.anchorMin = spec.anchorMax = new Vector2(0.5f, 0.5f);
        spec.pivot = new Vector2(0.5f, 0.5f);
        spec.sizeDelta = new Vector2(ow - 2f * (t1 + t2 + 8f), Mathf.Max(2f, t2 * 0.22f));
        spec.anchoredPosition = new Vector2(0f, oh * 0.5f - t1 - t2 * 0.38f);

        var spec2 = CreateImage(root, "SpecGoldLo", MulAlpha(Lighten(gold, 0.35f), 0.14f));
        spec2.anchorMin = spec2.anchorMax = new Vector2(0.5f, 0.5f);
        spec2.sizeDelta = new Vector2(ow - 2f * (t1 + 10f), Mathf.Max(2f, t3 * 0.18f));
        spec2.anchoredPosition = new Vector2(0f, -oh * 0.5f + t1 + t3 * 0.45f);
    }

    static void AddInnerOpeningRim(RectTransform root, float ow, float oh, float depthInner, Color rim)
    {
        float inset = depthInner + 1f;
        float w = Mathf.Max(8f, ow - 2f * inset);
        float h = Mathf.Max(8f, oh - 2f * inset);

        var top = CreateImage(root, "OpenRim_T", rim);
        top.anchorMin = top.anchorMax = new Vector2(0.5f, 0.5f);
        top.sizeDelta = new Vector2(w, 1.2f);
        top.anchoredPosition = new Vector2(0f, h * 0.5f - 0.5f);

        var bot = CreateImage(root, "OpenRim_B", rim);
        bot.anchorMin = bot.anchorMax = new Vector2(0.5f, 0.5f);
        bot.sizeDelta = new Vector2(w, 1.2f);
        bot.anchoredPosition = new Vector2(0f, -h * 0.5f + 0.5f);

        var le = CreateImage(root, "OpenRim_L", rim);
        le.anchorMin = le.anchorMax = new Vector2(0.5f, 0.5f);
        le.sizeDelta = new Vector2(1.2f, h - 2f);
        le.anchoredPosition = new Vector2(-w * 0.5f + 0.5f, 0f);

        var ri = CreateImage(root, "OpenRim_R", rim);
        ri.anchorMin = ri.anchorMax = new Vector2(0.5f, 0.5f);
        ri.sizeDelta = new Vector2(1.2f, h - 2f);
        ri.anchoredPosition = new Vector2(w * 0.5f - 0.5f, 0f);
    }

    static void AddCornerRosettes(
        RectTransform root,
        float ow,
        float oh,
        float depthInner,
        float size,
        Color dark,
        Color mid,
        Color hi)
    {
        float hx = ow * 0.5f - depthInner - size * 0.2f;
        float hy = oh * 0.5f - depthInner - size * 0.2f;
        Vector2[] corners =
        {
            new Vector2(-hx, hy),
            new Vector2(hx, hy),
            new Vector2(-hx, -hy),
            new Vector2(hx, -hy),
        };
        Color wine = new Color(0.38f, 0.09f, 0.13f, 1f);
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = corners[i];
            var halo = CreateImage(root, $"RosetteHalo_{i}", MulAlpha(Lighten(hi, 0.2f), 0.22f));
            halo.anchorMin = halo.anchorMax = new Vector2(0.5f, 0.5f);
            halo.sizeDelta = new Vector2(size * 1.48f, size * 1.48f);
            halo.localRotation = Quaternion.Euler(0f, 0f, 45f);
            halo.anchoredPosition = p;

            var ring = CreateImage(root, $"RosetteRing_{i}", Darken(mid, 0.35f));
            ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = new Vector2(size * 1.28f, size * 1.28f);
            ring.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ring.anchoredPosition = p;

            var back = CreateImage(root, $"RosetteB_{i}", Darken(mid, 0.22f));
            back.anchorMin = back.anchorMax = new Vector2(0.5f, 0.5f);
            back.sizeDelta = new Vector2(size * 1.12f, size * 1.12f);
            back.localRotation = Quaternion.Euler(0f, 0f, 45f);
            back.anchoredPosition = p;

            var midD = CreateImage(root, $"RosetteM_{i}", mid);
            midD.anchorMin = midD.anchorMax = new Vector2(0.5f, 0.5f);
            midD.sizeDelta = new Vector2(size * 0.9f, size * 0.9f);
            midD.localRotation = Quaternion.Euler(0f, 0f, 45f);
            midD.anchoredPosition = p;

            var gem = CreateImage(root, $"RosetteGem_{i}", wine);
            gem.anchorMin = gem.anchorMax = new Vector2(0.5f, 0.5f);
            gem.sizeDelta = new Vector2(size * 0.28f, size * 0.28f);
            gem.localRotation = Quaternion.Euler(0f, 0f, 45f);
            gem.anchoredPosition = p;

            var dot = CreateImage(root, $"RosetteH_{i}", Lighten(hi, 0.18f));
            dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(size * 0.32f, size * 0.32f);
            dot.localRotation = Quaternion.Euler(0f, 0f, 45f);
            dot.anchoredPosition = p;

            var pin = CreateImage(root, $"RosetteP_{i}", MulAlpha(dark, 0.92f));
            pin.anchorMin = pin.anchorMax = new Vector2(0.5f, 0.5f);
            pin.sizeDelta = new Vector2(size * 0.14f, size * 0.14f);
            pin.anchoredPosition = p;
        }
    }

    static void AddEdgeNotchesSoft(RectTransform root, float ow, float oh, float depth, int count, Color dark, Color light)
    {
        count = Mathf.Clamp(count, 0, 20);
        if (count == 0) return;

        float inset = depth + 6f;
        float topY = oh * 0.5f - inset;
        float botY = -oh * 0.5f + inset;
        float leftX = -ow * 0.5f + inset;
        float rightX = ow * 0.5f - inset;
        float spanX = ow - 2f * inset;
        float spanY = oh - 2f * inset;

        float notchW = 4f;
        float notchH = 5f;

        for (int i = 0; i < count; i++)
        {
            float t = (i + 1f) / (count + 1f);
            Color c = Color.Lerp(
                MulAlpha(dark, 0.88f),
                MulAlpha(light, 0.75f),
                (i & 1) == 0 ? 0.35f : 0.72f);

            var top = CreateImage(root, $"NotchT_{i}", c);
            top.anchorMin = top.anchorMax = new Vector2(0.5f, 0.5f);
            top.sizeDelta = new Vector2(notchW, notchH);
            top.anchoredPosition = new Vector2(leftX + t * spanX, topY - notchH * 0.5f);

            var bot = CreateImage(root, $"NotchB_{i}", c);
            bot.anchorMin = bot.anchorMax = new Vector2(0.5f, 0.5f);
            bot.sizeDelta = new Vector2(notchW, notchH);
            bot.anchoredPosition = new Vector2(leftX + t * spanX, botY + notchH * 0.5f);

            var le = CreateImage(root, $"NotchL_{i}", c);
            le.anchorMin = le.anchorMax = new Vector2(0.5f, 0.5f);
            le.sizeDelta = new Vector2(notchH, notchW);
            le.anchoredPosition = new Vector2(leftX + notchH * 0.5f, botY + t * spanY);

            var ri = CreateImage(root, $"NotchR_{i}", c);
            ri.anchorMin = ri.anchorMax = new Vector2(0.5f, 0.5f);
            ri.sizeDelta = new Vector2(notchH, notchW);
            ri.anchoredPosition = new Vector2(rightX - notchH * 0.5f, botY + t * spanY);
        }
    }

    static void AddVerdigrisPatina(RectTransform root, float ow, float oh, float thick1, Color bandDark)
    {
        Color pat = new Color(0.14f, 0.34f, 0.3f, 0.11f);
        float patch = Mathf.Clamp(thick1 * 2.4f, 22f, 56f);
        Vector2[] at =
        {
            new Vector2(-ow * 0.5f + patch * 0.45f, oh * 0.5f - patch * 0.4f),
            new Vector2(ow * 0.5f - patch * 0.45f, oh * 0.5f - patch * 0.4f),
            new Vector2(-ow * 0.5f + patch * 0.45f, -oh * 0.5f + patch * 0.4f),
            new Vector2(ow * 0.5f - patch * 0.45f, -oh * 0.5f + patch * 0.4f),
        };
        for (int i = 0; i < 4; i++)
        {
            var g = CreateImage(root, $"Patina_{i}", pat);
            g.anchorMin = g.anchorMax = new Vector2(0.5f, 0.5f);
            g.sizeDelta = new Vector2(patch * 0.9f, patch * 0.65f);
            g.localRotation = Quaternion.Euler(0f, 0f, 38f + i * 7f);
            g.anchoredPosition = at[i];
        }

        var wash = CreateImage(root, "PatinaWash", MulAlpha(Color.Lerp(pat, bandDark, 0.5f), 0.06f));
        wash.anchorMin = wash.anchorMax = new Vector2(0.5f, 0.5f);
        wash.sizeDelta = new Vector2(ow - thick1 * 2f, oh - thick1 * 2f);
        wash.anchoredPosition = Vector2.zero;
    }

    static void AddCastleMerlons(RectTransform root, float ow, float oh, float thick1, Color bandDark)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(ow / 50f), 7, 16);
        float pad = Mathf.Max(10f, thick1 * 0.8f);
        float span = ow - 2f * pad;
        float merlonW = Mathf.Clamp(span / (count * 1.35f), 7f, 13f);
        float gap = count > 1 ? (span - count * merlonW) / (count - 1) : 0f;
        float merlonH = Mathf.Clamp(thick1 * 0.42f, 5f, 11f);
        float y = oh * 0.5f - merlonH * 0.5f + 1f;
        Color c = Color.Lerp(Darken(bandDark, 0.15f), bandDark, 0.4f);

        for (int i = 0; i < count; i++)
        {
            float xLeft = -ow * 0.5f + pad + i * (merlonW + gap);
            float xc = xLeft + merlonW * 0.5f;
            var msh = CreateImage(root, $"MerlonDrop_{i}", MulAlpha(Color.black, 0.24f));
            msh.anchorMin = msh.anchorMax = new Vector2(0.5f, 0.5f);
            msh.sizeDelta = new Vector2(merlonW + 3f, merlonH + 4f);
            msh.anchoredPosition = new Vector2(xc + 2.2f, y - 3f);

            var m = CreateImage(root, $"Merlon_{i}", c);
            m.anchorMin = m.anchorMax = new Vector2(0.5f, 0.5f);
            m.sizeDelta = new Vector2(merlonW, merlonH);
            m.anchoredPosition = new Vector2(xc, y);

            var cap = CreateImage(root, $"MerlonCap_{i}", Lighten(c, 0.12f));
            cap.anchorMin = cap.anchorMax = new Vector2(0.5f, 0.5f);
            cap.sizeDelta = new Vector2(merlonW * 0.55f, 2.2f);
            cap.anchoredPosition = new Vector2(xc, y + merlonH * 0.5f + 1f);
        }
    }

    static void AddIronRivets(RectTransform root, float ow, float oh, float thick1, Color dark, Color hi)
    {
        float inset = Mathf.Max(8f, thick1 * 0.55f);
        int n = 3;
        float rivet = 5f;
        int rid = 0;

        void Rivet(Vector2 pos)
        {
            var drop = CreateImage(root, $"Rivet_{rid}_sh", MulAlpha(Color.black, 0.2f));
            drop.anchorMin = drop.anchorMax = new Vector2(0.5f, 0.5f);
            drop.sizeDelta = new Vector2(rivet + 3.5f, rivet + 3.5f);
            drop.anchoredPosition = pos + new Vector2(1.8f, -2.2f);

            var baseR = CreateImage(root, $"Rivet_{rid}_b", Darken(dark, 0.05f));
            baseR.anchorMin = baseR.anchorMax = new Vector2(0.5f, 0.5f);
            baseR.sizeDelta = new Vector2(rivet + 1.2f, rivet + 1.2f);
            baseR.anchoredPosition = pos;

            var ring = CreateImage(root, $"Rivet_{rid}_r", MulAlpha(hi, 0.45f));
            ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = new Vector2(rivet, rivet);
            ring.anchoredPosition = pos;

            var nail = CreateImage(root, $"Rivet_{rid}_n", MulAlpha(Lighten(hi, 0.1f), 0.85f));
            nail.anchorMin = nail.anchorMax = new Vector2(0.5f, 0.5f);
            nail.sizeDelta = new Vector2(rivet * 0.38f, rivet * 0.38f);
            nail.anchoredPosition = pos + new Vector2(0.4f, 0.35f);
            rid++;
        }

        for (int i = 1; i <= n; i++)
        {
            float t = i / (n + 1f);
            float x = -ow * 0.5f + inset + t * (ow - 2f * inset);
            Rivet(new Vector2(x, oh * 0.5f - inset));
            Rivet(new Vector2(x, -oh * 0.5f + inset));
        }

        for (int i = 1; i <= n; i++)
        {
            float t = i / (n + 1f);
            float y = -oh * 0.5f + inset + t * (oh - 2f * inset);
            Rivet(new Vector2(-ow * 0.5f + inset, y));
            Rivet(new Vector2(ow * 0.5f - inset, y));
        }
    }

    static void AddInnerColonetteFrieze(RectTransform root, float ow, float oh, float depth, Color inner, Color goldHi)
    {
        float inset = depth + 5f;
        float w = ow - 2f * inset;
        float h = oh - 2f * inset;
        int perEdge = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(w, h) / 38f), 5, 14);
        float tickW = 1.4f;
        float tickH = 5f;

        for (int i = 0; i < perEdge; i++)
        {
            float t = (i + 0.5f) / perEdge;
            Color c = (i & 1) == 0 ? MulAlpha(inner, 0.95f) : MulAlpha(goldHi, 0.55f);

            var a = CreateImage(root, $"FriezeT_{i}", c);
            a.anchorMin = a.anchorMax = new Vector2(0.5f, 0.5f);
            a.sizeDelta = new Vector2(tickW, tickH);
            a.anchoredPosition = new Vector2(-w * 0.5f + t * w, h * 0.5f - 1f);

            var b = CreateImage(root, $"FriezeB_{i}", c);
            b.anchorMin = b.anchorMax = new Vector2(0.5f, 0.5f);
            b.sizeDelta = new Vector2(tickW, tickH);
            b.anchoredPosition = new Vector2(-w * 0.5f + t * w, -h * 0.5f + 1f);

            var le = CreateImage(root, $"FriezeL_{i}", c);
            le.anchorMin = le.anchorMax = new Vector2(0.5f, 0.5f);
            le.sizeDelta = new Vector2(tickH, tickW);
            le.anchoredPosition = new Vector2(-w * 0.5f + 1f, -h * 0.5f + t * h);

            var ri = CreateImage(root, $"FriezeR_{i}", c);
            ri.anchorMin = ri.anchorMax = new Vector2(0.5f, 0.5f);
            ri.sizeDelta = new Vector2(tickH, tickW);
            ri.anchoredPosition = new Vector2(w * 0.5f - 1f, -h * 0.5f + t * h);
        }
    }

    static void AddTopCabochon(RectTransform root, float ow, float oh, float thick1, Color wine, Color hi)
    {
        float y = oh * 0.5f - thick1 * 0.55f;
        var cabDrop = CreateImage(root, "CabochonDrop", MulAlpha(Color.black, 0.32f));
        cabDrop.anchorMin = cabDrop.anchorMax = new Vector2(0.5f, 0.5f);
        cabDrop.sizeDelta = new Vector2(30f, 30f);
        cabDrop.localRotation = Quaternion.Euler(0f, 0f, 45f);
        cabDrop.anchoredPosition = new Vector2(3.5f, y - 4f);

        var baseG = CreateImage(root, "CabochonB", Darken(wine, 0.2f));
        baseG.anchorMin = baseG.anchorMax = new Vector2(0.5f, 0.5f);
        baseG.sizeDelta = new Vector2(22f, 22f);
        baseG.localRotation = Quaternion.Euler(0f, 0f, 45f);
        baseG.anchoredPosition = new Vector2(0f, y);

        var mid = CreateImage(root, "CabochonM", wine);
        mid.anchorMin = mid.anchorMax = new Vector2(0.5f, 0.5f);
        mid.sizeDelta = new Vector2(16f, 16f);
        mid.localRotation = Quaternion.Euler(0f, 0f, 45f);
        mid.anchoredPosition = new Vector2(0f, y);

        var glint = CreateImage(root, "CabochonG", MulAlpha(Lighten(hi, 0.25f), 0.7f));
        glint.anchorMin = glint.anchorMax = new Vector2(0.5f, 0.5f);
        glint.sizeDelta = new Vector2(7f, 7f);
        glint.localRotation = Quaternion.Euler(0f, 0f, 45f);
        glint.anchoredPosition = new Vector2(2.5f, y + 3f);

        var mount = CreateImage(root, "CabochonMount", MulAlpha(Darken(wine, 0.5f), 0.9f));
        mount.anchorMin = mount.anchorMax = new Vector2(0.5f, 0.5f);
        mount.sizeDelta = new Vector2(28f, 5f);
        mount.anchoredPosition = new Vector2(0f, y - 12f);
    }

    /// <summary>Кольцо подложки только между рамкой и полем — центр пустой (фон под пазлом не трогаем).</summary>
    static void AddInnerPadRing(RectTransform root, Vector2 boardSize, float pad, Color color)
    {
        if (pad <= 0.25f || color.a < 0.001f) return;
        float bw = Mathf.Max(1f, boardSize.x * 0.5f);
        float bh = Mathf.Max(1f, boardSize.y * 0.5f);

        var top = CreateImage(root, "InnerPad_T", color);
        top.anchorMin = top.anchorMax = new Vector2(0.5f, 0.5f);
        top.pivot = new Vector2(0.5f, 0.5f);
        top.sizeDelta = new Vector2((bw + pad) * 2f, pad);
        top.anchoredPosition = new Vector2(0f, bh + pad * 0.5f);

        var bot = CreateImage(root, "InnerPad_B", color);
        bot.anchorMin = bot.anchorMax = new Vector2(0.5f, 0.5f);
        bot.pivot = new Vector2(0.5f, 0.5f);
        bot.sizeDelta = new Vector2((bw + pad) * 2f, pad);
        bot.anchoredPosition = new Vector2(0f, -(bh + pad * 0.5f));

        float sideH = 2f * bh + 2f * pad;
        var left = CreateImage(root, "InnerPad_L", color);
        left.anchorMin = left.anchorMax = new Vector2(0.5f, 0.5f);
        left.pivot = new Vector2(0.5f, 0.5f);
        left.sizeDelta = new Vector2(pad, sideH);
        left.anchoredPosition = new Vector2(-(bw + pad * 0.5f), 0f);

        var right = CreateImage(root, "InnerPad_R", color);
        right.anchorMin = right.anchorMax = new Vector2(0.5f, 0.5f);
        right.pivot = new Vector2(0.5f, 0.5f);
        right.sizeDelta = new Vector2(pad, sideH);
        right.anchoredPosition = new Vector2(bw + pad * 0.5f, 0f);
    }

    static void PlaceRelativeToGrid(RectTransform gridRt, RectTransform frameRoot, bool overTiles)
    {
        Transform parent = gridRt.parent;
        if (parent == null) return;
        int gridIdx = gridRt.GetSiblingIndex();
        int frameIdx = frameRoot.GetSiblingIndex();
        if (overTiles)
        {
            int want = Mathf.Clamp(gridIdx + 1, 0, parent.childCount - 1);
            if (frameIdx != want)
                frameRoot.SetSiblingIndex(want);
        }
        else
        {
            if (frameIdx > gridIdx)
                frameRoot.SetSiblingIndex(gridIdx);
        }
    }

    static RectTransform CreateImage(RectTransform parent, string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = WhiteSprite;
        img.color = c;
        img.raycastTarget = false;
        img.preserveAspect = false;
        return rt;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Color Darken(Color c, float t) =>
        Color.Lerp(c, new Color(0.04f, 0.025f, 0.015f, c.a), Mathf.Clamp01(t));

    static Color Lighten(Color c, float t) =>
        Color.Lerp(c, new Color(1f, 0.96f, 0.82f, c.a), Mathf.Clamp01(t));

    static Color MulAlpha(Color c, float a)
    {
        c.a *= a;
        return c;
    }

}
