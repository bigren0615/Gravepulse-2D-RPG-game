using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Parry button UI — no cooldown overlay, but fires a yellow ring ripple on a
/// successful parry. Extends ActionButtonUI for shared input-hint and tint logic.
///
/// CHILD ORDER (top = drawn first = behind):
///
///   ParryButton  (this script + OnScreenButton)
///   ├─ ParryRing    ← FIRST child: yellow filled circle, starts at button size.
///   │                 Background covers its center so only the expanded
///   │                 border beyond the button boundary is ever visible → outline ring.
///   ├─ Background   ← solid circle
///   └─ Icon         ← parry/shield sprite on top
///
/// Optional hint children (same as DashButtonUI):
///   ParryButton
///   ├─ InputHintBg  (assign → inputHintBgImage)
///   └─ InputHint    (assign → inputHintImage)
/// </summary>
public class ParryButtonUI : ActionButtonUI
{
    [Header("References")]
    [Tooltip("PlayerController on the player GameObject.")]
    public PlayerController playerController;

    [Header("Parry Ring")]
    [Tooltip("Ring image that ripples outward on a successful parry. Assign the first child of this button.")]
    public Image parryRingImage;
    [Tooltip("Colour of the ring (default: yellow).")]
    public Color ringColor = new Color(1f, 0.9f, 0.1f, 1f);
    [Tooltip("How much the ring expands relative to button size (1.4 = 40% larger).")]
    public float ringExpandMultiplier = 1.4f;
    [Tooltip("Duration (seconds) of the expanding ring animation.")]
    public float pulseDuration = 0.4f;

    // ── internal state ─────────────────────────────────────────────
    private Vector2 _ringStartSize;
    private Coroutine _pulseCoroutine;
    private float _lastKnownParryTime = -1f;

    // ───────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake(); // mobile detection + silhouette generation

        _ringStartSize = GetComponent<RectTransform>().sizeDelta;

        if (parryRingImage != null)
        {
            parryRingImage.rectTransform.sizeDelta = _ringStartSize;
            // Apply ring RGB, start fully transparent
            Color c = ringColor;
            c.a = 0f;
            parryRingImage.color = c;
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        // Always show at full ready tint (no cooldown to track)
        ApplyProgressTint(1f);

        // Detect parry success by timestamp change
        float t = playerController.LastParrySuccessTime;
        if (t != _lastKnownParryTime)
        {
            _lastKnownParryTime = t;
            TriggerParryRing();
        }

        UpdateInputHintIfNeeded();
    }

    private void TriggerParryRing()
    {
        if (parryRingImage == null) return;

        // Ensure the ring uses the configured colour before each ripple
        Color c = ringColor;
        c.a = 0f;
        parryRingImage.color = c;

        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(RingRipple());
    }

    private IEnumerator RingRipple()
    {
        yield return RingRippleRoutine(parryRingImage, _ringStartSize, ringExpandMultiplier, pulseDuration);
        _pulseCoroutine = null;
    }
}
