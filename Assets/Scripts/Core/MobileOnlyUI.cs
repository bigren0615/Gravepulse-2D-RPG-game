using UnityEngine;

public class MobileOnlyUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!Application.isMobilePlatform)
        {
            gameObject.SetActive(false);
        }
    }
}
