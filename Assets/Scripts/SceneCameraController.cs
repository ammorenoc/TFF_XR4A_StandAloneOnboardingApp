
using UnityEngine;

[DisallowMultipleComponent]
public class SceneCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float fastSpeedMultiplier = 3f;
    public bool flyMode = true;              // If false, WASD movement is constrained to XZ plane
    public KeyCode upKey = KeyCode.E;
    public KeyCode downKey = KeyCode.Q;

    [Header("Orbit")]
    public float orbitSensitivity = 180f;    // deg/sec @ mouse delta = 1
    public bool invertOrbitY = false;
    [Range(-89f, 0f)] public float minPitch = -85f;
    [Range(0f, 89f)] public float maxPitch = 85f;

    [Header("Pan")]
    public float panSpeed = 1.0f;            // world units per normalized mouse delta
    public bool invertPanY = false;

    [Header("Zoom / Dolly")]
    public float zoomSpeed = 8f;             // wheel speed, scaled by distance
    public float dollyDragSpeed = 8f;        // MMB drag speed
    public float minDistance = 0.3f;
    public float maxDistance = 500f;

    [Header("Smoothing")]
    [Tooltip("Lower = snappier, Higher = smoother")]
    public float positionSmoothTime = 0.08f;
    public float rotationSmoothTime = 0.05f;

    [Header("Target / Picking")]
    public Transform stickyTargetTransform;   // Optional: set to a moving object to orbit it
    public LayerMask pickLayers = ~0;         // What layers to hit when picking target
    public KeyCode focusKey = KeyCode.F;
    public bool lockCursorWhileDragging = false;

    [Header("Gizmos")]
    public bool drawTargetGizmo = true;
    public Color targetGizmoColor = new Color(1f, 0.6f, 0.1f, 0.9f);
    public float targetGizmoSize = 0.1f;

    // --- Internal state ---
    private Vector3 targetPosition;          // Pivot point for orbit
    private float desiredYaw;
    private float desiredPitch;
    private float desiredDistance;

    private float yaw;                       // Smoothed yaw
    private float pitch;                     // Smoothed pitch
    private float yawVel;                    // For SmoothDampAngle
    private float pitchVel;

    private Vector3 camVel;                  // For SmoothDamp position

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;

        // Initialize target at a point in front of the camera if none provided
        if (stickyTargetTransform != null)
            targetPosition = stickyTargetTransform.position;
        else
            targetPosition = transform.position + transform.forward * 5f;

        // Initialize orbit state from current transform
        Vector3 toCam = transform.position - targetPosition;
        desiredDistance = Mathf.Clamp(toCam.magnitude, minDistance, Mathf.Max(minDistance + 0.01f, toCam.magnitude));
        Vector3 eul = transform.rotation.eulerAngles;
        // Convert Unity's 0..360 to signed (-180..180) for pitch handling
        desiredYaw = eul.y;
        desiredPitch = NormalizePitch(eul.x);
        yaw = desiredYaw;
        pitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
    }

    void Update()
    {
        // Keep target synced if an explicit transform is assigned
        if (stickyTargetTransform != null)
            targetPosition = stickyTargetTransform.position;

        HandleMouseOrbitPanZoom();
        HandleKeyboardMove();
        HandleTargetPickingAndFocus();

        // Compute desired camera transform from (target, yaw, pitch, distance)
        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
        Quaternion targetRot = Quaternion.Euler(desiredPitch, desiredYaw, 0f);

        // Smooth rotation using damped yaw/pitch angles
        yaw = Mathf.SmoothDampAngle(yaw, desiredYaw, ref yawVel, rotationSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, desiredPitch, ref pitchVel, rotationSmoothTime);
        Quaternion smoothRot = Quaternion.Euler(pitch, yaw, 0f);

        // Camera desired position from smoothed rotation around target
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        Vector3 desiredPos = targetPosition - (smoothRot * Vector3.forward) * desiredDistance;

        // Smooth position
        Vector3 smoothPos = Vector3.SmoothDamp(transform.position, desiredPos, ref camVel, positionSmoothTime);

        // Apply
        transform.SetPositionAndRotation(smoothPos, smoothRot);
    }

    private void HandleMouseOrbitPanZoom()
    {
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        bool lmb = Input.GetMouseButton(0);
        bool rmb = Input.GetMouseButton(1);
        bool mmb = Input.GetMouseButton(2);

        // Optionally lock cursor while dragging for nicer feel
        if (lockCursorWhileDragging && (lmb || rmb || mmb))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (lockCursorWhileDragging && !lmb && !rmb && !mmb)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ORBIT (LMB drag)
        if (lmb)
        {
            float ySign = invertOrbitY ? 1f : -1f;
            desiredYaw += mx * orbitSensitivity * Time.deltaTime;
            desiredPitch += my * orbitSensitivity * ySign * Time.deltaTime;
        }

        // PAN (RMB drag) — move target and camera together in camera plane
        if (rmb)
        {
            float ySign = invertPanY ? 1f : -1f;

            // Scale pan speed by distance so it feels consistent at any zoom
            float distanceScale = Mathf.Max(0.1f, desiredDistance);
            Vector3 right = transform.right;
            Vector3 up = transform.up;

            Vector3 pan = (-mx * right + ySign * -my * up) * panSpeed * distanceScale * Time.deltaTime * 10f;
            targetPosition += pan; // camera position follows via SmoothDamp from target
        }

        // ZOOM (Mouse wheel) — change orbit distance
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            // Scale zoom by distance for consistent feel
            float zoomFactor = 1f + desiredDistance * 0.1f;
            desiredDistance -= scroll * zoomSpeed * zoomFactor;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        }

        // DOLLY (MMB drag up/down)
        if (mmb)
        {
            desiredDistance -= my * dollyDragSpeed * Time.deltaTime * Mathf.Max(1f, desiredDistance);
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        }
    }

    private void HandleKeyboardMove()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        bool fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = moveSpeed * (fast ? fastSpeedMultiplier : 1f);

        Vector3 move = Vector3.zero;

        // Forward that doesn't include vertical component unless flyMode
        Vector3 fwd = transform.forward;
        if (!flyMode) fwd.y = 0f;
        fwd.Normalize();

        Vector3 right = transform.right;
        if (!flyMode) right.y = 0f;
        right.Normalize();

        move += fwd * v;
        move += right * h;

        if (flyMode)
        {
            if (Input.GetKey(upKey)) move += Vector3.up;
            if (Input.GetKey(downKey)) move += Vector3.down;
        }

        if (move.sqrMagnitude > 0f)
        {
            move = move.normalized * speed * Time.deltaTime;
            // Move both camera rig and target together so orbit stays coherent
            targetPosition += move;
        }
    }

    private void HandleTargetPickingAndFocus()
    {
        // Alt + LMB CLICK: set orbit pivot to point under cursor (no framing)
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            if (Input.GetMouseButtonDown(0) && RayFromMouse(out Ray ray))
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 10000f, pickLayers, QueryTriggerInteraction.Ignore))
                {
                    stickyTargetTransform = null; // decouple from any previous sticky target
                    targetPosition = hit.point;
                    // Keep distance, keep orientation
                }
            }
        }

        // F: Focus — set target to object under cursor and frame it into view
        if (Input.GetKeyDown(focusKey) && RayFromMouse(out Ray fray))
        {
            if (Physics.Raycast(fray, out RaycastHit hit, 10000f, pickLayers, QueryTriggerInteraction.Ignore))
            {
                stickyTargetTransform = hit.collider.transform;

                // Try to frame the renderer bounds; fallback to hit.point
                Renderer rend = stickyTargetTransform.GetComponentInParent<Renderer>();
                Vector3 focusPoint = hit.point;
                float requiredDistance = desiredDistance;

                if (rend != null)
                {
                    Bounds b = rend.bounds;
                    focusPoint = b.center;

                    // Approx distance to fit bounds based on FOV (assumes perspective camera)
                    float radius = b.extents.magnitude; // conservative
                    float fovRad = cam != null ? cam.fieldOfView * Mathf.Deg2Rad : 60f * Mathf.Deg2Rad;
                    float fitDist = radius / Mathf.Max(0.01f, Mathf.Tan(0.5f * fovRad));
                    requiredDistance = Mathf.Clamp(fitDist * 1.2f, minDistance, maxDistance);
                }

                targetPosition = focusPoint;
                desiredDistance = requiredDistance;
            }
        }
    }

    private bool RayFromMouse(out Ray ray)
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) { ray = new Ray(); return false; }
        ray = cam.ScreenPointToRay(Input.mousePosition);
        return true;
    }

    private static float NormalizePitch(float xAngle)
    {
        // Convert [0..360) to [-180..180) then clamp later
        float a = xAngle;
        if (a > 180f) a -= 360f;
        return a;
    }

    private void OnDrawGizmos()
    {
        if (!drawTargetGizmo) return;
        Gizmos.color = targetGizmoColor;
        Gizmos.DrawSphere(targetPosition, targetGizmoSize);
        Gizmos.color = new Color(targetGizmoColor.r, targetGizmoColor.g, targetGizmoColor.b, 0.4f);
        Gizmos.DrawLine(targetPosition, transform.position);
    }

    public void SyncToCurrentTransformPose(bool recenterPivotInFront = true, float fallbackDistance = 5f)
    {
        // 1) Extract yaw/pitch from the current camera rotation
        Vector3 eul = transform.rotation.eulerAngles;
        float newYaw = eul.y;

        float newPitch = eul.x;
        if (newPitch > 180f) newPitch -= 360f; // normalize to [-180, 180)
        newPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);

        // 2) Decide the pivot and distance
        if (stickyTargetTransform != null)
        {
            // If orbiting a sticky target, keep it as pivot
            targetPosition = stickyTargetTransform.position;
            desiredDistance = Mathf.Clamp(
                Vector3.Distance(transform.position, targetPosition),
                minDistance, maxDistance
            );
        }
        else if (recenterPivotInFront)
        {
            // Recenter pivot directly in front of the camera using the controller's desiredDistance (or a fallback)
            float dist = desiredDistance > 0f ? desiredDistance : fallbackDistance;
            dist = Mathf.Clamp(dist, minDistance, maxDistance);

            targetPosition = transform.position + transform.forward * dist;
            desiredDistance = dist;
        }
        else
        {
            // If you previously had a pivot, recompute the distance from it; otherwise fall back to front recenter
            if (targetPosition == default)
            {
                float dist = desiredDistance > 0f ? desiredDistance : fallbackDistance;
                dist = Mathf.Clamp(dist, minDistance, maxDistance);
                targetPosition = transform.position + transform.forward * dist;
                desiredDistance = dist;
            }
            else
            {
                desiredDistance = Mathf.Clamp(
                    Vector3.Distance(transform.position, targetPosition),
                    minDistance, maxDistance
                );
            }
        }

        // 3) Apply angles to both desired and smoothed states; reset damping velocities
        desiredYaw = newYaw;
        desiredPitch = newPitch;

        yaw = desiredYaw;
        pitch = desiredPitch;

        yawVel = 0f;
        pitchVel = 0f;
        camVel = Vector3.zero;
    }

    /// <summary>
    /// Hard snap the camera to a pose and rebuild orbit pivot in front using current desiredDistance.
    /// Useful if you want an API to externally set pose and keep controller coherent instantly.
    /// </summary>
    public void SnapToPose(Vector3 worldPos, Quaternion worldRot, float fallbackDistance = 5f)
    {
        transform.SetPositionAndRotation(worldPos, worldRot);
        SyncToCurrentTransformPose(recenterPivotInFront: true, fallbackDistance: fallbackDistance);
    }

    /// <summary>
    /// Recenter the pivot directly in front of the camera using current desiredDistance (or fallback).
    /// </summary>
    public void RecenterPivotAtLookDistance(float fallbackDistance = 5f)
    {
        float dist = desiredDistance > 0f ? desiredDistance : fallbackDistance;
        dist = Mathf.Clamp(dist, minDistance, maxDistance);

        targetPosition = transform.position + transform.forward * dist;
        desiredDistance = dist;
    }

    /// <summary>
    /// If you already have a pivot, recompute desiredDistance from the current transform.
    /// </summary>
    public void SyncDistanceFromCurrentPivot()
    {
        desiredDistance = Mathf.Clamp(
            Vector3.Distance(transform.position, targetPosition),
            minDistance, maxDistance
        );
    }


}
