// ===================================================================
// Map Wars: Tactical Conquest - UI Manager (UI Script)
// Description: Manages all user interface elements including
//              HUD, menus, popups, energy display, and end screens.
// Uses Unity's Canvas system for responsive layout.
// ===================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages all UI screens and HUD elements in the game.
/// Handles transitions between screens, energy display,
/// coin counter, attack line visualization, and end-game popups.
/// Attach to "UIManager" GameObject with Canvas component.
/// </summary>
public class UIManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR REFERENCES - SCREENS
    // =========================================================

    [Header("Screen Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _gameplayHUD;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _endScreenPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _levelSelectPanel;
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private GameObject _noEnergyPopup;

    // =========================================================
    // INSPECTOR REFERENCES - HUD ELEMENTS
    // =========================================================

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI _energyText;
    [SerializeField] private TextMeshProUGUI _coinsText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Slider _energySlider;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _militarySupportButton; // Rewarded ad button

    // =========================================================
    // INSPECTOR REFERENCES - END SCREEN
    // =========================================================

    [Header("End Screen Elements")]
    [SerializeField] private TextMeshProUGUI _resultTitleText;
    [SerializeField] private TextMeshProUGUI _coinsEarnedText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _starsText;
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;

    // =========================================================
    // INSPECTOR REFERENCES - ATTACK LINE
    // =========================================================

    [Header("Attack Line")]
    [SerializeField] private LineRenderer _attackLineRenderer;
    [SerializeField] private float _attackLineWidth = 0.08f;

    // =========================================================
    // INSPECTOR REFERENCES - NODE COUNT DISPLAY
    // =========================================================

    [Header("Node Count Display")]
    [SerializeField] private TextMeshProUGUI _playerNodesText;
    [SerializeField] private TextMeshProUGUI _enemyNodesText;
    [SerializeField] private TextMeshProUGUI _neutralNodesText;

    // =========================================================
    // INSPECTOR REFERENCES - LOADING
    // =========================================================

    [Header("Loading")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private Slider _loadingBar;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Coroutine _hudUpdateCoroutine;
    private bool _isInitialized = false;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUI();
    }

    private void Start()
    {
        SubscribeToEvents();
        ShowScreen(_mainMenuPanel);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// Sets up all UI components, button listeners, and initial values.
    /// </summary>
    private void InitializeUI()
    {
        // Setup buttons
        if (_pauseButton != null)
            _pauseButton.onClick.AddListener(OnPauseClicked);

        if (_militarySupportButton != null)
            _militarySupportButton.onClick.AddListener(OnMilitarySupportClicked);

        if (_nextLevelButton != null)
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);

        if (_menuButton != null)
            _menuButton.onClick.AddListener(OnMenuClicked);

        // Setup attack line
        if (_attackLineRenderer != null)
        {
            _attackLineRenderer.startWidth = _attackLineWidth;
            _attackLineRenderer.endWidth = _attackLineWidth;
            _attackLineRenderer.enabled = false;
        }

        // Load saved data into UI
        UpdateEnergyDisplay();
        UpdateCoinsDisplay();

        _isInitialized = true;
        Debug.Log("[UIManager] Initialized successfully");
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnNodeCaptured += HandleNodeCaptured;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnNodeCaptured -= HandleNodeCaptured;
        }
    }

    // =========================================================
    // SCREEN MANAGEMENT
    // =========================================================

    /// <summary>
    /// Shows a specific screen panel and hides all others.
    /// </summary>
    public void ShowScreen(GameObject screenToShow)
    {
        // Hide all screens
        _mainMenuPanel?.SetActive(false);
        _gameplayHUD?.SetActive(false);
        _pausePanel?.SetActive(false);
        _endScreenPanel?.SetActive(false);
        _settingsPanel?.SetActive(false);
        _levelSelectPanel?.SetActive(false);
        _shopPanel?.SetActive(false);
        _noEnergyPopup?.SetActive(false);
        _loadingPanel?.SetActive(false);

        // Show target screen
        screenToShow?.SetActive(true);
    }

    /// <summary>
    /// Hides all screen panels.
    /// </summary>
    public void HideAllScreens()
    {
        _mainMenuPanel?.SetActive(false);
        _gameplayHUD?.SetActive(false);
        _pausePanel?.SetActive(false);
        _endScreenPanel?.SetActive(false);
        _settingsPanel?.SetActive(false);
        _levelSelectPanel?.SetActive(false);
        _shopPanel?.SetActive(false);
        _noEnergyPopup?.SetActive(false);
    }

    // =========================================================
    // GAME STATE HANDLING
    // =========================================================

    /// <summary>
    /// Responds to game state changes by showing appropriate UI.
    /// </summary>
    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Menu:
                ShowScreen(_mainMenuPanel);
                break;

            case GameState.Loading:
                ShowScreen(_loadingPanel);
                StartCoroutine(SimulateLoading());
                break;

            case GameState.Playing:
                ShowScreen(_gameplayHUD);
                StartHUDUpdates();
                break;

            case GameState.Paused:
                _pausePanel?.SetActive(true);
                break;

            case GameState.Victory:
            case GameState.Defeat:
                StopHUDUpdates();
                ShowScreen(_endScreenPanel);
                break;
        }
    }

    /// <summary>
    /// Simulates a loading bar for visual polish.
    /// </summary>
    private IEnumerator SimulateLoading()
    {
        if (_loadingBar != null)
        {
            _loadingBar.value = 0f;
            float duration = 0.8f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                _loadingBar.value = timer / duration;
                yield return null;
            }
            _loadingBar.value = 1f;
        }
    }

    // =========================================================
    // HUD UPDATES
    // =========================================================

    /// <summary>
    /// Starts the periodic HUD update coroutine.
    /// </summary>
    private void StartHUDUpdates()
    {
        StopHUDUpdates();
        _hudUpdateCoroutine = StartCoroutine(UpdateHUDRoutine());
    }

    /// <summary>
    /// Stops the HUD update coroutine.
    /// </summary>
    private void StopHUDUpdates()
    {
        if (_hudUpdateCoroutine != null)
        {
            StopCoroutine(_hudUpdateCoroutine);
            _hudUpdateCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that updates HUD elements periodically.
    /// Runs at ~4 updates per second to minimize performance impact.
    /// </summary>
    private IEnumerator UpdateHUDRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.25f);

        while (GameManager.Instance.CurrentState == GameState.Playing)
        {
            UpdateHUDValues();
            yield return wait;
        }
    }

    /// <summary>
    /// Updates all dynamic HUD values (energy, coins, timer, node counts).
    /// </summary>
    private void UpdateHUDValues()
    {
        UpdateEnergyDisplay();
        UpdateCoinsDisplay();
        UpdateTimerDisplay();
        UpdateNodeCountDisplay();
        UpdateLevelDisplay();
    }

    /// <summary>
    /// Updates the energy display text and slider.
    /// </summary>
    public void UpdateEnergyDisplay()
    {
        int current = SaveSystem.Instance.CurrentEnergy;
        int max = SaveSystem.Instance.MaxEnergy;

        if (_energyText != null)
            _energyText.text = $"{current}/{max}";

        if (_energySlider != null)
        {
            _energySlider.maxValue = max;
            _energySlider.value = current;
        }
    }

    /// <summary>
    /// Updates the coins display text.
    /// </summary>
    public void UpdateCoinsDisplay()
    {
        if (_coinsText != null)
            _coinsText.text = SaveSystem.Instance.Coins.ToString();
    }

    /// <summary>
    /// Updates the gameplay timer display.
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (_timerText != null)
        {
            float time = GameManager.Instance.GetGameTime();
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            _timerText.text = $"{minutes:D2}:{seconds:D2}";
        }
    }

    /// <summary>
    /// Updates the node count display showing how many nodes
    /// each faction controls.
    /// </summary>
    private void UpdateNodeCountDisplay()
    {
        if (GameManager.Instance == null) return;

        int playerCount = GameManager.Instance.GetNodesByFaction(Faction.Player).Count;
        int enemyCount = GameManager.Instance.GetNodesByFaction(Faction.Enemy).Count;
        int neutralCount = GameManager.Instance.GetNeutralNodes().Count;

        if (_playerNodesText != null)
            _playerNodesText.text = playerCount.ToString();
        if (_enemyNodesText != null)
            _enemyNodesText.text = enemyCount.ToString();
        if (_neutralNodesText != null)
            _neutralNodesText.text = neutralCount.ToString();
    }

    /// <summary>
    /// Updates the current level display text.
    /// </summary>
    private void UpdateLevelDisplay()
    {
        if (_levelText != null)
            _levelText.text = $"Level {GameManager.Instance.CurrentLevel}";
    }

    // =========================================================
    // END SCREEN
    // =========================================================

    /// <summary>
    /// Shows the end-of-level screen with results.
    /// </summary>
    /// <param name="isVictory">Whether the player won</param>
    /// <param name="coinsEarned">Coins earned this level</param>
    /// <param name="timeSeconds">Time taken in seconds</param>
    public void ShowEndScreen(bool isVictory, int coinsEarned, int timeSeconds)
    {
        if (_resultTitleText != null)
            _resultTitleText.text = isVictory ? "VICTORY!" : "DEFEAT";

        if (_coinsEarnedText != null)
            _coinsEarnedText.text = $"+{coinsEarned} Coins";

        if (_timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeSeconds / 60f);
            int seconds = timeSeconds % 60;
            _timeText.text = $"Time: {minutes:D2}:{seconds:D2}";
        }

        // Calculate stars (1-3 based on speed and performance)
        int stars = CalculateStars(isVictory, timeSeconds);
        if (_starsText != null)
            _starsText.text = new string('★', stars) + new string('☆', 3 - stars);

        // Show/hide next level button
        if (_nextLevelButton != null)
            _nextLevelButton.gameObject.SetActive(isVictory);
    }

    /// <summary>
    /// Calculates star rating for the level.
    /// </summary>
    private int CalculateStars(bool isVictory, int timeSeconds)
    {
        if (!isVictory) return 0;
        if (timeSeconds < 30) return 3;
        if (timeSeconds < 60) return 2;
        return 1;
    }

    // =========================================================
    // ATTACK LINE VISUALIZATION
    // =========================================================

    /// <summary>
    /// Updates the attack line visual during player drag.
    /// Draws a line from source to the current drag position.
    /// </summary>
    /// <param name="startPos">Source node position</param>
    /// <param name="currentPos">Current drag position</param>
    public void UpdateAttackLine(Vector3 startPos, Vector3 currentPos)
    {
        if (_attackLineRenderer == null) return;

        _attackLineRenderer.enabled = true;
        _attackLineRenderer.SetPosition(0, startPos);
        _attackLineRenderer.SetPosition(1, currentPos);
    }

    /// <summary>
    /// Hides the attack line visual.
    /// </summary>
    public void HideAttackLine()
    {
        if (_attackLineRenderer != null)
            _attackLineRenderer.enabled = false;
    }

    // =========================================================
    // POPUPS & NOTIFICATIONS
    // =========================================================

    /// <summary>
    /// Shows the "no energy" popup when the player tries to play without energy.
    /// </summary>
    public void ShowNoEnergyPopup()
    {
        _noEnergyPopup?.SetActive(true);
    }

    /// <summary>
    /// Hides the no energy popup.
    /// </summary>
    public void HideNoEnergyPopup()
    {
        _noEnergyPopup?.SetActive(false);
    }

    /// <summary>
    /// Shows a temporary toast notification at the top of the screen.
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="duration">Duration in seconds</param>
    public void ShowToast(string message, float duration = 2f)
    {
        StartCoroutine(ToastRoutine(message, duration));
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        // Create a temporary text element
        GameObject toastObj = new GameObject("Toast");
        toastObj.transform.SetParent(transform);

        TextMeshProUGUI text = toastObj.AddComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;

        RectTransform rect = toastObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.9f);
        rect.anchorMax = new Vector2(0.5f, 0.9f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600, 60);

        toastObj.SetActive(true);

        // Fade in
        CanvasGroup canvasGroup = toastObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        float timer = 0f;
        while (timer < 0.3f)
        {
            canvasGroup.alpha = timer / 0.3f;
            timer += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        // Fade out
        timer = 0f;
        while (timer < 0.3f)
        {
            canvasGroup.alpha = 1f - (timer / 0.3f);
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(toastObj);
    }

    // =========================================================
    // BUTTON HANDLERS
    // =========================================================

    private void OnPauseClicked()
    {
        GameManager.Instance.SetGameState(GameState.Paused);
    }

    private void OnMilitarySupportClicked()
    {
        MonetizationManager.Instance?.ShowRewardedAd(
            onReward: () =>
            {
                // Grant +20 soldiers to all player nodes
                var playerNodes = GameManager.Instance.GetNodesByFaction(Faction.Player);
                foreach (var node in playerNodes)
                {
                    node.AddSoldiers(20);
                }
                ShowToast("Military Support: +20 soldiers!");
            },
            onFailure: () =>
            {
                ShowToast("Ad not available");
            }
        );
    }

    private void OnNextLevelClicked()
    {
        GameManager.Instance.NextLevel();
    }

    private void OnRestartClicked()
    {
        GameManager.Instance.RestartLevel();
    }

    private void OnMenuClicked()
    {
        GameManager.Instance.ReturnToMenu();
    }

    /// <summary>
    /// Called when the player captures a node. Updates node count display.
    /// </summary>
    private void HandleNodeCaptured(NodeController node, Faction oldOwner, Faction newOwner)
    {
        UpdateNodeCountDisplay();

        // Show capture toast for player
        if (newOwner == Faction.Player)
        {
            ShowToast("Base Captured!");
        }
    }

    // =========================================================
    // PUBLIC METHODS FOR BUTTON EVENTS
    // =========================================================

    /// <summary>
    /// Opens the shop panel. Called from UI button.
    /// </summary>
    public void OpenShop()
    {
        ShowScreen(_shopPanel);
    }

    /// <summary>
    /// Closes the shop panel. Called from UI button.
    /// </summary>
    public void CloseShop()
    {
        ShowScreen(_mainMenuPanel);
    }

    /// <summary>
    /// Opens the level select screen. Called from UI button.
    /// </summary>
    public void OpenLevelSelect()
    {
        ShowScreen(_levelSelectPanel);
    }

    /// <summary>
    /// Starts a specific level from the level select screen.
    /// </summary>
    /// <param name="levelNumber">Level to start</param>
    public void OnLevelSelected(int levelNumber)
    {
        if (SaveSystem.Instance.CanConsumeEnergy())
        {
            SaveSystem.Instance.ConsumeEnergy();
            GameManager.Instance.StartLevel(levelNumber);
        }
        else
        {
            ShowNoEnergyPopup();
        }
    }

    /// <summary>
    /// Resumes the game from pause state. Called from UI button.
    /// </summary>
    public void OnResumeClicked()
    {
        GameManager.Instance.SetGameState(GameState.Playing);
    }

    /// <summary>
    /// Quits the application. Called from UI button.
    /// </summary>
    public void OnQuitClicked()
    {
        Application.Quit();
    }
}
