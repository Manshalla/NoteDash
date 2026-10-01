using UnityEngine;

/// <summary>
/// Makes a hint (e.g. "Move with Mouse") pulse between two sizes to catch the player's eye,
/// and fades it out as soon as the player zooms or drags the camera for the first time.
/// Put it on the hint text (works for UI texts, world texts and sprites).
/// </summary>
public class PulseHint : MonoBehaviour
{
    [Header("Pulse")]
    [Tooltip("Size at the smallest point of the pulse (1 = original size)")]
    public float minScale = 0.9f;
    [Tooltip("Size at the biggest point of the pulse (1 = original size)")]
    public float maxScale = 1.1f;
    [Tooltip("Seconds for one full grow-and-shrink cycle")]
    public float period = 1.4f;

    [Header("Hide")]
    [Tooltip("Hide the hint when the player zooms or drags the camera")]
    public bool hideOnCameraMove = true;
    [Tooltip("Seconds the fade-out takes")]
    public float fadeOutTime = 0.4f;

    Vector3 baseScale;
    CanvasGroup canvasGroup;     // UI
    SpriteRenderer sprite;       // world sprite
    TMPro.TMP_Text text;         // world or UI text
    float fadeStart = -1f;
    float startAlpha = 1f;

    void Awake()
    {
        baseScale = transform.localScale;
        if (transform is RectTransform)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        sprite = GetComponent<SpriteRenderer>();
        text = GetComponent<TMPro.TMP_Text>();
    }

    void OnEnable() { if (hideOnCameraMove) CameraController.UserMovedCamera += Hide; }
    void OnDisable() { CameraController.UserMovedCamera -= Hide; }

    /// <summary>Starts fading the hint out (also callable from elsewhere).</summary>
    public void Hide()
    {
        if (fadeStart >= 0f) return;
        fadeStart = Time.unscaledTime;
        startAlpha = GetAlpha();
        CameraController.UserMovedCamera -= Hide;
    }

    void Update()
    {
        // smooth sine pulse between minScale and maxScale
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.01f, period));
        transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, wave);

        if (fadeStart < 0f) return;
        float k = fadeOutTime <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - fadeStart) / fadeOutTime);
        SetAlpha(startAlpha * (1f - k));
        if (k >= 1f) gameObject.SetActive(false);
    }

    float GetAlpha()
    {
        if (canvasGroup != null) return canvasGroup.alpha;
        if (sprite != null) return sprite.color.a;
        if (text != null) return text.alpha;
        return 1f;
    }

    void SetAlpha(float a)
    {
        if (canvasGroup != null) { canvasGroup.alpha = a; return; }
        if (sprite != null) { Color c = sprite.color; c.a = a; sprite.color = c; return; }
        if (text != null) text.alpha = a;
    }
}
