// ===================================================================
// Map Wars: Tactical Conquest - Grid Background (Visual Script)
// Description: Renders a dark background with a subtle technical
//              grid pattern. Creates the minimalist neon aesthetic
//              for the game environment.
// ===================================================================

using UnityEngine;

/// <summary>
/// Renders the game background with a dark color and subtle grid lines.
/// The grid creates a tactical/tech feel that matches the neon aesthetic.
/// Uses a LineRenderer grid for performance efficiency on mobile.
/// Attach to a "Background" GameObject in the scene.
/// </summary>
[RequireComponent(typeof(Camera))]
public class GridBackground : MonoBehaviour
{
    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Background Settings")]
    [SerializeField] private Color _backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
    [SerializeField] private Color _gridColor = new Color(0.15f, 0.15f, 0.25f, 0.3f);
    [SerializeField] private Color _gridAccentColor = new Color(0.2f, 0.2f, 0.35f, 0.5f);

    [Header("Grid Settings")]
    [SerializeField] private float _gridSpacing = 2f;
    [SerializeField] private float _majorGridEvery = 4;  // Every N lines is thicker
    [SerializeField] private float _gridLineWidth = 0.02f;
    [SerializeField] private float _majorGridLineWidth = 0.04f;

    [Header("Animation")]
    [SerializeField] private bool _animateGrid = true;
    [SerializeField] private float _scrollSpeed = 0.2f;
    [SerializeField] private float _pulseSpeed = 0.5f;
    [SerializeField] private float _pulseAmount = 0.02f;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Camera _camera;
    private Material _gridMaterial;
    private float _animationOffset = 0f;
    private float _pulseTimer = 0f;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        SetupCamera();
        CreateGridMaterial();
    }

    private void Start()
    {
        RenderGrid();
    }

    private void Update()
    {
        if (_animateGrid)
        {
            AnimateGrid();
        }
    }

    // =========================================================
    // SETUP
    // =========================================================

    /// <summary>
    /// Configures the camera background color.
    /// </summary>
    private void SetupCamera()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = _backgroundColor;
    }

    /// <summary>
    /// Creates a shader material for rendering the grid lines.
    /// Uses a simple unlit transparent shader for performance.
    /// </summary>
    private void CreateGridMaterial()
    {
        // Create a simple material for the grid lines
        Shader gridShader = Shader.Find("Unlit/Transparent");
        if (gridShader != null)
        {
            _gridMaterial = new Material(gridShader);
            _gridMaterial.color = _gridColor;
        }
    }

    // =========================================================
    // GRID RENDERING
    // =========================================================

    /// <summary>
    /// Renders the grid using LineRenderers for efficient mobile rendering.
    /// Creates vertical and horizontal lines across the visible area.
    /// </summary>
    private void RenderGrid()
    {
        if (_camera == null) return;

        float cameraHeight = _camera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * _camera.aspect;

        Vector3 cameraPos = _camera.transform.position;
        float minX = cameraPos.x - cameraWidth;
        float maxX = cameraPos.x + cameraWidth;
        float minY = cameraPos.y - cameraHeight;
        float maxY = cameraPos.y + cameraHeight;

        // Snap to grid
        float startX = Mathf.Floor(minX / _gridSpacing) * _gridSpacing;
        float startY = Mathf.Floor(minY / _gridSpacing) * _gridSpacing;

        // Create grid parent
        GameObject gridParent = new GameObject("GridLines");

        // Vertical lines
        int columnIndex = 0;
        for (float x = startX; x <= maxX; x += _gridSpacing)
        {
            bool isMajor = columnIndex % _majorGridEvery == 0;
            CreateGridLine(
                parent: gridParent.transform,
                start: new Vector3(x, minY, 0),
                end: new Vector3(x, maxY, 0),
                color: isMajor ? _gridAccentColor : _gridColor,
                width: isMajor ? _majorGridLineWidth : _gridLineWidth
            );
            columnIndex++;
        }

        // Horizontal lines
        int rowIndex = 0;
        for (float y = startY; y <= maxY; y += _gridSpacing)
        {
            bool isMajor = rowIndex % _majorGridEvery == 0;
            CreateGridLine(
                parent: gridParent.transform,
                start: new Vector3(minX, y, 0),
                end: new Vector3(maxX, y, 0),
                color: isMajor ? _gridAccentColor : _gridColor,
                width: isMajor ? _majorGridLineWidth : _gridLineWidth
            );
            rowIndex++;
        }
    }

    /// <summary>
    /// Creates a single grid line using a LineRenderer.
    /// </summary>
    private void CreateGridLine(Transform parent, Vector3 start, Vector3 end, Color color, float width)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(parent);

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.material = _gridMaterial;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        // Reduce sorting order so grid appears behind everything
        line.sortingOrder = -10;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    /// <summary>
    /// Animates the grid with subtle pulsing and color variation.
    /// Creates a "breathing" effect that makes the background feel alive.
    /// </summary>
    private void AnimateGrid()
    {
        _animationOffset += Time.deltaTime * _scrollSpeed;
        _pulseTimer += Time.deltaTime * _pulseSpeed;

        // Pulse the grid color intensity
        float pulse = Mathf.Sin(_pulseTimer) * _pulseAmount;
        Color pulsedColor = new Color(
            _gridColor.r + pulse,
            _gridColor.g + pulse,
            _gridColor.b + pulse,
            _gridColor.a
        );

        if (_gridMaterial != null)
        {
            _gridMaterial.color = pulsedColor;
        }
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>
    /// Updates the background color dynamically.
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
        if (_camera != null)
        {
            _camera.backgroundColor = color;
        }
    }

    /// <summary>
    /// Updates the grid color dynamically.
    /// </summary>
    public void SetGridColor(Color color)
    {
        _gridColor = color;
    }

    /// <summary>
    /// Enables or disables grid animation.
    /// </summary>
    public void SetAnimationEnabled(bool enabled)
    {
        _animateGrid = enabled;
    }
}
