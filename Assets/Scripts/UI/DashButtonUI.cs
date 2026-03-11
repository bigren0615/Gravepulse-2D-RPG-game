using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zenless Zone Zero-style dash button — radial cooldown overlay + ready-ring ripple.
/// Extends ActionButtonUI for shared input-hint and colour-tint logic.
///
/// CHILD ORDER MATTERS — list top-to-bottom in the Unity Hierarchy panel
/// (top = drawn first = behind everything below it):
///
///   DashButton  (this script + OnScreenButton)
///   ├─ ReadyRing    ← FIRST child: white filled circle, starts at button size.
///   │                 Background covers its center so only the expanded
///   │                 border beyond the button boundary is ever visible → outline ring.
///   ├─ Background   ← solid light-grey circle, covers ReadyRing center
///   ├─ CooldownMask ← dark radial-fill sweep (Filled/Radial360)
///   └─ Icon         ← up-arrow sprite on top
/// </summary>
public class DashButtonUI : ActionButtonUI
{
    [Header("References")]
    [Tooltip("PlayerController on the player GameObject.")]
    public PlayerController playerController;

    [Header("Cooldown Overlay")]
    [Tooltip("Radial-fill image used for the cooldown sweep overlay.")]
    public Image cooldownMaskImage;
    [Tooltip("White circle that flashes briefly when dash becomes ready.")]
    public Image readyFlashImage;
    [Tooltip("Cooldown overlay colour (dark sweep).")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.72f);

    [Header("Ready Ring")]
    [Tooltip("How much the ring expands relative to button size (1.0 = no expand, 1.4 = 40% larger).")]
    public float ringExpandMultiplier = 1.0f;
    [Tooltip("Duration (seconds) of the expanding ring animation.")]
    public float pulseDuration = 0.35f;

    // ── internal state ──────────────────────────────────────────────
    private bool wasReady = true;
    private Coroutine pulseCoroutine;
    private Vector2 _flashStartSize;

    // ───────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake(); // mobile detection + silhouette generation

        // The ring must start at the exact button root size.
        // Background covers its center, so only the expanded border is visible → outline effect.
        _flashStartSize = GetComponent<RectTransform>().sizeDelta;
        if (readyFlashImage != null)
            readyFlashImage.rectTransform.sizeDelta = _flashStartSize;

        // Configure the radial fill overlay
        if (cooldownMaskImage != null)
        {
            cooldownMaskImage.type          = Image.Type.Filled;
            cooldownMaskImage.fillMethod    = Image.FillMethod.Radial360;
            cooldownMaskImage.fillOrigin    = (int)Image.Origin360.Top;
            cooldownMaskImage.fillClockwise = true;
            cooldownMaskImage.color         = overlayColor;
            cooldownMaskImage.fillAmount    = 0f;
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
        bool  isReady  = playerController.IsDashReady;

        // ── Cooldown overlay ──────────────────────────────────────
        if (cooldownMaskImage != null)
            cooldownMaskImage.fillAmount = 1f - progress; // 1 = fully covered, shrinks to 0

        // ── Background & icon tint ────────────────────────────────
        ApplyProgressTint(progress);

        // ── Ready transition: ring ripple ─────────────────────────
        if (isReady && !wasReady)
            TriggerReadyPulse();

        wasReady = isReady;

        // ── Input hint icon ───────────────────────────────────────
        UpdateInputHintIfNeeded();
    }

    private void TriggerReadyPulse()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(RingRippleRoutine(
            readyFlashImage, _flashStartSize, ringExpandMultiplier, pulseDuration));
    }
}
