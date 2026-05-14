using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "TownScene";
    [SerializeField] private Vector2 spawnPosition;
    [SerializeField] private Vector2 targetFacing = new Vector2(0, -1);
    [Tooltip("Fade duration in seconds. Typical: 0.2-0.5s")]
    [SerializeField] private float fadeDuration = 0.3f;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasTriggered) return;
        if (SceneTransitionManager.IsTransitioning) return;

        hasTriggered = true;

        // Use transition manager for smooth fade + input locking
        SceneTransitionManager.Instance.TransitionToScene(
            targetSceneName,
            spawnPosition,
            targetFacing,
            fadeDuration
        );
    }
}

// 辅助类：存数据用（不需要挂在物体上）
public static class PlayerStaticData
{
    public static Vector2 NextSpawnPos;
    public static Vector2 NextFacing;
}
