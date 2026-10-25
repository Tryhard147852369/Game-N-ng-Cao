using UnityEngine;

public class WeaponData : MonoBehaviour
{
    [Header("--- THÔNG SỐ VŨ KHÍ ---")]
    public string weaponName = "Axe_01";
    public int damage = 10;
    public int maxDurability = 4;
    public int currentDurability = 4;

    private void Awake()
    {
        if (currentDurability <= 0) currentDurability = maxDurability;
    }

    // Hàm gọi khi chém trúng kẻ địch / công trình để trừ độ bền
    public void UseDurability(int amount = 1)
    {
        currentDurability -= amount;

        // Cập nhật thanh Slider UI ở tất cả các slot kho đồ/hotbar
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.UpdateWeaponDurabilityUI(weaponName, currentDurability, maxDurability);
        }

        // HẾT ĐỘ BỀN -> XÓA VŨ KHÍ
        if (currentDurability <= 0)
        {
            currentDurability = 0;
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveWeaponFromInventory(weaponName);
            }
            gameObject.SetActive(false);
        }
    }

    // Hàm Reset độ bền khi nhặt vật phẩm mới
    public void ResetDurability()
    {
        currentDurability = maxDurability > 0 ? maxDurability : 4;
    }
}