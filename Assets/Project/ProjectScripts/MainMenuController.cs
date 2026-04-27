using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] string gameSceneName = "GameScene";
    [SerializeField] TMP_FontAsset menuFont;
    [SerializeField] Sprite menuButtonParchment;
    [SerializeField] Color menuTextColor = new Color(0.32f, 0.16f, 0.06f);
    [SerializeField] Vector2 menuButtonSize = new Vector2(420f, 72f);

    Button newGameButton;
    Button continueButton;

    void Awake()
    {
        var playGo = GameObject.Find("Button_Play");
        if (playGo != null)
        {
            newGameButton = playGo.GetComponent<Button>();
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGame);
            StyleMenuButton(playGo, "NEW GAME");
            HideChildIcon(playGo.transform);
        }

        EnsureContinueButton();
        if (continueButton == null)
            continueButton = GameObject.Find("Button_Continue")?.GetComponent<Button>();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinue);
            StyleMenuButton(continueButton.gameObject, "CONTINUE");
            HideChildIcon(continueButton.transform);
        }

        FixMenuButtonOrder();

        var quit = GameObject.Find("QuitButton")?.GetComponent<Button>();
        quit?.onClick.AddListener(OnQuit);
    }

    void Start()
    {
        SyncContinueAvailability();
    }

    void OnNewGame()
    {
        GameRunPersistence.Clear();
        GameSession.RequestContinue = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    void OnContinue()
    {
        if (!GameRunPersistence.HasSave()) return;
        GameSession.RequestContinue = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void EnsureContinueButton()
    {
        if (GameObject.Find("Button_Continue") != null) return;
        var play = GameObject.Find("Button_Play");
        var container = GameObject.Find("Buttons_Container")?.transform;
        if (play == null || container == null) return;

        var clone = Instantiate(play, container);
        clone.name = "Button_Continue";
        clone.transform.SetAsLastSibling();
        continueButton = clone.GetComponent<Button>();
    }

    void FixMenuButtonOrder()
    {
        if (newGameButton == null || continueButton == null) return;
        newGameButton.transform.SetAsFirstSibling();
        continueButton.transform.SetSiblingIndex(1);
    }

    void SyncContinueAvailability()
    {
        if (continueButton == null) return;
        continueButton.interactable = true;
        var colors = continueButton.colors;
        colors.disabledColor = colors.normalColor;
        continueButton.colors = colors;
        foreach (var tmp in continueButton.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.color = menuTextColor;
    }

    void StyleMenuButton(GameObject root, string label)
    {
        var img = root.GetComponent<Image>();
        if (img != null && menuButtonParchment != null)
        {
            img.sprite = menuButtonParchment;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }

        var rt = root.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = menuButtonSize;

        var layout = root.GetComponent<LayoutElement>();
        if (layout == null) layout = root.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = menuButtonSize.x;
        layout.preferredWidth = menuButtonSize.x;
        layout.minHeight = menuButtonSize.y;
        layout.preferredHeight = menuButtonSize.y;

        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.text = label;
            if (menuFont != null)
            {
                tmp.font = menuFont;
                tmp.fontSharedMaterial = menuFont.material;
            }
            tmp.color = menuTextColor;
            tmp.fontSize = 36f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = 44f;
        }
    }

    static void HideChildIcon(Transform root)
    {
        var icon = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in icon)
        {
            if (t != null && t.name == "icon")
            {
                t.gameObject.SetActive(false);
                return;
            }
        }
    }
}
