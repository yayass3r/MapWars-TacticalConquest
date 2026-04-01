// ===================================================================
// Map Wars: Tactical Conquest - Screen Adapter (Config Script)
// Description: Handles responsive layout for different Android
//              screen sizes and aspect ratios. Ensures the game
//              looks correct on phones, tablets, and foldables.
// ===================================================================

using UnityEngine;

/// <summary>
/// Adapts the game's camera and layout to work with different
/// Android screen sizes and aspect ratios. Ensures the gameplay
/// area is properly scaled and centered regardless of device.
/// Attach to "ScreenAdapter" GameObject in the scene.
/// </summary>
public class ScreenAdapter : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static ScreenAdapter _instance;
    public static ScreenAdapter Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<ScreenAdapter>();
            return _instance;
        }
    }

    // =========================================================
    // CONFIGURATION
    // =========================================================

    [Header("Camera Settings")]
    [SerializeField] private float _referenceHeight = 10f;    // Design resolution height
    [SerializeField] private float _minOrthoSize = 5f;
    [SerializeField] private float _maxOrthoSize = 15f;
    [SerializeField] private bool _maintainAspectRatio = true;

    [Header("Safe Area")]
    [SerializeField] private bool _applySafeArea = true;
    [SerializeField] private RectTransform _safeAreaPanel;

    [Header("UI Scaling")]
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private float _referenceDPI = 160f;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Camera _mainCamera;
    private float _currentAspectRatio;
    private Vector2 _lastScreenSize;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        _mainCamera = Camera.main;
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    private void Start()
    {
        AdaptScreen();
    }

    private void Update()
    {
        // Check for screen size changes (rotation, folding, etc.)
        if (Screen.width != (int)_lastScreenSize.x || Screen.height != (int)_lastScreenSize.y)
        {
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            AdaptScreen();
        }
    }

    // =========================================================
    // ADAPTATION
    // =========================================================

    /// <summary>
    /// Adapts all game elements to the current screen size.
    /// Adjusts camera orthographic size, safe area, and UI scaling.
    /// </summary>
    public void AdaptScreen()
    {
        AdaptCamera();
        AdaptSafeArea();
        AdaptUIScaling();

        Debug.Log($"[ScreenAdapter] Adapted to: {Screen.width}x{Screen.height} " +
                  $"@{Screen.dpi}dpi, Aspect: {(float)Screen.width / Screen.height:F2}");
    }

    /// <summary>
    /// Adjusts the camera's orthographic size based on screen aspect ratio.
    /// Wider screens show more horizontal content; taller screens adjust vertical.
    /// </summary>
    private void AdaptCamera()
    {
        if (_mainCamera == null) return;

        float aspect = (float)Screen.width / Screen.height;
        _currentAspectRatio = aspect;

        // Base orthographic size from reference height
        float targetSize = _referenceHeight / 2f;

        // Adjust for extreme aspect ratios
        if (aspect < 0.5f)  // Very tall (folded or unusual)
        {
            targetSize *= 1.3f;
        }
        else if (aspect > 2.2f) // Very wide (landscape tablets, ultra-wide)
        {
            targetSize *= 0.85f;
        }

        // Clamp to allowed range
        targetSize = Mathf.Clamp(targetSize, _minOrthoSize, _maxOrthoSize);

        _mainCamera.orthographicSize = targetSize;
    }

    /// <summary>
    /// Applies device safe area to UI elements.
    /// Accounts for notch, camera cutout, and system bars.
    /// </summary>
    private void AdaptSafeArea()
    {
        if (!_applySafeArea || _safeAreaPanel == null) return;

        Rect safeArea = Screen.safeArea;

        // Convert safe area to anchor coordinates
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _safeAreaPanel.anchorMin = anchorMin;
        _safeAreaPanel.anchorMax = anchorMax;
        _safeAreaPanel.sizeDelta = Vector2.zero;

        Debug.Log($"[ScreenAdapter] Safe area: {safeArea}");
    }

    /// <summary>
    /// Adjusts UI canvas scaling factor based on screen resolution and DPI.
    /// Ensures UI elements are readable on all devices.
    /// </summary>
    private void AdaptUIScaling()
    {
        if (_uiCanvas == null) return;

        CanvasScaler scaler = _uiCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;

        // Set scale with screen height to ensure consistent UI size
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Screen.width, _referenceHeight * 100f);
        scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
        scaler.referencePixelsPerUnit = 100;
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>
    /// Gets the current screen aspect ratio.
    /// </summary>
    public float GetAspectRatio() => _currentAspectRatio;

    /// <summary>
    /// Checks if the device is in landscape orientation.
    /// </summary>
    public bool IsLandscape() => Screen.width > Screen.height;

    /// <summary>
    /// Checks if the device is a tablet (based on screen diagonal).
    /// </summary>
    public bool IsTablet()
    {
        float diagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        return diagonal > 1800f; // Approximate tablet threshold in pixels
    }

    /// <summary>
    /// Gets the device's DPI (dots per inch).
    /// </summary>
    public int GetDPI() => Screen.dpi;
}
