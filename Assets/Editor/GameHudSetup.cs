#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Одноразовая сборка HUD под твоей иерархией: UI_Panel + HintButton.
/// Unity → Medivoria → Setup GameScene HUD, затем Ctrl+S.
/// </summary>
public static class GameHudSetup
{
    [MenuItem("Medivoria/Setup GameScene HUD")]
    public static void Run()
    {
        var panelGo = GameObject.Find("UI_Panel");
        if (panelGo == null)
        {
            Debug.LogError("GameHudSetup: не найден GameObject UI_Panel.");
            return;
        }

        var panel = panelGo.transform;
        var hint = panel.Find("HintButton");

        EnsureTimerSlider(panel);
        if (hint != null)
        {
            EnsureClonedButton(panel, hint, "RefreshButton", "Обновить");
            EnsureClonedButton(panel, hint, "PauseButton", "Пауза");
            EnsureClonedButton(panel, hint, "SoundButton", "Звук");
            EnsureClonedButton(panel, hint, "RestartButton", "Заново");
            EnsureClonedButton(panel, hint, "MainMenuButton", "Меню");
        }
        else
            Debug.LogWarning("GameHudSetup: HintButton не найден — кнопки не созданы.");

        EnsureTimerText(panel);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("GameHudSetup: готово. Сохрани сцену (Ctrl+S).");
    }

    static void EnsureTimerSlider(Transform panel)
    {
        if (panel.Find("TimerSlider") != null)
            return;

        var res = BuildUiResources();
        if (res.standard == null)
        {
            Debug.LogError("GameHudSetup: не найден встроенный UISprite — создай Slider вручную в Unity.");
            return;
        }

        var go = DefaultControls.CreateSlider(res);
        go.name = "TimerSlider";
        Undo.RegisterCreatedObjectUndo(go, "Create TimerSlider");
        Undo.SetTransformParent(go.transform, panel, "Parent TimerSlider");

        var rt = go.GetComponent<RectTransform>();
        rt.SetAsFirstSibling();
        rt.anchorMin = new Vector2(0.22f, 1f);
        rt.anchorMax = new Vector2(0.78f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 28f);
        rt.anchoredPosition = new Vector2(0f, -56f);

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 360f;
        slider.value = 360f;
        slider.wholeNumbers = false;
        slider.interactable = false;
    }

    static void EnsureClonedButton(Transform panel, Transform hintTemplate, string objectName, string caption)
    {
        if (panel.Find(objectName) != null)
            return;

        var clone = Object.Instantiate(hintTemplate.gameObject, panel);
        clone.name = objectName;
        Undo.RegisterCreatedObjectUndo(clone, "Create " + objectName);

        var label = clone.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = caption;
            label.enableWordWrapping = false;
        }
    }

    static void EnsureTimerText(Transform panel)
    {
        var tt = panel.Find("TimerText");
        if (tt == null)
            return;

        if (tt.GetComponent<TextMeshProUGUI>() != null)
            return;

        var tmp = Undo.AddComponent<TextMeshProUGUI>(tt.gameObject);
        tmp.text = "06:00";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    static DefaultControls.Resources BuildUiResources()
    {
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (sprite == null)
            sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        return new DefaultControls.Resources
        {
            standard = sprite,
            background = sprite,
            inputField = sprite,
            knob = sprite,
            checkmark = sprite,
            dropdown = sprite,
            mask = sprite
        };
    }

    [MenuItem("Medivoria/Open Main Menu Scene")]
    public static void OpenMainMenuScene()
    {
        EditorSceneManager.OpenScene("Assets/Project/ProjectScenes/MainMenu.unity");
    }

    [MenuItem("Medivoria/Open Game Scene")]
    public static void OpenGameScene()
    {
        EditorSceneManager.OpenScene("Assets/GameScene.unity");
    }
}
#endif
