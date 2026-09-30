using UnityEngine;

public class Entity : MonoBehaviour
{
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ---- position animation
    private bool move = false;
    private Vector3 startPos, desiredPos;
    private float t;
    private float tmax = 1;

    // ---- orthographic size animation (only when this Entity is on a Camera)
    private Camera cam;
    private bool zoom = false;
    private float startSize, desiredSize;
    private float zoomT;
    private float zoomTmax = 1;

    /// <summary>True while a position animation is running.</summary>
    public bool IsMoving => move;

    /// <summary>True while an orthographic-size animation is running.</summary>
    public bool IsZooming => zoom;

    public void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void Update()
    {
        if (move)
        {
            t += Time.deltaTime;
            float k = movementCurve.Evaluate(Mathf.Clamp01(t / tmax));
            transform.position = Vector3.LerpUnclamped(startPos, desiredPos, k);
            if (t >= tmax)
            {
                transform.position = desiredPos;
                move = false;
                t = 0;
            }
        }

        if (zoom)
        {
            zoomT += Time.deltaTime;
            float k = movementCurve.Evaluate(Mathf.Clamp01(zoomT / zoomTmax));
            cam.orthographicSize = Mathf.LerpUnclamped(startSize, desiredSize, k);
            if (zoomT >= zoomTmax)
            {
                cam.orthographicSize = desiredSize;
                zoom = false;
                zoomT = 0;
            }
        }
    }

    /// <summary>
    /// Moves the object to 'to' over 'time' seconds.
    /// Does nothing (returns false) while a previous move is still running.
    /// </summary>
    public bool Animate(Vector3 to, float time)
    {
        if (move) return false;

        if (time < 0.1f)
        {
            transform.position = to;
            return true;
        }

        startPos = transform.position;
        desiredPos = to;
        tmax = time;
        t = 0;
        move = true;
        return true;
    }

    /// <summary>
    /// Animates the camera's orthographic size to 'size' over 'time' seconds.
    /// Only works if this Entity is on an orthographic Camera.
    /// Does nothing (returns false) while a previous zoom is still running.
    /// </summary>
    public bool AnimateOrthographicSize(float size, float time)
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning($"{name}: AnimateOrthographicSize needs an orthographic Camera on the same GameObject.", this);
            return false;
        }
        if (zoom) return false;

        if (time < 0.1f)
        {
            cam.orthographicSize = size;
            return true;
        }

        startSize = cam.orthographicSize;
        desiredSize = size;
        zoomTmax = time;
        zoomT = 0;
        zoom = true;
        return true;
    }
}
