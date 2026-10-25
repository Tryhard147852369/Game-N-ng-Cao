using UnityEngine;

public class HotbarWeaponController : MonoBehaviour
{
    [Header("1. Danh sách 3 ô Slot trong InventoryPanel (1)")]
    public InventorySlotUI[] hotbarSlots; // Slot 0 = phím 1, Slot 1 = phím 2, Slot 2 = phím 3

    [Header("2. Danh sách các mô hình 3D vũ khí nằm trong RightHand")]
    public GameObject[] weapon3DModels;

    private int selectedIndex = 0; // Mặc định chọn ô phím 1 (Index 0)

    private void Start()
    {
        SelectHotbarSlot(0);
    }

    private void Update()
    {
        // Lắng nghe phím 1, 2, 3
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbarSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbarSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbarSlot(2);

        UpdateEquippedWeapon();
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;

        selectedIndex = index;
        UpdateEquippedWeapon();
    }

    // --- HÀM TRỪ 1 SỐ LƯỢNG Ở Ô HOTBAR ĐANG CHỌN ---
    public bool DeductCurrentSlotCount(int amount = 1)
    {
        if (selectedIndex >= 0 && selectedIndex < hotbarSlots.Length)
        {
            InventorySlotUI slot = hotbarSlots[selectedIndex];
            if (slot != null)
            {
                // Trừ số lượng gián tiếp qua InventorySlotUI.DeductCount()
                return slot.DeductCount(amount);
            }
        }
        return true; // Trả về true nếu ô đã hết sạch hoặc không tìm thấy slot
    }

    public void ClearCurrentSlot()
    {
        if (selectedIndex >= 0 && selectedIndex < hotbarSlots.Length)
        {
            if (hotbarSlots[selectedIndex] != null)
            {
                hotbarSlots[selectedIndex].ClearSlot();
            }
        }
    }

    private void UpdateEquippedWeapon()
    {
        if (selectedIndex < 0 || selectedIndex >= hotbarSlots.Length)
        {
            HideAllWeapons();
            return;
        }

        InventorySlotUI slot = hotbarSlots[selectedIndex];

        if (slot != null && slot.item != null)
        {
            string itemName = slot.item.name;
            string cleanItemName = itemName.Replace("_Data", "").Replace("_Item", "").Trim();

            bool foundWeapon = false;

            foreach (GameObject weapon in weapon3DModels)
            {
                if (weapon == null) continue;

                string cleanWeaponName = weapon.name.Replace("_Data", "").Replace("_Item", "").Trim();

                if (weapon.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) ||
                    cleanWeaponName.StartsWith(cleanItemName, System.StringComparison.OrdinalIgnoreCase) ||
                    cleanItemName.StartsWith(cleanWeaponName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!weapon.activeSelf) weapon.SetActive(true);
                    foundWeapon = true;
                }
                else
                {
                    if (weapon.activeSelf) weapon.SetActive(false); // Khi bị tắt ở đây, WaterConsumable tự động nhận Buff ngay!
                }
            }

            if (!foundWeapon)
            {
                HideAllWeapons();
            }
        }
        else
        {
            HideAllWeapons();
        }
    }

    private void HideAllWeapons()
    {
        foreach (GameObject weapon in weapon3DModels)
        {
            if (weapon != null && weapon.activeSelf)
            {
                weapon.SetActive(false);
            }
        }
    }
}