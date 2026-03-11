using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Abstract base for action button UI elements.
/// Handles input-scheme detection, per-scheme hint icons, filled-silhouette
/// background generation, background/icon tint interpolation, and the
/// shared expanding ring-ripple animation.
///
/// Subclasses (DashButtonUI, AttackButtonUI, ParryButtonUI, …) call:
///   • ApplyProgressTint(progress)                          — lerps bg + icon tints
///   • UpdateInputHintIfNeeded()                            — refreshes the hint icon
///   • RingRippleRoutine(image, startSize, mult, duration)  — expanding ring coroutine
///   • SetImageAlpha(image, alpha)                          — alpha helper
/// </summary>
public abstract class ActionButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Solid background circle image.")]
    public Image backgroundImage;
    [Tooltip("Icon image displayed on top of the background.")]
    public Image iconImage;

    [Header("Input Hint")]
    [Tooltip("Image that shows the bound key/button. Leave null to disable.")]
    public Image inputHintImage;
    [Tooltip("Optional dark background plate behind the hint icon.")]
    public Image inputHintBgImage;
    [Tooltip("Extra pixels added on each side of the icon to size the background plate.")]
    public Vector2 hintBgPadding = new Vector2(8f, 8f);

    [Tooltip("PC/KB+Mouse hint sprite.")]
    public Sprite kbmSprite;
    [Tooltip("Size of the KB/Mouse hint image.")]
    public Vector2 kbmSize = new Vector2(30f, 48f);
    [Tooltip("PlayStation hint sprite.")]
    public Sprite psSprite;
    [Tooltip("Size of the PlayStation hint image.")]
    public Vector2 psSize = new Vector2(36f, 36f);
    [Tooltip("Xbox hint sprite.")]
    public Sprite xboxSprite;
    [Tooltip("Size of the Xbox hint image.")]
    public Vector2 xboxSize = new Vector2(36f, 36f);
    [Tooltip("Fill colour of the auto-generated silhouette background.\nRequires Read/Write enabled on each hint sprite's texture import settings.")]
    public Color hintBgColor = new Color(0f, 0f, 0f, 0.75f);

    [Header("Colours")]
    [Tooltip("Background tint when the action is ready.")]
    public Color bgReadyColor    = new Color(0.78f, 0.78f, 0.78f, 1f);
    [Tooltip("Background tint while the action is on cooldown.")]
    public Color bgCooldownColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("Icon tint when the action is ready.")]
    public Color iconReadyColor    = Color.white;
    [Tooltip("Icon tint while the action is on cooldown.")]
    public Color iconCooldownColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    // ── internal state ────────────────────────────────────────────────
    protected enum InputScheme { KeyboardMouse, PlayStation, Xbox }
    protected InputScheme _currentScheme = InputScheme.KeyboardMouse;
    protected bool _isMobile;

    private Sprite _kbmSilhouette;
    private Sprite _psSilhouette;
    private Sprite _xboxSilhouette;

    // ─────────────────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        _isMobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;

        // Hide hint entirely on mobile — on-screen buttons replace keyboard/controller hints.
        if (inputHintImage != null)
            inputHintImage.gameObject.SetActive(!_isMobile);
        if (inputHintBgImage != null)
            inputHintBgImage.gameObject.SetActive(!_isMobile);

        // Pre-generate filled silhouette sprites for the background plate.
        if (!_isMobile && inputHintBgImage != null)
        {
            _kbmSilhouette  = BuildFilledSilhouette(kbmSprite,  hintBgColor);
            _psSilhouette   = BuildFilledSilhouette(psSprite,   hintBgColor);
            _xboxSilhouette = BuildFilledSilhouette(xboxSprite, hintBgColor);
        }
    }

    /// <summary>
    /// Lerps background and icon tint between cooldown and ready colours.
    /// Call from subclass Update() with progress in [0,1] (0 = just used, 1 = ready).
    /// </summary>
    protected void ApplyProgressTint(float progress)
    {
        if (backgroundImage != null)
            backgroundImage.color = Color.Lerp(bgCooldownColor, bgReadyColor, progress);
        if (iconImage != null)
            iconImage.color = Color.Lerp(iconCooldownColor, iconReadyColor, progress);
    }

    /// <summary>
    /// Refreshes the input hint icon for the current input scheme.
    /// Call from subclass Update().
    /// </summary>
    protected void UpdateInputHintIfNeeded()
    {
        if (!_isMobile && inputHintImage != null)
            UpdateInputHint();
    }

    // ── Input hint ────────────────────────────────────────────────────
    private void UpdateInputHint()
    {
        DetectInputScheme();

        Sprite  target;
        Sprite  targetBg;
        Vector2 targetSize;
        switch (_currentScheme)
        {
            case InputScheme.PlayStation: target = psSprite;   targetBg = _psSilhouette;   targetSize = psSize;   break;
            case InputScheme.Xbox:        target = xboxSprite; targetBg = _xboxSilhouette; targetSize = xboxSize; break;
            default:                      target = kbmSprite;  targetBg = _kbmSilhouette;  targetSize = kbmSize;  break;
        }

        if (inputHintImage.sprite != target)
            inputHintImage.sprite = target;

        // Always sync sizes so the bg follows the icon's exact shape every frame
        inputHintImage.rectTransform.sizeDelta = targetSize;
        if (inputHintBgImage != null)
        {
            if (inputHintBgImage.sprite != targetBg)
                inputHintBgImage.sprite = targetBg;
            inputHintBgImage.rectTransform.sizeDelta = targetSize + hintBgPadding * 2f;
        }

        // Keep image (and its background plate) visible only when a sprite is assigned
        bool show = target != null;
        if (inputHintImage.gameObject.activeSelf != show)
            inputHintImage.gameObject.SetActive(show);
        if (inputHintBgImage != null && inputHintBgImage.gameObject.activeSelf != show)
            inputHintBgImage.gameObject.SetActive(show);
    }

    private void DetectInputScheme()
    {
        // ── Gamepad: only switch when there is REAL intentional input ──
        Gamepad gp = Gamepad.current;
        if (gp != null && HasGamepadActivity(gp))
        {
            _currentScheme = IsPlayStation(gp) ? InputScheme.PlayStation : InputScheme.Xbox;
            return;
        }

        // ── KB/Mouse ──
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
    /// </summary>
    private static bool HasGamepadActivity(Gamepad gp)
    {
        const float stickDeadzone   = 0.2f;
        const float triggerDeadzone = 0.1f;

        return gp.buttonSouth.wasPressedThisFrame   ||
               gp.buttonNorth.wasPressedThisFrame   ||
               gp.buttonEast.wasPressedThisFrame    ||
               gp.buttonWest.wasPressedThisFrame    ||
               gp.startButton.wasPressedThisFrame   ||
               gp.selectButton.wasPressedThisFrame  ||
               gp.leftShoulder.wasPressedThisFrame  ||
               gp.rightShoulder.wasPressedThisFrame ||
               gp.dpad.up.wasPressedThisFrame       ||
               gp.dpad.down.wasPressedThisFrame     ||
               gp.dpad.left.wasPressedThisFrame     ||
               gp.dpad.right.wasPressedThisFrame    ||
               gp.leftTrigger.ReadValue()  > triggerDeadzone  ||
               gp.rightTrigger.ReadValue() > triggerDeadzone  ||
               gp.leftStick.ReadValue().sqrMagnitude  > stickDeadzone * stickDeadzone ||
               gp.rightStick.ReadValue().sqrMagnitude > stickDeadzone * stickDeadzone;
    }

    /// <summary>
    /// Returns true when the gamepad appears to be a PlayStation controller.
    /// </summary>
    private static bool IsPlayStation(Gamepad gp)
    {
        string combined = ((gp.description.product ?? "") + " " + gp.name).ToLowerInvariant();
        return combined.Contains("dualshock")   ||
               combined.Contains("dualsense")   ||
               combined.Contains("playstation") ||
               combined.Contains("ps4")         ||
               combined.Contains("ps5")         ||
               combined.Contains("sony");
    }

    // ── Ring ripple helpers (shared by DashButtonUI, ParryButtonUI, etc.) ─────────────

    /// <summary>
    /// Expanding outline ring ripple coroutine — the ring grows outward and fades.
    /// After the animation the ring is reset to <paramref name="startSize"/> at alpha 0.
    /// </summary>
    protected IEnumerator RingRippleRoutine(Image ringImage, Vector2 startSize,
                                             float expandMultiplier, float duration)
    {
        if (ringImage == null) yield break;

        RectTransform ringRT  = ringImage.rectTransform;
        Vector2       endSize = startSize * expandMultiplier;
        float         elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            ringRT.sizeDelta = Vector2.Lerp(startSize, endSize, t);

            // Alpha: sharp rise (first 25%) then smooth fade (rest)
            float alpha = t < 0.25f
                ? Mathf.InverseLerp(0f, 0.25f, t) * 0.9f
                : Mathf.Lerp(0.9f, 0f, Mathf.InverseLerp(0.25f, 1f, t));
            SetImageAlpha(ringImage, alpha);

            yield return null;
        }

        // Reset to resting state
        ringRT.sizeDelta = startSize;
        SetImageAlpha(ringImage, 0f);
    }

    /// <summary>Sets only the alpha channel of an Image's colour.</summary>
    protected static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    /// <summary>
    /// Generates a filled solid-colour silhouette sprite from <paramref name="source"/> at runtime.
    /// BFS flood-fill from border transparent pixels marks them "external"; every remaining pixel
    /// is painted with <paramref name="fillColor"/>, turning hollow outline sprites into solid shapes.
    /// Returns null if source is null or the texture has Read/Write disabled.
    /// </summary>
    protected static Sprite BuildFilledSilhouette(Sprite source, Color fillColor)
    {
        if (source == null) return null;

        Texture2D src = source.texture;
        Rect r  = source.textureRect;
        int  x0 = (int)r.x,    y0 = (int)r.y;
        int  w  = (int)r.width, h  = (int)r.height;

        Color32[] srcPx;
        try { srcPx = src.GetPixels32(); }
        catch
        {
            Debug.LogWarning($"[ActionButtonUI] Silhouette bg skipped for '{source.name}': " +
                             "enable Read/Write in the texture's Import Settings.");
            return null;
        }

        // Copy the sprite's sub-rect into a working array
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = srcPx[(y0 + y) * src.width + (x0 + x)];

        // BFS: seed border transparent pixels; flood outwards through transparent pixels.
        const byte THRESH = 16;
        var external = new bool[w * h];
        var queue    = new Queue<int>();

        void Seed(int i)
        {
            if (!external[i] && px[i].a < THRESH) { external[i] = true; queue.Enqueue(i); }
        }

        for (int x = 0; x < w; x++) { Seed(x); Seed((h - 1) * w + x); }
        for (int y = 1; y < h - 1; y++) { Seed(y * w); Seed(y * w + w - 1); }

        while (queue.Count > 0)
        {
            int i  = queue.Dequeue();
            int ix = i % w, iy = i / w;
            if (ix > 0)     Seed(i - 1);
            if (ix < w - 1) Seed(i + 1);
            if (iy > 0)     Seed(i - w);
            if (iy < h - 1) Seed(i + w);
        }

        // Non-external → fillColor; external transparent → clear
        Color32 fill  = (Color32)fillColor;
        Color32 clear = new Color32(0, 0, 0, 0);
        var result = new Color32[w * h];
        for (int i = 0; i < px.Length; i++)
            result[i] = (!external[i] || px[i].a >= THRESH) ? fill : clear;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = src.filterMode,
            wrapMode   = TextureWrapMode.Clamp
        };
        tex.SetPixels32(result);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h),
                             new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
    }
}
