using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Dữ liệu vật phẩm")]
    public ItemData itemData; // Kéo file ItemData (Knife hoặc Axe) tương ứng vào đây
    public int amount = 1;

    // Hàm Pickup nhận tham số InventoryManager từ PlayerInteraction truyền sang
    public void Pickup(InventoryManager inventory)
    {
        Debug.Log($"[PICKUP TEST] Đã bấm nhặt thành công vật thể: {gameObject.name}");

        if (itemData == null)
        {
            Debug.LogError($"[ItemPickup] Ô ItemData trên {gameObject.name} bị TRỐNG!");
            return;
        }

        InventoryManager targetInventory = inventory != null ? inventory : InventoryManager.Instance;

        if (targetInventory != null)
        {
            bool success = targetInventory.AddItem(itemData, amount);

            if (success)
            {
                if (ItemNotification.Instance != null)
                {
                    ItemNotification.Instance.ClearHint();
                }

                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("[PICKUP TEST] Hàm AddItem trả về FALSE (Có thể kho đầy hoặc MaxStack = 0)!");
            }
        }
    }
}