// ===================================================================
// Map Wars: Tactical Conquest - Effects Manager (Effects Script)
// Description: Centralized particle effects system that manages
//              all visual effects in the game including capture
//              explosions, impact particles, boost effects, and
//              neon glow trails.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized effects system that creates and manages all visual
/// particle effects in the game. Uses object pooling for performance
/// on mobile devices.
/// Attach to "EffectsManager" GameObject.
/// </summary>
public class EffectsManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static EffectsManager _instance;
    public static EffectsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EffectsManager>();
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR REFERENCES
    // =========================================================

    [Header("Particle Effect Prefabs")]
    [SerializeField] private ParticleSystem _captureExplosionPrefab;
    [SerializeField] private ParticleSystem _impactParticlePrefab;
    [SerializeField] private ParticleSystem _boostEffectPrefab;
    [SerializeField] private ParticleSystem _nodeCaptureParticlesPrefab;
    [SerializeField] private ParticleSystem _troopTrailPrefab;

    [Header("Color Configuration")]
    [SerializeField] private Color _playerParticleColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color _enemyParticleColor = new Color(1f, 0.2f, 0.3f, 1f);
    [SerializeField] private Color _neutralParticleColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color _boostParticleColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Header("Pool Settings")]
    [SerializeField] private int _poolSizePerType = 10;

    // =========================================================
    // OBJECT POOL
    // =========================================================

    private Dictionary<string, Queue<ParticleSystem>> _particlePools = new Dictionary<string, Queue<ParticleSystem>>();
    private Dictionary<string, ParticleSystem> _prefabLookup = new Dictionary<string, ParticleSystem>();

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

        InitializePools();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // POOL INITIALIZATION
    // =========================================================

    /// <summary>
    /// Pre-creates particle effect instances for each type
    /// to minimize runtime allocations and garbage collection.
    /// </summary>
    private void InitializePools()
    {
        RegisterPrefab("capture", _captureExplosionPrefab);
        RegisterPrefab("impact", _impactParticlePrefab);
        RegisterPrefab("boost", _boostEffectPrefab);
        RegisterPrefab("capture_particles", _nodeCaptureParticlesPrefab);
        RegisterPrefab("trail", _troopTrailPrefab);

        foreach (var kvp in _prefabLookup)
        {
            if (kvp.Value == null) continue;

            Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
            for (int i = 0; i < _poolSizePerType; i++)
            {
                ParticleSystem ps = Instantiate(kvp.Value, transform);
                ps.gameObject.SetActive(false);
                pool.Enqueue(ps);
            }
            _particlePools[kvp.Key] = pool;
        }
    }

    private void RegisterPrefab(string key, ParticleSystem prefab)
    {
        if (prefab != null)
        {
            _prefabLookup[key] = prefab;
        }
    }

    // =========================================================
    // EFFECT SPAWNING
    // =========================================================

    /// <summary>
    /// Spawns a large explosion effect when a node is captured.
    /// Creates a burst of colored particles that radiate outward.
    /// </summary>
    /// <param name="position">World position of the effect</param>
    /// <param name="newOwner">Faction color to use</param>
    public void SpawnCaptureExplosion(Vector3 position, Faction newOwner)
    {
        ParticleSystem ps = GetPooledParticle("capture");
        if (ps == null) return;

        ps.transform.position = position;
        ps.gameObject.SetActive(true);

        // Set faction color
        Color factionColor = GetFactionColor(newOwner);
        SetParticleColor(ps, factionColor);

        // Configure for explosion
        var mainModule = ps.main;
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        mainModule.gravityModifier = 0.5f;

        ps.Play();

        // Return to pool after emission
        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
        StartCoroutine(ReturnToPool(ps, "capture", duration));
    }

    /// <summary>
    /// Spawns small impact particles when troop projectiles hit a target.
    /// Creates a short burst of particles at the impact point.
    /// </summary>
    /// <param name="position">World position of impact</param>
    /// <param name="attackingFaction">Faction color for the particles</param>
    public void SpawnImpactParticles(Vector3 position, Faction attackingFaction)
    {
        ParticleSystem ps = GetPooledParticle("impact");
        if (ps == null) return;

        ps.transform.position = position;
        ps.gameObject.SetActive(true);

        Color factionColor = GetFactionColor(attackingFaction);
        SetParticleColor(ps, factionColor);

        var mainModule = ps.main;
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);

        ps.Play();

        float duration = ps.main.duration + 0.5f;
        StartCoroutine(ReturnToPool(ps, "impact", duration));
    }

    /// <summary>
    /// Spawns a golden boost effect when the player receives
    /// military support (rewarded ad).
    /// Creates upward-floating sparkle particles.
    /// </summary>
    /// <param name="position">World position of the boosted node</param>
    public void SpawnBoostEffect(Vector3 position)
    {
        ParticleSystem ps = GetPooledParticle("boost");
        if (ps == null) return;

        ps.transform.position = position;
        ps.gameObject.SetActive(true);

        SetParticleColor(ps, _boostParticleColor);

        var mainModule = ps.main;
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        mainModule.gravityModifier = -1f; // Float upward

        ps.Play();

        float duration = ps.main.duration + 1f;
        StartCoroutine(ReturnToPool(ps, "boost", duration));
    }

    /// <summary>
    /// Spawns decorative particles around a node when it's captured.
    /// These orbit briefly around the node before fading.
    /// </summary>
    /// <param name="position">World position of the captured node</param>
    /// <param name="owner">Faction color for the particles</param>
    public void SpawnCaptureParticles(Vector3 position, Faction owner)
    {
        ParticleSystem ps = GetPooledParticle("capture_particles");
        if (ps == null) return;

        ps.transform.position = position;
        ps.gameObject.SetActive(true);

        Color factionColor = GetFactionColor(owner);
        SetParticleColor(ps, factionColor);

        var mainModule = ps.main;
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        // Add shape module for orbital effect
        var shapeModule = ps.shape;
        shapeModule.shape = ParticleSystemShapeType.Circle;
        shapeModule.radius = 1.0f;

        ps.Play();

        float duration = ps.main.duration + 1f;
        StartCoroutine(ReturnToPool(ps, "capture_particles", duration));
    }

    // =========================================================
    // POOL MANAGEMENT
    // =========================================================

    /// <summary>
    /// Gets a particle system from the pool for the specified type.
    /// </summary>
    private ParticleSystem GetPooledParticle(string type)
    {
        if (!_particlePools.TryGetValue(type, out Queue<ParticleSystem> pool))
        {
            Debug.LogWarning($"[EffectsManager] No pool for type: {type}");
            return null;
        }

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // Pool exhausted - create a new instance
        if (_prefabLookup.TryGetValue(type, out ParticleSystem prefab))
        {
            ParticleSystem newPs = Instantiate(prefab, transform);
            return newPs;
        }

        return null;
    }

    /// <summary>
    /// Returns a particle system to the pool after it finishes playing.
    /// </summary>
    private System.Collections.IEnumerator ReturnToPool(ParticleSystem ps, string type, float delay)
    {
        yield return new WaitForSeconds(delay);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);

        if (_particlePools.TryGetValue(type, out Queue<ParticleSystem> pool))
        {
            pool.Enqueue(ps);
        }
        else
        {
            Destroy(ps.gameObject);
        }
    }

    // =========================================================
    // UTILITY
    // =========================================================

    /// <summary>
    /// Gets the particle color associated with a faction.
    /// </summary>
    private Color GetFactionColor(Faction faction)
    {
        switch (faction)
        {
            case Faction.Player: return _playerParticleColor;
            case Faction.Enemy: return _enemyParticleColor;
            default: return _neutralParticleColor;
        }
    }

    /// <summary>
    /// Sets the start color of a particle system's main module.
    /// </summary>
    private void SetParticleColor(ParticleSystem ps, Color color)
    {
        if (ps == null) return;

        var mainModule = ps.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(color);

        // Also set color over lifetime for fade effect
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Color(color.r, color.g, color.b, 1f),
            new Color(color.r, color.g, color.b, 0f)
        );
        colorOverLifetime.enabled = true;
    }
}
