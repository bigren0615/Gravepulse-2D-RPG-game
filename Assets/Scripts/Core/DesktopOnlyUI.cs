using UnityEngine;

public class DesktopOnlyUI : MonoBehaviour
{
    void Start()
    {
        bool isMobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
        if (isMobile)
        {
            gameObject.SetActive(false);
        }
    }
}
