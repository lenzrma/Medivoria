using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Рассыпание иконки плитки в мелкие «песчинки» (UI Images + простая физика в локали канваса).</summary>
public static class TileSandBurstEffect
{
    struct Grain
    {
        public RectTransform Rt;
        public Image Img;
        public Vector2 Vel;
        public float RotSpeed;
        public float StartAlpha;
    }

    static Sprite _white;

    static Sprite WhiteSprite
    {
        get
        {
            if (_white == null)
                _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            return _white;
        }
    }

    public static IEnumerator Run(RectTransform tileRoot, Image iconImage, Image tileBackground)
    {
        if (tileRoot == null || iconImage == null || iconImage.sprite == null)
            yield break;

        Canvas canvas = tileRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
            yield break;

        RectTransform canvasRt = canvas.rootCanvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Color sandLo = new Color(0.5f, 0.42f, 0.32f, 1f);
        Color sandHi = new Color(0.72f, 0.64f, 0.5f, 1f);
        Color iconColorStart = iconImage.color;

        Vector3[] corners = new Vector3[4];
        iconImage.rectTransform.GetWorldCorners(corners);

        const int count = 42;
        float gravity = -620f;
        float duration = 1.18f;

        var grains = new List<Grain>(count);
        for (int i = 0; i < count; i++)
        {
            float rx = Random.value;
            float ry = Random.value;
            Vector3 bottom = Vector3.Lerp(corners[0], corners[3], rx);
            Vector3 top = Vector3.Lerp(corners[1], corners[2], rx);
            Vector3 world = Vector3.Lerp(bottom, top, ry);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    RectTransformUtility.WorldToScreenPoint(cam, world),
                    cam,
                    out Vector2 local))
                continue;

            var go = new GameObject("SandGrain", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasRt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float sz = Random.Range(1f, 2.35f);
            rt.sizeDelta = new Vector2(sz, sz * Random.Range(0.85f, 1.12f));
            rt.anchoredPosition = local;
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var img = go.GetComponent<Image>();
            img.sprite = WhiteSprite;
            img.raycastTarget = false;
            img.preserveAspect = false;
            Color c = Color.Lerp(
                Color.Lerp(sandLo, sandHi, Random.value),
                iconColorStart,
                Random.Range(0.15f, 0.45f));
            c.a = 1f;
            img.color = c;
            go.transform.SetAsLastSibling();

            Vector2 vel = new Vector2(
                Random.Range(-95f, 95f),
                Random.Range(18f, 85f));
            grains.Add(new Grain
            {
                Rt = rt,
                Img = img,
                Vel = vel,
                RotSpeed = Random.Range(-320f, 320f),
                StartAlpha = c.a
            });
        }

        Color bgStart = tileBackground != null ? tileBackground.color : Color.clear;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float dt = Time.deltaTime;
            float u = elapsed / duration;

            float tileFade = Mathf.Clamp01(u * 1.55f);
            if (tileBackground != null)
            {
                var bc = bgStart;
                bc.a = Mathf.Lerp(bgStart.a, 0f, tileFade);
                tileBackground.color = bc;
            }
            if (iconImage != null)
            {
                Color ic = iconColorStart;
                ic.a = Mathf.Lerp(iconColorStart.a, 0f, tileFade);
                iconImage.color = ic;
            }

            for (int i = 0; i < grains.Count; i++)
            {
                Grain g = grains[i];
                if (g.Rt == null) continue;
                g.Vel.y += gravity * dt;
                g.Rt.anchoredPosition += g.Vel * dt;
                g.Rt.Rotate(0f, 0f, g.RotSpeed * dt);
                float fade = 1f - (u * u);
                Color col = g.Img.color;
                col.a = g.StartAlpha * fade;
                g.Img.color = col;
                float sc = Mathf.Lerp(1f, 0.25f, u);
                g.Rt.localScale = Vector3.one * sc;
                grains[i] = g;
            }

            yield return null;
        }

        for (int i = 0; i < grains.Count; i++)
        {
            if (grains[i].Rt != null)
                Object.Destroy(grains[i].Rt.gameObject);
        }

        if (iconImage != null)
        {
            Color ic = iconColorStart;
            ic.a = 0f;
            iconImage.color = ic;
        }
    }
}
