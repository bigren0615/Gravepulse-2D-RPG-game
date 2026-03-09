using UnityEngine;

public class MobileOnlyUI : MonoBehaviour
{
    void Start()
    {
        // Show only on actual handheld devices (phones and tablets)
        bool isPhoneOrTablet = Application.isMobilePlatform && SystemInfo.deviceType == DeviceType.Handheld;
        if (!isPhoneOrTablet)
        {
            gameObject.SetActive(false);
        }
    }
}
