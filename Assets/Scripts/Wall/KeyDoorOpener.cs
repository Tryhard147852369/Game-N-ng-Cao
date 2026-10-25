using UnityEngine;

public class KeyDoorOpener : MonoBehaviour
{
    [Header("Mã định danh chìa khóa này")]
    public string keyID = "Key_01";

    [Header("Khoảng cách mở cửa tối đa")]
    public float interactDistance = 5f;

    private void Update()
    {
        // Khi chìa khóa đang được cầm trên tay và bấm Chuột Phải
        if (Input.GetMouseButtonDown(1))
        {
            TryUnlockDoor();
        }
    }

    private void TryUnlockDoor()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Bắn tia Raycast từ tâm màn hình
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            // Tìm script DoorTarget trên Cửa được nhắm tới (bao gồm cả object con)
            DoorTarget door = hit.transform.GetComponentInParent<DoorTarget>();

            if (door != null)
            {
                // So sánh mã chìa khóa với mã Cửa yêu cầu
                if (door.requiredKeyID == keyID)
                {
                    Debug.Log($"<color=green>[SUCCESS]</color> Chìa khóa '{keyID}' khớp với Cửa! Đã mở.");

                    // 1. Mở / Ẩn cửa
                    door.OpenDoor();

                    // 2. Xóa chìa khóa khỏi túi đồ
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.RemoveWeaponFromInventory("Key_01");
                    }

                    // 3. Ẩn chìa khóa trên tay
                    gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"[FAIL] Chìa khóa '{keyID}' KHÔNG khớp! Cửa này cần chìa '{door.requiredKeyID}'.");
                }
            }
        }
    }
}