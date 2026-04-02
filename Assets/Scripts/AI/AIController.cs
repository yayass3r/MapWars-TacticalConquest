// ===================================================================
// Map Wars: Tactical Conquest - AI Controller (AI Script)
// Description: Implements intelligent enemy AI that makes strategic
//              decisions about attacking, defending, and expanding.
// The AI evaluates threats, opportunities, and resource management
// to provide a challenging opponent.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enum representing the AI's current strategic priority.
/// Used to determine behavior patterns and attack timing.
/// </summary>
public enum AIDifficulty
{
    Easy = 0,        // Slow reactions, suboptimal choices
    Normal = 1,      // Balanced strategy
    Hard = 2,        // Aggressive, optimal targeting
    Nightmare = 3    // Ruthless, coordinates multiple attacks
}

/// <summary>
/// Controls the AI opponent's behavior. The AI periodically evaluates
/// the game state and makes decisions about when and where to attack.
/// It prioritizes capturing neutral nodes, attacking weak player nodes,
/// and defending its own vulnerable positions.
/// Attach to a dedicated "AIController" GameObject in the scene.
/// </summary>
public class AIController : MonoBehaviour
{
    // =========================================================
    // SINGLETON PATTERN
    // =========================================================

    private static AIController _instance;
    public static AIController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AIController>();
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("AI Difficulty Settings")]
    [SerializeField] private AIDifficulty _difficulty = AIDifficulty.Normal;
    [SerializeField, Range(0.5f, 5f)] private float _decisionInterval = 2.0f;
    [SerializeField, Range(0f, 1f)] private float _aggressionLevel = 0.5f;

    [Header("Attack Parameters")]
    [SerializeField, Range(0.3f, 0.8f)] private float _attackThreshold = 0.5f;
    [SerializeField] private int _minimumSurplusSoldiers = 5;
    [SerializeField, Range(1f, 3f)] private float _distanceWeight = 1.5f;

    [Header("Defense Parameters")]
    [SerializeField] private int _defenseThreshold = 3;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private List<NodeController> _allNodes = new List<NodeController>();
    private float _decisionTimer = 0f;
    private bool _isActive = false;
    private NodeController _lastAttackedTarget = null;
    private int _consecutiveSameTarget = 0;

    // Difficulty-based timing multipliers
    private float _reactionDelay;
    private float _attackSuccessThreshold;
    private float _expansionPriority;

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

        SetupDifficultyParameters();
    }

    private void Update()
    {
        if (!_isActive) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        _decisionTimer += Time.deltaTime;

        if (_decisionTimer >= _decisionInterval)
        {
            _decisionTimer = 0f;
            EvaluateAndAct();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// Sets up difficulty-dependent parameters that control
    /// how aggressively and intelligently the AI plays.
    /// </summary>
    private void SetupDifficultyParameters()
    {
        switch (_difficulty)
        {
            case AIDifficulty.Easy:
                _decisionInterval = 3.5f;
                _aggressionLevel = 0.3f;
                _reactionDelay = 1.5f;
                _attackSuccessThreshold = 0.8f;
                _expansionPriority = 0.4f;
                break;

            case AIDifficulty.Normal:
                _decisionInterval = 2.0f;
                _aggressionLevel = 0.5f;
                _reactionDelay = 0.8f;
                _attackSuccessThreshold = 0.6f;
                _expansionPriority = 0.6f;
                break;

            case AIDifficulty.Hard:
                _decisionInterval = 1.2f;
                _aggressionLevel = 0.7f;
                _reactionDelay = 0.3f;
                _attackSuccessThreshold = 0.4f;
                _expansionPriority = 0.7f;
                break;

            case AIDifficulty.Nightmare:
                _decisionInterval = 0.8f;
                _aggressionLevel = 0.9f;
                _reactionDelay = 0.1f;
                _attackSuccessThreshold = 0.3f;
                _expansionPriority = 0.8f;
                break;
        }
    }

    /// <summary>
    /// Initializes the AI with the current game nodes.
    /// Called by GameManager when a level starts.
    /// </summary>
    /// <param name="nodes">All nodes on the map</param>
    public void Initialize(List<NodeController> nodes)
    {
        _allNodes = nodes;
        _decisionTimer = 0f;
        _isActive = true;
        _lastAttackedTarget = null;
        _consecutiveSameTarget = 0;

        Debug.Log($"[AIController] Initialized with {_allNodes.Count} nodes | Difficulty: {_difficulty}");
    }

    // =========================================================
    // MAIN AI LOGIC
    // =========================================================

    /// <summary>
    /// The core AI decision-making function. Called periodically
    /// to evaluate the game state and take action.
    /// The AI follows this priority order:
    /// 1. Defend vulnerable nodes
    /// 2. Capture weak neutral nodes
    /// 3. Attack weak player nodes
    /// 4. Build up forces at strongest node
    /// </summary>
    private void EvaluateAndAct()
    {
        if (!_isActive || _allNodes.Count == 0) return;

        // Refresh node list
        var enemyNodes = GetNodesByFaction(Faction.Enemy);
        var playerNodes = GetNodesByFaction(Faction.Player);
        var neutralNodes = GetNeutralNodes();

        // No enemy nodes left - AI is defeated
        if (enemyNodes.Count == 0)
        {
            _isActive = false;
            return;
        }

        // Step 1: Check for defensive needs
        if (EvaluateDefense(enemyNodes, playerNodes))
            return;

        // Step 2: Try to expand into neutral territory
        if (neutralNodes.Count > 0 && EvaluateExpansion(enemyNodes, neutralNodes))
            return;

        // Step 3: Try to attack player nodes
        if (playerNodes.Count > 0 && EvaluateOffense(enemyNodes, playerNodes))
            return;

        // Step 4: If no good moves, consider reinforcing weakest owned node
        EvaluateReinforcement(enemyNodes);
    }

    // =========================================================
    // DEFENSE EVALUATION
    // =========================================================

    /// <summary>
    /// Evaluates if any AI nodes are under threat and need
    /// reinforcement by sending troops from nearby friendly nodes.
    /// </summary>
    private bool EvaluateDefense(List<NodeController> enemyNodes, List<NodeController> playerNodes)
    {
        foreach (var aiNode in enemyNodes)
        {
            // Check if any player node nearby has significantly more soldiers
            foreach (var playerNode in playerNodes)
            {
                float distance = Vector3.Distance(aiNode.transform.position, playerNode.transform.position);

                // Only consider nearby threats
                if (distance > 10f) continue;

                // Is the player node strong enough to be threatening?
                if (playerNode.CurrentSoldiers > aiNode.CurrentSoldiers * 1.5f)
                {
                    // Find a nearby AI node that can send reinforcements
                    NodeController reinforcingNode = FindStrongestNeighbor(aiNode, Faction.Enemy, excludeNode: aiNode);

                    if (reinforcingNode != null && reinforcingNode.CurrentSoldiers > _defenseThreshold + 5)
                    {
                        int troopsToSend = Mathf.CeilToInt(reinforcingNode.CurrentSoldiers * 0.3f);
                        GameManager.Instance.LaunchAttack(reinforcingNode, aiNode);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // =========================================================
    // EXPANSION EVALUATION
    // =========================================================

    /// <summary>
    /// Evaluates neutral nodes for capture opportunities.
    /// Targets the weakest neutral nodes that are closest to
    /// AI-controlled nodes.
    /// </summary>
    private bool EvaluateExpansion(List<NodeController> enemyNodes, List<NodeController> neutralNodes)
    {
        // Score each neutral node based on distance and soldier count
        var scoredTargets = new List<(NodeController target, NodeController source, float score)>();

        foreach (var neutralNode in neutralNodes)
        {
            foreach (var aiNode in enemyNodes)
            {
                float distance = Vector3.Distance(aiNode.transform.position, neutralNode.transform.position);
                int surplus = aiNode.CurrentSoldiers - neutralNode.CurrentSoldiers;

                // Only consider if we have enough troops to capture
                if (surplus <= _minimumSurplusSoldiers) continue;

                // Score: higher surplus and closer distance = better target
                float distanceScore = 1f / (1f + distance * _distanceWeight * 0.1f);
                float surplusScore = surplus / 50f;
                float totalScore = (distanceScore + surplusScore) * _expansionPriority;

                scoredTargets.Add((neutralNode, aiNode, totalScore));
            }
        }

        // Sort by score (highest first)
        scoredTargets.Sort((a, b) => b.score.CompareTo(a.score));

        // Attack the best target if the score is high enough
        if (scoredTargets.Count > 0 && scoredTargets[0].score > 0.2f)
        {
            var (target, source, score) = scoredTargets[0];

            // Avoid attacking the same target repeatedly
            if (_lastAttackedTarget == target)
            {
                _consecutiveSameTarget++;
                if (_consecutiveSameTarget > 3 && scoredTargets.Count > 1)
                {
                    // Pick second best target instead
                    target = scoredTargets[1].target;
                    source = scoredTargets[1].source;
                }
            }
            else
            {
                _consecutiveSameTarget = 0;
            }

            _lastAttackedTarget = target;
            GameManager.Instance.LaunchAttack(source, target);
            return true;
        }

        return false;
    }

    // =========================================================
    // OFFENSE EVALUATION
    // =========================================================

    /// <summary>
    /// Evaluates player nodes for attack opportunities.
    /// Targets the weakest player nodes to maximize territory gain.
    /// </summary>
    private bool EvaluateOffense(List<NodeController> enemyNodes, List<NodeController> playerNodes)
    {
        // Only attack if we have enough total forces (aggression check)
        int totalEnemySoldiers = enemyNodes.Sum(n => n.CurrentSoldiers);
        int totalPlayerSoldiers = playerNodes.Sum(n => n.CurrentSoldiers);

        // Be more aggressive when we have more total soldiers
        if (totalEnemySoldiers < totalPlayerSoldiers * _aggressionLevel) return false;

        // Score each player node
        var scoredTargets = new List<(NodeController target, NodeController source, float score)>();

        foreach (var playerNode in playerNodes)
        {
            foreach (var aiNode in enemyNodes)
            {
                float distance = Vector3.Distance(aiNode.transform.position, playerNode.transform.position);
                int surplus = aiNode.CurrentSoldiers - playerNode.CurrentSoldiers;

                // Need sufficient advantage to attack
                float requiredSurplus = playerNode.CurrentSoldiers * _attackSuccessThreshold;
                if (surplus < requiredSurplus) continue;

                // Score calculation
                float distanceScore = 1f / (1f + distance * _distanceWeight * 0.1f);
                float weaknessScore = 1f - (playerNode.CurrentSoldiers / (totalPlayerSoldiers + 1f));
                float totalScore = (distanceScore * 0.6f + weaknessScore * 0.4f) * _aggressionLevel;

                scoredTargets.Add((playerNode, aiNode, totalScore));
            }
        }

        // Sort by score
        scoredTargets.Sort((a, b) => b.score.CompareTo(a.score));

        // Attack if a good opportunity exists
        if (scoredTargets.Count > 0 && scoredTargets[0].score > 0.15f)
        {
            var (target, source, score) = scoredTargets[0];

            // Multiple attacks on harder difficulties
            if (_difficulty >= AIDifficulty.Hard && scoredTargets.Count > 1 && Random.value > 0.5f)
            {
                // Launch a secondary attack from another node
                var secondary = scoredTargets[1];
                if (secondary.source != source)
                {
                    GameManager.Instance.LaunchAttack(secondary.source, secondary.target);
                }
            }

            GameManager.Instance.LaunchAttack(source, target);
            return true;
        }

        return false;
    }

    // =========================================================
    // REINFORCEMENT
    // =========================================================

    /// <summary>
    /// When no attack is advantageous, the AI consolidates forces
    /// by sending troops from weaker nodes to stronger ones.
    /// </summary>
    private void EvaluateReinforcement(List<NodeController> enemyNodes)
    {
        if (enemyNodes.Count <= 1) return;

        // Find weakest and strongest AI nodes
        NodeController weakest = null;
        NodeController strongest = null;
        int minSoldiers = int.MaxValue;
        int maxSoldiers = 0;

        foreach (var node in enemyNodes)
        {
            if (node.CurrentSoldiers < minSoldiers)
            {
                minSoldiers = node.CurrentSoldiers;
                weakest = node;
            }
            if (node.CurrentSoldiers > maxSoldiers)
            {
                maxSoldiers = node.CurrentSoldiers;
                strongest = node;
            }
        }

        // If weakest node has very few troops and strongest is far, consolidate
        if (weakest != null && strongest != null && weakest != strongest)
        {
            float distance = Vector3.Distance(weakest.transform.position, strongest.transform.position);
            if (distance < 8f && weakest.CurrentSoldiers < _minimumSurplusSoldiers)
            {
                GameManager.Instance.LaunchAttack(weakest, strongest);
            }
        }
    }

    // =========================================================
    // UTILITY HELPERS
    // =========================================================

    /// <summary>
    /// Returns all nodes owned by the specified faction.
    /// </summary>
    private List<NodeController> GetNodesByFaction(Faction faction)
    {
        return _allNodes.Where(n => n != null && n.Owner == faction).ToList();
    }

    /// <summary>
    /// Returns all neutral (unclaimed) nodes.
    /// </summary>
    private List<NodeController> GetNeutralNodes()
    {
        return _allNodes.Where(n => n != null && n.Owner == Faction.Neutral).ToList();
    }

    /// <summary>
    /// Finds the strongest node of a given faction near a reference point.
    /// Optionally excludes a specific node from consideration.
    /// </summary>
    private NodeController FindStrongestNeighbor(NodeController reference, Faction faction, NodeController excludeNode = null, float maxRange = 15f)
    {
        NodeController strongest = null;
        int maxSoldiers = 0;

        foreach (var node in GetNodesByFaction(faction))
        {
            if (node == excludeNode) continue;

            float distance = Vector3.Distance(reference.transform.position, node.transform.position);
            if (distance > maxRange) continue;

            if (node.CurrentSoldiers > maxSoldiers)
            {
                maxSoldiers = node.CurrentSoldiers;
                strongest = node;
            }
        }

        return strongest;
    }

    /// <summary>
    /// Notifies the AI that a node has changed ownership.
    /// Used to trigger immediate re-evaluation if needed.
    /// </summary>
    public void OnNodeChanged(NodeController node, Faction oldOwner, Faction newOwner)
    {
        // On higher difficulties, re-evaluate immediately when losing a node
        if (oldOwner == Faction.Enemy && _difficulty >= AIDifficulty.Hard)
        {
            _decisionTimer = _decisionInterval; // Force evaluation next frame
        }
    }

    /// <summary>
    /// Sets the AI difficulty level dynamically.
    /// </summary>
    public void SetDifficulty(AIDifficulty difficulty)
    {
        _difficulty = difficulty;
        SetupDifficultyParameters();
    }

    /// <summary>
    /// Activates or deactivates the AI system.
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
    }
}
