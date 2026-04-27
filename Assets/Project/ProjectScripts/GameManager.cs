using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References (буttons in Canvas)")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI hintCountText;
    public Button hintButton;
    public Button refreshButton;
    public Button pauseButton;
    public Button soundButton;
    public Button homeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public GameObject pausePanel;

    [Header("Confirmation Dialog (создаётся автоматически)")]
    public GameObject confirmationPanel;
    public TextMeshProUGUI confirmationTitle;
    public TextMeshProUGUI confirmationSubtitle;
    public Button confirmationYesButton;
    public Button confirmationNoButton;

    [Header("Game Over UI (создаётся автоматически если не задано)")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverTitle;
    public TextMeshProUGUI gameOverSubtitle;
    public Button gameOverRestartButton;
    public Button gameOverMenuButton;
    public Button gameOverNextButton;
    public Sprite parchmentSprite;

    [Header("Levels")]
    public int maxLevel = 10;
    public bool resetHintsBetweenLevels = true;

    [Header("Typography")]
    public TMP_FontAsset medievalFont;
    public Color textColorDark = new Color(0.32f, 0.16f, 0.06f);
    public Color textColorLight = new Color(0.98f, 0.92f, 0.78f);

    [Header("Timer Colors")]
    public Color timerNormalColor = new Color(0.32f, 0.16f, 0.06f);
    public Color timerWarningColor = new Color(0.7f, 0.1f, 0.1f);
    public float timerWarningThreshold = 30f;

    [Header("Timer Background (пергамент за таймером)")]
    public bool buildTimerParchment = false;   // ВЫКЛЮЧЕНО — больше не плодим пустой пергамент
    public Sprite timerBackgroundSprite;
    public Vector2 timerBackgroundPadding = new Vector2(16f, 8f);

    [Header("Timer Bar")]
    public Image timerBar;
    public Slider timerSlider;

    [Header("Settings")]
    public float gameDuration = 360f;
    public int hintCount = 5;
    [Range(1, 10)] public int currentLevel = 1;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    [Header("HUD Layout")]
    public bool autoArrangeLayout = true;      
    public float iconButtonSize = 90f;
    public float buttonSpacing = 20f;
    public float screenMargin = 60f;
    [Tooltip("Если выключено — Restart/MainMenu на HUD прячутся (остаются Home и кнопки в панелях).")]
    public bool showRestartAndMainMenuOnHud = true;

    [Header("Play Area — пергамент и сетка по центру под таймером")]
    public bool applyPlayAreaLayout = true;   
    public float playAreaSideInset = 200f;     
    public float playAreaTopInset = 160f;      
    public float playAreaBottomInset = 40f;

    private float currentTime;
    private bool isGameRunning = false;
    private bool isPaused = false;
    private bool isSoundOn = true;
    private int matchesFound = 0;
    private int totalPairs = 72;
    private bool roundSettled = false;
    private System.Action pendingConfirmAction;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (hintButton == null)        hintButton = GameObject.Find("HintButton")?.GetComponent<Button>();
        if (refreshButton == null)     refreshButton = GameObject.Find("RefreshButton")?.GetComponent<Button>();
        if (pauseButton == null)       pauseButton = GameObject.Find("PauseButton")?.GetComponent<Button>();
        if (soundButton == null)       soundButton = GameObject.Find("SoundButton")?.GetComponent<Button>();
        if (homeButton == null)        homeButton = GameObject.Find("HomeButton")?.GetComponent<Button>();
        if (restartButton == null)     restartButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
        if (mainMenuButton == null)    mainMenuButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();
        if (pausePanel == null)        pausePanel = GameObject.Find("PausePanel");

        if (timerText == null)     timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        if (hintCountText == null) hintCountText = GameObject.Find("HintCountText")?.GetComponent<TextMeshProUGUI>();
        if (timerBar == null)      timerBar = GameObject.Find("TimerBar")?.GetComponent<Image>();
        if (timerSlider == null)   timerSlider = GameObject.Find("TimerSlider")?.GetComponent<Slider>();

        hintButton?.onClick.AddListener(OnHintButton);
        refreshButton?.onClick.AddListener(OnRefreshButton);
        pauseButton?.onClick.AddListener(OnPauseButton);
        soundButton?.onClick.AddListener(OnSoundButton);
        homeButton?.onClick.AddListener(OnHomeButton);
        restartButton?.onClick.AddListener(OnRestartRequest);
        mainMenuButton?.onClick.AddListener(OnMainMenuButton);
    }

    void Start()
    {
        if (gameDuration < 60f) gameDuration = 360f;

        currentTime = gameDuration;
        if (pausePanel != null) pausePanel.SetActive(false);

        BuildGameOverPanelIfNeeded();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        BuildConfirmationPanelIfNeeded();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        if (!showRestartAndMainMenuOnHud)
        {
            if (restartButton != null) restartButton.gameObject.SetActive(false);
            if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        }

        ConfigureTimerSlider();
        UpdateHintUI();

        ApplyTypographyAndBadge();

        if (buildTimerParchment) BuildTimerParchmentIfNeeded();
        if (applyPlayAreaLayout) ApplyPlayAreaLayout();
        if (autoArrangeLayout) ArrangeHudLayout();

        StartGame();
    }

    void BuildTimerParchmentIfNeeded()
    {
        if (timerText == null) return;
        Sprite spr = timerBackgroundSprite != null ? timerBackgroundSprite : parchmentSprite;
        if (spr == null) return;

        Transform parent = timerText.transform.parent;
        if (parent == null) return;

        Transform existing = parent.Find("TimerParchment");
        if (existing != null) DestroyImmediate(existing.gameObject);

        Canvas.ForceUpdateCanvases();

        GameObject bg = new GameObject("TimerParchment", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);

        var img = bg.GetComponent<Image>();
        img.sprite = spr;
        img.color = Color.white;
        img.preserveAspect = false;
        img.raycastTarget = false;
        if (spr.border != Vector4.zero) img.type = Image.Type.Sliced;

        var rt = bg.GetComponent<RectTransform>();
        var trt = timerText.rectTransform;

        Vector2 timerSize = trt.rect.size;
        rt.anchorMin = trt.anchorMin;
        rt.anchorMax = trt.anchorMax;
        rt.pivot = trt.pivot;
        rt.anchoredPosition = trt.anchoredPosition;

        rt.sizeDelta = timerSize + timerBackgroundPadding;

        rt.anchoredPosition += new Vector2(
            (rt.pivot.x - 0.5f) * timerBackgroundPadding.x,
            (rt.pivot.y - 0.5f) * timerBackgroundPadding.y
        );

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        int timerIndex = timerText.transform.GetSiblingIndex();
        bg.transform.SetSiblingIndex(timerIndex);
    }

    void Update()
    {
        if (isGameRunning && !isPaused && !roundSettled)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                UpdateTimerUI();
                GameOver(false);
                return;
            }
            UpdateTimerUI();
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        roundSettled = false;
        isPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (GameSession.RequestContinue && TryBeginFromSave())
            return;

        currentTime = gameDuration;
        isGameRunning = true;
        matchesFound = 0;
        totalPairs = (GridManager.Instance.columns * GridManager.Instance.rows) / 2;

        ConfigureTimerSlider();
        UpdateTimerUI();
        GridManager.Instance.SetupLevel(currentLevel);
        GridManager.Instance.GenerateGrid();
    }

    bool TryBeginFromSave()
    {
        GameSession.RequestContinue = false;
        GameRunSaveData data = GameRunPersistence.Load();
        if (data == null || data.version != 1) return false;
        if (data.columns != GridManager.Instance.columns || data.rows != GridManager.Instance.rows) return false;

        if (data.saveKind == GameRunSaveData.KindNextLevelAfterVictory)
        {
            currentLevel = Mathf.Clamp(data.level, 1, maxLevel);
            currentTime = Mathf.Max(0f, data.timeRemaining);
            hintCount = Mathf.Max(0, data.hints);
            matchesFound = 0;
            isGameRunning = true;
            totalPairs = (GridManager.Instance.columns * GridManager.Instance.rows) / 2;

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            ConfigureTimerSlider();
            UpdateHintUI();
            UpdateTimerUI();
            GridManager.Instance.SetupLevel(currentLevel);
            GridManager.Instance.GenerateGrid();
            AudioManager.Instance?.PauseMusic(false);
            return true;
        }

        if (data.cells == null || data.cells.Length != data.columns * data.rows) return false;

        currentLevel = Mathf.Clamp(data.level, 1, maxLevel);
        currentTime = Mathf.Max(0f, data.timeRemaining);
        hintCount = Mathf.Max(0, data.hints);
        matchesFound = Mathf.Clamp(data.matches, 0, (data.columns * data.rows) / 2);
        isGameRunning = true;
        totalPairs = (GridManager.Instance.columns * GridManager.Instance.rows) / 2;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        ConfigureTimerSlider();
        UpdateHintUI();
        UpdateTimerUI();
        GridManager.Instance.SetupLevel(currentLevel);
        GridManager.Instance.RestoreGridFromSave(data.cells);
        AudioManager.Instance?.PauseMusic(false);
        return true;
    }

    void OnApplicationQuit() => TrySaveRun();

    void TrySaveRun()
    {
        if (GridManager.Instance == null) return;
        if (!isGameRunning || roundSettled) return;

        var d = new GameRunSaveData
        {
            saveKind = GameRunSaveData.KindMidGame,
            columns = GridManager.Instance.columns,
            rows = GridManager.Instance.rows,
            level = currentLevel,
            timeRemaining = currentTime,
            hints = hintCount,
            matches = matchesFound,
            cells = GridManager.Instance.ExportGridFlat()
        };
        GameRunPersistence.Save(d);
    }

    void SaveAfterVictoryCheckpoint()
    {
        if (GridManager.Instance == null) return;
        if (currentLevel >= maxLevel)
        {
            GameRunPersistence.Clear();
            return;
        }

        var d = new GameRunSaveData
        {
            saveKind = GameRunSaveData.KindNextLevelAfterVictory,
            columns = GridManager.Instance.columns,
            rows = GridManager.Instance.rows,
            level = currentLevel + 1,
            timeRemaining = gameDuration,
            hints = resetHintsBetweenLevels ? 5 : hintCount,
            matches = 0,
            cells = System.Array.Empty<int>()
        };
        GameRunPersistence.Save(d);
    }

    void UpdateTimerUI()
    {
        float t = Mathf.Max(0f, currentTime);
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            timerText.color = t <= timerWarningThreshold ? timerWarningColor : timerNormalColor;
        }
        if (timerBar != null)
        {
            timerBar.fillAmount = t / gameDuration;
            timerBar.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.8f, 0.3f), t / gameDuration);
        }
        if (timerSlider != null && gameDuration > 0f)
        {
            timerSlider.maxValue = gameDuration;
            timerSlider.value = t;
        }
    }

    void ConfigureTimerSlider()
    {
        if (timerSlider == null) return;
        timerSlider.minValue = 0f;
        timerSlider.maxValue = Mathf.Max(1f, gameDuration);
        timerSlider.wholeNumbers = false;
        timerSlider.interactable = false;
        timerSlider.value = Mathf.Max(0f, currentTime);
    }

    public void OnHintButton()
    {
        if (GridManager.Instance.IsBusy || hintCount <= 0 || !isGameRunning || isPaused || roundSettled) return;
        hintCount--;
        UpdateHintUI();
        AudioManager.Instance?.PlayHint();
        GridManager.Instance.ShowHint();
    }

    public void OnRefreshButton()
    {
        if (!isGameRunning || isPaused || roundSettled || GridManager.Instance.IsBusy) return;
        AudioManager.Instance?.PlayShuffle();
        GridManager.Instance.ForceShuffle();
    }

    void UpdateHintUI()
    {
        if (hintCountText != null) hintCountText.text = "x" + hintCount;
        if (hintButton != null) hintButton.interactable = hintCount > 0;
    }

    public void OnPauseButton()
    {
        if (roundSettled) return;
        isPaused = !isPaused;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        AudioManager.Instance?.PauseMusic(isPaused);
        AudioManager.Instance?.PlayPause();
    }

    public void OnResumeButton()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
        AudioManager.Instance?.PauseMusic(false);
    }

    public void OnSoundButton()
    {
        isSoundOn = !isSoundOn;
        if (AudioManager.Instance != null) AudioManager.Instance.SetMuted(!isSoundOn);
        else AudioListener.volume = isSoundOn ? 1f : 0f;
        if (soundButton != null && soundOnSprite != null && soundOffSprite != null)
            soundButton.image.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
    }

    public void OnRestartButton()
    {
        GameRunPersistence.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuButton()
    {
        TrySaveRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnHomeButton()
    {
        if (roundSettled)
        {
            OnMainMenuButton();
            return;
        }

        ShowConfirmation(
            "QUIT TO MENU?",
            "Your progress will be lost",
            ConfirmQuitToMenu
        );
    }

    private void ConfirmQuitToMenu()
    {
        GameRunPersistence.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRestartRequest()
    {
        if (roundSettled) { OnRestartButton(); return; }
        ShowConfirmation("RESTART LEVEL?", "Your progress will reset", OnRestartButton);
    }

    public void OnNextLevelButton()
    {
        GameRunPersistence.Clear();
        if (currentLevel >= maxLevel)
        {
            OnRestartButton();
            return;
        }
        currentLevel++;
        if (resetHintsBetweenLevels) hintCount = 5;
        UpdateHintUI();
        StartGame();
        AudioManager.Instance?.PauseMusic(false);
    }

    public void OnMatchFound()
    {
        matchesFound++;
        AudioManager.Instance?.PlayMatch();
        if (matchesFound >= totalPairs) GameOver(true);
    }

    public void OnWrongMatch() => AudioManager.Instance?.PlayWrong();

    void GameOver(bool won)
    {
        if (roundSettled) return;
        if (won)
        {
            if (currentLevel >= maxLevel) GameRunPersistence.Clear();
            else SaveAfterVictoryCheckpoint();
        }
        else GameRunPersistence.Clear();

        roundSettled = true;
        isGameRunning = false;
        Time.timeScale = 0f;

        if (won) AudioManager.Instance?.PlayWin();
        else     AudioManager.Instance?.PlayLose();
        AudioManager.Instance?.PauseMusic(true);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            bool isFinalLevel = won && currentLevel >= maxLevel;

            if (gameOverTitle != null)
            {
                if (isFinalLevel) gameOverTitle.text = "GAME COMPLETE!";
                else if (won)     gameOverTitle.text = "VICTORY!";
                else              gameOverTitle.text = "TIME'S UP!";
            }
            if (gameOverSubtitle != null)
            {
                if (isFinalLevel) gameOverSubtitle.text = "All levels cleared";
                else if (won)     gameOverSubtitle.text = $"Level {currentLevel} cleared!";
                else              gameOverSubtitle.text = "Try again";
            }

            if (gameOverNextButton != null)
                gameOverNextButton.gameObject.SetActive(won && !isFinalLevel);
        }
    }

    public bool IsGameRunning() => isGameRunning && !isPaused && !roundSettled;

    void BuildGameOverPanelIfNeeded()
    {
        if (gameOverPanel != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("Canvas не найден — GameOverPanel не построен."); return; }

        GameObject overlay = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;
        var oRT = overlay.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.offsetMin = Vector2.zero; oRT.offsetMax = Vector2.zero;
        overlay.transform.SetAsLastSibling();

        GameObject parchment = new GameObject("Parchment", typeof(RectTransform), typeof(Image));
        parchment.transform.SetParent(overlay.transform, false);
        var pImg = parchment.GetComponent<Image>();
        if (parchmentSprite != null) pImg.sprite = parchmentSprite;
        else pImg.color = new Color(0.95f, 0.87f, 0.7f);
        pImg.preserveAspect = true;
        var pRT = parchment.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(560f, 560f);
        pRT.anchoredPosition = Vector2.zero;

        gameOverTitle = CreateText(parchment.transform, "GameOverTitle", "VICTORY!",
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.92f),
            76, textColorDark);

        gameOverSubtitle = CreateText(parchment.transform, "GameOverSubtitle", "Try again",
            new Vector2(0.12f, 0.55f), new Vector2(0.88f, 0.7f),
            34, new Color(0.45f, 0.2f, 0.08f));

        gameOverNextButton = CreateButton(parchment.transform, "GameOverNextButton", "NEXT LEVEL",
            new Vector2(0f, -70f), new Vector2(320f, 70f),
            new Color(0.25f, 0.45f, 0.2f));
        gameOverNextButton.onClick.AddListener(OnNextLevelButton);
        gameOverNextButton.gameObject.SetActive(false);

        gameOverRestartButton = CreateButton(parchment.transform, "GameOverRestartButton", "RESTART",
            new Vector2(-90f, -160f), new Vector2(160f, 56f));
        gameOverRestartButton.onClick.AddListener(OnRestartButton);

        gameOverMenuButton = CreateButton(parchment.transform, "GameOverMenuButton", "MENU",
            new Vector2(90f, -160f), new Vector2(160f, 56f));
        gameOverMenuButton.onClick.AddListener(OnMainMenuButton);

        gameOverPanel = overlay;
    }

    void BuildConfirmationPanelIfNeeded()
    {
        if (confirmationPanel != null) return;
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject overlay = new GameObject("ConfirmationPanel", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        overlayImg.raycastTarget = true;
        var oRT = overlay.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.offsetMin = Vector2.zero; oRT.offsetMax = Vector2.zero;
        overlay.transform.SetAsLastSibling();

        GameObject parchment = new GameObject("ConfirmParchment", typeof(RectTransform), typeof(Image));
        parchment.transform.SetParent(overlay.transform, false);
        var pImg = parchment.GetComponent<Image>();
        if (parchmentSprite != null) pImg.sprite = parchmentSprite;
        else pImg.color = new Color(0.95f, 0.87f, 0.7f);
        pImg.preserveAspect = true;
        var pRT = parchment.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(520f, 460f);
        pRT.anchoredPosition = Vector2.zero;

        confirmationTitle = CreateText(parchment.transform, "ConfirmTitle", "ARE YOU SURE?",
            new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.85f),
            56, textColorDark);

        confirmationSubtitle = CreateText(parchment.transform, "ConfirmSubtitle", "",
            new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.62f),
            26, new Color(0.45f, 0.2f, 0.08f));

        confirmationYesButton = CreateButton(parchment.transform, "ConfirmYesButton", "YES",
            new Vector2(-90f, -120f), new Vector2(160f, 60f),
            new Color(0.25f, 0.45f, 0.2f));
        confirmationYesButton.onClick.AddListener(() =>
        {
            var action = pendingConfirmAction;
            HideConfirmation();
            action?.Invoke();
        });

        confirmationNoButton = CreateButton(parchment.transform, "ConfirmNoButton", "NO",
            new Vector2(90f, -120f), new Vector2(160f, 60f));
        confirmationNoButton.onClick.AddListener(HideConfirmation);

        confirmationPanel = overlay;
    }

    public void ShowConfirmation(string title, string subtitle, System.Action onYes)
    {
        if (confirmationPanel == null) BuildConfirmationPanelIfNeeded();
        if (confirmationPanel == null) { onYes?.Invoke(); return; }

        if (confirmationTitle != null) confirmationTitle.text = title;
        if (confirmationSubtitle != null) confirmationSubtitle.text = subtitle;

        pendingConfirmAction = onYes;
        confirmationPanel.SetActive(true);
        confirmationPanel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void HideConfirmation()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        pendingConfirmAction = null;
        if (!isPaused && !roundSettled) Time.timeScale = 1f;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, string text,
                                Vector2 anchorMin, Vector2 anchorMax,
                                float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (medievalFont != null) tmp.font = medievalFont;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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
        tmp.color = textColorLight;
        tmp.fontStyle = FontStyles.Bold;
        if (medievalFont != null) tmp.font = medievalFont;
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return btn;
    }

    void ApplyTypographyAndBadge()
    {
        if (timerText != null && medievalFont != null)
            timerText.font = medievalFont;

        if (hintCountText != null && hintButton != null)
        {
            if (hintCountText.transform.parent != hintButton.transform)
                hintCountText.transform.SetParent(hintButton.transform, false);

            var rt = hintCountText.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(40f, 28f);
            rt.anchoredPosition = new Vector2(-4f, 4f);

            if (medievalFont != null) hintCountText.font = medievalFont;
            hintCountText.fontSize = 22f;
            hintCountText.alignment = TextAlignmentOptions.Center;
            hintCountText.fontStyle = FontStyles.Bold;
            hintCountText.color = textColorLight;
            hintCountText.transform.SetAsLastSibling();
        }
    }

    void ApplyPlayAreaLayout()
    {
        var bg = GameObject.Find("GameBackground")?.GetComponent<RectTransform>();
        var gridRt = GameObject.Find("Grid")?.GetComponent<RectTransform>();
        if (bg == null || gridRt == null || GridManager.Instance == null) return;

        float side = Mathf.Max(0f, playAreaSideInset);
        float top = Mathf.Max(0f, playAreaTopInset);
        float bottom = Mathf.Max(0f, playAreaBottomInset);

        ApplySymmetricScreenInsets(bg, side, top, bottom);
        ApplySymmetricScreenInsets(gridRt, side, top, bottom);

        Canvas.ForceUpdateCanvases();

        var gridLayout = gridRt.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return;

        int cols = GridManager.Instance.columns;
        int rows = GridManager.Instance.rows;
        if (cols <= 0 || rows <= 0) return;

        float w = gridRt.rect.width;
        float h = gridRt.rect.height;
        if (w <= 1f || h <= 1f) return;

        float cell = Mathf.Floor(Mathf.Min(w / cols, h / rows));
        if (cell < 1f) return;

        gridLayout.cellSize = new Vector2(cell, cell);

        float padX = (w - cell * cols) * 0.5f;
        float padY = (h - cell * rows) * 0.5f;
        gridLayout.padding.left = Mathf.Max(0, Mathf.RoundToInt(padX));
        gridLayout.padding.right = Mathf.Max(0, Mathf.RoundToInt(padX));
        gridLayout.padding.top = Mathf.Max(0, Mathf.RoundToInt(padY));
        gridLayout.padding.bottom = Mathf.Max(0, Mathf.RoundToInt(padY));
    }

    static void ApplySymmetricScreenInsets(RectTransform rt, float side, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(side, bottom);
        rt.offsetMax = new Vector2(-side, -top);
    }

    void ArrangeHudLayout()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        float m = screenMargin;
        float s = iconButtonSize;
        float sp = buttonSpacing;

        // ── ЛЕВАЯ СТОРОНА (Hint, Refresh) ──
        PlaceIcon(hintButton,    new Vector2(0f, 1f), new Vector2(m, -m),                    s);
        PlaceIcon(refreshButton, new Vector2(0f, 1f), new Vector2(m, -(m + s + sp)),         s);

        // ── ПРАВАЯ СТОРОНА (Home, Sound, Pause сверху вниз) ──
        PlaceIcon(homeButton,    new Vector2(1f, 1f), new Vector2(-m, -m),                          s);
        PlaceIcon(soundButton,   new Vector2(1f, 1f), new Vector2(-m, -(m + s + sp)),               s);
        PlaceIcon(pauseButton,   new Vector2(1f, 1f), new Vector2(-m, -(m + (s + sp) * 2)),         s);

        // ── HINT-COUNTER на кнопке Hint ──
        if (hintCountText != null && hintButton != null)
        {
            if (hintCountText.transform.parent != hintButton.transform)
                hintCountText.transform.SetParent(hintButton.transform, false);

            var rt = hintCountText.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(40f, 28f);
            rt.anchoredPosition = new Vector2(-4f, 4f);

            hintCountText.fontSize = 22f;
            hintCountText.alignment = TextAlignmentOptions.Center;
            hintCountText.fontStyle = FontStyles.Bold;
            hintCountText.color = textColorLight;
            if (medievalFont != null) hintCountText.font = medievalFont;
            hintCountText.transform.SetAsLastSibling();
        }

        // ── ТАЙМЕР ПО ЦЕНТРУ СВЕРХУ ──
        if (timerText != null)
        {
            if (timerText.transform.parent != canvas.transform)
                timerText.transform.SetParent(canvas.transform, false);

            var rt = timerText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(320f, 80f);
            rt.anchoredPosition = new Vector2(0f, -m);
            timerText.fontSize = 64f;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.fontStyle = FontStyles.Bold;
            if (medievalFont != null) timerText.font = medievalFont;
        }

        if (timerBar != null)
        {
            var rt = timerBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(440f, 16f);
            rt.anchoredPosition = new Vector2(0f, -(m + 90f));
        }

        if (timerSlider != null)
        {
            var rt = timerSlider.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(440f, 16f);
            rt.anchoredPosition = new Vector2(0f, -(m + 90f));
        }
    }

    void PlaceIcon(Button button, Vector2 anchor, Vector2 anchoredPosition, float size)
    {
        if (button == null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null && button.transform.parent != canvas.transform)
            button.transform.SetParent(canvas.transform, false);

        var rt = button.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = anchoredPosition;

        foreach (Transform child in button.transform)
        {
            var childRT = child as RectTransform;
            if (childRT == null) continue;
            childRT.anchorMin = Vector2.zero;
            childRT.anchorMax = Vector2.one;
            childRT.offsetMin = Vector2.zero;
            childRT.offsetMax = Vector2.zero;
            childRT.localScale = Vector3.one;
            var childImg = child.GetComponent<Image>();
            if (childImg != null) childImg.preserveAspect = true;
        }

        var img = button.GetComponent<Image>();
        if (img != null) img.preserveAspect = true;

        button.transform.SetAsLastSibling();
    }
}