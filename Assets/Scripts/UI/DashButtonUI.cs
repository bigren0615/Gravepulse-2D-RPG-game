using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Input Hint")]
    [Tooltip("Image placed below (or inside) the button that shows the bound key/button. Leave null to disable hint.")]
    public Image inputHintImage;
    [Tooltip("PC/KB+Mouse hint — assign Assets/Sprites/Ui/pc/mouse_right_outline.png")]
    public Sprite kbmSprite;
    [Tooltip("Size of the KB/Mouse hint image (mouse icon is usually taller than square controller buttons).")]
    public Vector2 kbmSize = new Vector2(30f, 48f);
    [Tooltip("PlayStation hint — assign Assets/Sprites/Ui/ps/playstation_button_cross.png")]
    public Sprite psSprite;
    [Tooltip("Size of the PlayStation hint image.")]
    public Vector2 psSize = new Vector2(36f, 36f);
    [Tooltip("Xbox hint — assign Assets/Sprites/Ui/xbox/xbox_button_x.png")]
    public Sprite xboxSprite;
    [Tooltip("Size of the Xbox hint image.")]
    public Vector2 xboxSize = new Vector2(36f, 36f);

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

    private enum InputScheme { KeyboardMouse, PlayStation, Xbox }
    private InputScheme _currentScheme = InputScheme.KeyboardMouse;
    private bool _isMobile;

    // ───────────────────────────────────────────────────────────────
    private void Awake()
    {
        _isMobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;

        // Hide hint entirely on mobile — on-screen buttons replace keyboard/controller hints.
        if (inputHintImage != null)
            inputHintImage.gameObject.SetActive(!_isMobile);
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

        // ── Input hint icon ───────────────────────────────────────
        if (!_isMobile && inputHintImage != null)
            UpdateInputHint();
    }

    // ── Input scheme detection ────────────────────────────────────
    private void UpdateInputHint()
    {
        DetectInputScheme();

        Sprite target;
        Vector2 targetSize;
        switch (_currentScheme)
        {
            case InputScheme.PlayStation: target = psSprite;   targetSize = psSize;   break;
            case InputScheme.Xbox:        target = xboxSprite; targetSize = xboxSize; break;
            default:                      target = kbmSprite;  targetSize = kbmSize;  break;
        }

        if (inputHintImage.sprite != target)
        {
            inputHintImage.sprite = target;
            inputHintImage.rectTransform.sizeDelta = targetSize;
        }

        // Keep image visible only when a sprite is assigned
        bool show = target != null;
        if (inputHintImage.gameObject.activeSelf != show)
            inputHintImage.gameObject.SetActive(show);
    }

    private void DetectInputScheme()
    {
        // ── Gamepad: only switch when there is REAL intentional input ──
        // wasUpdatedThisFrame fires every frame on a connected controller (driver polling),
        // so we must check for actual button presses or meaningful stick movement instead.
        Gamepad gp = Gamepad.current;
        if (gp != null && HasGamepadActivity(gp))
        {
            _currentScheme = IsPlayStation(gp) ? InputScheme.PlayStation : InputScheme.Xbox;
            return;
        }

        // ── KB/Mouse: switch on key press, mouse click, or mouse movement ──
        // Mouse.wasUpdatedThisFrame also fires continuously, so we check delta/buttons.
        bool kbPressed    = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mouseMoved   = Mouse.current    != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1f;
        bool mouseClicked = Mouse.current    != null &&
                            (Mouse.current.leftButton.wasPressedThisFrame ||
                             Mouse.current.rightButton.wasPressedThisFrame);
        if (kbPressed || mouseMoved || mouseClicked)
            _currentScheme = InputScheme.KeyboardMouse;
    }

    /// <summary>
    /// Returns true only when the gamepad has intentional input this frame —
    /// a button press OR a stick/trigger past its deadzone.
    /// This prevents the connected-but-idle controller from triggering scheme switches.
    /// </summary>
    private static bool HasGamepadActivity(Gamepad gp)
    {
        const float stickDeadzone    = 0.2f;
        const float triggerDeadzone  = 0.1f;

        return gp.buttonSouth.wasPressedThisFrame  ||
               gp.buttonNorth.wasPressedThisFrame  ||
               gp.buttonEast.wasPressedThisFrame   ||
               gp.buttonWest.wasPressedThisFrame   ||
               gp.startButton.wasPressedThisFrame  ||
               gp.selectButton.wasPressedThisFrame ||
               gp.leftShoulder.wasPressedThisFrame ||
               gp.rightShoulder.wasPressedThisFrame||
               gp.dpad.up.wasPressedThisFrame      ||
               gp.dpad.down.wasPressedThisFrame    ||
               gp.dpad.left.wasPressedThisFrame    ||
               gp.dpad.right.wasPressedThisFrame   ||
               gp.leftTrigger.ReadValue()  > triggerDeadzone  ||
               gp.rightTrigger.ReadValue() > triggerDeadzone  ||
               gp.leftStick.ReadValue().sqrMagnitude  > stickDeadzone * stickDeadzone ||
               gp.rightStick.ReadValue().sqrMagnitude > stickDeadzone * stickDeadzone;
    }

    /// <summary>
    /// Returns true when the gamepad appears to be a PlayStation controller.
    /// Relies on the device description string supplied by the OS/driver.
    /// </summary>
    private static bool IsPlayStation(Gamepad gp)
    {
        string combined = ((gp.description.product ?? "") + " " + gp.name).ToLowerInvariant();
        return combined.Contains("dualshock") ||
               combined.Contains("dualsense") ||
               combined.Contains("playstation") ||
               combined.Contains("ps4") ||
               combined.Contains("ps5") ||
               combined.Contains("sony");
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
