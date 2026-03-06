using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Track all enemies currently in combat (using GameObject for compatibility with new component system)
    private HashSet<GameObject> enemiesInCombat = new HashSet<GameObject>();
    private bool isBattleMusicPlaying = false;

    // ===== VITAL VIEW (ZZZ-style bullet time dodge) =====
    private bool isVitalViewActive = false;
    private Coroutine vitalViewCoroutine;

    // Screen overlay for Vital View visual feedback (created at runtime, no prefab needed)
    private Canvas vitalViewCanvas;
    private UnityEngine.UI.Image vitalViewOverlay;

    public bool IsVitalViewActive() => isVitalViewActive;

    /// <summary>
    /// Trigger ZZZ-style Vital View: slow time to slowScale for duration real seconds.
    /// Default 0.05 (5% speed) for 1.2s is very dramatic and impossible to miss.
    /// Player movement is unaffected (PlayerController uses unscaled delta when active).
    /// </summary>
    public void TriggerVitalView(float duration = 1.2f, float slowScale = 0.05f)
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

        // Show blue screen flash overlay
        EnsureVitalViewOverlay();
        StartCoroutine(AnimateVitalViewOverlay(duration));

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
    /// Creates a full-screen semi-transparent canvas overlay (once, lazily).
    /// No prefab required — built entirely at runtime.
    /// </summary>
    private void EnsureVitalViewOverlay()
    {
        if (vitalViewCanvas != null) return;

        GameObject canvasGO = new GameObject("VitalViewCanvas");
        DontDestroyOnLoad(canvasGO);

        vitalViewCanvas = canvasGO.AddComponent<Canvas>();
        vitalViewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vitalViewCanvas.sortingOrder = 999; // Always on top

        // Full-screen image child
        GameObject imgGO = new GameObject("Overlay");
        imgGO.transform.SetParent(canvasGO.transform, false);

        vitalViewOverlay = imgGO.AddComponent<UnityEngine.UI.Image>();
        vitalViewOverlay.raycastTarget = false; // Don't block clicks
        vitalViewOverlay.color = new Color(0.1f, 0.45f, 1f, 0f); // blue tint, starts transparent

        // Stretch to fill entire screen
        RectTransform rt = vitalViewOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Animates the Vital View screen overlay: instant blue flash, then slow fade out.
    /// Uses unscaled time so the animation is unaffected by our own time slowdown.
    /// </summary>
    private IEnumerator AnimateVitalViewOverlay(float duration)
    {
        if (vitalViewOverlay == null) yield break;

        const float peakAlpha = 0.30f;
        const float flashInTime = 0.07f; // snap-in

        // Snap blue overlay in
        float e = 0f;
        while (e < flashInTime)
        {
            e += Time.unscaledDeltaTime;
            vitalViewOverlay.color = new Color(0.1f, 0.45f, 1f,
                Mathf.Lerp(0f, peakAlpha, e / flashInTime));
            yield return null;
        }

        // Brief hold
        yield return new WaitForSecondsRealtime(duration * 0.2f);

        // Fade out over the remaining duration
        float fadeTime = duration * 0.7f;
        e = 0f;
        while (e < fadeTime)
        {
            e += Time.unscaledDeltaTime;
            vitalViewOverlay.color = new Color(0.1f, 0.45f, 1f,
                Mathf.Lerp(peakAlpha, 0f, e / fadeTime));
            yield return null;
        }

        vitalViewOverlay.color = new Color(0.1f, 0.45f, 1f, 0f);
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
