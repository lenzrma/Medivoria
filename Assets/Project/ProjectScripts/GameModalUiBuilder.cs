using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Процедурные модальные панели Game Over / Confirm (SRP).</summary>
public sealed class GameModalUiBuilder
{
    readonly TMP_FontAsset _font;
    readonly Color _textDark;
    readonly Color _textLight;
    readonly Sprite _parchment;

    public GameModalUiBuilder(TMP_FontAsset font, Color textDark, Color textLight, Sprite parchment)
    {
        _font = font;
        _textDark = textDark;
        _textLight = textLight;
        _parchment = parchment;
    }

    public GameObject BuildGameOverPanel(
        Canvas canvas,
        out TextMeshProUGUI title,
        out TextMeshProUGUI subtitle,
        out Button nextButton,
        out Button restartButton,
        out Button menuButton,
        UnityEngine.Events.UnityAction onNext,
        UnityEngine.Events.UnityAction onRestart,
        UnityEngine.Events.UnityAction onMenu)
    {
        GameObject overlay = CreateOverlay(canvas.transform, "GameOverPanel");
        GameObject parchment = CreateParchment(overlay.transform, 560f);

        title = CreateText(parchment.transform, "GameOverTitle", "VICTORY!",
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.92f), 76, _textDark);
        subtitle = CreateText(parchment.transform, "GameOverSubtitle", "Try again",
            new Vector2(0.12f, 0.55f), new Vector2(0.88f, 0.7f), 34, new Color(0.45f, 0.2f, 0.08f));

        nextButton = CreateButton(parchment.transform, "GameOverNextButton", "NEXT LEVEL",
            new Vector2(0f, -70f), new Vector2(320f, 70f), new Color(0.25f, 0.45f, 0.2f));
        nextButton.onClick.AddListener(onNext);
        nextButton.gameObject.SetActive(false);

        restartButton = CreateButton(parchment.transform, "GameOverRestartButton", "RESTART",
            new Vector2(-90f, -160f), new Vector2(160f, 56f));
        restartButton.onClick.AddListener(onRestart);

        menuButton = CreateButton(parchment.transform, "GameOverMenuButton", "MENU",
            new Vector2(90f, -160f), new Vector2(160f, 56f));
        menuButton.onClick.AddListener(onMenu);

        return overlay;
    }

    public GameObject BuildConfirmationPanel(
        Canvas canvas,
        out TextMeshProUGUI title,
        out TextMeshProUGUI subtitle,
        out Button yesButton,
        out Button noButton,
        UnityEngine.Events.UnityAction onYes,
        UnityEngine.Events.UnityAction onNo)
    {
        GameObject overlay = CreateOverlay(canvas.transform, "ConfirmationPanel");
        GameObject parchment = CreateParchment(overlay.transform, 520f);

        title = CreateText(parchment.transform, "ConfirmTitle", "ARE YOU SURE?",
            new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.85f), 56, _textDark);
        subtitle = CreateText(parchment.transform, "ConfirmSubtitle", "",
            new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.62f), 26, new Color(0.45f, 0.2f, 0.08f));

        yesButton = CreateButton(parchment.transform, "ConfirmYesButton", "YES",
            new Vector2(-90f, -120f), new Vector2(160f, 60f), new Color(0.25f, 0.45f, 0.2f));
        yesButton.onClick.AddListener(onYes);

        noButton = CreateButton(parchment.transform, "ConfirmNoButton", "NO",
            new Vector2(90f, -120f), new Vector2(160f, 60f));
        noButton.onClick.AddListener(onNo);

        return overlay;
    }

    GameObject CreateOverlay(Transform parent, string name)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;
        var oRT = overlay.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero;
        oRT.anchorMax = Vector2.one;
        oRT.offsetMin = Vector2.zero;
        oRT.offsetMax = Vector2.zero;
        overlay.transform.SetAsLastSibling();
        return overlay;
    }

    GameObject CreateParchment(Transform parent, float size)
    {
        GameObject parchment = new GameObject("Parchment", typeof(RectTransform), typeof(Image));
        parchment.transform.SetParent(parent, false);
        var pImg = parchment.GetComponent<Image>();
        if (_parchment != null) pImg.sprite = _parchment;
        else pImg.color = new Color(0.95f, 0.87f, 0.7f);
        pImg.preserveAspect = true;
        var pRT = parchment.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(size, size);
        pRT.anchoredPosition = Vector2.zero;
        return parchment;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (_font != null) tmp.font = _font;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return tmp;
    }

    Button CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, Color? bgColor = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bgColor ?? new Color(0.45f, 0.28f, 0.15f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        GameObject txt = new GameObject("Label", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = _textLight;
        tmp.fontStyle = FontStyles.Bold;
        if (_font != null) tmp.font = _font;
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }
}
