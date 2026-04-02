// ===================================================================
// Map Wars: Tactical Conquest - Haptic Feedback Manager (Effects Script)
// Description: Manages haptic feedback (vibration) on Android devices.
//              Provides different vibration patterns for game events
//              like node capture, attack launch, and game over.
// ===================================================================

using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Manages device vibration/haptic feedback for different game events.
/// Provides calibrated vibration patterns optimized for mobile gameplay.
/// Uses Android Vibrator service with fallbacks for other platforms.
/// Attach to "HapticFeedbackManager" GameObject.
/// </summary>
public class HapticFeedbackManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static HapticFeedbackManager _instance;
    public static HapticFeedbackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HapticFeedbackManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("HapticFeedbackManager");
                    _instance = go.AddComponent<HapticFeedbackManager>();
                }
            }
            return _instance;
        }
    }

    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Haptic Settings")]
    [SerializeField] private bool _hapticsEnabled = true;
    [SerializeField] private float _captureVibrationDuration = 0.15f;
    [SerializeField] private float _attackVibrationDuration = 0.05f;
    [SerializeField] private float _defeatVibrationDuration = 0.5f;

    [Header("Haptic Intensity (0.0 to 1.0)")]
    [SerializeField, Range(0f, 1f)] private float _captureIntensity = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _attackIntensity = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _defeatIntensity = 1.0f;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private AndroidJavaObject _vibrator;
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

        InitializeVibrator();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    /// <summary>
    /// Initializes the Android vibrator service.
    /// Requests VIBRATE permission if not already granted.
    /// </summary>
    private void InitializeVibrator()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Vibrate))
            {
                Permission.RequestUserPermission(Permission.Vibrate);
            }

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (activity != null)
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                _isInitialized = true;
                Debug.Log("[HapticFeedback] Vibrator initialized");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticFeedback] Failed to initialize: {e.Message}");
            _isInitialized = false;
        }
#else
        Debug.Log("[HapticFeedback] Haptics not available (non-Android platform)");
        _isInitialized = false;
#endif
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>
    /// Light haptic feedback for initiating an attack.
    /// Short, subtle vibration to confirm player action.
    /// </summary>
    public void TriggerAttackHaptic()
    {
        if (!_hapticsEnabled || !_isInitialized) return;
        Vibrate(_attackVibrationDuration, _attackIntensity);
    }

    /// <summary>
    /// Medium haptic feedback when the player captures a node.
    /// More noticeable vibration to celebrate the event.
    /// </summary>
    public void TriggerCaptureHaptic()
    {
        if (!_hapticsEnabled || !_isInitialized) return;

        // Double-tap pattern for capture
        Vibrate(_captureVibrationDuration, _captureIntensity);
        StartCoroutine(DelayedVibrate(_captureVibrationDuration * 2f, _captureVibrationDuration * 0.5f, _captureIntensity * 0.5f));
    }

    /// <summary>
    /// Strong haptic feedback on game over (defeat).
    /// Longer, more intense vibration to convey impact.
    /// </summary>
    public void TriggerDefeatHaptic()
    {
        if (!_hapticsEnabled || !_isInitialized) return;
        Vibrate(_defeatVibrationDuration, _defeatIntensity);
    }

    /// <summary>
    /// Light tick haptic for UI button presses.
    /// Very short and subtle.
    /// </summary>
    public void TriggerUIHaptic()
    {
        if (!_hapticsEnabled || !_isInitialized) return;
        Vibrate(0.03f, 0.1f);
    }

    /// <summary>
    /// Enables or disables haptic feedback globally.
    /// </summary>
    public void SetHapticsEnabled(bool enabled)
    {
        _hapticsEnabled = enabled;
        PlayerPrefs.SetInt("HapticsEnabled", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Gets whether haptic feedback is currently enabled.
    /// </summary>
    public bool IsHapticsEnabled()
    {
        return _hapticsEnabled;
    }

    // =========================================================
    // VIBRATION METHODS
    // =========================================================

    /// <summary>
    /// Triggers device vibration with specified duration and amplitude.
    /// </summary>
    /// <param name="duration">Duration in seconds</param>
    /// <param name="intensity">Amplitude from 0.0 to 1.0</param>
    private void Vibrate(float duration, float intensity)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_vibrator == null) return;

        try
        {
            // Convert to milliseconds
            long durationMs = (long)(duration * 1000f);
            // Convert intensity to Android amplitude (1-255)
            int amplitude = Mathf.RoundToInt(Mathf.Lerp(1, 255, intensity));

            // Android API 26+ uses VibrationEffect
            if (IsAndroidOreoOrHigher())
            {
                AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                    "createOneShot", durationMs, amplitude
                );
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", durationMs);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticFeedback] Vibration failed: {e.Message}");
        }
#else
        // Fallback for editor and non-Android platforms
        if (Application.isEditor)
        {
            Debug.Log($"[HapticFeedback] Vibration: {duration}s @ {intensity:P0} (editor)");
        }
#endif
    }

    /// <summary>
    /// Triggers a vibration pattern (long[], amplitudes[]) for complex patterns.
    /// </summary>
    private void VibratePattern(long[] pattern, int[] amplitudes, int repeat)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_vibrator == null) return;

        try
        {
            if (IsAndroidOreoOrHigher())
            {
                AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform", pattern, amplitudes, repeat
                );
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", pattern, repeat);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticFeedback] Pattern vibration failed: {e.Message}");
        }
#endif
    }

    // =========================================================
    // HELPERS
    // =========================================================

    /// <summary>
    /// Checks if the Android version is Oreo (API 26) or higher,
    /// which supports VibrationEffect with amplitude control.
    /// </summary>
    private bool IsAndroidOreoOrHigher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass buildClass = new AndroidJavaClass("android.os.Build$VERSION");
            int sdkVersion = buildClass.GetStatic<int>("SDK_INT");
            return sdkVersion >= 26;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    /// <summary>
    /// Coroutine for delayed secondary vibration (creates patterns).
    /// </summary>
    private System.Collections.IEnumerator DelayedVibrate(float delay, float duration, float intensity)
    {
        yield return new WaitForSeconds(delay);
        Vibrate(duration, intensity);
    }
}
