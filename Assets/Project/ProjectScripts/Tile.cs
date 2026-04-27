using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    [Header("Components")]
    public Image iconImage;
    public Image tileBackground;
    public GameObject selectionGlow;

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 1f);
    public Color selectedColor = new Color(0.7f, 1f, 0.7f, 1f);
    public Color highlightColor = new Color(1f, 1f, 0.3f, 1f);

    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int TileType { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsSelected { get; private set; }

    public void Init(int x, int y, int type, Sprite sprite)
    {
        GridX = x; GridY = y; TileType = type;
        IsMatched = false; IsSelected = false;
        if (iconImage) { iconImage.sprite = sprite; iconImage.enabled = true; }
        if (tileBackground) tileBackground.color = normalColor;
        if (selectionGlow) selectionGlow.SetActive(false);
        var cg = GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; }
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);
    }

    public void Reinit(int type, Sprite sprite)
    {
        TileType = type;
        IsMatched = false;
        IsSelected = false;
        if (iconImage) { iconImage.sprite = sprite; iconImage.enabled = true; }
        if (tileBackground) tileBackground.color = normalColor;
        if (selectionGlow) selectionGlow.SetActive(false);
        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    public void SetEmpty()
    {
        IsMatched = true;
        IsSelected = false;
        if (selectionGlow) selectionGlow.SetActive(false);
        if (iconImage) iconImage.enabled = false;
        if (tileBackground) tileBackground.color = new Color(0, 0, 0, 0);
        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 0f;
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsMatched) return;
        GridManager.Instance.OnTileClicked(this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (tileBackground) tileBackground.color = selected ? selectedColor : normalColor;
        if (selectionGlow) selectionGlow.SetActive(selected);
    }

    public void SetMatched()
    {
        IsMatched = true; IsSelected = false;
        StartCoroutine(DisappearAnimation());
    }

    public void SetHighlight(bool on)
    {
        if (tileBackground) tileBackground.color = on ? highlightColor : normalColor;
    }

    IEnumerator DisappearAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cg.alpha = 1f - t;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }

        cg.alpha = 0f;
        SetEmpty();
    }
}
