using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zenless Zone Zero-style dash button with radial cooldown overlay.
///
/// CHILD ORDER MATTERS — list top-to-bottom in the Unity Hierarchy panel
/// (top = drawn first = behind everything below it):
///
///   DashButton  (this script + OnScreenButton)
///   ├─ ReadyRing    ← FIRST child: white filled circle, starts at button size.
///   │                 Background covers its center so only the expanded
///   │                 border beyond the button is ever visible → outline ring.
///   ├─ Background   ← solid light-grey circle, covers ReadyRing center
///   ├─ CooldownMask ← dark radial-fill sweep (Filled/Radial360)
///   └─ Icon         ← up-arrow sprite on top
/// </summary>
public class DashButtonUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("PlayerController on the player GameObject.")]
    public PlayerController playerController;

    [Header("UI Elements")]
    public Image backgroundImage;
    public Image iconImage;
    [Tooltip("Radial-fill image used for the cooldown sweep overlay.")]
    public Image cooldownMaskImage;
    [Tooltip("White circle that flashes briefly when dash becomes ready.")]
    public Image readyFlashImage;

    [Header("Colours")]
    [Tooltip("Background tint when dash is ready.")]
    public Color bgReadyColor = new Color(0.78f, 0.78f, 0.78f, 1f);   // light grey
    [Tooltip("Background tint while on cooldown (slightly darker).")]
    public Color bgCooldownColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("Icon tint when ready.")]
    public Color iconReadyColor = Color.white;
    [Tooltip("Icon tint while on cooldown.")]
    public Color iconCooldownColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [Tooltip("Cooldown overlay colour (dark sweep).")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.72f);

    [Header("Ready Ring")]
    [Tooltip("How much the ring expands relative to button size (1.4 = 40% larger).")]
    public float ringExpandMultiplier = 1.0f;
    [Tooltip("Duration (seconds) of the expanding ring animation.")]
    public float pulseDuration = 0.35f;

    // ── internal state ──────────────────────────────────────────────
    private bool wasReady = true;
    private Coroutine pulseCoroutine;
    private Vector2 _flashStartSize;

    // ───────────────────────────────────────────────────────────────
    private void Awake()
    {
        // The ring must start at the exact button root size.
        // Background (a sibling drawn above it) covers the ring's center,
        // so only the part that expands BEYOND the button boundary is visible
        // — giving a pure outline effect with no extra sprites needed.
        _flashStartSize = GetComponent<RectTransform>().sizeDelta;
        if (readyFlashImage != null)
            readyFlashImage.rectTransform.sizeDelta = _flashStartSize;

        // Configure the radial fill overlay
        if (cooldownMaskImage != null)
        {
            cooldownMaskImage.type        = Image.Type.Filled;
            cooldownMaskImage.fillMethod  = Image.FillMethod.Radial360;
            cooldownMaskImage.fillOrigin  = (int)Image.Origin360.Top;
            cooldownMaskImage.fillClockwise = true;
            cooldownMaskImage.color       = overlayColor;
            cooldownMaskImage.fillAmount  = 0f;
        }

        // Ready flash starts fully transparent
        if (readyFlashImage != null)
        {
            Color c = Color.white;
            c.a = 0f;
            readyFlashImage.color = c;
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        float progress = playerController.DashCooldownProgress; // 0 = just used, 1 = ready
        bool isReady   = playerController.IsDashReady;

        // ── Cooldown overlay ──────────────────────────────────────
        if (cooldownMaskImage != null)
        {
            // fillAmount = 1 means fully covered (just used), shrinks to 0 as ready
            cooldownMaskImage.fillAmount = 1f - progress;
        }

        // ── Background & icon tint ────────────────────────────────
        if (backgroundImage != null)
            backgroundImage.color = Color.Lerp(bgCooldownColor, bgReadyColor, progress);

        if (iconImage != null)
            iconImage.color = Color.Lerp(iconCooldownColor, iconReadyColor, progress);

        // ── Ready transition: pulse + flash ───────────────────────
        if (isReady && !wasReady)
            TriggerReadyPulse();

        wasReady = isReady;
    }

    private void TriggerReadyPulse()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(RingRippleRoutine());
    }

    /// <summary>
    /// Expanding outline ring ripple — the ring image grows outward and fades,
    /// leaving the button itself completely stationary (ZZZ ready-indicator style).
    /// </summary>
    private IEnumerator RingRippleRoutine()
    {
        if (readyFlashImage == null) { pulseCoroutine = null; yield break; }

        RectTransform ringRT  = readyFlashImage.rectTransform;
        Vector2       endSize = _flashStartSize * ringExpandMultiplier;
        float         elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);

            // Ring expands steadily outward
            ringRT.sizeDelta = Vector2.Lerp(_flashStartSize, endSize, t);

            // Alpha: sharp rise (first 25%) then smooth fade (rest)
            float alpha = t < 0.25f
                ? Mathf.InverseLerp(0f, 0.25f, t) * 0.9f
                : Mathf.Lerp(0.9f, 0f, Mathf.InverseLerp(0.25f, 1f, t));
            SetFlashAlpha(alpha);

            yield return null;
        }

        // Reset ring to resting state
        ringRT.sizeDelta = _flashStartSize;
        SetFlashAlpha(0f);
        pulseCoroutine = null;
    }

    private void SetFlashAlpha(float a)
    {
        if (readyFlashImage == null) return;
        Color c = readyFlashImage.color;
        c.a = a;
        readyFlashImage.color = c;
    }
}
