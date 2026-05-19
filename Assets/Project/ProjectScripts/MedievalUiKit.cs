using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Средневековый UI: виньетка фона, процедурные круглые кнопки, полоска времени «свеча».</summary>
public static class MedievalAtmosphere
{
    const string HolderName = "MedievalAtmosphere";

    /// <summary>Затемнение углов и лёгкая «тканевая» кайма — поверх GameBackground, под игровым полем.</summary>
    public static void Ensure(Transform canvasRoot, Transform insertAfter, bool enabled)
    {
        if (canvasRoot == null) return;
        Transform existing = canvasRoot.Find(HolderName);
        if (!enabled)
        {
            if (existing != null) Object.Destroy(existing.gameObject);
            return;
        }

        RectTransform holder;
        if (existing == null)
        {
            var go = new GameObject(HolderName, typeof(RectTransform));
            holder = go.GetComponent<RectTransform>();
            holder.SetParent(canvasRoot, false);
            Stretch(holder);
            BuildLayers(holder);
        }
        else holder = existing.GetComponent<RectTransform>();

        int idx = insertAfter != null ? insertAfter.GetSiblingIndex() + 1 : 0;
        holder.SetSiblingIndex(Mathf.Clamp(idx, 0, canvasRoot.childCount - 1));
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void BuildLayers(RectTransform root)
    {
        var vignette = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vignette.transform.SetParent(root, false);
        var vrt = vignette.GetComponent<RectTransform>();
        Stretch(vrt);
        var vim = vignette.GetComponent<Image>();
        vim.sprite = UiWhiteSprite.Sprite;
        vim.color = new Color(0.04f, 0.02f, 0.02f, 0.1f);
        vim.raycastTarget = false;

        float corner = 0.38f;
        void Corner(string name, Vector2 anchor, Vector2 pivot)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            g.transform.SetParent(root, false);
            var rt = g.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(720f, 720f);
            rt.anchoredPosition = Vector2.zero;
            var img = g.GetComponent<Image>();
            img.sprite = UiWhiteSprite.Sprite;
            img.color = new Color(0.02f, 0.015f, 0.01f, 0.38f);
            img.raycastTarget = false;
        }

        Corner("VigTL", new Vector2(0f, 1f), new Vector2(0f, 1f));
        Corner("VigTR", new Vector2(1f, 1f), new Vector2(1f, 1f));
        Corner("VigBL", new Vector2(0f, 0f), new Vector2(0f, 0f));
        Corner("VigBR", new Vector2(1f, 0f), new Vector2(1f, 0f));

        var bandT = UiImageFactory.Image(root, "TapestryTop", new Color(0.12f, 0.07f, 0.04f, 0.12f));
        bandT.anchorMin = new Vector2(0f, 1f);
        bandT.anchorMax = new Vector2(1f, 1f);
        bandT.pivot = new Vector2(0.5f, 1f);
        bandT.sizeDelta = new Vector2(0f, 56f);
        bandT.anchoredPosition = new Vector2(0f, -6f);

        var bandB = UiImageFactory.Image(root, "TapestryBottom", new Color(0.1f, 0.06f, 0.035f, 0.1f));
        bandB.anchorMin = new Vector2(0f, 0f);
        bandB.anchorMax = new Vector2(1f, 0f);
        bandB.pivot = new Vector2(0.5f, 0f);
        bandB.sizeDelta = new Vector2(0f, 48f);
        bandB.anchoredPosition = new Vector2(0f, 4f);

        // Задел под будущую анимацию (роза, частицы): пустой маркер — можно повесить скрипт позже.
        new GameObject("FutureAmbientAnimationAnchor", typeof(RectTransform)).transform.SetParent(root, false);
    }
}

public enum MedievalHudIconKind
{
    Home,
    SoundOn,
    SoundOff,
    Pause,
    Refresh,
    Hint
}

/// <summary>Процедурные круглые кнопки и пиктограммы без внешних PNG.</summary>
public static class MedievalHudButtons
{
    const string RootName = "MedievalProc_Root";

    public static void ApplyAll(
        Button home, Button sound, Button pause, Button refresh, Button hint,
        Color rim, Color inner, Color glyph,
        Sprite customHintSprite = null,
        Sprite customRefreshSprite = null)
    {
        if (home != null) Apply(home, MedievalHudIconKind.Home, rim, inner, glyph);
        if (sound != null) Apply(sound, MedievalHudIconKind.SoundOn, rim, inner, glyph);
        if (pause != null) Apply(pause, MedievalHudIconKind.Pause, rim, inner, glyph);
        if (refresh != null)
        {
            if (customRefreshSprite != null) ApplyCustomSprite(refresh, customRefreshSprite);
            else Apply(refresh, MedievalHudIconKind.Refresh, rim, inner, glyph);
        }
        if (hint != null)
        {
            if (customHintSprite != null) ApplyCustomSprite(hint, customHintSprite);
            else Apply(hint, MedievalHudIconKind.Hint, rim, inner, glyph);
        }
    }

    public static void ApplySoundState(Button sound, bool soundOn, Color rim, Color inner, Color glyph)
    {
        if (sound == null) return;
        Apply(sound, soundOn ? MedievalHudIconKind.SoundOn : MedievalHudIconKind.SoundOff, rim, inner, glyph);
    }

    static void ClearProceduralChildren(Button button)
    {
        for (int i = button.transform.childCount - 1; i >= 0; i--)
        {
            var ch = button.transform.GetChild(i);
            if (ch.GetComponent<TextMeshProUGUI>() != null) continue;
            if (ch.name == MedievalHudIconOverlays.OverlayName) continue;
            if (ch.name == RootName || ch.name.StartsWith("MedievalProc"))
                Object.Destroy(ch.gameObject);
        }
    }

    /// <summary>Полная замена кнопки на PNG (без круга и процедурного глифа). Дочерний TMP (счётчик подсказок) не трогаем.</summary>
    static void ApplyCustomSprite(Button button, Sprite sprite)
    {
        if (button == null || sprite == null) return;
        var img = button.image;
        if (img == null)
        {
            img = button.GetComponent<Image>();
            if (img == null)
                img = button.gameObject.AddComponent<Image>();
            button.targetGraphic = img;
        }

        ClearProceduralChildren(button);

        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = true;
        button.targetGraphic = img;
    }

    public static void Apply(Button button, MedievalHudIconKind kind, Color rim, Color inner, Color glyph)
    {
        if (button == null) return;
        var img = button.image;
        if (img == null)
        {
            img = button.GetComponent<Image>();
            if (img == null)
                img = button.gameObject.AddComponent<Image>();
            button.targetGraphic = img;
        }

        ClearProceduralChildren(button);

        img.sprite = UiWhiteSprite.Sprite;
        img.type = Image.Type.Simple;
        img.color = rim;
        img.preserveAspect = false;

        var rootGo = new GameObject(RootName, typeof(RectTransform));
        rootGo.transform.SetParent(button.transform, false);
        var root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = new Vector2(5f, 5f);
        root.offsetMax = new Vector2(-5f, -5f);

        var face = UiImageFactory.Image(root, "Face", inner);
        Stretch(face);
        face.offsetMin = new Vector2(3f, 3f);
        face.offsetMax = new Vector2(-3f, -3f);

        BuildGlyph(face.transform as RectTransform, kind, glyph);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void BuildGlyph(RectTransform parent, MedievalHudIconKind kind, Color c)
    {
        float s = 1f;
        switch (kind)
        {
            case MedievalHudIconKind.Home:
                var roof = UiImageFactory.Image(parent, "Roof", c);
                roof.anchorMin = roof.anchorMax = new Vector2(0.5f, 0.58f);
                roof.sizeDelta = new Vector2(22f * s, 14f * s);
                roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var body = UiImageFactory.Image(parent, "Body", c);
                body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.38f);
                body.sizeDelta = new Vector2(20f * s, 16f * s);
                break;

            case MedievalHudIconKind.SoundOn:
                Box(parent, "Spk", new Vector2(0.42f, 0.5f), new Vector2(10f, 14f), c);
                Box(parent, "W1", new Vector2(0.58f, 0.55f), new Vector2(4f, 6f), c);
                Box(parent, "W2", new Vector2(0.64f, 0.5f), new Vector2(4f, 12f), c);
                Box(parent, "W3", new Vector2(0.71f, 0.5f), new Vector2(4f, 18f), c);
                break;

            case MedievalHudIconKind.SoundOff:
                Box(parent, "Spk", new Vector2(0.42f, 0.5f), new Vector2(10f, 14f), c);
                var line = UiImageFactory.Image(parent, "Line", c);
                line.anchorMin = line.anchorMax = new Vector2(0.62f, 0.5f);
                line.sizeDelta = new Vector2(3f, 22f);
                line.localRotation = Quaternion.Euler(0f, 0f, 52f);
                break;

            case MedievalHudIconKind.Pause:
                Box(parent, "P1", new Vector2(0.44f, 0.5f), new Vector2(5f, 18f), c);
                Box(parent, "P2", new Vector2(0.56f, 0.5f), new Vector2(5f, 18f), c);
                break;

            case MedievalHudIconKind.Refresh:
                Box(parent, "ArrV", new Vector2(0.5f, 0.62f), new Vector2(4f, 12f), c);
                var hook = UiImageFactory.Image(parent, "Hook", c);
                hook.anchorMin = hook.anchorMax = new Vector2(0.52f, 0.48f);
                hook.sizeDelta = new Vector2(18f, 4f);
                hook.localRotation = Quaternion.Euler(0f, 0f, -18f);
                var tip = UiImageFactory.Image(parent, "Tip", c);
                tip.anchorMin = tip.anchorMax = new Vector2(0.62f, 0.66f);
                tip.sizeDelta = new Vector2(8f, 8f);
                tip.localRotation = Quaternion.Euler(0f, 0f, 40f);
                break;

            case MedievalHudIconKind.Hint:
                var bulb = UiImageFactory.Image(parent, "Bulb", c);
                bulb.anchorMin = bulb.anchorMax = new Vector2(0.5f, 0.58f);
                bulb.sizeDelta = new Vector2(16f, 16f);
                var stem = UiImageFactory.Image(parent, "Stem", c);
                stem.anchorMin = stem.anchorMax = new Vector2(0.5f, 0.32f);
                stem.sizeDelta = new Vector2(4f, 12f);
                break;
        }
    }

    static void Box(RectTransform parent, string name, Vector2 anchorNorm, Vector2 size, Color c)
    {
        var rt = UiImageFactory.Image(parent, name, c);
        rt.anchorMin = rt.anchorMax = anchorNorm;
        rt.sizeDelta = size;
    }
}

/// <summary>Полоска «сгоревшей свечи»: воск слева на всю длину остатка, пусто справа — тёмный фон Char; fill слева, убывание = сгорание справа налево.</summary>
public static class MedievalCandleTimer
{
    public const string RootName = "CandleTimerStrip";

    public static void Ensure(
        RectTransform boardParent,
        Vector3 topMidWorld,
        float width,
        out RectTransform root,
        out Image waxFill,
        out Image charBase)
    {
        root = null;
        waxFill = null;
        charBase = null;
        if (boardParent == null) return;

        Transform t = boardParent.Find(RootName);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(RootName, typeof(RectTransform));
            go.transform.SetParent(boardParent, false);
            root = go.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(width, 28f);

            var rim = UiImageFactory.Image(root, "Rim", new Color(0.22f, 0.14f, 0.06f, 1f));
            Stretch(rim);

            charBase = UiImageFactory.Image(root, "Char", new Color(0.12f, 0.1f, 0.09f, 1f)).GetComponent<Image>();
            charBase.rectTransform.anchorMin = new Vector2(0.02f, 0.15f);
            charBase.rectTransform.anchorMax = new Vector2(0.98f, 0.85f);
            charBase.rectTransform.offsetMin = Vector2.zero;
            charBase.rectTransform.offsetMax = Vector2.zero;

            var waxGo = new GameObject("Wax", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            waxGo.transform.SetParent(root, false);
            waxFill = waxGo.GetComponent<Image>();
            waxFill.sprite = UiWhiteSprite.Sprite;
            waxFill.type = Image.Type.Filled;
            waxFill.fillMethod = Image.FillMethod.Horizontal;
            waxFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            waxFill.fillAmount = 1f;
            waxFill.color = new Color(0.72f, 0.55f, 0.22f, 1f);
            waxFill.raycastTarget = false;
            var wrt = waxGo.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.02f, 0.15f);
            wrt.anchorMax = new Vector2(0.98f, 0.85f);
            wrt.offsetMin = Vector2.zero;
            wrt.offsetMax = Vector2.zero;

            var hi = UiImageFactory.Image(root, "WaxHi", new Color(1f, 0.92f, 0.65f, 0.35f));
            hi.anchorMin = new Vector2(0.02f, 0.55f);
            hi.anchorMax = new Vector2(0.98f, 0.78f);
            hi.offsetMin = Vector2.zero;
            hi.offsetMax = Vector2.zero;
        }
        else
        {
            root = t.GetComponent<RectTransform>();
            charBase = root.Find("Char")?.GetComponent<Image>();
            waxFill = root.Find("Wax")?.GetComponent<Image>();
            if (waxFill != null)
            {
                waxFill.type = Image.Type.Filled;
                waxFill.fillMethod = Image.FillMethod.Horizontal;
                waxFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        root.sizeDelta = new Vector2(width, 28f);
        root.position = topMidWorld;
        root.localScale = Vector3.one;
        root.localRotation = Quaternion.identity;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-2f, -2f);
        rt.offsetMax = new Vector2(2f, 2f);
    }
}

static class UiWhiteSprite
{
    static Sprite _sprite;

    public static Sprite Sprite
    {
        get
        {
            if (_sprite == null)
                _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            return _sprite;
        }
    }
}

/// <summary>Декоративные PNG-иконки поверх бежевой процедурной кнопки.</summary>
public static class MedievalHudIconOverlays
{
    public const string OverlayName = "HudIconOverlay";
    public const string MuteLineName = "HudMuteLine";

    public static void Apply(Button button, Sprite sprite, float inset = 4f)
    {
        if (button == null || sprite == null) return;

        Transform existing = button.transform.Find(OverlayName);
        RectTransform rt;
        Image img;
        if (existing == null)
        {
            var go = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(button.transform, false);
            rt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
        }
        else
        {
            rt = existing.GetComponent<RectTransform>();
            img = existing.GetComponent<Image>();
        }

        StretchWithInset(rt, inset);
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;
        rt.SetAsLastSibling();
    }

    /// <summary>Красная диагональ поверх иконки звука (mute), без смены спрайта.</summary>
    public static void SetMuteLine(Button button, bool show)
    {
        if (button == null) return;

        Transform host = button.transform.Find(OverlayName);
        if (host == null) host = button.transform;

        Transform existing = host.Find(MuteLineName);
        if (!show)
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        RectTransform rt;
        Image img;
        if (existing == null)
        {
            var go = new GameObject(MuteLineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(host, false);
            rt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
            img.sprite = UiWhiteSprite.Sprite;
            img.color = new Color(0.88f, 0.1f, 0.08f, 0.95f);
            img.raycastTarget = false;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(5f, 78f);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, 42f);
        }
        else
        {
            existing.gameObject.SetActive(true);
            rt = existing.GetComponent<RectTransform>();
        }

        rt.SetAsLastSibling();
    }

    public static void StretchWithInset(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}

static class UiImageFactory
{
    public static RectTransform Image(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = UiWhiteSprite.Sprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = false;
        return rt;
    }
}
