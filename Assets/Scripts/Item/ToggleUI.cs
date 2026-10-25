using UnityEngine;

public class ToggleUI : MonoBehaviour
{
    [Header("Bảng UI cần bật/tắt")]
    public GameObject inventoryPanel; // Kéo InventoryPanel vào đây

    [Header("Phím phím tắt")]
    public KeyCode toggleKey = KeyCode.B; // Phím B

    void Start()
    {
        // Ban đầu ẩn bảng túi đồ đi
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Kiểm tra bấm phím B
        if (Input.GetKeyDown(toggleKey))
        {
            if (inventoryPanel != null)
            {
                // Đảo ngược trạng thái bật/tắt hiện tại
                bool newState = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(newState);

                // Khóa/Mở con trỏ chuột khi bật/tắt UI
                Cursor.lockState = newState ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = newState;

                Debug.Log($"[ToggleUI] Đã bấm phím {toggleKey} -> Trạng thái kho đồ: {newState}");
            }
            else
            {
                Debug.LogError("[ToggleUI] LỖI: Chưa kéo InventoryPanel vào ô Inspector!");
            }
        }
    }
}