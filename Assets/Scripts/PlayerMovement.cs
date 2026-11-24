using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Third-person player movement using CharacterController.
/// - WASD / Arrow Keys: camera-relative movement with Slerp rotation.
/// - Space (Jump): jump with isGrounded check.
/// - Animator parameters "Speed" (float) and "IsGrounded" (bool) are updated
///   safely only when they exist in the current AnimatorController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Walk speed in metres/second.")]
    public float moveSpeed = 4f;

    [Tooltip("How fast the character rotates toward the move direction.")]
    [Range(1f, 30f)]
    public float rotationSpeed = 10f;

    [Header("Jump & Gravity")]
    [Tooltip("Initial upward velocity on jump.")]
    public float jumpForce = 5f;

    [Tooltip("Gravity applied every second (negative = downward).")]
    public float gravity = -15f;

    // ── internal ──────────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Animator _animator;
    private Vector3 _velocity;          // accumulated vertical velocity

    // cached parameter hashes (0 = not found in this AnimatorController)
    private int _hashSpeed;
    private bool _hasSpeed;
    private int _hashIsGrounded;
    private bool _hasIsGrounded;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cc       = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        CacheAnimatorParameters();
    }

    private void OnValidate()
    {
        // Refresh cached parameters when inspector changes.
        if (_animator != null) CacheAnimatorParameters();
    }

    private void CacheAnimatorParameters()
    {
        _hasSpeed      = false;
        _hasIsGrounded = false;

        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in _animator.parameters)
        {
            if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float)
            {
                _hashSpeed = Animator.StringToHash("Speed");
                _hasSpeed  = true;
            }
            else if (p.name == "IsGrounded" && p.type == AnimatorControllerParameterType.Bool)
            {
                _hashIsGrounded = Animator.StringToHash("IsGrounded");
                _hasIsGrounded  = true;
            }
        }
    }

    private void Update()
    {
        bool grounded = _cc.isGrounded;

        // ── Reset downward velocity when grounded ─────────────────────────
        if (grounded && _velocity.y < 0f)
            _velocity.y = -2f;   // small negative keeps isGrounded reliable

        // ── Gather WASD / Arrow input ─────────────────────────────────────
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Build a world-space move direction relative to the main camera.
        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(Camera.main.transform.right,   Vector3.up).normalized;
        Vector3 moveDir    = (camForward * v + camRight * h);

        bool isMoving = moveDir.sqrMagnitude > 0.01f;
        if (isMoving) moveDir.Normalize();

        // ── Rotate toward move direction ─────────────────────────────────
        if (isMoving)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot,
                                                    rotationSpeed * Time.deltaTime);
        }

        // ── Horizontal movement ──────────────────────────────────────────
        _cc.Move(moveDir * moveSpeed * Time.deltaTime);

        // ── Jump ─────────────────────────────────────────────────────────
        if (grounded && Input.GetButtonDown("Jump"))
            _velocity.y = jumpForce;

        // ── Gravity ───────────────────────────────────────────────────────
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(new Vector3(0f, _velocity.y * Time.deltaTime, 0f));

        // ── Animator ─────────────────────────────────────────────────────
        if (_hasSpeed)
            _animator.SetFloat(_hashSpeed, isMoving ? moveDir.magnitude * moveSpeed : 0f);

        if (_hasIsGrounded)
            _animator.SetBool(_hashIsGrounded, grounded);
    }
}
