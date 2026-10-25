using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("--- CẤU HÌNH ĐÈN PIN ---")]
    public Light flashlightLight; // Kéo Spot Light vào đây
    public KeyCode toggleKey = KeyCode.F; // Phím F để Bật/Tắt đèn pin

    private bool isOn = true;

    private void OnEnable()
    {
        // Mỗi khi đổi sang item Đèn Pin trên tay -> Tự động bật đèn
        isOn = true;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = true;
        }
    }

    private void Update()
    {
        // Nhấn phím F để Bật / Tắt ánh sáng khi đang cầm đèn
        if (Input.GetKeyDown(toggleKey))
        {
            isOn = !isOn;
            if (flashlightLight != null)
            {
                flashlightLight.enabled = isOn;
            }
        }
    }
}