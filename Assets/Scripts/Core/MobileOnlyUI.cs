using UnityEngine;

public class MobileOnlyUI : MonoBehaviour
{
    void Start()
    {
        // Show on mobile platforms (iOS/Android) OR devices Unity classifies as Handheld (some tablets)
        bool isPhoneOrTablet = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
        if (!isPhoneOrTablet)
        {
            gameObject.SetActive(false);
        }
    }
}
