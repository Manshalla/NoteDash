using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Mouse controls for the orthographic camera:
///  - Mouse wheel: smooth zoom (towards the cursor, optional)
///  - Left mouse button + drag: move the camera ("grab the sheet")
/// The camera is kept inside adjustable bounds (shown as a yellow box in the Scene view when selected).
///
/// Put this on the Main Camera (next to Entity). While Entity is animating the camera
/// (e.g. the Escape zoom-out in InputHandler) this script stays out of the way; afterwards
/// the mouse works again (also while paused). If an animation ends outside the bounds,
/// the camera is not snapped back, it just can't be moved further out.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Zoom (mouse wheel)")]
    [Tooltip("How much one wheel notch zooms (0.1 = 10 % per notch)")]
    [Range(0.01f, 0.5f)] public float zoomStep = 0.12f;
    [Tooltip("Time in seconds to reach the target zoom. 0 = instant")]
    [Range(0f, 1f)] public float zoomSmoothTime = 0.15f;
    public float minOrthographicSize = 2f;
    public float maxOrthographicSize = 15f;
    [Tooltip("Keep the point under the mouse cursor fixed while zooming")]
    public bool zoomTowardsCursor = true;

    [Header("Drag (left mouse button)")]
    public bool enableDrag = true;
    [Tooltip("Time in seconds the camera takes to follow the drag. 0 = sticks exactly to the mouse")]
    [Range(0f, 0.5f)] public float dragSmoothTime = 0f;

    [Header("Bounds (world units) - the visible area stays inside this box")]
    public bool useBounds = true;
    public Vector2 boundsMin = new Vector2(-10f, -15f);
    public Vector2 boundsMax = new Vector2(10f, 15f);
    [Tooltip("Don't allow zooming out further than the bounds are big")]
    public bool limitZoomToBounds = true;

    [Header("When")]
    [Tooltip("Ignore the mouse while it is over UI (buttons, panels)")]
    public bool ignoreWhenOverUI = true;

    Camera cam;
    Entity entity;

    float targetSize;
    float zoomVelocity;

    bool dragging;
    Vector3 targetPosition;
    Vector3 dragVelocity;
    Vector2 lastMousePosition;

    // "soft" limits: if an animation left the camera outside the bounds (e.g. the Escape view),
    // it isn't snapped back - it just can't move further out, and the limits tighten as it moves in.
    Vector2 softMin, softMax;
    float softMaxSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        entity = GetComponent<Entity>();
        targetSize = cam.orthographicSize;
        targetPosition = transform.position;
        ResetSoftLimits();
    }

    // LateUpdate so Entity animations (which run in Update) are already applied this frame
    void LateUpdate()
    {
        Mouse mouse = Mouse.current;

        // Entity is animating the camera (e.g. Escape menu) -> don't interfere, just follow its values
        bool entityBusy = entity != null && (entity.IsMoving || entity.IsZooming);
        bool inputAllowed = mouse != null && !entityBusy;   // works while playing and while paused

        if (!inputAllowed)
        {
            SyncToCamera();
            return;
        }

        Vector2 mousePos = mouse.position.ReadValue();
        bool overUI = ignoreWhenOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        HandleZoom(mouse, mousePos, overUI);
        HandleDrag(mouse, mousePos, overUI);

        if (useBounds) ClampToBounds();
        lastMousePosition = mousePos;
    }

    // ------------------------------------------------------------------ zoom

    void HandleZoom(Mouse mouse, Vector2 mousePos, bool overUI)
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (!overUI && Mathf.Abs(scroll) > 0.01f)
        {
            // Some platforms report 120 per notch, others 1 per notch
            float notches = Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
            targetSize *= Mathf.Pow(1f - zoomStep, notches);
        }
        softMaxSize = Mathf.Max(MaxSize(), Mathf.Min(softMaxSize, cam.orthographicSize));
        targetSize = Mathf.Clamp(targetSize, minOrthographicSize, softMaxSize);

        float oldSize = cam.orthographicSize;
        float newSize = zoomSmoothTime <= 0f
            ? targetSize
            : Mathf.SmoothDamp(oldSize, targetSize, ref zoomVelocity, zoomSmoothTime);
        if (Mathf.Abs(newSize - targetSize) < 0.0005f) newSize = targetSize;
        if (Mathf.Approximately(newSize, oldSize)) return;

        if (zoomTowardsCursor && !overUI && !dragging)
        {
            Vector3 before = cam.ScreenToWorldPoint(mousePos);
            cam.orthographicSize = newSize;
            Vector3 after = cam.ScreenToWorldPoint(mousePos);
            Vector3 shift = before - after;
            shift.z = 0f;
            transform.position += shift;
            targetPosition += shift;
        }
        else
        {
            cam.orthographicSize = newSize;
        }
    }

    float MaxSize()
    {
        float max = maxOrthographicSize;
        if (useBounds && limitZoomToBounds)
        {
            float byHeight = (boundsMax.y - boundsMin.y) * 0.5f;
            float byWidth = (boundsMax.x - boundsMin.x) * 0.5f / Mathf.Max(cam.aspect, 0.01f);
            max = Mathf.Min(max, byHeight, byWidth);
        }
        return Mathf.Max(max, minOrthographicSize);
    }

    // ------------------------------------------------------------------ drag

    void HandleDrag(Mouse mouse, Vector2 mousePos, bool overUI)
    {
        if (!enableDrag) { dragging = false; return; }

        if (mouse.leftButton.wasPressedThisFrame && !overUI)
        {
            dragging = true;
            targetPosition = transform.position;
            lastMousePosition = mousePos;
        }
        if (!mouse.leftButton.isPressed) dragging = false;

        if (dragging)
        {
            // world distance the mouse moved -> move the camera the opposite way
            Vector3 from = cam.ScreenToWorldPoint(lastMousePosition);
            Vector3 to = cam.ScreenToWorldPoint(mousePos);
            Vector3 delta = from - to;
            delta.z = 0f;
            targetPosition += delta;
        }

        if (dragSmoothTime <= 0f)
        {
            if (dragging) transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref dragVelocity, dragSmoothTime);
        }
    }

    // ------------------------------------------------------------------ bounds

    void ClampToBounds()
    {
        transform.position = Clamp(transform.position);
        targetPosition = Clamp(targetPosition);
        ClampToBoundsSoft();
    }

    void ClampToBoundsSoft()
    {
        RealLimits(out Vector2 lo, out Vector2 hi);
        Vector3 p = transform.position;

        // tighten the soft limits as the camera moves back inside
        softMin.x = Mathf.Min(lo.x, Mathf.Max(softMin.x, p.x));
        softMin.y = Mathf.Min(lo.y, Mathf.Max(softMin.y, p.y));
        softMax.x = Mathf.Max(hi.x, Mathf.Min(softMax.x, p.x));
        softMax.y = Mathf.Max(hi.y, Mathf.Min(softMax.y, p.y));
    }

    /// <summary>Allowed camera-centre range for the current zoom.</summary>
    void RealLimits(out Vector2 lo, out Vector2 hi)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        lo = new Vector2(boundsMin.x + halfW, boundsMin.y + halfH);
        hi = new Vector2(boundsMax.x - halfW, boundsMax.y - halfH);
        if (lo.x > hi.x) lo.x = hi.x = (boundsMin.x + boundsMax.x) * 0.5f;
        if (lo.y > hi.y) lo.y = hi.y = (boundsMin.y + boundsMax.y) * 0.5f;
    }

    Vector3 Clamp(Vector3 p)
    {
        RealLimits(out Vector2 lo, out Vector2 hi);
        p.x = Mathf.Clamp(p.x, Mathf.Min(lo.x, softMin.x), Mathf.Max(hi.x, softMax.x));
        p.y = Mathf.Clamp(p.y, Mathf.Min(lo.y, softMin.y), Mathf.Max(hi.y, softMax.y));
        return p;
    }

    void ResetSoftLimits()
    {
        if (cam == null) cam = GetComponent<Camera>();
        Vector3 p = transform.position;
        softMin = softMax = new Vector2(p.x, p.y);
        softMaxSize = cam.orthographicSize;
    }

    /// <summary>Takes over the camera's current position/size as the new targets (after external animations).</summary>
    public void SyncToCamera()
    {
        if (cam == null) cam = GetComponent<Camera>();
        targetSize = cam.orthographicSize;
        targetPosition = transform.position;
        zoomVelocity = 0f;
        dragVelocity = Vector3.zero;
        dragging = false;
        ResetSoftLimits();
    }

    void OnValidate()
    {
        if (boundsMax.x < boundsMin.x) boundsMax.x = boundsMin.x;
        if (boundsMax.y < boundsMin.y) boundsMax.y = boundsMin.y;
        if (maxOrthographicSize < minOrthographicSize) maxOrthographicSize = minOrthographicSize;
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((boundsMin.x + boundsMax.x) * 0.5f, (boundsMin.y + boundsMax.y) * 0.5f, 0f);
        Vector3 size = new Vector3(boundsMax.x - boundsMin.x, boundsMax.y - boundsMin.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
