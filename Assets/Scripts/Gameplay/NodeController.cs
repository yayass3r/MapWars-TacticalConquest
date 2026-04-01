// ===================================================================
// Map Wars: Tactical Conquest - Node Controller (Gameplay Script)
// Description: Manages individual base/node behavior including
//              soldier production, ownership, visual updates,
//              and attack receiving logic.
// Attach to: Each base/node GameObject in the scene.
// ===================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Defines the type of node, affecting its visual size and
/// soldier production capacity.
/// </summary>
public enum NodeType
{
    Small = 0,      // Small circle, base production
    Medium = 1,     // Medium circle, 1.5x production
    Large = 2,      // Large circle, 2x production
    Fortress = 3    // Extra large, 3x production, slower capture
}

/// <summary>
/// Controls a single node/base on the game map. Each node has
/// a faction owner, soldier count that auto-increments, and
/// handles receiving attacks from troop projectiles.
/// </summary>
public class NodeController : MonoBehaviour
{
    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer _nodeBackground;
    [SerializeField] private SpriteRenderer _nodeRing;
    [SerializeField] private TextMeshPro_Text _soldierText;  // Or Text if no TMP
    [SerializeField] private Canvas _textCanvas;
    [SerializeField] private GameObject _highlightIndicator;

    [Header("Node Properties")]
    [SerializeField] private NodeType _nodeType = NodeType.Small;
    [SerializeField] private float _nodeRadius = 0.5f;

    [Header("Visual Settings")]
    [SerializeField] private Color _playerColor = new Color(0.2f, 0.6f, 1f, 1f);    // Neon Blue
    [SerializeField] private Color _enemyColor = new Color(1f, 0.2f, 0.3f, 1f);      // Neon Red
    [SerializeField] private Color _neutralColor = new Color(0.7f, 0.7f, 0.7f, 1f);  // Dim White
    [SerializeField] private Color _highlightColor = Color.yellow;

    [Header("Animation Settings")]
    [SerializeField] private float _pulseSpeed = 2f;
    [SerializeField] private float _pulseAmount = 0.1f;

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    /// <summary>Current faction that owns this node</summary>
    public Faction Owner { get; private set; } = Faction.Neutral;

    /// <summary>Current number of soldiers stationed at this node</summary>
    public int CurrentSoldiers { get; private set; } = 0;

    /// <summary>Maximum number of soldiers this node can hold</summary>
    public int MaxSoldiers { get; private set; } = 999;

    /// <summary>The type of this node (affects size and production)</summary>
    public NodeType NodeType => _nodeType;

    /// <summary>The radius of this node for collision/interaction detection</summary>
    public float NodeRadius => _nodeRadius;

    /// <summary>Production rate multiplier based on node type</summary>
    public float ProductionMultiplier { get; private set; } = 1f;

    /// <summary>Whether this node is currently interactable</summary>
    public bool IsInteractable { get; private set; } = true;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private float _productionTimer = 0f;
    private float _pulseTimer = 0f;
    private Vector3 _baseScale = Vector3.one;
    private int _displaySoldiers = 0;
    private bool _isHighlighted = false;

    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>Fired when this node is captured by a new faction</summary>
    public event System.Action<NodeController, Faction, Faction> OnNodeCaptured;

    /// <summary>Fired when the soldier count changes significantly</summary>
    public event System.Action<NodeController, int> OnSoldierCountChanged;

    /// <summary>Fired when the player starts dragging from this node</summary>
    public event UnityAction<NodeController> OnDragStart;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        _baseScale = transform.localScale;
        SetupNodeType();
        UpdateVisuals();
    }

    private void Start()
    {
        // Face the camera
        if (_textCanvas != null && Camera.main != null)
        {
            _textCanvas.worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Pulse animation for player and enemy nodes
        if (Owner != Faction.Neutral)
        {
            UpdatePulseAnimation();
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// Configures the node type parameters including size,
    /// production multiplier, and max soldiers.
    /// </summary>
    private void SetupNodeType()
    {
        switch (_nodeType)
        {
            case NodeType.Small:
                _nodeRadius = 0.5f;
                ProductionMultiplier = 1.0f;
                MaxSoldiers = 999;
                _baseScale = new Vector3(1f, 1f, 1f);
                break;

            case NodeType.Medium:
                _nodeRadius = 0.7f;
                ProductionMultiplier = 1.5f;
                MaxSoldiers = 999;
                _baseScale = new Vector3(1.4f, 1.4f, 1.4f);
                break;

            case NodeType.Large:
                _nodeRadius = 0.9f;
                ProductionMultiplier = 2.0f;
                MaxSoldiers = 999;
                _baseScale = new Vector3(1.8f, 1.8f, 1.8f);
                break;

            case NodeType.Fortress:
                _nodeRadius = 1.1f;
                ProductionMultiplier = 3.0f;
                MaxSoldiers = 999;
                _baseScale = new Vector3(2.2f, 2.2f, 2.2f);
                break;
        }

        transform.localScale = _baseScale;
    }

    /// <summary>
    /// Initializes the node with specific ownership and soldier count.
    /// Called by GameManager when setting up a level.
    /// </summary>
    /// <param name="owner">The initial faction owner</param>
    /// <param name="soldierCount">Starting number of soldiers</param>
    /// <param name="worldPos">Position in world space</param>
    public void Initialize(Faction owner, int soldierCount, Vector3 worldPos)
    {
        transform.position = worldPos;
        Owner = owner;
        CurrentSoldiers = soldierCount;
        _displaySoldiers = soldierCount;
        _productionTimer = 0f;
        IsInteractable = true;

        SetupNodeType();
        UpdateVisuals();
        UpdateSoldierText();
    }

    // =========================================================
    // SOLDIER PRODUCTION
    // =========================================================

    /// <summary>
    /// Called every frame by GameManager to update soldier production.
    /// Nodes owned by a faction (not neutral) produce soldiers over time.
    /// </summary>
    /// <param name="deltaTime">Time since last frame</param>
    public void UpdateNode(float deltaTime)
    {
        if (Owner == Faction.Neutral) return;
        if (CurrentSoldiers >= MaxSoldiers) return;

        // Accumulate production time
        float productionRate = GameManager.Instance.SoldierProductionRate * ProductionMultiplier;
        _productionTimer += deltaTime;

        // Produce one soldier when timer reaches the production rate
        if (_productionTimer >= 1f / productionRate)
        {
            _productionTimer -= 1f / productionRate;
            AddSoldiers(1);
        }
    }

    /// <summary>
    /// Adds soldiers to this node, capped at the maximum.
    /// </summary>
    /// <param name="amount">Number of soldiers to add</param>
    public void AddSoldiers(int amount)
    {
        int oldCount = CurrentSoldiers;
        CurrentSoldiers = Mathf.Min(CurrentSoldiers + amount, MaxSoldiers);

        if (CurrentSoldiers != oldCount)
        {
            UpdateSoldierText();
            OnSoldierCountChanged?.Invoke(this, CurrentSoldiers);
        }
    }

    /// <summary>
    /// Removes soldiers from this node for launching attacks.
    /// </summary>
    /// <param name="amount">Number of soldiers to remove</param>
    public void RemoveSoldiers(int amount)
    {
        CurrentSoldiers = Mathf.Max(CurrentSoldiers - amount, 0);
        UpdateSoldierText();
        OnSoldierCountChanged?.Invoke(this, CurrentSoldiers);
    }

    // =========================================================
    // ATTACK RECEPTION
    // =========================================================

    /// <summary>
    /// Receives an incoming attack from enemy troop projectiles.
    /// Handles damage calculation and ownership transfer.
    /// </summary>
    /// <param name="attackSoldiers">Number of attacking soldiers</param>
    /// <param name="attackingFaction">The faction that launched the attack</param>
    public void ReceiveAttack(int attackSoldiers, Faction attackingFaction)
    {
        if (attackSoldiers <= 0) return;

        Faction oldOwner = Owner;

        if (Owner == Faction.Neutral || Owner != attackingFaction)
        {
            // Reduce defenders
            CurrentSoldiers -= attackSoldiers;

            if (CurrentSoldiers <= 0)
            {
                // Node captured! Transfer ownership
                CurrentSoldiers = Mathf.Abs(CurrentSoldiers); // Remaining attackers become garrison
                Faction newOwner = attackingFaction;
                ChangeOwnership(oldOwner, newOwner);
            }
            else
            {
                UpdateSoldierText();
            }
        }
        else
        {
            // Reinforcement: same faction troops arriving
            AddSoldiers(attackSoldiers);
        }
    }

    /// <summary>
    /// Changes the ownership of this node to a new faction.
    /// Handles visual updates, effects, and event firing.
    /// </summary>
    private void ChangeOwnership(Faction oldOwner, Faction newOwner)
    {
        Owner = newOwner;

        // Reset production timer
        _productionTimer = 0f;

        // Update visuals to reflect new ownership
        UpdateVisuals();
        UpdateSoldierText();

        // Spawn capture effects
        if (EffectsManager.Instance != null)
        {
            EffectsManager.Instance.SpawnCaptureExplosion(transform.position, newOwner);
        }

        // Haptic feedback for player captures
        if (newOwner == Faction.Player)
        {
            if (HapticFeedbackManager.Instance != null)
            {
                HapticFeedbackManager.Instance.TriggerCaptureHaptic();
            }
        }

        // Fire capture event
        OnNodeCaptured?.Invoke(this, oldOwner, newOwner);
    }

    // =========================================================
    // VISUAL UPDATES
    // =========================================================

    /// <summary>
    /// Updates all visual elements to match the current state
    /// (ownership color, ring, highlight, etc.)
    /// </summary>
    public void UpdateVisuals()
    {
        Color factionColor = GetFactionColor();

        // Update background color with neon glow effect
        if (_nodeBackground != null)
        {
            _nodeBackground.color = factionColor;

            // Add slight glow for player and enemy nodes
            if (Owner != Faction.Neutral)
            {
                _nodeBackground.material?.SetColor("_GlowColor", factionColor);
            }
        }

        // Update ring color (slightly brighter)
        if (_nodeRing != null)
        {
            Color ringColor = new Color(
                factionColor.r * 1.3f,
                factionColor.g * 1.3f,
                factionColor.b * 1.3f,
                0.8f
            );
            _nodeRing.color = ringColor;
        }

        // Update highlight
        if (_highlightIndicator != null)
        {
            _highlightIndicator.SetActive(_isHighlighted);
        }
    }

    /// <summary>
    /// Gets the color associated with a specific faction.
    /// </summary>
    private Color GetFactionColor()
    {
        switch (Owner)
        {
            case Faction.Player: return _playerColor;
            case Faction.Enemy: return _enemyColor;
            default: return _neutralColor;
        }
    }

    /// <summary>
    /// Updates the soldier count display text.
    /// Shows abbreviated numbers for large counts (e.g., "1.2K").
    /// </summary>
    private void UpdateSoldierText()
    {
        if (_soldierText != null)
        {
            _soldierText.text = FormatSoldierCount(CurrentSoldiers);
        }
    }

    /// <summary>
    /// Formats large soldier counts into readable abbreviated form.
    /// </summary>
    private string FormatSoldierCount(int count)
    {
        if (count >= 1000000)
            return $"{count / 1000000f:F1}M";
        if (count >= 1000)
            return $"{count / 1000f:F1}K";
        return count.ToString();
    }

    /// <summary>
    /// Smooth pulse animation that makes active nodes feel alive.
    /// </summary>
    private void UpdatePulseAnimation()
    {
        _pulseTimer += Time.deltaTime * _pulseSpeed;
        float pulseFactor = 1f + Mathf.Sin(_pulseTimer) * _pulseAmount;
        transform.localScale = _baseScale * pulseFactor;
    }

    // =========================================================
    // INTERACTION HANDLING
    // =========================================================

    /// <summary>
    /// Sets whether this node responds to player touch input.
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        IsInteractable = interactable;
    }

    /// <summary>
    /// Sets the visual highlight state (shown during drag).
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        _isHighlighted = highlighted;
        if (_highlightIndicator != null)
        {
            _highlightIndicator.SetActive(highlighted);
        }
    }

    // =========================================================
    // TOUCH INPUT (CALLED FROM INPUT MANAGER OR NODE COLLIDER)
    // =========================================================

    /// <summary>
    /// Called when the player touches this node. Initiates drag if
    /// the node is player-owned and interactable.
    /// </summary>
    public void OnPointerDown()
    {
        if (!IsInteractable) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        if (Owner != Faction.Player) return;

        OnDragStart?.Invoke(this);
    }

    /// <summary>
    /// Gets the world position of this node.
    /// </summary>
    public Vector3 GetWorldPosition() => transform.position;

    /// <summary>
    /// Gets the distance from this node to a world position.
    /// </summary>
    public float DistanceTo(Vector3 worldPos) => Vector3.Distance(transform.position, worldPos);

    // =========================================================
    // DEBUG & INSPECTOR HELPERS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Draw node radius in editor
        Gizmos.color = Owner == Faction.Player ? _playerColor :
                        Owner == Faction.Enemy ? _enemyColor : _neutralColor;
        Gizmos.DrawWireSphere(transform.position, _nodeRadius);
    }
}
