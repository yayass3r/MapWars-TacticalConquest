// ===================================================================
// Map Wars: Tactical Conquest - Audio Manager (Effects Script)
// Description: Centralized audio system for all game sounds.
//              Manages background music, SFX, and volume controls.
//              Uses object pooling for audio sources.
// ===================================================================

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enum of all sound effects used in the game.
/// </summary>
public enum SoundType
{
    NodeCapture,        // Played when a node is captured
    NodeLost,           // Played when a player node is lost
    AttackLaunch,       // Played when troops are sent
    TroopImpact,        // Played when troops hit a target
    ButtonClick,        // Played for UI button interactions
    Victory,            // Played on level completion
    Defeat,             // Played on level failure
    EnergyRefill,       // Played when energy is refilled
    CoinCollect,        // Played when coins are earned
    AdReward,           // Played when rewarded ad completes
    SkinPurchase,       // Played when a skin is bought
    LevelSelect         // Played when selecting a level
}

/// <summary>
/// Manages all audio in the game. Handles background music and
/// sound effects with volume controls and pooling.
/// Attach to "AudioManager" GameObject.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR REFERENCES
    // =========================================================

    [Header("Music")]
    [SerializeField] private AudioClip _backgroundMusic;
    [SerializeField] private AudioSource _musicSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip _nodeCaptureSFX;
    [SerializeField] private AudioClip _nodeLostSFX;
    [SerializeField] private AudioClip _attackLaunchSFX;
    [SerializeField] private AudioClip _troopImpactSFX;
    [SerializeField] private AudioClip _buttonClickSFX;
    [SerializeField] private AudioClip _victorySFX;
    [SerializeField] private AudioClip _defeatSFX;
    [SerializeField] private AudioClip _coinCollectSFX;
    [SerializeField] private AudioClip _energyRefillSFX;

    [Header("Audio Settings")]
    [SerializeField] private int _sfxPoolSize = 8;
    [SerializeField] private float _musicFadeDuration = 1f;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Dictionary<SoundType, AudioClip> _sfxLookup = new Dictionary<SoundType, AudioClip>();
    private List<AudioSource> _sfxPool = new List<AudioSource>();
    private int _poolIndex = 0;
    private float _targetMusicVolume = 1f;

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

        BuildSFXLookup();
        InitializeSFXPool();
        LoadVolumeSettings();
        PlayMusic();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// Maps SoundType enums to their corresponding AudioClip references.
    /// </summary>
    private void BuildSFXLookup()
    {
        _sfxLookup[SoundType.NodeCapture] = _nodeCaptureSFX;
        _sfxLookup[SoundType.NodeLost] = _nodeLostSFX;
        _sfxLookup[SoundType.AttackLaunch] = _attackLaunchSFX;
        _sfxLookup[SoundType.TroopImpact] = _troopImpactSFX;
        _sfxLookup[SoundType.ButtonClick] = _buttonClickSFX;
        _sfxLookup[SoundType.Victory] = _victorySFX;
        _sfxLookup[SoundType.Defeat] = _defeatSFX;
        _sfxLookup[SoundType.CoinCollect] = _coinCollectSFX;
        _sfxLookup[SoundType.EnergyRefill] = _energyRefillSFX;
    }

    /// <summary>
    /// Creates a pool of AudioSource components for SFX playback.
    /// Pooled sources are reused to minimize garbage collection.
    /// </summary>
    private void InitializeSFXPool()
    {
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            GameObject sfxObj = new GameObject($"SFXSource_{i}");
            sfxObj.transform.SetParent(transform);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            _sfxPool.Add(source);
        }
    }

    /// <summary>
    /// Loads saved volume settings from SaveSystem.
    /// </summary>
    private void LoadVolumeSettings()
    {
        if (SaveSystem.Instance != null)
        {
            _musicSource.volume = SaveSystem.Instance.MusicVolume;
            _targetMusicVolume = SaveSystem.Instance.MusicVolume;
        }
    }

    // =========================================================
    // MUSIC
    // =========================================================

    /// <summary>
    /// Plays the background music on loop.
    /// </summary>
    public void PlayMusic()
    {
        if (_musicSource == null || _backgroundMusic == null) return;

        _musicSource.clip = _backgroundMusic;
        _musicSource.loop = true;
        _musicSource.volume = 0f;
        _musicSource.Play();

        StartCoroutine(FadeMusic(SaveSystem.Instance.MusicVolume));
    }

    /// <summary>
    /// Stops the background music with a fade out.
    /// </summary>
    public void StopMusic()
    {
        StartCoroutine(FadeMusic(0f, () =>
        {
            if (_musicSource != null)
                _musicSource.Stop();
        }));
    }

    /// <summary>
    /// Gradually fades music volume to the target level.
    /// </summary>
    private System.Collections.IEnumerator FadeMusic(float targetVolume, System.Action onComplete = null)
    {
        if (_musicSource == null) yield break;

        float startVolume = _musicSource.volume;
        float timer = 0f;

        while (timer < _musicFadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / _musicFadeDuration;
            _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        _musicSource.volume = targetVolume;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Sets the music volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        _targetMusicVolume = Mathf.Clamp01(volume);
        if (_musicSource != null)
        {
            _musicSource.volume = _targetMusicVolume;
        }
    }

    // =========================================================
    // SOUND EFFECTS
    // =========================================================

    /// <summary>
    /// Plays a sound effect of the specified type.
    /// Uses the pooled AudioSource system for efficiency.
    /// </summary>
    /// <param name="type">The sound effect to play</param>
    /// <param name="volumeScale">Optional volume multiplier</param>
    public void PlaySFX(SoundType type, float volumeScale = 1f)
    {
        if (!_sfxLookup.TryGetValue(type, out AudioClip clip))
        {
            Debug.LogWarning($"[AudioManager] No clip for sound type: {type}");
            return;
        }

        if (clip == null) return;

        // Get next available AudioSource from pool
        AudioSource source = GetNextAvailableSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = SaveSystem.Instance.SFXVolume * volumeScale;
        source.pitch = 1f;
        source.Play();
    }

    /// <summary>
    /// Plays a sound effect with random pitch variation for variety.
    /// </summary>
    public void PlaySFXWithVariation(SoundType type, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (!_sfxLookup.TryGetValue(type, out AudioClip clip)) return;
        if (clip == null) return;

        AudioSource source = GetNextAvailableSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = SaveSystem.Instance.SFXVolume;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.Play();
    }

    /// <summary>
    /// Gets the next available AudioSource from the pool.
    /// Cycles through sources in round-robin fashion.
    /// </summary>
    private AudioSource GetNextAvailableSource()
    {
        if (_sfxPool.Count == 0) return null;

        // Try to find a source that isn't playing
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            int idx = (_poolIndex + i) % _sfxPool.Count;
            if (!_sfxPool[idx].isPlaying)
            {
                _poolIndex = (idx + 1) % _sfxPool.Count;
                return _sfxPool[idx];
            }
        }

        // All sources are busy - use the oldest one
        AudioSource source = _sfxPool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _sfxPool.Count;
        return source;
    }

    /// <summary>
    /// Plays the node capture sound with appropriate faction.
    /// </summary>
    public void PlayCaptureSound(Faction newOwner)
    {
        if (newOwner == Faction.Player)
            PlaySFX(SoundType.NodeCapture);
        else
            PlaySFX(SoundType.NodeLost);
    }
}
