using UnityEngine;

public class PersistentSystem : MonoBehaviour
{
    // 用来记录是否已经存在一个实例
    private static PersistentSystem instance;

    private void Awake()
    {
        // 核心逻辑：单例检查
        if (instance != null)
        {
            // 如果已经有一个老兵在场了，新兵直接原地退役
            Destroy(gameObject);
            return;
        }

        // 如果我是第一个，就把自己标记为“老兵”
        instance = this;

        // 确保切换场景时不被销毁
        DontDestroyOnLoad(gameObject);
    }
}
