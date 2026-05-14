using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles smooth scene transitions with fade-to-black effect and input locking.
/// Industry standard approach: lock input → fade out → load scene → position player → fade in → unlock input.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Auto-create if doesn't exist
                GameObject go = new GameObject("SceneTransitionManager");
                instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Canvas fadeCanvas;
    private Image fadeImage;
    private bool isTransitioning = false;
    private bool sceneLoadComplete = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        // Create persistent fade overlay canvas
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);
        
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000; // Above everything else
        
        canvasObj.AddComponent<CanvasScaler>();
        // REMOVED GraphicRaycaster - we don't need to interact with this canvas!

        // Create black image for fade effect
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Start transparent
        fadeImage.raycastTarget = false; // CRITICAL: Don't block UI clicks when invisible!
        
        // Stretch to fill screen
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        DontDestroyOnLoad(canvasObj);
    }

    /// <summary>
    /// Transition to a new scene with fade effect and proper input handling.
    /// </summary>
    public void TransitionToScene(string targetScene, Vector2 spawnPos, Vector2 targetFacing, float fadeDuration = 0.3f)
    {
        if (!isTransitioning)
            StartCoroutine(TransitionCoroutine(targetScene, spawnPos, targetFacing, fadeDuration));
    }

    private IEnumerator TransitionCoroutine(string targetScene, Vector2 spawnPos, Vector2 targetFacing, float fadeDuration)
    {
        isTransitioning = true;
        sceneLoadComplete = false;

        // 1. Lock player controls
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerController pc = player?.GetComponent<PlayerController>();
        if (pc != null)
            pc.LockControls(true);

        // Play a small 'move' SFX when beginning the transition
        AudioManager.Instance?.PlaySFX(SFXType.Move);

        // 2. Fade to black
        yield return StartCoroutine(Fade(1f, fadeDuration));

        // 3. Load new scene (happens while screen is black)
        PlayerStaticData.NextSpawnPos = spawnPos;
        PlayerStaticData.NextFacing = targetFacing;
        
        SceneManager.sceneLoaded += OnSceneLoadedAfterTransition;
        SceneManager.LoadScene(targetScene);

        // Wait for scene to actually load and player to be positioned
        float timeout = 2f;
        float elapsed = 0f;
        while (!sceneLoadComplete && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 4. Fade back in
        yield return StartCoroutine(Fade(0f, fadeDuration));

        // 5. Unlock controls AFTER everything is ready
        player = GameObject.FindGameObjectWithTag("Player");
        pc = player?.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.LockControls(false);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager: Could not find player to unlock controls!");
        }

        isTransitioning = false;
    }

    private void OnSceneLoadedAfterTransition(Scene scene, LoadSceneMode mode)
    {
        // Position and orient player in new scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = PlayerStaticData.NextSpawnPos;
            
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetFacingDirection(PlayerStaticData.NextFacing);
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoadedAfterTransition;
        sceneLoadComplete = true; // Signal to coroutine that scene is ready
    }

    /// <summary>
    /// Fade the screen to/from black.
    /// </summary>
    /// <param name="targetAlpha">0 = transparent, 1 = black</param>
    /// <param name="duration">Fade duration in seconds</param>
    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled so it works during pause/slowmo
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
    }
}
