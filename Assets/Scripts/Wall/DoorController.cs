using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Chìa khóa trên tay Player (RightHand -> Key_01)")]
    public GameObject keyInHand;

    [Header("Khoảng cách mở cửa tối đa")]
    public float interactDistance = 10f;

    [Header("Tùy chọn: Xóa chìa khóa khỏi tay & kho đồ sau khi mở?")]
    public bool destroyKeyAfterUse = true;

    private void Update()
    {
        // Bấm Chuột Trái (Left Click)
        if (Input.GetMouseButtonDown(0))
        {
            TryOpenDoor();
        }
    }

    private void TryOpenDoor()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[DOOR TEST] Không tìm thấy Main Camera!");
            return;
        }

        // Bắn tia Raycast từ tâm màn hình
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Lấy tất cả vật thể nằm trên đường bắn
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);

        bool foundDoor = false;

        foreach (RaycastHit hit in hits)
        {
            // Bỏ qua nếu tia đâm trúng bản thân Player
            if (hit.transform.CompareTag("Player")) continue;

            // Kiểm tra nếu trúng Cửa này hoặc phần mô hình con thuộc Cửa
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.root == transform.root)
            {
                foundDoor = true;

                // Kiểm tra chìa khóa Key_01 trên tay Player có đang bật (đang cầm) hay không
                if (keyInHand != null && keyInHand.activeSelf)
                {
                    Debug.Log("<color=green>[DOOR TEST] MỞ CỬA THÀNH CÔNG!</color>");

                    if (destroyKeyAfterUse)
                    {
                        keyInHand.SetActive(false);

                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.RemoveWeaponFromInventory("Key_01");
                        }
                    }

                    // Cửa ẩn đi
                    gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[DOOR TEST] Cửa bị khóa! Bạn cần bấm phím Hotbar cầm Key_01 lên tay.");
                }

                break;
            }
        }

        if (!foundDoor)
        {
            Debug.Log("[DOOR TEST] Hãy tiến lại gần Cửa hơn và chĩa tâm ngắm chính xác vào Cửa.");
        }
    }
}