using UnityEngine;

/// <summary>
/// Smooth third-person orbit camera.
/// - Follows the target with SmoothDamp.
/// - Right-mouse drag OR touch rotates pitch and yaw freely (360 °).
/// - Mouse scroll wheel zooms in/out.
/// - Assign targetTransform in Inspector or let AutoCharacterSetup do it.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The root transform of the character to follow.")]
    public Transform targetTransform;

    [Header("Orbit")]
    [Tooltip("Horizontal mouse sensitivity.")]
    public float sensitivityX = 3f;
    [Tooltip("Vertical mouse sensitivity.")]
    public float sensitivityY = 2f;

    [Tooltip("Minimum vertical angle (look down limit, degrees).")]
    public float minPitch = -20f;
    [Tooltip("Maximum vertical angle (look up limit, degrees).")]
    public float maxPitch = 60f;

    [Header("Distance")]
    public float distance    = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 10f;
    public float zoomSpeed   = 4f;

    [Header("Follow smoothing")]
    [Tooltip("Position follow time (smaller = snappier).")]
    public float followSmoothTime = 0.1f;

    [Header("Offset")]
    [Tooltip("World-space offset added to the target position (raises the focus point).")]
    public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

    // ── internal ──────────────────────────────────────────────────────────────
    private float _yaw;
    private float _pitch = 15f;
    private Vector3 _currentVelocity;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Initialise yaw so camera starts directly behind the character.
        if (targetTransform != null)
            _yaw = targetTransform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (targetTransform == null) return;

        // ── Orbit input (hold RMB or always-on for a game controller) ──────
        bool rotate = Input.GetMouseButton(1);  // Right Mouse Button

        if (rotate)
        {
            _yaw   += Input.GetAxis("Mouse X") * sensitivityX;
            _pitch -= Input.GetAxis("Mouse Y") * sensitivityY;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // ── Zoom ───────────────────────────────────────────────────────────
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        // ── Compute desired camera position ───────────────────────────────
        Quaternion rotation     = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3   focusPoint    = targetTransform.position + targetOffset;
        Vector3   desiredPos    = focusPoint - rotation * Vector3.forward * distance;

        // ── Smooth follow ─────────────────────────────────────────────────
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos,
                                                ref _currentVelocity, followSmoothTime);

        // ── Look at focus point ───────────────────────────────────────────
        transform.LookAt(focusPoint);
    }
}
