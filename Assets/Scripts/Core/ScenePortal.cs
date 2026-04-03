using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "TownScene";
    [SerializeField] private Vector2 spawnPosition;
    [SerializeField] private Vector2 targetFacing = new Vector2(0, -1);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 存下坐标和朝向（跨场景传递数据最稳的方法）
            PlayerStaticData.NextSpawnPos = spawnPosition;
            PlayerStaticData.NextFacing = targetFacing;

            // 2. 注册场景加载完成后的回调函数
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 3. 切场景
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // 这个方法会在新场景加载完毕后自动被调用一次
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 1. 瞬间移动
            player.transform.position = PlayerStaticData.NextSpawnPos;

            // 2. 强行设置 Animator
            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("moveX", PlayerStaticData.NextFacing.x);
                anim.SetFloat("moveY", PlayerStaticData.NextFacing.y);
                // 强制更新一帧动画，防止 Blend Tree 没反应
                anim.Update(0f); 
            }
        }

        // 重要：用完之后注销回调，否则下次切场景会执行多次！
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

// 辅助类：存数据用（不需要挂在物体上）
public static class PlayerStaticData
{
    public static Vector2 NextSpawnPos;
    public static Vector2 NextFacing;
}
