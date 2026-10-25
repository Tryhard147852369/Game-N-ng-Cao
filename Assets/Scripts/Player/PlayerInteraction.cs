using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera mainCamera;
    public float pickupDistance = 3f;
    public KeyCode pickupKey = KeyCode.E;
    public InventoryManager inventoryManager;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();
    }

    void Update()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            ItemPickup item = hit.collider.GetComponentInParent<ItemPickup>();

            if (item != null && item.itemData != null)
            {
                // 1. Chỉ hiện chữ gợi ý khi nhìn vào đồ
                if (ItemNotification.Instance != null)
                {
                    ItemNotification.Instance.ShowHint("Nhấn [" + pickupKey + "] để nhặt " + item.itemData.itemName);
                }

                // 2. Nhấn E để nhặt (Đã bỏ chữ "Đã nhặt: ...")
                if (Input.GetKeyDown(pickupKey))
                {
                    item.Pickup(inventoryManager);
                }

                return;
            }
        }

        // Tự động tắt chữ khi không nhìn vào đồ
        if (ItemNotification.Instance != null)
        {
            ItemNotification.Instance.ClearHint();
        }
    }
}