// ===================================================================
// Map Wars: Tactical Conquest - Level Manager (Core Script)
// Description: Manages level configurations, progression,
//              and generates level layouts procedurally.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Holds configuration data for a single game level.
/// Defines node positions, types, ownership, and initial soldier counts.
/// Create instances in code or as ScriptableObjects.
/// </summary>
[System.Serializable]
public class LevelConfig
{
    [Tooltip("Level number (1-based)")]
    public int levelNumber;

    [Tooltip("Name displayed to the player")]
    public string levelName;

    [Tooltip("Difficulty multiplier for AI (0.0 to 1.0)")]
    public float aiDifficulty = 0.5f;

    [Tooltip("Grid positions for each node (normalized 0-1 screen coordinates)")]
    public List<Vector2> nodePositions = new List<Vector2>();

    [Tooltip("Ownership type for each node (must match nodePositions count)")]
    public List<Faction> nodeTypes = new List<Faction>();

    [Tooltip("Initial soldier counts for each node")]
    public List<int> initialSoldierCounts = new List<int>();

    [Tooltip("Prefab to use for nodes in this level")]
    public GameObject nodePrefab;

    [Tooltip("Maximum time bonus threshold in seconds")]
    public float timeBonusThreshold = 60f;

    [Tooltip("Base coins awarded for completing this level")]
    public int baseCoinReward = 50;

    /// <summary>
    /// Validates that the configuration is complete and consistent.
    /// </summary>
    public bool IsValid()
    {
        if (nodePositions.Count == 0) return false;
        if (nodeTypes.Count != nodePositions.Count) return false;
        if (initialSoldierCounts.Count != nodePositions.Count) return false;
        if (levelNumber <= 0) return false;

        return true;
    }
}

/// <summary>
/// Manages all level data, generates new levels procedurally,
/// and tracks level completion progress.
/// Attach to "LevelManager" GameObject in the scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static LevelManager _instance;
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("LevelManager");
                    _instance = go.AddComponent<LevelManager>();
                }
            }
            return _instance;
        }
    }

    // =========================================================
    // CONFIGURATION
    // =========================================================

    [Header("Level Generation Settings")]
    [SerializeField] private int _maxNodes = 12;
    [SerializeField] private int _minNodes = 5;
    [SerializeField] private float _minNodeDistance = 2.0f;
    [SerializeField] private float _screenPadding = 0.1f;

    [Header("Procedural Generation Parameters")]
    [SerializeField, Range(0f, 1f)] private float _neutralRatio = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _enemyRatio = 0.3f;
    [SerializeField, Range(1, 20)] private int _baseSoldierCount = 10;
    [SerializeField] private AnimationCurve _difficultyCurve;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Dictionary<int, LevelConfig> _predefinedLevels = new Dictionary<int, LevelConfig>();
    private int _highestUnlockedLevel = 1;
    private GameObject _defaultNodePrefab;

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

        _highestUnlockedLevel = SaveSystem.Instance.GetHighestUnlockedLevel();
        GeneratePredefinedLevels();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // LEVEL CONFIGURATION
    // =========================================================

    /// <summary>
    /// Gets the configuration for a specific level number.
    /// Returns a predefined config if available, otherwise generates one procedurally.
    /// </summary>
    /// <param name="levelNumber">The level to get (1-based)</param>
    /// <returns>LevelConfig for the requested level</returns>
    public LevelConfig GetLevelConfig(int levelNumber)
    {
        // Check predefined levels first
        if (_predefinedLevels.TryGetValue(levelNumber, out LevelConfig predefined))
        {
            return predefined;
        }

        // Generate procedurally
        return GenerateProceduralLevel(levelNumber);
    }

    /// <summary>
    /// Generates all predefined (hand-crafted) levels.
    /// These levels are designed to introduce mechanics gradually.
    /// </summary>
    private void GeneratePredefinedLevels()
    {
        // === LEVEL 1: Tutorial - Simple 3-node introduction ===
        _predefinedLevels[1] = CreateLevel(
            levelNumber: 1,
            name: "First Contact",
            positions: new List<Vector2>
            {
                new Vector2(0.25f, 0.5f),  // Player base
                new Vector2(0.5f, 0.5f),   // Neutral
                new Vector2(0.75f, 0.5f)   // Enemy base
            },
            factions: new List<Faction>
            {
                Faction.Player,
                Faction.Neutral,
                Faction.Enemy
            },
            soldiers: new List<int> { 15, 5, 10 },
            difficulty: 0.2f
        );

        // === LEVEL 2: 5 nodes - Multiple neutrals ===
        _predefinedLevels[2] = CreateLevel(
            levelNumber: 2,
            name: "Expansion",
            positions: new List<Vector2>
            {
                new Vector2(0.2f, 0.7f),
                new Vector2(0.4f, 0.3f),
                new Vector2(0.5f, 0.6f),
                new Vector2(0.7f, 0.4f),
                new Vector2(0.8f, 0.7f)
            },
            factions: new List<Faction>
            {
                Faction.Player,
                Faction.Player,
                Faction.Neutral,
                Faction.Neutral,
                Faction.Enemy
            },
            soldiers: new List<int> { 10, 8, 5, 4, 12 },
            difficulty: 0.3f
        );

        // === LEVEL 3: 6 nodes - First real challenge ===
        _predefinedLevels[3] = CreateLevel(
            levelNumber: 3,
            name: "Skirmish",
            positions: new List<Vector2>
            {
                new Vector2(0.15f, 0.5f),
                new Vector2(0.35f, 0.25f),
                new Vector2(0.35f, 0.75f),
                new Vector2(0.65f, 0.35f),
                new Vector2(0.65f, 0.7f),
                new Vector2(0.85f, 0.5f)
            },
            factions: new List<Faction>
            {
                Faction.Player,
                Faction.Neutral,
                Faction.Neutral,
                Faction.Enemy,
                Faction.Enemy,
                Faction.Neutral
            },
            soldiers: new List<int> { 15, 5, 6, 8, 8, 4 },
            difficulty: 0.4f
        );

        // === LEVEL 4: 7 nodes - Introduction of medium nodes ===
        _predefinedLevels[4] = CreateLevel(
            levelNumber: 4,
            name: "Fortification",
            positions: new List<Vector2>
            {
                new Vector2(0.2f, 0.5f),
                new Vector2(0.35f, 0.3f),
                new Vector2(0.35f, 0.7f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.65f, 0.3f),
                new Vector2(0.65f, 0.7f),
                new Vector2(0.8f, 0.5f)
            },
            factions: new List<Faction>
            {
                Faction.Player,
                Faction.Neutral,
                Faction.Player,
                Faction.Neutral,
                Faction.Enemy,
                Faction.Neutral,
                Faction.Enemy
            },
            soldiers: new List<int> { 12, 5, 8, 8, 10, 5, 12 },
            difficulty: 0.45f
        );

        // === LEVEL 5: 8 nodes - Balanced warfare ===
        _predefinedLevels[5] = CreateLevel(
            levelNumber: 5,
            name: "Full Assault",
            positions: new List<Vector2>
            {
                new Vector2(0.15f, 0.3f),
                new Vector2(0.15f, 0.7f),
                new Vector2(0.35f, 0.5f),
                new Vector2(0.5f, 0.25f),
                new Vector2(0.5f, 0.75f),
                new Vector2(0.65f, 0.5f),
                new Vector2(0.85f, 0.3f),
                new Vector2(0.85f, 0.7f)
            },
            factions: new List<Faction>
            {
                Faction.Player,
                Faction.Player,
                Faction.Neutral,
                Faction.Neutral,
                Faction.Neutral,
                Faction.Neutral,
                Faction.Enemy,
                Faction.Enemy
            },
            soldiers: new List<int> { 15, 10, 6, 5, 5, 6, 12, 10 },
            difficulty: 0.5f
        );
    }

    /// <summary>
    /// Helper method to create a LevelConfig object with all parameters.
    /// </summary>
    private LevelConfig CreateLevel(int levelNumber, string name, List<Vector2> positions,
        List<Faction> factions, List<int> soldiers, float difficulty)
    {
        return new LevelConfig
        {
            levelNumber = levelNumber,
            levelName = name,
            nodePositions = positions,
            nodeTypes = factions,
            initialSoldierCounts = soldiers,
            aiDifficulty = difficulty,
            nodePrefab = _defaultNodePrefab,
            baseCoinReward = 40 + levelNumber * 15,
            timeBonusThreshold = 60f + levelNumber * 10f
        };
    }

    // =========================================================
    // PROCEDURAL GENERATION
    // =========================================================

    /// <summary>
    /// Generates a level procedurally based on the level number.
    /// Difficulty scales with level number, increasing node count
    /// and AI aggressiveness while decreasing player advantages.
    /// </summary>
    public LevelConfig GenerateProceduralLevel(int levelNumber)
    {
        float difficultyFactor = Mathf.Clamp01((levelNumber - 1f) / 50f); // Maxes at level 51

        // Determine node count based on level
        int nodeCount = Mathf.RoundToInt(Mathf.Lerp(_minNodes, _maxNodes, difficultyFactor * 0.8f));

        // Generate valid positions
        List<Vector2> positions = GenerateNodePositions(nodeCount);

        // Assign factions
        List<Faction> factions = GenerateFactions(nodeCount, difficultyFactor);

        // Assign soldier counts
        List<int> soldiers = GenerateSoldierCounts(nodeCount, difficultyFactor);

        // Create and return config
        return new LevelConfig
        {
            levelNumber = levelNumber,
            levelName = $"Battle Zone {levelNumber}",
            nodePositions = positions,
            nodeTypes = factions,
            initialSoldierCounts = soldiers,
            aiDifficulty = difficultyFactor,
            nodePrefab = _defaultNodePrefab,
            baseCoinReward = 40 + levelNumber * 15,
            timeBonusThreshold = 60f + levelNumber * 5f
        };
    }

    /// <summary>
    /// Generates non-overlapping node positions across the screen.
    /// Uses Poisson-disc sampling principles for even distribution.
    /// </summary>
    private List<Vector2> GenerateNodePositions(int count)
    {
        List<Vector2> positions = new List<Vector2>();
        int maxAttempts = 1000;
        int attempts = 0;

        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;

            // Random position within screen bounds (with padding)
            float x = Random.Range(_screenPadding, 1f - _screenPadding);
            float y = Random.Range(_screenPadding, 1f - _screenPadding);
            Vector2 candidate = new Vector2(x, y);

            // Check minimum distance from all existing positions
            bool isValid = true;
            foreach (Vector2 existing in positions)
            {
                float distance = Vector2.Distance(candidate, existing);
                if (distance < _minNodeDistance)
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                positions.Add(candidate);
            }
        }

        // Fallback: if we couldn't place enough, use grid positions
        if (positions.Count < count)
        {
            positions = GenerateGridPositions(count);
        }

        return positions;
    }

    /// <summary>
    /// Fallback grid-based position generator when random placement fails.
    /// Ensures consistent spacing between nodes.
    /// </summary>
    private List<Vector2> GenerateGridPositions(int count)
    {
        List<Vector2> positions = new List<Vector2>();
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);

        float cellWidth = (1f - 2 * _screenPadding) / cols;
        float cellHeight = (1f - 2 * _screenPadding) / rows;

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;
            int col = i % cols;
            float x = _screenPadding + cellWidth * (col + 0.5f);
            float y = _screenPadding + cellHeight * (row + 0.5f);
            positions.Add(new Vector2(x, y));
        }

        return positions;
    }

    /// <summary>
    /// Assigns factions to nodes based on difficulty and balance requirements.
    /// Ensures at least one player and one enemy node exist.
    /// </summary>
    private List<Faction> GenerateFactions(int count, float difficulty)
    {
        List<Faction> factions = new List<Faction>();

        // Always assign: 1 player, 1 enemy, rest distributed
        factions.Add(Faction.Player);
        factions.Add(Faction.Enemy);

        int remaining = count - 2;
        float adjustedNeutralRatio = Mathf.Lerp(0.6f, 0.2f, difficulty);
        float adjustedEnemyRatio = Mathf.Lerp(0.1f, 0.4f, difficulty);

        for (int i = 0; i < remaining; i++)
        {
            float roll = Random.value;
            if (roll < adjustedNeutralRatio)
            {
                factions.Add(Faction.Neutral);
            }
            else if (roll < adjustedNeutralRatio + adjustedEnemyRatio)
            {
                factions.Add(Faction.Enemy);
            }
            else
            {
                factions.Add(Faction.Player);
            }
        }

        // Shuffle to prevent predictable placement
        for (int i = factions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (factions[i], factions[j]) = (factions[j], factions[i]);
        }

        // Ensure player is on the left side and enemy on the right
        // (Helps with visual clarity and spatial orientation)
        int playerIndex = factions.IndexOf(Faction.Player);
        if (playerIndex > factions.Count / 2)
        {
            int neutralOnLeft = -1;
            for (int i = 0; i < factions.Count / 2; i++)
            {
                if (factions[i] == Faction.Neutral) { neutralOnLeft = i; break; }
            }
            if (neutralOnLeft >= 0)
            {
                factions[playerIndex] = Faction.Neutral;
                factions[neutralOnLeft] = Faction.Player;
            }
        }

        return factions;
    }

    /// <summary>
    /// Generates initial soldier counts for each node.
    /// Higher difficulty means enemy nodes start stronger.
    /// </summary>
    private List<int> GenerateSoldierCounts(int count, float difficulty)
    {
        List<int> soldiers = new List<int>();

        for (int i = 0; i < count; i++)
        {
            // Base count scales with level
            int baseCount = _baseSoldierCount + Mathf.RoundToInt(difficulty * 20);
            int randomVariance = Random.Range(-3, 5);
            int count = Mathf.Max(3, baseCount + randomVariance);
            soldiers.Add(count);
        }

        return soldiers;
    }

    // =========================================================
    // LEVEL PROGRESSION
    // =========================================================

    /// <summary>
    /// Gets the highest level the player has unlocked.
    /// </summary>
    public int GetHighestUnlockedLevel() => _highestUnlockedLevel;

    /// <summary>
    /// Checks if a specific level is unlocked.
    /// </summary>
    public bool IsLevelUnlocked(int levelNumber) => levelNumber <= _highestUnlockedLevel;

    /// <summary>
    /// Sets the highest unlocked level (called from SaveSystem).
    /// </summary>
    public void SetHighestUnlockedLevel(int level) => _highestUnlockedLevel = level;

    /// <summary>
    /// Gets the total number of predefined levels.
    /// </summary>
    public int GetTotalPredefinedLevels() => _predefinedLevels.Count;
}
