using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public TextMeshProUGUI countText;

    [Header("--- UI THANH ĐỘ BỀN Ở ĐÁY SLOT ---")]
    public Slider durabilitySlider;

    [HideInInspector] public ItemData item;
    [HideInInspector] public int count;

    private void Awake()
    {
        if (item == null) ClearSlot();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && item != null)
        {
            // Xử lý sử dụng Vật phẩm Tiêu thụ (Đồ ăn / Nước uống / Máu)
            if (item.itemType == ItemType.Consumable)
            {
                UseConsumable();
            }
            // Chuyển đồ giữa Kho B và Hotbar
            else if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TransferItemSlot(this);
            }
        }
    }

    // =========================================================================
    // HÀM SỬ DỤNG VẬT PHẨM TIÊU THỤ (ĐÃ TỐI ƯU ĐỒNG BỘ VỚI MÔ HÌNH TRÊN TAY)
    // =========================================================================
    public void UseConsumable()
    {
        if (item == null || count <= 0) return;

        if (InventoryManager.Instance != null)
        {
            // 1. Kiểm tra mô hình vật phẩm đang cầm trên tay
            GameObject handObj = InventoryManager.Instance.FindWeaponInHand(item.itemName);
            if (handObj != null && handObj.activeInHierarchy)
            {
                FoodConsumable food = handObj.GetComponent<FoodConsumable>();
                if (food != null)
                {
                    food.EatFood();
                    return;
                }

                WaterConsumable water = handObj.GetComponent<WaterConsumable>();
                if (water != null)
                {
                    water.DrinkWater();
                    return;
                }
            }

            // 2. Nếu không dùng hiệu ứng trên tay -> Nhận Buff và trừ đồ ngay lập tức
            InventoryManager.Instance.ApplyConsumableEffect(item);
            DeductCount(1);
        }
    }

    public void SetItem(ItemData newItem, int newCount)
    {
        item = newItem;
        count = newCount;

        if (item != null && count > 0)
        {
            if (icon != null)
            {
                icon.sprite = item.icon;
                icon.enabled = true;
                icon.color = Color.white;
            }

            UpdateCountUI();

            if (item.itemType == ItemType.Weapon)
            {
                ShowWeaponDurability();
            }
            else
            {
                if (durabilitySlider != null) durabilitySlider.gameObject.SetActive(false);
            }
        }
        else
        {
            ClearSlot();
        }
    }

    // --- TRỪ SỐ LƯỢNG VẬT PHẨM VÀ TỰ ĐỘNG XÓA KHI HẾT ---
    public bool DeductCount(int amountToRemove = 1)
    {
        count -= amountToRemove;

        if (count <= 0)
        {
            ClearSlot();
            return true; // Đã hết sạch vật phẩm trong ô
        }
        else
        {
            UpdateCountUI();
            return false; // Vẫn còn vật phẩm trong ô
        }
    }

    public void UpdateCountUI()
    {
        if (countText != null)
        {
            if (item != null && (item.maxStack > 1 || count > 1))
            {
                countText.text = count.ToString();
                countText.enabled = true;
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.text = "";
                countText.enabled = false;
                countText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowWeaponDurability()
    {
        if (durabilitySlider == null) return;

        if (item != null && item.itemType == ItemType.Weapon)
        {
            int cur = 4;
            int max = 4;

            if (InventoryManager.Instance != null)
            {
                GameObject weaponObj = InventoryManager.Instance.FindWeaponInHand(item.itemName);
                if (weaponObj != null)
                {
                    WeaponData wData = weaponObj.GetComponent<WeaponData>();
                    if (wData != null)
                    {
                        cur = wData.currentDurability;
                        max = wData.maxDurability > 0 ? wData.maxDurability : 4;
                    }
                }
            }

            durabilitySlider.gameObject.SetActive(true);
            durabilitySlider.minValue = 0;
            durabilitySlider.maxValue = max;
            durabilitySlider.value = Mathf.Clamp(cur, 0, max);
        }
        else
        {
            durabilitySlider.gameObject.SetActive(false);
        }
    }

    public void RefreshDurability()
    {
        ShowWeaponDurability();
    }

    public void UpdateDurabilityUI(int currentDurability, int maxDurability)
    {
        if (durabilitySlider == null) return;

        if (item != null && item.itemType == ItemType.Weapon && currentDurability > 0)
        {
            durabilitySlider.gameObject.SetActive(true);
            durabilitySlider.minValue = 0;
            durabilitySlider.maxValue = maxDurability > 0 ? maxDurability : 4;
            durabilitySlider.value = Mathf.Clamp(currentDurability, 0, durabilitySlider.maxValue);
        }
        else
        {
            durabilitySlider.gameObject.SetActive(false);
        }
    }

    public void ClearSlot()
    {
        item = null;
        count = 0;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (countText != null)
        {
            countText.text = "";
            countText.enabled = false;
            if (countText.gameObject != null) countText.gameObject.SetActive(false);
        }

        if (durabilitySlider != null)
        {
            durabilitySlider.gameObject.SetActive(false);
        }

        // ĐÃ SỬA: Chỉ cất vật phẩm trên tay NẾU ô bị xóa chính là ô nhân vật đang cầm
        if (InventoryManager.Instance != null && InventoryManager.Instance.GetCurrentEquippedSlot() == this)
        {
            InventoryManager.Instance.HideAllWeaponsInHand();
        }
    }
}