using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Track all enemies currently in combat (using GameObject for compatibility with new component system)
    private HashSet<GameObject> enemiesInCombat = new HashSet<GameObject>();
    private bool isBattleMusicPlaying = false;

    // ===== VITAL VIEW (ZZZ-style bullet time dodge) =====
    private bool isVitalViewActive = false;
    private Coroutine vitalViewCoroutine;

    // Canvas overlay elements (created at runtime, no prefab needed)
    private Canvas vitalViewCanvas;
    private UnityEngine.UI.Image vitalViewFlash;        // warm orange/white burst on activation
    private UnityEngine.UI.RawImage vitalViewScanLines; // subtle scan-line pattern
    private Coroutine vvOverlayCoroutine;

    // Post-processing runtime volume (ZZZ desaturation + vignette + chromatic aberration)
    private PostProcessVolume vvPPVolume;
    private ColorGrading vvColorGrading;
    private Vignette vvVignette;
    private ChromaticAberration vvCA;

    public bool IsVitalViewActive() => isVitalViewActive;

    /// <summary>
    /// Trigger ZZZ-style Vital View: slow time to slowScale for duration real seconds.
    /// Default 0.05 (5% speed) for 1.0s real time.
    /// Player movement is unaffected (PlayerController uses unscaled delta when active).
    /// </summary>
    public void TriggerVitalView(float duration = 0.5f, float slowScale = 0.05f)
    {
        if (isVitalViewActive) return; // Already active, no stacking
        if (vitalViewCoroutine != null)
            StopCoroutine(vitalViewCoroutine);
        vitalViewCoroutine = StartCoroutine(VitalViewCoroutine(duration, slowScale));
    }

    private IEnumerator VitalViewCoroutine(float duration, float slowScale)
    {
        isVitalViewActive = true;
        Time.timeScale = slowScale;
        Time.fixedDeltaTime = 0.02f * slowScale; // Keep physics step proportional

        Debug.Log($"[VitalView] ACTIVATED — timeScale={slowScale}, duration={duration}s real time");

        // Slow music pitch to match time scale (dramatic deep-pitch slow-motion audio)
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetAllMusicPitch(slowScale);

        // Show ZZZ-style screen overlay (desaturate + vignette + orange flash + scan-lines)
        EnsureVitalViewOverlay();
        if (vvOverlayCoroutine != null) StopCoroutine(vvOverlayCoroutine);
        vvOverlayCoroutine = StartCoroutine(AnimateVitalViewOverlay(duration));

        yield return new WaitForSecondsRealtime(duration);

        // Restore normal time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Restore music pitch
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetAllMusicPitch(1f);

        Debug.Log("[VitalView] ENDED — time restored to normal");

        isVitalViewActive = false;
        vitalViewCoroutine = null;
    }

    /// <summary>
    /// ZZZ-style overlay setup: Canvas (flash + scan-lines) + runtime PostProcess volume.
    /// Created lazily on first Vital View trigger; both persist and are reused.
    /// </summary>
    private void EnsureVitalViewOverlay()
    {
        // ---- Canvas elements ----
        if (vitalViewCanvas == null)
        {
            GameObject canvasGO = new GameObject("VitalViewCanvas");
            DontDestroyOnLoad(canvasGO);

            vitalViewCanvas = canvasGO.AddComponent<Canvas>();
            vitalViewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            vitalViewCanvas.sortingOrder = 999; // Always on top
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

            // --- Flash image: warm orange/white burst on activation ---
            GameObject flashGO = new GameObject("VV_Flash");
            flashGO.transform.SetParent(canvasGO.transform, false);
            vitalViewFlash = flashGO.AddComponent<UnityEngine.UI.Image>();
            vitalViewFlash.raycastTarget = false;
            vitalViewFlash.color = new Color(1f, 0.72f, 0.2f, 0f);
            RectTransform flashRT = vitalViewFlash.rectTransform;
            flashRT.anchorMin = Vector2.zero;
            flashRT.anchorMax = Vector2.one;
            flashRT.offsetMin = Vector2.zero;
            flashRT.offsetMax = Vector2.zero;

            // --- Scan-lines: subtle digital pattern that holds during bullet time ---
            GameObject scanGO = new GameObject("VV_ScanLines");
            scanGO.transform.SetParent(canvasGO.transform, false);
            vitalViewScanLines = scanGO.AddComponent<UnityEngine.UI.RawImage>();
            vitalViewScanLines.raycastTarget = false;
            vitalViewScanLines.color = new Color(0f, 0f, 0f, 0f);
            vitalViewScanLines.texture = CreateScanLineTexture();
            RectTransform scanRT = vitalViewScanLines.rectTransform;
            scanRT.anchorMin = Vector2.zero;
            scanRT.anchorMax = Vector2.one;
            scanRT.offsetMin = Vector2.zero;
            scanRT.offsetMax = Vector2.zero;
        }

        // ---- Post-processing volume ----
        if (vvPPVolume == null)
        {
            vvColorGrading = ScriptableObject.CreateInstance<ColorGrading>();
            vvColorGrading.enabled.Override(true);
            vvColorGrading.saturation.Override(-45f);     // muted colours, not full grayscale
            vvColorGrading.temperature.Override(18f);     // faint warm tone
            vvColorGrading.postExposure.Override(-0.25f); // slightly darker

            vvVignette = ScriptableObject.CreateInstance<Vignette>();
            vvVignette.enabled.Override(true);
            vvVignette.color.Override(Color.black);
            vvVignette.intensity.Override(0.25f);   // soft dark corners only
            vvVignette.smoothness.Override(0.70f);  // high smoothness = soft gradual falloff
            vvVignette.rounded.Override(true);

            vvCA = ScriptableObject.CreateInstance<ChromaticAberration>();
            vvCA.enabled.Override(true);
            vvCA.intensity.Override(0f);

            vvPPVolume = PostProcessManager.instance.QuickVolume(
                gameObject.layer, 100f, vvColorGrading, vvVignette, vvCA);
            vvPPVolume.weight = 0f;
            DontDestroyOnLoad(vvPPVolume.gameObject);
        }
    }

    /// <summary>Creates a 1×4 Texture2D with alternating opaque/transparent rows for scan-lines.</summary>
    private Texture2D CreateScanLineTexture()
    {
        var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixels(new[]
        {
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0.25f),
            new Color(0f, 0f, 0f, 0.25f)
        });
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// ZZZ-style Vital View animation: four phases driven by unscaled time.
    ///   Phase 1 (~0.06s)   — Snap: orange flash spikes, PP desaturates, CA glitch peaks
    ///   Phase 2 (25% dur)  — Flash fades quickly; CA fades; scan-lines + PP hold
    ///   Phase 3 (40% dur)  — Hold: world stays desaturated + dark vignette + scan-lines
    ///   Phase 4 (30% dur)  — Gradual fade: PP weight 1→0, scan-lines fade
    /// </summary>
    private IEnumerator AnimateVitalViewOverlay(float duration)
    {
        if (vitalViewFlash == null || vitalViewScanLines == null || vvPPVolume == null)
            yield break;

        const float flashSnapTime  = 0.06f;
        const float flashPeakAlpha = 0.50f;
        const float caPeak         = 0.80f;
        const float scanAlpha      = 0.18f;

        // Tile scan-lines to screen resolution (4 px per period)
        vitalViewScanLines.uvRect = new Rect(0f, 0f, 1f, Screen.height / 4f);

        // ── Phase 1: Snap-in ──
        float e = 0f;
        while (e < flashSnapTime)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(e / flashSnapTime);
            vitalViewFlash.color      = new Color(1f, 0.72f, 0.2f, Mathf.Lerp(0f, flashPeakAlpha, t));
            vitalViewScanLines.color  = new Color(0f, 0f, 0f,       Mathf.Lerp(0f, scanAlpha, t));
            vvPPVolume.weight         = Mathf.Lerp(0f, 1f, t);
            vvCA.intensity.Override(Mathf.Lerp(0f, caPeak, t));
            yield return null;
        }
        vitalViewFlash.color     = new Color(1f, 0.72f, 0.2f, flashPeakAlpha);
        vitalViewScanLines.color = new Color(0f, 0f, 0f, scanAlpha);
        vvPPVolume.weight        = 1f;
        vvCA.intensity.Override(caPeak);

        // ── Phase 2: Flash & CA fade ──
        float phase2 = duration * 0.25f;
        e = 0f;
        while (e < phase2)
        {
            e += Time.unscaledDeltaTime;
            float tFlash = Mathf.Clamp01(e / (phase2 * 0.5f));  // flash gone at 50% of phase 2
            float tCA    = Mathf.Clamp01(e / phase2);           // CA gone at end of phase 2
            vitalViewFlash.color = new Color(1f, 0.72f, 0.2f, Mathf.Lerp(flashPeakAlpha, 0f, tFlash));
            vvCA.intensity.Override(Mathf.Lerp(caPeak, 0f, tCA));
            yield return null;
        }
        vitalViewFlash.color = new Color(1f, 0.72f, 0.2f, 0f);
        vvCA.intensity.Override(0f);

        // ── Phase 3: Hold (desaturation + vignette + scan-lines) ──
        yield return new WaitForSecondsRealtime(duration * 0.40f);

        // ── Phase 4: Fade everything out ──
        float phase4 = duration * 0.30f;
        e = 0f;
        while (e < phase4)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(e / phase4);
            vvPPVolume.weight        = Mathf.Lerp(1f, 0f, t);
            vitalViewScanLines.color = new Color(0f, 0f, 0f, Mathf.Lerp(scanAlpha, 0f, t));
            yield return null;
        }

        // Ensure everything is cleanly reset
        vvPPVolume.weight        = 0f;
        vitalViewScanLines.color = new Color(0f, 0f, 0f, 0f);
        vitalViewFlash.color     = new Color(1f, 0.72f, 0.2f, 0f);
        vvCA.intensity.Override(0f);
        vvOverlayCoroutine       = null;
    }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AudioManager.Instance.PlayMusic(MusicType.AmbientBGM);
    }

    // Called when an enemy enters combat (hit by player or chasing)
    public void EnterCombat(GameObject enemy)
    {
        if (enemy == null) return;
        
        bool wasEmpty = enemiesInCombat.Count == 0;
        enemiesInCombat.Add(enemy);

        // Start battle music if this is the first enemy in combat
        if (wasEmpty && !isBattleMusicPlaying)
        {
            AudioManager.Instance.CrossfadeMusic(MusicType.BattleBGM1, 0.5f);
            isBattleMusicPlaying = true;
            Debug.Log("Battle music started! Enemies in combat: " + enemiesInCombat.Count);
        }
    }

    // Called when an enemy exits combat (died or stopped chasing)
    public void ExitCombat(GameObject enemy)
    {
        if (enemy == null) return;
        
        enemiesInCombat.Remove(enemy);

        // Return to ambient music if no enemies left in combat
        if (enemiesInCombat.Count == 0 && isBattleMusicPlaying)
        {
            AudioManager.Instance.CrossfadeMusic(MusicType.AmbientBGM, 1f);
            isBattleMusicPlaying = false;
            Debug.Log("Battle music stopped! No enemies in combat.");
        }
    }

    // Check if currently in combat
    public bool IsInCombat()
    {
        return enemiesInCombat.Count > 0;
    }

    // Get number of enemies in combat
    public int GetCombatEnemyCount()
    {
        return enemiesInCombat.Count;
    }
}
