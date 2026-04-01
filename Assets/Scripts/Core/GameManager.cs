// ===================================================================
// Map Wars: Tactical Conquest - Game Manager (Core Script)
// Engine: Unity with C#
// Description: Main game logic controller, state management,
//              and central coordinator for all game systems.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enum representing the possible ownership states of a game node.
/// Determines which faction controls a base/node on the map.
/// </summary>
public enum Faction
{
    Neutral = 0,    // Gray - unclaimed bases
    Player = 1,     // Blue neon - player controlled
    Enemy = 2       // Red neon - AI controlled
}

/// <summary>
/// Enum representing the current state of the game session.
/// Used for flow control and state machine logic.
/// </summary>
public enum GameState
{
    Menu,           // Main menu or level selection screen
    Loading,        // Level is being initialized
    Playing,        // Active gameplay in progress
    Paused,         // Game is paused by the player
    Victory,        // Player has captured all enemy nodes
    Defeat,         // AI has captured all player nodes
    LevelComplete   // Level finished (victory or defeat processed)
}

/// <summary>
/// Central game manager that orchestrates all game systems.
/// This is a singleton that persists throughout gameplay and manages
/// the core game loop, node references, scoring, and state transitions.
/// Attach this to a dedicated GameObject called "GameManager" in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON PATTERN
    // =========================================================
    
    private static GameManager _instance;
    /// <summary>
    /// Global singleton reference to the GameManager instance.
    /// All other scripts access the manager through this property.
    /// </summary>
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Game Configuration")]
    [Tooltip("Default soldier production rate per second for each node")]
    [SerializeField] private float _soldierProductionRate = 1.0f;

    [Tooltip("Percentage of soldiers sent when attacking (0.0 to 1.0)")]
    [SerializeField, Range(0.1f, 0.9f)] private float _attackPercentage = 0.5f;

    [Tooltip("Speed of projectile/soldier movement in units per second")]
    [SerializeField] private float _troopSpeed = 5.0f;

    [Tooltip("Minimum soldiers required to initiate an attack")]
    [SerializeField] private int _minAttackSoldiers = 2;

    [Header("Visual References")]
    [Tooltip("Prefab for the projectile/troop game object")]
    [SerializeField] private GameObject _troopPrefab;

    [Tooltip("Line renderer used for drawing attack paths")]
    [SerializeField] private GameObject _attackLinePrefab;

    [Header("References")]
    [SerializeField] private Transform _nodesParent;
    [SerializeField] private Camera _mainCamera;

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    /// <summary>Current game state used for state machine logic</summary>
    public GameState CurrentState { get; private set; } = GameState.Menu;

    /// <summary>Current level number being played</summary>
    public int CurrentLevel { get; private set; } = 1;

    /// <summary>Production rate multiplier (can be modified by powerups)</summary>
    public float SoldierProductionRate => _soldierProductionRate;

    /// <summary>Percentage of soldiers sent per attack</summary>
    public float AttackPercentage => _attackPercentage;

    /// <summary>Movement speed for troop projectiles</summary>
    public float TroopSpeed => _troopSpeed;

    /// <summary>Minimum soldiers needed to attack</summary>
    public int MinAttackSoldiers => _minAttackSoldiers;

    /// <summary>Reference to the troop projectile prefab</summary>
    public GameObject TroopPrefab => _troopPrefab;

    /// <summary>Reference to the attack line visual prefab</summary>
    public GameObject AttackLinePrefab => _attackLinePrefab;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private List<NodeController> _allNodes = new List<NodeController>();
    private List<TroopProjectile> _activeTroops = new List<TroopProjectile>();
    private NodeController _sourceNode;
    private bool _isDragging = false;
    private float _gameTimer = 0f;
    private int _totalNodesCount = 0;

    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>Fired when the game state changes</summary>
    public event System.Action<GameState> OnGameStateChanged;

    /// <summary>Fired when a node changes ownership</summary>
    public event System.Action<NodeController, Faction, Faction> OnNodeCaptured;

    /// <summary>Fired when the game ends (victory or defeat)</summary>
    public event System.Action<bool> OnGameEnded;

    // =========================================================
    // UNITY LIFECYCLE METHODS
    // =========================================================

    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGameManager();
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        _gameTimer += Time.deltaTime;
        UpdateGameplay();
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
    /// Sets up the game manager, initializes all systems,
    /// and prepares the game for the first run.
    /// </summary>
    private void InitializeGameManager()
    {
        _allNodes.Clear();
        _activeTroops.Clear();

        // Find all NodeControllers in the scene
        if (_nodesParent != null)
        {
            NodeController[] nodes = _nodesParent.GetComponentsInChildren<NodeController>();
            _allNodes = new List<NodeController>(nodes);
        }
        else
        {
            _allNodes = new List<NodeController>(FindObjectsOfType<NodeController>());
        }

        _totalNodesCount = _allNodes.Count;

        Debug.Log($"[GameManager] Initialized with {_totalNodesCount} nodes");
    }

    /// <summary>
    /// Subscribes to node events for tracking ownership changes
    /// and win/loss conditions.
    /// </summary>
    private void SubscribeToEvents()
    {
        foreach (var node in _allNodes)
        {
            node.OnNodeCaptured += HandleNodeCaptured;
        }
    }

    /// <summary>
    /// Cleans up event subscriptions to prevent memory leaks.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        foreach (var node in _allNodes)
        {
            if (node != null)
                node.OnNodeCaptured -= HandleNodeCaptured;
        }
    }

    // =========================================================
    // GAME STATE MANAGEMENT
    // =========================================================

    /// <summary>
    /// Changes the current game state and fires the state change event.
    /// All state transitions go through this method for consistency.
    /// </summary>
    /// <param name="newState">The new state to transition to</param>
    public void SetGameState(GameState newState)
    {
        GameState oldState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameManager] State: {oldState} -> {newState}");
        OnGameStateChanged?.Invoke(newState);

        HandleStateEnter(newState);
    }

    /// <summary>
    /// Handles initialization logic when entering a new game state.
    /// </summary>
    private void HandleStateEnter(GameState state)
    {
        switch (state)
        {
            case GameState.Loading:
                PrepareLevel();
                break;

            case GameState.Playing:
                EnableNodeInteraction(true);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;

            case GameState.Victory:
            case GameState.Defeat:
                EnableNodeInteraction(false);
                ProcessGameEnd(state == GameState.Victory);
                break;

            case GameState.Menu:
                Time.timeScale = 1f;
                break;
        }
    }

    // =========================================================
    // LEVEL MANAGEMENT
    // =========================================================

    /// <summary>
    /// Starts loading a new level. Clears existing state and
    /// initializes the level based on the provided configuration.
    /// </summary>
    /// <param name="levelNumber">The level number to load (1-based)</param>
    public void StartLevel(int levelNumber)
    {
        CurrentLevel = levelNumber;
        SetGameState(GameState.Loading);
    }

    /// <summary>
    /// Prepares the level by loading configuration, positioning nodes,
    /// and initializing all game systems for gameplay.
    /// </summary>
    private void PrepareLevel()
    {
        // Load level configuration
        LevelConfig config = LevelManager.Instance.GetLevelConfig(CurrentLevel);
        if (config == null)
        {
            Debug.LogError($"[GameManager] No config found for level {CurrentLevel}");
            return;
        }

        // Clear existing troops
        ClearActiveTroops();

        // Setup nodes from configuration
        SetupNodes(config);

        // Initialize AI
        if (AIController.Instance != null)
        {
            AIController.Instance.Initialize(_allNodes);
        }

        // Reset game timer
        _gameTimer = 0f;

        // Re-subscribe to node events
        SubscribeToEvents();

        // Transition to playing state
        SetGameState(GameState.Playing);
    }

    /// <summary>
    /// Sets up all nodes based on the level configuration.
    /// Creates or reuses node objects and positions them correctly.
    /// </summary>
    private void SetupNodes(LevelConfig config)
    {
        _allNodes.Clear();

        for (int i = 0; i < config.nodePositions.Count; i++)
        {
            Vector2 screenPos = config.nodePositions[i];
            Vector3 worldPos = ScreenToWorldPosition(screenPos);

            // Check if we can reuse an existing node
            if (i < _nodesParent.childCount)
            {
                Transform child = _nodesParent.GetChild(i);
                NodeController node = child.GetComponent<NodeController>();
                if (node != null)
                {
                    node.Initialize(config.nodeTypes[i], config.initialSoldierCounts[i], worldPos);
                    _allNodes.Add(node);
                    continue;
                }
            }

            // Create new node if needed
            GameObject nodeObj = Instantiate(config.nodePrefab, _nodesParent);
            nodeObj.transform.position = worldPos;
            NodeController newnode = nodeObj.GetComponent<NodeController>();
            newnode.Initialize(config.nodeTypes[i], config.initialSoldierCounts[i], worldPos);
            _allNodes.Add(newnode);
        }
    }

    /// <summary>
    /// Clears all active troop projectiles from the field.
    /// Called when loading a new level or restarting.
    /// </summary>
    private void ClearActiveTroops()
    {
        foreach (var troop in _activeTroops)
        {
            if (troop != null) Destroy(troop.gameObject);
        }
        _activeTroops.Clear();
    }

    // =========================================================
    // GAMEPLAY UPDATE LOOP
    // =========================================================

    /// <summary>
    /// Main gameplay update called every frame while the game is in Playing state.
    /// Updates all nodes, processes active troops, and checks win conditions.
    /// </summary>
    private void UpdateGameplay()
    {
        // Update all nodes (soldier production)
        foreach (var node in _allNodes)
        {
            if (node != null)
            {
                node.UpdateNode(Time.deltaTime);
            }
        }

        // Update active troops
        UpdateActiveTroops();

        // Check win/loss conditions periodically
        if (Time.frameCount % 30 == 0) // Check every ~0.5 seconds at 60fps
        {
            CheckWinLossConditions();
        }
    }

    /// <summary>
    /// Updates all active troop projectiles, removing ones that have
    /// reached their target or been destroyed.
    /// </summary>
    private void UpdateActiveTroops()
    {
        for (int i = _activeTroops.Count - 1; i >= 0; i--)
        {
            if (_activeTroops[i] == null || !_activeTroops[i].isActiveAndEnabled)
            {
                _activeTroops.RemoveAt(i);
            }
        }
    }

    // =========================================================
    // ATTACK SYSTEM
    // =========================================================

    /// <summary>
    /// Called by NodeController when the player starts dragging
    /// from a player-owned node. Initiates the attack gesture.
    /// </summary>
    /// <param name="source">The node being dragged from</param>
    public void OnDragStarted(NodeController source)
    {
        if (CurrentState != GameState.Playing) return;
        if (source == null || source.Owner != Faction.Player) return;
        if (source.CurrentSoldiers < _minAttackSoldiers) return;

        _sourceNode = source;
        _isDragging = true;

        // Visual feedback: highlight source node
        source.SetHighlight(true);

        Debug.Log($"[GameManager] Drag started from node: {source.name}");
    }

    /// <summary>
    /// Called while the player is dragging. Updates the attack
    /// line visual to show the projected path.
    /// </summary>
    /// <param name="worldPosition">Current finger/mouse position in world space</param>
    public void OnDragUpdated(Vector3 worldPosition)
    {
        if (!_isDragging || _sourceNode == null) return;

        // Find nearest node to the drag position
        NodeController target = FindNearestNode(worldPosition, _sourceNode);

        // Update attack line visual
        if (AttackLinePrefab != null)
        {
            // The UIManager handles showing the line preview
            UIManager.Instance?.UpdateAttackLine(_sourceNode.transform.position, worldPosition);
        }
    }

    /// <summary>
    /// Called when the player releases their finger. If released
    /// over a valid target node, launches the attack.
    /// </summary>
    /// <param name="worldPosition">The release position in world space</param>
    public void OnDragEnded(Vector3 worldPosition)
    {
        if (!_isDragging || _sourceNode == null)
        {
            ResetDrag();
            return;
        }

        // Find target node
        NodeController target = FindNearestNode(worldPosition, _sourceNode);

        if (target != null && target != _sourceNode)
        {
            // Execute the attack
            LaunchAttack(_sourceNode, target);
        }

        ResetDrag();
    }

    /// <summary>
    /// Resets the drag state and cleans up visual feedback.
    /// </summary>
    private void ResetDrag()
    {
        if (_sourceNode != null)
        {
            _sourceNode.SetHighlight(false);
        }
        _sourceNode = null;
        _isDragging = false;

        UIManager.Instance?.HideAttackLine();
    }

    /// <summary>
    /// Launches an attack from the source node to the target node.
    /// Creates troop projectiles and deducts soldiers from the source.
    /// </summary>
    /// <param name="source">The attacking node</param>
    /// <param name="target">The node being attacked</param>
    public void LaunchAttack(NodeController source, NodeController target)
    {
        if (source == null || target == null) return;
        if (source.CurrentSoldiers < _minAttackSoldiers) return;

        // Calculate number of soldiers to send
        int soldiersToSend = Mathf.CeilToInt(source.CurrentSoldiers * _attackPercentage);
        soldiersToSend = Mathf.Max(soldiersToSend, _minAttackSoldiers);

        // Deduct soldiers from source
        source.RemoveSoldiers(soldiersToSend);

        // Spawn troop projectiles
        SpawnTroopProjectiles(source, target, soldiersToSend);

        Debug.Log($"[GameManager] Attack: {source.name} -> {target.name} ({soldiersToSend} troops)");
    }

    /// <summary>
    /// Spawns individual troop projectile objects that travel from
    /// source to target. Each projectile represents a group of soldiers.
    /// </summary>
    /// <param name="source">Origin node</param>
    /// <param name="target">Destination node</param>
    /// <param name="totalSoldiers">Total number of soldiers to send</param>
    private void SpawnTroopProjectiles(NodeController source, NodeController target, int totalSoldiers)
    {
        if (_troopPrefab == null)
        {
            Debug.LogError("[GameManager] Troop prefab is not assigned!");
            return;
        }

        // Calculate projectile group size (each projectile = 1-5 soldiers)
        int soldiersPerProjectile = Mathf.Clamp(totalSoldiers / 5, 1, 5);
        int projectileCount = Mathf.CeilToInt((float)totalSoldiers / soldiersPerProjectile);

        // Create parent object for the troop wave
        GameObject waveParent = new GameObject($"TroopWave_{source.name}_{target.name}");

        for (int i = 0; i < projectileCount; i++)
        {
            int soldiersInThisProjectile = Mathf.Min(soldiersPerProjectile, totalSoldiers - (i * soldiersPerProjectile));
            if (soldiersInThisProjectile <= 0) break;

            // Add slight delay between projectiles for visual effect
            float delay = i * 0.08f;

            // Spawn projectile at source position with slight random offset
            Vector3 offset = Random.insideUnitCircle * 0.3f;
            Vector3 spawnPos = source.transform.position + offset;

            GameObject troopObj = Instantiate(_troopPrefab, spawnPos, Quaternion.identity, waveParent.transform);
            TroopProjectile projectile = troopObj.GetComponent<TroopProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(source, target, soldiersInThisProjectile, _troopSpeed, delay);
                projectile.OnTroopReached += HandleTroopReached;
                _activeTroops.Add(projectile);
            }
        }

        // Auto-destroy wave parent when all children are gone
        Destroy(waveParent, 15f);
    }

    /// <summary>
    /// Handles the event when a troop projectile reaches its target node.
    /// Applies damage and checks for capture conditions.
    /// </summary>
    private void HandleTroopReached(NodeController target, int soldiers, Faction attackingFaction)
    {
        if (target == null) return;

        // Apply soldiers to target node
        target.ReceiveAttack(soldiers, attackingFaction);

        // Spawn particle effects at target
        if (EffectsManager.Instance != null)
        {
            Faction hitColor = attackingFaction == Faction.Player ? Faction.Player : Faction.Enemy;
            EffectsManager.Instance.SpawnCaptureParticles(target.transform.position, hitColor);
        }
    }

    // =========================================================
    // NODE SEARCH UTILITIES
    // =========================================================

    /// <summary>
    /// Finds the nearest node to a given world position.
    /// Optional exclusion parameter prevents selecting a specific node.
    /// </summary>
    /// <param name="worldPos">Position to search from</param>
    /// <param name="exclude">Node to exclude from search (e.g., the source)</param>
    /// <param name="maxDistance">Maximum distance to consider a node valid</param>
    /// <returns>The nearest NodeController or null if none found</returns>
    public NodeController FindNearestNode(Vector3 worldPos, NodeController exclude = null, float maxDistance = 2.5f)
    {
        NodeController nearest = null;
        float nearestDist = maxDistance;

        foreach (var node in _allNodes)
        {
            if (node == null || node == exclude) continue;

            float dist = Vector3.Distance(worldPos, node.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = node;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Gets all nodes owned by a specific faction.
    /// Used by AI and win condition checking.
    /// </summary>
    public List<NodeController> GetNodesByFaction(Faction faction)
    {
        return _allNodes.Where(n => n != null && n.Owner == faction).ToList();
    }

    /// <summary>
    /// Gets all neutral (unclaimed) nodes on the map.
    /// </summary>
    public List<NodeController> GetNeutralNodes()
    {
        return _allNodes.Where(n => n != null && n.Owner == Faction.Neutral).ToList();
    }

    // =========================================================
    // WIN/LOSS CONDITIONS
    // =========================================================

    /// <summary>
    /// Checks if the game has ended by verifying if any faction
    /// has lost all their nodes.
    /// </summary>
    private void CheckWinLossConditions()
    {
        int playerNodes = 0;
        int enemyNodes = 0;

        foreach (var node in _allNodes)
        {
            if (node == null) continue;

            switch (node.Owner)
            {
                case Faction.Player:
                    playerNodes++;
                    break;
                case Faction.Enemy:
                    enemyNodes++;
                    break;
            }
        }

        // Victory: Player captured all nodes (no enemy or neutral)
        if (enemyNodes == 0 && GetNeutralNodes().Count == 0 && playerNodes > 0)
        {
            SetGameState(GameState.Victory);
            return;
        }

        // Defeat: Player lost all nodes
        if (playerNodes == 0)
        {
            SetGameState(GameState.Defeat);
            return;
        }
    }

    /// <summary>
    /// Called when a node is captured. Checks if this capture
    /// triggers a game-ending condition.
    /// </summary>
    private void HandleNodeCaptured(NodeController node, Faction oldOwner, Faction newOwner)
    {
        OnNodeCaptured?.Invoke(node, oldOwner, newOwner);

        // Haptic feedback on capture
        if (newOwner == Faction.Player && HapticFeedbackManager.Instance != null)
        {
            HapticFeedbackManager.Instance.TrightCaptureHaptic();
        }

        // Notify AI of node change
        if (AIController.Instance != null)
        {
            AIController.Instance.OnNodeChanged(node, oldOwner, newOwner);
        }
    }

    /// <summary>
    /// Processes the end of a game session. Awards resources,
    /// saves progress, and triggers interstitial ads.
    /// </summary>
    /// <param name="isVictory">True if the player won</param>
    private void ProcessGameEnd(bool isVictory)
    {
        // Award coins based on performance
        int coinsEarned = isVictory ? CurrentLevel * 50 : CurrentLevel * 10;
        SaveSystem.Instance.AddCoins(coinsEarned);

        // Award bonus for quick victory
        if (isVictory && _gameTimer < 60f)
        {
            int timeBonus = Mathf.RoundToInt((60f - _gameTimer) * 2);
            SaveSystem.Instance.AddCoins(timeBonus);
            coinsEarned += timeBonus;
        }

        // Notify UI
        UIManager.Instance?.ShowEndScreen(isVictory, coinsEarned, Mathf.RoundToInt(_gameTimer));

        // Save level progress
        if (isVictory)
        {
            SaveSystem.Instance.UnlockLevel(CurrentLevel);
        }

        // Show interstitial ad every 3 levels
        if (CurrentLevel % 3 == 0)
        {
            MonetizationManager.Instance?.ShowInterstitialAd();
        }

        OnGameEnded?.Invoke(isVictory);
    }

    // =========================================================
    // UTILITY METHODS
    // =========================================================

    /// <summary>
    /// Enables or disables player interaction with nodes.
    /// Used when pausing the game or during end-game screens.
    /// </summary>
    private void EnableNodeInteraction(bool enabled)
    {
        foreach (var node in _allNodes)
        {
            if (node != null)
            {
                node.SetInteractable(enabled);
            }
        }
    }

    /// <summary>
    /// Converts a screen position (0-1 range) to world position.
    /// Uses the main camera to perform the conversion.
    /// </summary>
    private Vector3 ScreenToWorldPosition(Vector2 screenPos)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null) return Vector3.zero;

        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x * Screen.width, screenPos.y * Screen.height, _mainCamera.nearClipPlane + 10f)
        );
        return worldPos;
    }

    /// <summary>
    /// Restarts the current level from the beginning.
    /// Consumes one energy point.
    /// </summary>
    public void RestartLevel()
    {
        if (SaveSystem.Instance.CanConsumeEnergy())
        {
            SaveSystem.Instance.ConsumeEnergy();
            StartLevel(CurrentLevel);
        }
        else
        {
            UIManager.Instance?.ShowNoEnergyPopup();
        }
    }

    /// <summary>
    /// Advances to the next level after victory.
    /// </summary>
    public void NextLevel()
    {
        if (SaveSystem.Instance.CanConsumeEnergy())
        {
            SaveSystem.Instance.ConsumeEnergy();
            StartLevel(CurrentLevel + 1);
        }
        else
        {
            UIManager.Instance?.ShowNoEnergyPopup();
        }
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMenu()
    {
        SetGameState(GameState.Menu);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Gets the total number of nodes in the current level.
    /// </summary>
    public int GetTotalNodesCount() => _totalNodesCount;

    /// <summary>
    /// Gets the current game time in seconds.
    /// </summary>
    public float GetGameTime() => _gameTimer;
}
