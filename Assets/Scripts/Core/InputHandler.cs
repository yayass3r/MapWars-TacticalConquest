// ===================================================================
// Map Wars: Tactical Conquest - Input Handler (Core Script)
// Description: Handles all touch and mouse input for the game.
//              Translates screen touches into game actions like
//              initiating attacks, dragging, and UI interactions.
// Works with both touch (mobile) and mouse (editor) input.
// ===================================================================

using UnityEngine;

/// <summary>
/// Manages player input for the game. Detects touch/mouse events
/// on game nodes and translates them into attack commands.
/// Handles the drag gesture from source node to target node.
/// Attach to "InputManager" GameObject (same as Camera).
/// </summary>
public class InputHandler : MonoBehaviour
{
    // =========================================================
    // INSPECTOR CONFIGURATION
    // =========================================================

    [Header("Input Settings")]
    [SerializeField] private float _maxDragDistance = 50f;
    [SerializeField] private LayerMask _nodeLayer;
    [SerializeField] private float _raycastDistance = 100f;

    [Header("Visual Feedback")]
    [SerializeField] private float _dragLineSmoothing = 10f;

    // =========================================================
    // PRIVATE FIELDS
    // =========================================================

    private Camera _mainCamera;
    private bool _isDragging = false;
    private NodeController _dragSourceNode = null;
    private Vector3 _dragStartWorldPos;
    private Vector3 _currentDragWorldPos;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();
        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    // =========================================================
    // MOUSE INPUT (EDITOR)
    // =========================================================

    /// <summary>
    /// Handles mouse input for testing in the Unity Editor.
    /// Maps mouse buttons to touch equivalents.
    /// </summary>
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnPointerDown(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            OnPointerMove(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnPointerUp(Input.mousePosition);
        }
    }

    // =========================================================
    // TOUCH INPUT (MOBILE)
    // =========================================================

    /// <summary>
    /// Handles multi-touch input for mobile devices.
    /// Only processes the first touch for simplicity.
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                OnPointerDown(touch.position);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                OnPointerMove(touch.position);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnPointerUp(touch.position);
                break;
        }
    }

    // =========================================================
    // POINTER EVENT HANDLERS
    // =========================================================

    /// <summary>
    /// Called when the player touches/clicks the screen.
    /// Checks if a node was tapped to initiate a drag.
    /// </summary>
    /// <param name="screenPos">Screen position of the touch/click</param>
    private void OnPointerDown(Vector2 screenPos)
    {
        Vector3 worldPos = ScreenToWorld(screenPos);
        _dragStartWorldPos = worldPos;

        // Raycast to find if a node was tapped
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0.5f, _nodeLayer);

        if (hit.collider != null)
        {
            NodeController node = hit.collider.GetComponent<NodeController>();
            if (node != null && node.Owner == Faction.Player)
            {
                _dragSourceNode = node;
                _isDragging = true;
                _currentDragWorldPos = worldPos;

                GameManager.Instance.OnDragStarted(node);

                // Trigger haptic feedback
                if (HapticFeedbackManager.Instance != null)
                {
                    HapticFeedbackManager.Instance.TriggerUIHaptic();
                }
            }
        }
    }

    /// <summary>
    /// Called when the player moves their finger/mouse while pressing.
    /// Updates the drag visualization and tracks position.
    /// </summary>
    private void OnPointerMove(Vector2 screenPos)
    {
        if (!_isDragging || _dragSourceNode == null) return;

        _currentDragWorldPos = ScreenToWorld(screenPos);

        // Check if drag distance exceeds maximum
        float distance = Vector3.Distance(_dragStartWorldPos, _currentDragWorldPos);
        if (distance > _maxDragDistance)
        {
            // Cancel drag if too far
            OnPointerUp(screenPos);
            return;
        }

        // Update attack line visualization
        GameManager.Instance.OnDragUpdated(_currentDragWorldPos);
    }

    /// <summary>
    /// Called when the player releases their finger/mouse.
    /// If released over a valid target node, launches the attack.
    /// </summary>
    private void OnPointerUp(Vector2 screenPos)
    {
        if (!_isDragging || _dragSourceNode == null)
        {
            ResetDrag();
            return;
        }

        Vector3 worldPos = ScreenToWorld(screenPos);

        // Check if released over a target node
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0.5f, _nodeLayer);

        if (hit.collider != null)
        {
            NodeController targetNode = hit.collider.GetComponent<NodeController>();
            if (targetNode != null && targetNode != _dragSourceNode)
            {
                // Launch attack via GameManager
                GameManager.Instance.OnDragEnded(worldPos);

                // Trigger attack haptic
                if (HapticFeedbackManager.Instance != null)
                {
                    HapticFeedbackManager.Instance.TriggerAttackHaptic();
                }

                ResetDrag();
                return;
            }
        }

        // Released on empty space - cancel attack
        GameManager.Instance.OnDragEnded(worldPos);
        ResetDrag();
    }

    // =========================================================
    // DRAG MANAGEMENT
    // =========================================================

    /// <summary>
    /// Resets all drag state variables.
    /// </summary>
    private void ResetDrag()
    {
        _isDragging = false;
        _dragSourceNode = null;
    }

    // =========================================================
    // COORDINATE CONVERSION
    // =========================================================

    /// <summary>
    /// Converts a screen position to a world position using the camera.
    /// </summary>
    /// <param name="screenPos">Screen pixel position</param>
    /// <returns>World position at the game plane</returns>
    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (_mainCamera == null) return Vector3.zero;

        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, _mainCamera.nearClipPlane + 10f)
        );

        return worldPos;
    }

    /// <summary>
    /// Converts a world position to a screen position.
    /// </summary>
    /// <param name="worldPos">World position</param>
    /// <returns>Screen pixel position</returns>
    private Vector2 WorldToScreen(Vector3 worldPos)
    {
        if (_mainCamera == null) return Vector2.zero;
        return _mainCamera.WorldToScreenPoint(worldPos);
    }
}
