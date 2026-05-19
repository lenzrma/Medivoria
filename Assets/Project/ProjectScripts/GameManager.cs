using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum PlayAreaClusterHorizontalAlign
{
    Center,
    Left,
    Right
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    /// <summary>Подложка под плитками внутри Grid — не участвует в расчёте кластера для рамки.</summary>
    public const string PlayBoardMatteChildName = "BoardMatte";

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
    [Tooltip("Цифровой текст MM:SS сверху. Время по-прежнему видно по полоске-свече и слайдеру (если включены).")]
    public bool showDigitalTimer = false;

    [Header("Timer Background (пергамент за таймером)")]
    public bool buildTimerParchment = false;
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
    public float buttonSpacing = 8f;
    public float screenMargin = 60f;
    [Tooltip("Доп. сдвиг вправо для таймера, Hint и Refresh (подгонка под рамку поля).")]
    public float hudNudgeRight = 0f;
    [Tooltip("Если выключено — Restart/MainMenu на HUD прячутся (остаются Home и кнопки в панелях).")]
    public bool showRestartAndMainMenuOnHud = true;

    [Header("Play Area — пергамент и сетка по центру под таймером")]
    public bool applyPlayAreaLayout = true;
    [Tooltip("Отступ слева/справа от края канваса до зоны Grid. Меньше — крупнее поле; оставь запас под колонки иконок.")]
    public float playAreaSideInset = 118f;
    [Tooltip("Отступ сверху (таймер, свеча). Меньше — выше/крупнее поле.")]
    public float playAreaTopInset = 118f;
    [Tooltip("Отступ снизу под декор и запас от края.")]
    public float playAreaBottomInset = 40f;
    [Tooltip("Доп. отступ слева и справа — меньше плитки (оставь запас под иконки слева/справа).")]
    public float playAreaShrinkExtra = 48f;
    [Tooltip("Доп. отступ только справа от края экрана: сдвигает всю зону игры влево, под картинку на фоне (принцесса/лес справа).")]
    public float playAreaNudgeLeft = 96f;
    [Tooltip("Кластер плиток внутри зоны: Left — прижать к левому краю зоны (логично вместе с nudge влево). По вертикали по-прежнему по центру зоны.")]
    public PlayAreaClusterHorizontalAlign playAreaClusterHorizAlign = PlayAreaClusterHorizontalAlign.Left;
    [Tooltip("Включить цветную рамку в зазоре между плитками и деревянной рамкой. ВКЛЮЧИ для синей каймы.")]
    public bool playAreaBoardMatteEnabled = false;
    [Tooltip("Цвет каймы в зазоре между плитками и рамкой. Синий под платье принцессы: ~(0.18, 0.30, 0.55).")]
    public Color playAreaBoardMatteColor = new Color(0.18f, 0.30f, 0.55f, 1f);

    [Header("Play Area — рамка (все уровни)")]
    [Tooltip("Собрать рамку из UI-слоёв (бронза/золото, узор) без PNG. Если включено — спрайт ниже игнорируется.")]
    public bool useProceduralBoardFrame = true;
    public Color proceduralFrameShadow = new Color(0f, 0f, 0f, 0.22f);
    public Color proceduralFrameBandDark = new Color(0.2f, 0.12f, 0.065f, 1f);
    public Color proceduralFrameGold = new Color(0.58f, 0.42f, 0.16f, 1f);
    public Color proceduralFrameGoldBright = new Color(0.88f, 0.76f, 0.42f, 1f);
    public Color proceduralFrameInnerLine = new Color(0.09f, 0.05f, 0.028f, 1f);
    public float proceduralFrameThick1 = 11f;
    public float proceduralFrameThick2 = 7f;
    public float proceduralFrameThick3 = 5f;
    public float proceduralFrameThick4 = 2f;
    [Range(0, 16)] public int proceduralFrameNotches = 6;
    [Tooltip("Цветной зазор только между внутренней кромкой рамки и полем (кольцо). Центр не закрашивается — картинка под пазлом не перекрывается. 0 = без заливки.")]
    public float proceduralFrameInnerPad = 14f;
    [Tooltip("Тёмно-синий под платье принцессы на фоне (или другой оттенок в инспекторе).")]
    public Color proceduralFrameInnerPadColor = new Color(0.07f, 0.14f, 0.38f, 1f);

    [Header("Medieval atmosphere & HUD")]
    [Tooltip("Виньетка по углам и лёгкие полосы — стильнее плоского пергамента. Якорь FutureAmbientAnimationAnchor для будущей розы/частиц.")]
    public bool enableMedievalAtmosphere = true;
    [Tooltip("Если задан — на Hint вместо процедурной иконки показывается этот спрайт (полная кнопка).")]
    public Sprite medievalHudHintSprite;
    [Tooltip("Если задан — на Refresh вместо процедурной иконки показывается этот спрайт.")]
    public Sprite medievalHudRefreshSprite;

    [Header("HUD — PNG-иконки поверх бежевых кнопок")]
    public bool useHudIconOverlays = true;
    [Range(0f, 24f)] public float hudIconOverlayInset = 2f;
    public Sprite hudIconHome;
    public Sprite hudIconSoundOn;
    public Sprite hudIconPause;
    public Sprite hudIconHint;
    public Sprite hudIconRefresh;

    [Tooltip("Полоска «свечи» над кластером плиток: слева обуглено, справа остаток времени.")]
    public bool useCandleTimerStrip = true;
    [Range(0.5f, 1f)] public float candleStripWidthFactor = 0.92f;
    [Tooltip("Доп. сдвиг полоски времени вверх от верха кластера плиток (зазор над рамкой).")]
    public float candleStripLift = 56f;

    [Header("Fast match time bonus (как в Mahjong)")]
    [Tooltip("Макс. секунд между двумя удачными парами, чтобы получить бонус.")]
    public float fastMatchPairWindow = 4f;
    [Tooltip("Секунд добавляется к таймеру за быструю серию.")]
    public float fastMatchBonusSeconds = 2.5f;
    [Tooltip("Потолок времени как доля от gameDuration (1.5 = не больше +50% к старту).")]
    public float timeBonusMaxMultiplier = 1.5f;

    [Tooltip("Спрайт рамки, если процедурная выключена.")]
    public Sprite playAreaFrameSprite;
    [Tooltip("Насколько рамка шире кластера плиток (reference resolution).")]
    public float playAreaFrameOutset = 24f;
    [Tooltip("Вкл: рамка поверх плиток — виден бордюр (используй Border Clearance). Выкл: рамка под плитками — бордюр может быть закрыт плитками.")]
    public bool playAreaFrameOverTiles = true;
    [Tooltip("Базовый отступ плиток от края панели Grid с каждой стороны (reference resolution).")]
    public float playAreaFrameInnerPadding = 0f;
    [Tooltip("Доп. отступ только когда рамка поверх плиток — зазор между плитками и внутренней кромкой рамки. Меньше = плотнее к рамке; если орнамент заезжает на плитки — чуть подними.")]
    public float playAreaFrameBorderClearance = 6f;

    private float currentTime;
    private bool isGameRunning = false;
    private bool isPaused = false;
    private bool isSoundOn = true;
    private int matchesFound = 0;
    private int totalPairs = 72;
    private bool roundSettled = false;
    private System.Action pendingConfirmAction;
    private Coroutine playBackgroundRevealRoutine;
    private static readonly Vector3[] CornerScratch = new Vector3[4];
    private Image candleWaxFill;
    private float lastMatchRealTime = -999f;
    private GameTimerPresenter _timerPresenter;
    private GameModalUiBuilder _modalUi;

    public void RefreshPlayAreaLayout()
    {
        if (applyPlayAreaLayout) ApplyPlayAreaLayout();
        if (autoArrangeLayout) ArrangeHudLayout();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (GetComponent<GameInputController>() == null)
            gameObject.AddComponent<GameInputController>();

        if (hintButton == null)        hintButton = GameObject.Find("HintButton")?.GetComponent<Button>();
        if (refreshButton == null)     refreshButton = GameObject.Find("RefreshButton")?.GetComponent<Button>();
        if (pauseButton == null)       pauseButton = GameObject.Find("PauseButton")?.GetComponent<Button>();
        if (soundButton == null)       soundButton = GameObject.Find("SoundButton")?.GetComponent<Button>();
        if (homeButton == null)        homeButton = GameObject.Find("HomeButton")?.GetComponent<Button>();
        if (homeButton == null)        homeButton = GameObject.Find("Button (TMP)")?.GetComponent<Button>();
        if (homeButton == null)
        {
            var backToMenu = FindFirstObjectByType<BackToMenu>();
            if (backToMenu != null) homeButton = backToMenu.GetComponent<Button>();
        }
        if (restartButton == null)     restartButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
        if (mainMenuButton == null)    mainMenuButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();
        if (pausePanel == null)        pausePanel = GameObject.Find("PausePanel");

        if (timerText == null)     timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        if (hintCountText == null) hintCountText = GameObject.Find("HintCountText")?.GetComponent<TextMeshProUGUI>();
        if (timerBar == null)      timerBar = GameObject.Find("TimerBar")?.GetComponent<Image>();
        if (timerSlider == null)   timerSlider = GameObject.Find("TimerSlider")?.GetComponent<Slider>();

        EnsureHudIconSprites();

        hintButton?.onClick.AddListener(OnHintButton);
        refreshButton?.onClick.AddListener(OnRefreshButton);
        pauseButton?.onClick.AddListener(OnPauseButton);
        soundButton?.onClick.AddListener(OnSoundButton);
        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(OnHomeButton);
        }
        restartButton?.onClick.AddListener(OnRestartRequest);
        mainMenuButton?.onClick.AddListener(OnMainMenuButton);
    }

    void Start()
    {
        if (gameDuration < 60f) gameDuration = 360f;

        currentTime = gameDuration;
        if (pausePanel != null) pausePanel.SetActive(false);

        _modalUi = new GameModalUiBuilder(medievalFont, textColorDark, textColorLight, parchmentSprite);

        BuildGameOverPanelIfNeeded();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        BuildConfirmationPanelIfNeeded();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        if (!showRestartAndMainMenuOnHud)
        {
            if (restartButton != null) restartButton.gameObject.SetActive(false);
            if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        }

        ApplyTypographyAndBadge();

        if (buildTimerParchment) BuildTimerParchmentIfNeeded();
        if (applyPlayAreaLayout)
        {
            ApplyPlayAreaLayout();
            StartCoroutine(ApplyPlayAreaLayoutEndOfFrame());
        }
        if (autoArrangeLayout) ArrangeHudLayout();

        if (!showDigitalTimer && timerText != null)
            timerText.gameObject.SetActive(false);

        GameScenePresentation.EnsureOnCamera(Camera.main, enableMedievalAtmosphere);

        if (!autoArrangeLayout)
            ApplyMedievalHudStyling();

        EnsureTimerPresenter();
        ConfigureTimerSlider();
        UpdateTimerUI();
        UpdateHintUI();

        StartGame();
    }

    IEnumerator ApplyPlayAreaLayoutEndOfFrame()
    {
        yield return null;
        if (applyPlayAreaLayout) ApplyPlayAreaLayout();
        if (autoArrangeLayout) ArrangeHudLayout();
        EnsureTimerPresenter();
        UpdateTimerUI();
    }

    void EnsureTimerPresenter()
    {
        if (_timerPresenter == null)
        {
            _timerPresenter = new GameTimerPresenter(
                timerText, timerBar, timerSlider, candleWaxFill,
                timerNormalColor, timerWarningColor, timerWarningThreshold,
                showDigitalTimer, timeBonusMaxMultiplier);
        }
        else
            _timerPresenter.SetCandleWaxFill(candleWaxFill);
    }

    void BuildTimerParchmentIfNeeded()
    {
        if (!showDigitalTimer || timerText == null) return;
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
                GameEvents.RaiseTimeUp();
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
        lastMatchRealTime = -999f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (GameSession.RequestContinue && TryBeginFromSave())
        {
            if (applyPlayAreaLayout) ApplyPlayAreaLayout();
            return;
        }

        currentTime = gameDuration;
        isGameRunning = true;
        matchesFound = 0;
        totalPairs = (GridManager.Instance.columns * GridManager.Instance.rows) / 2;

        ConfigureTimerSlider();
        UpdateTimerUI();
        GridManager.Instance.SetupLevel(currentLevel);
        GridManager.Instance.GenerateGrid();
        if (applyPlayAreaLayout) ApplyPlayAreaLayout();
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
            lastMatchRealTime = -999f;
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
        lastMatchRealTime = -999f;
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

    void UpdateTimerUI() => _timerPresenter?.Refresh(currentTime, gameDuration);

    void ConfigureTimerSlider() => _timerPresenter?.ConfigureSlider(gameDuration, currentTime);

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
        if (useHudIconOverlays)
            ApplySoundIconOverlay();
        else
            MedievalHudButtons.ApplySoundState(soundButton, isSoundOn,
                proceduralFrameGoldBright, proceduralFrameBandDark, proceduralFrameInnerLine);
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
        float now = Time.time;
        if (matchesFound > 0 && fastMatchPairWindow > 0f && fastMatchBonusSeconds > 0f)
        {
            float dt = now - lastMatchRealTime;
            if (dt > 0.02f && dt <= fastMatchPairWindow)
            {
                float cap = gameDuration * Mathf.Max(1f, timeBonusMaxMultiplier);
                currentTime = Mathf.Min(currentTime + fastMatchBonusSeconds, cap);
            }
        }
        lastMatchRealTime = now;

        matchesFound++;
        GameEvents.RaiseMatch();
        if (matchesFound >= totalPairs) GameOver(true);
    }

    /// <summary>Все плитки собраны (подстраховка если счётчик пар рассинхронизировался).</summary>
    public void OnAllTilesCleared()
    {
        if (roundSettled) return;
        matchesFound = totalPairs;
        GameOver(true);
    }

    void GameOver(bool won)
    {
        if (roundSettled) return;

        if (_modalUi == null)
            _modalUi = new GameModalUiBuilder(medievalFont, textColorDark, textColorLight, parchmentSprite);
        if (gameOverPanel == null)
            BuildGameOverPanelIfNeeded();

        if (won)
        {
            if (currentLevel >= maxLevel) GameRunPersistence.Clear();
            else SaveAfterVictoryCheckpoint();
        }
        else GameRunPersistence.Clear();

        roundSettled = true;
        isGameRunning = false;
        Time.timeScale = 0f;

        GameEvents.RaiseGameOver(won);

        if (gameOverPanel != null)
        {
            gameOverPanel.transform.SetAsLastSibling();
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
        if (gameOverPanel != null || _modalUi == null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogWarning("Canvas не найден — GameOverPanel не построен."); return; }

        gameOverPanel = _modalUi.BuildGameOverPanel(
            canvas,
            out gameOverTitle,
            out gameOverSubtitle,
            out gameOverNextButton,
            out gameOverRestartButton,
            out gameOverMenuButton,
            OnNextLevelButton,
            OnRestartButton,
            OnMainMenuButton);
    }

    void BuildConfirmationPanelIfNeeded()
    {
        if (confirmationPanel != null || _modalUi == null) return;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        confirmationPanel = _modalUi.BuildConfirmationPanel(
            canvas,
            out confirmationTitle,
            out confirmationSubtitle,
            out confirmationYesButton,
            out confirmationNoButton,
            () =>
            {
                var action = pendingConfirmAction;
                HideConfirmation();
                action?.Invoke();
            },
            HideConfirmation);
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

    void ApplyTypographyAndBadge()
    {
        if (showDigitalTimer && timerText != null && medievalFont != null)
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
        var frameRt = GameObject.Find("Frame")?.GetComponent<RectTransform>();
        if (gridRt == null || GridManager.Instance == null) return;

        float side = Mathf.Max(0f, playAreaSideInset);
        float top = Mathf.Max(0f, playAreaTopInset);
        float bottom = Mathf.Max(0f, playAreaBottomInset);
        float shrink = Mathf.Max(0f, playAreaShrinkExtra);
        float nudgeL = Mathf.Max(0f, playAreaNudgeLeft);
        float leftInset = side + shrink;
        float rightInset = side + shrink + nudgeL;

        // Только Grid стретчим в зону игры
        ApplyScreenInsets(gridRt, leftInset, rightInset, top, bottom);
        Canvas.ForceUpdateCanvases();

        var gridLayout = gridRt.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return;

        int cols = GridManager.Instance.columns;
        int rows = GridManager.Instance.rows;
        if (cols <= 0 || rows <= 0) return;

        float fullW = gridRt.rect.width;
        float fullH = gridRt.rect.height;
        if (fullW <= 1f || fullH <= 1f) return;

        float inset = Mathf.Max(0f, playAreaFrameInnerPadding);
        if (playAreaFrameOverTiles)
            inset += Mathf.Max(0f, playAreaFrameBorderClearance);
        float w = Mathf.Max(1f, fullW - 2f * inset);
        float h = Mathf.Max(1f, fullH - 2f * inset);

        float cell = Mathf.Floor(Mathf.Min(w / cols, h / rows));
        if (cell < 1f) return;

        gridLayout.cellSize = new Vector2(cell, cell);

        // Реальный размер видимой сетки плиток
        float realW = cell * cols;
        float realH = cell * rows;

        float slackX = fullW - realW;
        float slackY = fullH - realH;
        int slackXi = Mathf.Max(0, Mathf.FloorToInt(slackX + 0.0001f));
        int slackYi = Mathf.Max(0, Mathf.FloorToInt(slackY + 0.0001f));

        int padL, padR;
        switch (playAreaClusterHorizAlign)
        {
            case PlayAreaClusterHorizontalAlign.Left:
                padL = 0;
                padR = slackXi;
                break;
            case PlayAreaClusterHorizontalAlign.Right:
                padL = slackXi;
                padR = 0;
                break;
            default:
                padL = slackXi / 2;
                padR = slackXi - padL;
                break;
        }

        int padT = slackYi / 2;
        int padB = slackYi - padT;

        gridLayout.padding.left   = padL;
        gridLayout.padding.right  = padR;
        gridLayout.padding.top    = padT;
        gridLayout.padding.bottom = padB;

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRt);
        Canvas.ForceUpdateCanvases();

        EnsurePlayBoardMatte(gridRt);

        var parentRt = gridRt.parent as RectTransform;
        CleanupLegacyPlayAreaGapObjects(parentRt, gridRt);

        Vector3 worldCenter;
        Vector2 sizeLocal;
        if (parentRt != null && TryGetTileClusterBoundsInParent(GridManager.Instance.gridParent, parentRt, out Vector2 mn, out Vector2 mx))
        {
            Vector2 centerLp = (mn + mx) * 0.5f;
            worldCenter = parentRt.TransformPoint(centerLp);
            sizeLocal = mx - mn;
        }
        else
        {
            worldCenter = gridRt.TransformPoint(gridRt.rect.center);
            sizeLocal = new Vector2(realW, realH);
        }

        if (gridRt.gameObject.GetComponent<PlayAreaLayoutDriver>() == null)
            gridRt.gameObject.AddComponent<PlayAreaLayoutDriver>();

        // Картинка под пазлом = тот же прямоугольник, что и кластер плиток (без смещения относительно рамки).
        SizeAndCenterToGrid(bg,      gridRt, sizeLocal.x, sizeLocal.y, worldCenter);
        SyncPlayUnderlayImage(bg);
        SizeAndCenterToGrid(frameRt, gridRt, sizeLocal.x, sizeLocal.y, worldCenter);
        ApplyOrUpdatePlayAreaFrame(gridRt, sizeLocal, worldCenter);

        if (useCandleTimerStrip && parentRt != null)
        {
            float stripW = Mathf.Clamp(sizeLocal.x * candleStripWidthFactor, 220f, 920f);
            Vector3 worldPos;
            var canvas = parentRt.GetComponentInParent<Canvas>();
            if (TryGetPlayAreaTimerWorldPos(canvas, parentRt, out worldPos, ref stripW))
            { }
            else
            {
                gridRt.GetWorldCorners(CornerScratch);
                Vector3 topMid = (CornerScratch[1] + CornerScratch[2]) * 0.5f;
                Vector3 lift = gridRt.TransformVector(0f, candleStripLift, 0f);
                worldPos = topMid + lift;
            }

            MedievalCandleTimer.Ensure(parentRt, worldPos, stripW, out _, out var wax, out _);
            candleWaxFill = wax;
            _timerPresenter?.SetCandleWaxFill(candleWaxFill);
            Transform candleTr = parentRt.Find(MedievalCandleTimer.RootName);
            if (candleTr != null)
                candleTr.SetSiblingIndex(gridRt.GetSiblingIndex());
        }

        if (autoArrangeLayout) ArrangeHudLayout();
    }

    static Sprite s_PlayAreaUiWhite;

    static Sprite PlayAreaUiWhiteSprite()
    {
        if (s_PlayAreaUiWhite == null)
            s_PlayAreaUiWhite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return s_PlayAreaUiWhite;
    }

    /// <summary>
    /// Цветная рамка-кольцо в зазоре между плитками и деревянной рамкой.
    /// Создаёт контейнер размером (кластер + outset) и 4 тонкие полосы по периметру (толщиной outset каждая).
    /// Центр контейнера прозрачный — там сидят плитки, через зазоры просвечивает фоновая картинка.
    /// </summary>
    void EnsurePlayBoardMatte(RectTransform gridRt)
    {
        if (gridRt == null) return;

        // На всякий случай — если матте остался где-то снаружи Grid (от прежних версий)
        if (gridRt.parent != null)
        {
            Transform stray = gridRt.parent.Find(PlayBoardMatteChildName);
            if (stray != null && stray.parent != gridRt) Destroy(stray.gameObject);
        }

        Transform existing = gridRt.Find(PlayBoardMatteChildName);

        if (!playAreaBoardMatteEnabled)
        {
            if (existing != null) Destroy(existing.gameObject);
            return;
        }

        RectTransform container;
        if (existing == null)
        {
            var go = new GameObject(PlayBoardMatteChildName, typeof(RectTransform));
            container = go.GetComponent<RectTransform>();
            container.SetParent(gridRt, false);
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }
        else
        {
            container = (RectTransform)existing;
            // Если раньше матте был сплошной заливкой — снимаем компонент Image с контейнера
            var oldImg = container.GetComponent<Image>();
            if (oldImg != null) Destroy(oldImg);
            var le = container.GetComponent<LayoutElement>();
            if (le == null) le = container.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        var gridLayout = gridRt.GetComponent<GridLayoutGroup>();
        float padL = gridLayout != null ? gridLayout.padding.left   : 0f;
        float padR = gridLayout != null ? gridLayout.padding.right  : 0f;
        float padT = gridLayout != null ? gridLayout.padding.top    : 0f;
        float padB = gridLayout != null ? gridLayout.padding.bottom : 0f;
        float outset = Mathf.Max(0f, playAreaFrameOutset);

        // Контейнер растягиваем по Grid, отжимаем края под кластер плиток + расширяем на outset
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.pivot = new Vector2(0.5f, 0.5f);
        container.offsetMin = new Vector2(padL - outset, padB - outset);
        container.offsetMax = new Vector2(-(padR - outset), -(padT - outset));
        container.localScale = Vector3.one;
        container.localRotation = Quaternion.identity;

        // 4 тонкие полосы по периметру — цвет только в зазоре. Центр пустой.
        EnsureMatteStrip(container, "MatteTop",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -outset), Vector2.zero);
        EnsureMatteStrip(container, "MatteBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, outset));
        EnsureMatteStrip(container, "MatteLeft",   new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(outset, 0f));
        EnsureMatteStrip(container, "MatteRight",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-outset, 0f), Vector2.zero);

        container.SetAsFirstSibling();
    }

    /// <summary>Одна полоса матте: создаёт/обновляет цветной прямоугольник по якорям и отступам в контейнере.</summary>
    void EnsureMatteStrip(RectTransform parent, string name,
                          Vector2 anchorMin, Vector2 anchorMax,
                          Vector2 offsetMin, Vector2 offsetMax)
    {
        Transform existing = parent.Find(name);
        RectTransform rt;
        Image img;
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            img = go.GetComponent<Image>();
        }
        else
        {
            rt = (RectTransform)existing;
            img = existing.GetComponent<Image>();
            if (img == null) img = existing.gameObject.AddComponent<Image>();
        }

        img.sprite = PlayAreaUiWhiteSprite();
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = false;
        img.color = playAreaBoardMatteColor;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    static void CleanupLegacyPlayAreaGapObjects(RectTransform parentRt, RectTransform gridRt)
    {
        const string legacyGap = "PlayAreaFrameGapFill";
        const string legacyPad = "PlayAreaGridPaddingFill";
        if (parentRt != null)
        {
            Transform t = parentRt.Find(legacyGap);
            if (t != null) Object.Destroy(t.gameObject);
        }
        if (gridRt != null)
        {
            Transform t = gridRt.Find(legacyPad);
            if (t != null) Object.Destroy(t.gameObject);
        }
    }

    /// <summary>Ось-выровненный бокс кластера плиток в локали родителя Grid (нельзя брать InverseTransformVector(wmax-wmin) — ломает размер).</summary>
    static bool TryGetTileClusterBoundsInParent(Transform gridParent, RectTransform parentRt, out Vector2 minLocal, out Vector2 maxLocal)
    {
        minLocal = new Vector2(float.MaxValue, float.MaxValue);
        maxLocal = new Vector2(float.MinValue, float.MinValue);
        if (gridParent == null || parentRt == null) return false;

        for (int i = 0; i < gridParent.childCount; i++)
        {
            var crt = gridParent.GetChild(i) as RectTransform;
            if (crt == null || !crt.gameObject.activeInHierarchy) continue;
            if (crt.name == PlayBoardMatteChildName) continue;
            crt.GetWorldCorners(CornerScratch);
            for (int c = 0; c < 4; c++)
            {
                Vector2 lp = parentRt.InverseTransformPoint(CornerScratch[c]);
                minLocal = Vector2.Min(minLocal, lp);
                maxLocal = Vector2.Max(maxLocal, lp);
            }
        }
        return maxLocal.x > minLocal.x && maxLocal.y > minLocal.y;
    }

    static void SyncPlayUnderlayImage(RectTransform bg)
    {
        if (bg == null) return;
        var img = bg.GetComponent<Image>();
        if (img == null) return;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
    }

    void SizeAndCenterToGrid(RectTransform rt, RectTransform gridRt, float w, float h, Vector3 worldCenter)
    {
        if (rt == null) return;
        if (gridRt.parent != null && rt.parent != gridRt.parent)
            rt.SetParent(gridRt.parent, false);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.position = worldCenter;

        int gi = gridRt.GetSiblingIndex();
        if (rt.GetSiblingIndex() > gi)
            rt.SetSiblingIndex(gi);
    }

    void ApplyOrUpdatePlayAreaFrame(RectTransform gridRt, Vector2 boardSizeLocal, Vector3 worldCenter)
    {
        if (gridRt == null || gridRt.parent == null) return;

        Transform parent = gridRt.parent;
        if (useProceduralBoardFrame)
        {
            Transform oldSpriteFrame = parent.Find("PlayAreaFrame");
            if (oldSpriteFrame != null)
                Destroy(oldSpriteFrame.gameObject);

            ProceduralBoardFrame.BuildOrReplace(
                parent,
                gridRt,
                boardSizeLocal,
                worldCenter,
                playAreaFrameOverTiles,
                playAreaFrameOutset,
                proceduralFrameShadow,
                proceduralFrameBandDark,
                proceduralFrameGold,
                proceduralFrameGoldBright,
                proceduralFrameInnerLine,
                proceduralFrameThick1,
                proceduralFrameThick2,
                proceduralFrameThick3,
                proceduralFrameThick4,
                proceduralFrameNotches,
                proceduralFrameInnerPad,
                proceduralFrameInnerPadColor);
            return;
        }

        Transform proc = parent.Find("ProceduralBoardFrame");
        if (proc != null)
            Destroy(proc.gameObject);

        if (playAreaFrameSprite == null) return;

        float o = Mathf.Max(0f, playAreaFrameOutset);
        Transform t = parent.Find("PlayAreaFrame");
        RectTransform frameRt;
        Image img;
        if (t == null)
        {
            var go = new GameObject("PlayAreaFrame", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            frameRt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
        }
        else
        {
            frameRt = t.GetComponent<RectTransform>();
            img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
        }

        img.sprite = playAreaFrameSprite;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.type = playAreaFrameSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;

        float frameW = boardSizeLocal.x + o * 2f;
        float frameH = boardSizeLocal.y + o * 2f;
        if (img.preserveAspect && playAreaFrameSprite.rect.width > 1f && playAreaFrameSprite.rect.height > 1f)
        {
            float aspect = playAreaFrameSprite.rect.width / playAreaFrameSprite.rect.height;
            float boxAspect = frameW / frameH;
            if (boxAspect > aspect)
                frameW = frameH * aspect;
            else
                frameH = frameW / aspect;
        }

        frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.sizeDelta = new Vector2(frameW, frameH);
        frameRt.localScale = Vector3.one;
        frameRt.localRotation = Quaternion.identity;
        frameRt.position = worldCenter;

        int gridIdx = gridRt.GetSiblingIndex();
        int frameIdx = frameRt.GetSiblingIndex();
        if (playAreaFrameOverTiles)
        {
            int want = Mathf.Clamp(gridIdx + 1, 0, parent.childCount - 1);
            if (frameIdx != want)
                frameRt.SetSiblingIndex(want);
        }
        else
        {
            if (frameIdx > gridIdx)
                frameRt.SetSiblingIndex(gridIdx);
        }
    }

    /// <summary>Вызов после смены спрайта уровня: анимация появления фона (масштаб/альфа).</summary>
    public void PlayBackgroundReveal(float duration = 0.45f)
    {
        var go = GameObject.Find("GameBackground");
        if (go == null) return;
        if (playBackgroundRevealRoutine != null)
            StopCoroutine(playBackgroundRevealRoutine);
        playBackgroundRevealRoutine = StartCoroutine(BackgroundRevealRoutine(go.transform, duration));
    }

    IEnumerator BackgroundRevealRoutine(Transform bgTransform, float duration)
    {
        if (bgTransform == null)
        {
            playBackgroundRevealRoutine = null;
            yield break;
        }
        var cg = bgTransform.GetComponent<CanvasGroup>();
        if (cg == null) cg = bgTransform.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.alpha = 0f;
        Vector3 s0 = bgTransform.localScale;
        bgTransform.localScale = s0 * 0.92f;
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - (1f - k) * (1f - k);
            cg.alpha = e;
            bgTransform.localScale = Vector3.Lerp(s0 * 0.92f, s0, e);
            yield return null;
        }
        cg.alpha = 1f;
        bgTransform.localScale = s0;
        playBackgroundRevealRoutine = null;
    }

    static RectTransform ResolvePlayAreaFrameRect()
    {
        var gridRt = GameObject.Find("Grid")?.GetComponent<RectTransform>();
        if (gridRt == null || gridRt.parent == null) return null;
        Transform parent = gridRt.parent;
        Transform t = parent.Find("ProceduralBoardFrame")
            ?? parent.Find("PlayAreaFrame")
            ?? parent.Find("Frame");
        return t as RectTransform;
    }

    static RectTransform ResolvePlayAreaHudTarget()
    {
        RectTransform frameRt = ResolvePlayAreaFrameRect();
        if (frameRt != null) return frameRt;
        return GameObject.Find("Grid")?.GetComponent<RectTransform>();
    }

    bool TryGetPlayAreaHudBounds(Canvas canvas, out float left, out float right, out float top, out float bottom)
    {
        left = playAreaSideInset + playAreaShrinkExtra;
        right = left + 400f;
        top = 0f;
        bottom = 0f;
        if (canvas == null) return false;

        var canvasRt = canvas.transform as RectTransform;
        RectTransform target = ResolvePlayAreaHudTarget();
        if (target == null) return false;

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRt, target);
        if (bounds.size.x < 1f || bounds.size.y < 1f) return false;

        left = bounds.min.x;
        right = bounds.max.x;
        bottom = bounds.min.y;
        top = bounds.max.y;
        return true;
    }

    bool TryGetPlayAreaTimerWorldPos(Canvas canvas, Transform parentRt, out Vector3 worldPos, ref float stripWidth)
    {
        worldPos = Vector3.zero;
        if (canvas == null || parentRt == null) return false;
        if (!TryGetPlayAreaHudBounds(canvas, out float left, out float right, out float top, out _))
            return false;

        float centerX = (left + right) * 0.5f + hudNudgeRight;
        var canvasRt = canvas.transform as RectTransform;
        Vector3 localTop = new Vector3(centerX, top, 0f);
        worldPos = canvasRt.TransformPoint(localTop);
        worldPos += parentRt.TransformVector(0f, candleStripLift, 0f);

        float frameW = Mathf.Max(1f, right - left);
        stripWidth = Mathf.Clamp(frameW * candleStripWidthFactor, 220f, 920f);
        return true;
    }

    void RepositionCandleTimer()
    {
        if (!useCandleTimerStrip) return;
        var gridRt = GameObject.Find("Grid")?.GetComponent<RectTransform>();
        if (gridRt?.parent == null) return;

        Transform parentRt = gridRt.parent;
        Transform candleTr = parentRt.Find(MedievalCandleTimer.RootName);
        if (candleTr == null) return;

        var canvas = parentRt.GetComponentInParent<Canvas>();
        float stripW = candleTr.GetComponent<RectTransform>().sizeDelta.x;
        if (!TryGetPlayAreaTimerWorldPos(canvas, parentRt, out Vector3 worldPos, ref stripW)) return;

        var root = candleTr.GetComponent<RectTransform>();
        root.position = worldPos;
        root.sizeDelta = new Vector2(stripW, root.sizeDelta.y);
    }

    static void ApplyScreenInsets(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static bool TryWorldPointToTopRightAnchored(Canvas canvas, Vector3 world, out Vector2 anchored)
    {
        anchored = Vector2.zero;
        if (canvas == null) return false;

        var canvasRt = canvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                RectTransformUtility.WorldToScreenPoint(cam, world),
                cam,
                out Vector2 local))
            return false;

        Vector2 anchor = new Vector2(1f, 1f);
        anchored = new Vector2(
            local.x + canvasRt.rect.width * (canvasRt.pivot.x - anchor.x),
            local.y + canvasRt.rect.height * (canvasRt.pivot.y - anchor.y));
        return true;
    }

    bool TryGetGridTopAnchoredY(Canvas canvas, out float firstY)
    {
        firstY = -playAreaTopInset;

        var gridRt = GameObject.Find("Grid")?.GetComponent<RectTransform>();
        if (gridRt == null || canvas == null) return false;

        Canvas.ForceUpdateCanvases();
        gridRt.GetWorldCorners(CornerScratch);
        if (!TryWorldPointToTopRightAnchored(canvas, CornerScratch[1], out Vector2 topRightAnchored))
            return false;

        firstY = topRightAnchored.y;
        return true;
    }

    void ArrangeHudLayout()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Canvas.ForceUpdateCanvases();

        float m = screenMargin;
        float s = iconButtonSize;
        Vector2 rightAnchor = new Vector2(1f, 1f);
        float rightX = -m;

        float y = -playAreaTopInset;
        float step = s + buttonSpacing;
        TryGetGridTopAnchoredY(canvas, out y);

        PlaceIcon(homeButton,    rightAnchor, new Vector2(rightX, y - step * 0f), s);
        PlaceIcon(soundButton,   rightAnchor, new Vector2(rightX, y - step * 1f), s);
        PlaceIcon(pauseButton,   rightAnchor, new Vector2(rightX, y - step * 2f), s);
        PlaceIcon(hintButton,    rightAnchor, new Vector2(rightX, y - step * 3f), s);
        PlaceIcon(refreshButton, rightAnchor, new Vector2(rightX, y - step * 4f), s);

        if (hintButton != null) hintButton.transform.SetAsLastSibling();
        if (refreshButton != null) refreshButton.transform.SetAsLastSibling();

        RepositionCandleTimer();

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

        // ── Цифровой таймер сверху (опционально) ──
        if (showDigitalTimer && timerText != null)
        {
            timerText.gameObject.SetActive(true);
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

        ApplyMedievalHudStyling();
    }

    static void HideDecorativeButtonLabels(Button home, Button sound, Button pause, Button refresh, TextMeshProUGUI keep)
    {
        HideButtonLabel(home, keep);
        HideButtonLabel(sound, keep);
        HideButtonLabel(pause, keep);
        HideButtonLabel(refresh, keep);
    }

    void HideDecorativeButtonLabels() =>
        HideDecorativeButtonLabels(homeButton, soundButton, pauseButton, refreshButton, hintCountText);

    static void HideButtonLabel(Button button, TextMeshProUGUI keep)
    {
        if (button == null) return;
        foreach (Transform child in button.transform)
        {
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp == null || tmp == keep) continue;
            child.gameObject.SetActive(false);
        }
    }

    void PlaceIcon(Button button, Vector2 anchor, Vector2 anchoredPosition, float size)
    {
        if (button == null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
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
            if (child.GetComponent<TextMeshProUGUI>() != null) continue;
            if (child.name == "MedievalProc_Root"
                || child.name == MedievalHudIconOverlays.OverlayName
                || child.name == MedievalHudIconOverlays.MuteLineName) continue;
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

    void ApplyMedievalHudStyling()
    {
        MedievalHudButtons.ApplyAll(homeButton, soundButton, pauseButton, refreshButton, hintButton,
            proceduralFrameGoldBright, proceduralFrameBandDark, proceduralFrameInnerLine,
            medievalHudHintSprite, medievalHudRefreshSprite);
        if (!useHudIconOverlays)
            MedievalHudButtons.ApplySoundState(soundButton, isSoundOn,
                proceduralFrameGoldBright, proceduralFrameBandDark, proceduralFrameInnerLine);
        HideDecorativeButtonLabels();
        ApplyHudIconOverlays();

        if (useCandleTimerStrip)
        {
            if (timerBar != null) timerBar.gameObject.SetActive(false);
            if (timerSlider != null) timerSlider.gameObject.SetActive(false);
        }
    }

    void ApplyHudIconOverlays()
    {
        if (!useHudIconOverlays) return;
        float inset = hudIconOverlayInset;
        if (hudIconHome != null) MedievalHudIconOverlays.Apply(homeButton, hudIconHome, inset);
        ApplySoundIconOverlay();
        if (hudIconPause != null) MedievalHudIconOverlays.Apply(pauseButton, hudIconPause, inset);
        if (hudIconHint != null) MedievalHudIconOverlays.Apply(hintButton, hudIconHint, inset);
        if (hudIconRefresh != null) MedievalHudIconOverlays.Apply(refreshButton, hudIconRefresh, inset);

        if (hintCountText != null)
            hintCountText.transform.SetAsLastSibling();
    }

    void ApplySoundIconOverlay()
    {
        if (!useHudIconOverlays || soundButton == null) return;
        Sprite sprite = hudIconSoundOn;
        if (sprite == null) sprite = soundOnSprite;
        if (sprite == null) return;
        MedievalHudIconOverlays.Apply(soundButton, sprite, hudIconOverlayInset);
        MedievalHudIconOverlays.SetMuteLine(soundButton, !isSoundOn);
    }

    void EnsureHudIconSprites()
    {
        if (hudIconSoundOn == null) hudIconSoundOn = soundOnSprite;

#if UNITY_EDITOR
        const string folder = "Assets/Project/ProjectSprites/Buttons/";
        hudIconHome ??= LoadHudSprite(folder + "IMG_5480-removebg-preview.png");
        hudIconSoundOn ??= LoadHudSprite(folder + "IMG_5477-removebg-preview.png");
        hudIconPause ??= LoadHudSprite(folder + "IMG_5478-removebg-preview.png");
        hudIconHint ??= LoadHudSprite(folder + "IMG_5483-removebg-preview.png");
        hudIconRefresh ??= LoadHudSprite(folder + "IMG_5476-removebg-preview.png");
#endif
    }

#if UNITY_EDITOR
    static Sprite LoadHudSprite(string assetPath) =>
        UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

    void OnValidate()
    {
        EnsureHudIconSprites();
        if (!Application.isPlaying || !applyPlayAreaLayout) return;
        if (Instance != null && Instance != this) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled) return;
            if (Instance != this) return;
            ApplyPlayAreaLayout();
        };
    }
#endif
}

/// <summary>Пересчёт зоны игры при смене размера окна (Free Aspect) и масштаба Canvas.</summary>
[DisallowMultipleComponent]
public sealed class PlayAreaLayoutDriver : MonoBehaviour
{
    void OnRectTransformDimensionsChange()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.RefreshPlayAreaLayout();
    }
}
