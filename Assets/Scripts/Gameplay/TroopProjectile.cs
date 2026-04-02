// ===================================================================
// Map Wars: Tactical Conquest - Troop Projectile (Gameplay Script)
// Description: Represents a group of soldiers moving from a source
//              node to a target node. Handles movement, collision
//              detection with the target, and visual effects.
// Attach to: Troop projectile prefab.
// ===================================================================

using UnityEngine;
using System.Collections;

/// <summary>
/// A projectile that carries a group of soldiers from one node to another.
/// When it reaches the target node, it deposits its soldiers as an attack.
/// Multiple projectiles can be launched simultaneously from different nodes.
/// </summary>
public class TroopProjectile : MonoBehaviour
{
    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _arrivalThreshold = 0.3f;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer _projectileSprite;
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private float _projectileSize = 0.2f;
    [SerializeField] private ParticleSystem _trailParticle;

    [Header("Colors")]
    [SerializeField] private Color _playerTroopColor = new Color(0.3f, 0.7f, 1f, 0.9f);
    [SerializeField] private Color _enemyTroopColor = new Color(1f, 0.3f, 0.4f, 0.9f);

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    /// <summary>The node this projectile originated from</summary>
    public NodeController Source { get; private set; }

    /// <summary>The node this projectile is targeting</summary>
    public NodeController Target { get; private set; }

    /// <summary>Number of soldiers carried by this projectile</summary>
    public int SoldierCount { get; private set; }

    /// <summary>The faction that launched this projectile</summary>
    public Faction Faction { get; private set; }

    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>Fired when the projectile reaches its target node</summary>
    public event System.Action<NodeController, int, Faction> OnTroopReached;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Vector3 _targetPosition;
    private Vector3 _direction;
    private bool _isMoving = false;
    private float _launchDelay = 0f;
    private bool _hasArrived = false;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// Initializes the troop projectile with source, target, and soldier count.
    /// Called by GameManager when launching an attack.
    /// </summary>
    /// <param name="source">Origin node</param>
    /// <param name="target">Destination node</param>
    /// <param name="soldiers">Number of soldiers in this projectile</param>
    /// <param name="speed">Movement speed</param>
    /// <param name="delay">Delay before launch (stagger effect)</param>
    public void Initialize(NodeController source, NodeController target, int soldiers, float speed, float delay = 0f)
    {
        Source = source;
        Target = target;
        SoldierCount = soldiers;
        _speed = speed;
        _launchDelay = delay;
        Faction = source.Owner;
        _hasArrived = false;

        // Set initial position at source with slight random offset
        Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
        transform.position = source.transform.position + (Vector3)randomOffset;

        // Store target position (will be updated in Update for tracking)
        _targetPosition = target.transform.position;
        _direction = (_targetPosition - transform.position).normalized;

        // Setup visuals
        SetupVisuals();

        // Start movement (with optional delay)
        if (_launchDelay > 0f)
        {
            StartCoroutine(DelayedLaunch());
        }
        else
        {
            _isMoving = true;
        }

        // Auto-destroy safety net
        StartCoroutine(AutoDestroy(20f));
    }

    /// <summary>
    /// Configures visual elements based on faction ownership.
    /// Sets color, size, and trail effects.
    /// </summary>
    private void SetupVisuals()
    {
        Color factionColor = Faction == Faction.Player ? _playerTroopColor : _enemyTroopColor;

        // Set projectile color and size
        if (_projectileSprite != null)
        {
            _projectileSprite.color = factionColor;
            transform.localScale = Vector3.one * (_projectileSize + SoldierCount * 0.01f);
        }

        // Setup trail
        if (_trail != null)
        {
            _trail.startColor = factionColor;
            _trail.endColor = new Color(factionColor.r, factionColor.g, factionColor.b, 0f);
            _trail.widthMultiplier = _projectileSize * 0.5f;
        }

        // Setup trail particle
        if (_trailParticle != null)
        {
            var mainModule = _trailParticle.main;
            mainModule.startColor = factionColor;
        }
    }

    // =========================================================
    // MOVEMENT & UPDATE
    // =========================================================

    private void Update()
    {
        if (!_isMoving || _hasArrived) return;

        // Update target position (target node may have moved or we want tracking)
        if (Target != null)
        {
            _targetPosition = Target.transform.position;
            _direction = (_targetPosition - transform.position).normalized;
        }

        // Move towards target
        transform.position += _direction * _speed * Time.deltaTime;

        // Rotate to face movement direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Check if arrived at target
        float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);
        if (distanceToTarget <= _arrivalThreshold)
        {
            OnArrival();
        }
    }

    /// <summary>
    /// Handles the projectile reaching its target.
    /// Deposits soldiers and triggers effects.
    /// </summary>
    private void OnArrival()
    {
        if (_hasArrived) return;
        _hasArrived = true;
        _isMoving = false;

        // Spawn impact particles
        if (EffectsManager.Instance != null)
        {
            EffectsManager.Instance.SpawnImpactParticles(transform.position, Faction);
        }

        // Notify listeners (GameManager handles the actual logic)
        OnTroopReached?.Invoke(Target, SoldierCount, Faction);

        // Destroy the projectile
        StartCoroutine(FadeAndDestroy());
    }

    // =========================================================
    // COROUTINES
    // =========================================================

    /// <summary>
    /// Delays the launch of the projectile for staggered attack visuals.
    /// </summary>
    private IEnumerator DelayedLaunch()
    {
        // Shrink during delay
        float timer = 0f;
        float originalScale = transform.localScale.x;
        Vector3 originalPos = transform.position;

        while (timer < _launchDelay)
        {
            timer += Time.deltaTime;
            float progress = timer / _launchDelay;
            transform.localScale = Vector3.one * originalScale * progress;
            yield return null;
        }

        transform.localScale = Vector3.one * originalScale;
        _isMoving = true;
    }

    /// <summary>
    /// Fades out the projectile and then destroys it.
    /// </summary>
    private IEnumerator FadeAndDestroy()
    {
        float fadeDuration = 0.3f;
        float timer = 0f;

        // Disable trail immediately
        if (_trail != null)
        {
            _trail.emitting = false;
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - (timer / fadeDuration);
            if (_projectileSprite != null)
            {
                Color c = _projectileSprite.color;
                c.a = alpha;
                _projectileSprite.color = c;
            }
            transform.localScale *= 0.95f; // Shrink while fading
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Safety coroutine that destroys the projectile if it
    /// hasn't reached its target within the timeout.
    /// </summary>
    private IEnumerator AutoDestroy(float timeout)
    {
        yield return new WaitForSeconds(timeout);

        if (!_hasArrived)
        {
            // Force delivery if we timed out
            OnArrival();
        }
    }

    // =========================================================
    // UTILITY
    // =========================================================

    /// <summary>
    /// Gets the current world position of this projectile.
    /// </summary>
    public Vector3 GetPosition() => transform.position;

    /// <summary>
    /// Checks if this projectile is still active and moving.
    /// </summary>
    public bool IsMoving() => _isMoving && !_hasArrived;
}
