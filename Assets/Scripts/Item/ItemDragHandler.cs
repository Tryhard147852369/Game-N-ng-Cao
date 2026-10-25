using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventorySlotUI slotUI;
    private Canvas canvas;
    private GameObject dragIconObj;

    private void Awake()
    {
        slotUI = GetComponent<InventorySlotUI>();
        if (slotUI == null) slotUI = GetComponentInParent<InventorySlotUI>();

        canvas = GetComponentInParent<Canvas>();
    }

    // 1. KHI BẮT ĐẦU KÉO CHUỘT TRÁI
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (slotUI == null || slotUI.item == null || slotUI.icon == null || !slotUI.icon.enabled) return;

        // Tạo Icon tạm thời (Ghost Icon) hiển thị nổi lên trên cùng màn hình
        dragIconObj = new GameObject("TempDragIcon");
        dragIconObj.transform.SetParent(canvas.transform, false);
        dragIconObj.transform.SetAsLastSibling();

        Image dragImage = dragIconObj.AddComponent<Image>();
        dragImage.sprite = slotUI.icon.sprite;
        dragImage.raycastTarget = false; // Bắt buộc FALSE để Raycast xuyên qua trúng ô bên dưới khi nhả chuột

        RectTransform rect = dragIconObj.GetComponent<RectTransform>();
        rect.sizeDelta = slotUI.icon.rectTransform.sizeDelta;

        // Làm mờ nhẹ icon ở ô gốc khi đang kéo
        slotUI.icon.color = new Color(1f, 1f, 1f, 0.4f);
    }

    // 2. TRONG KHI ĐANG KÉO CHUỘT
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (dragIconObj != null)
        {
            dragIconObj.transform.position = eventData.position; // Icon bay theo con trỏ chuột
        }
    }

    // 3. KHI NHẢ CHUỘT
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Xóa Ghost Icon
        if (dragIconObj != null)
        {
            Destroy(dragIconObj);
        }

        // Phục hồi màu icon gốc ở ô
        if (slotUI != null && slotUI.icon != null)
        {
            slotUI.icon.color = Color.white;
        }

        if (slotUI == null || slotUI.item == null) return;

        // Kiểm tra đối tượng tại vị trí nhả chuột
        GameObject targetObj = eventData.pointerCurrentRaycast.gameObject;

        if (targetObj != null)
        {
            // Tìm ô Slot tại vị trí nhả chuột
            InventorySlotUI targetSlot = targetObj.GetComponent<InventorySlotUI>();
            if (targetSlot == null) targetSlot = targetObj.GetComponentInParent<InventorySlotUI>();

            // Nếu thả trúng ô Slot khác -> Đổi vị trí item
            if (targetSlot != null && targetSlot != slotUI)
            {
                SwapSlotItems(slotUI, targetSlot);
                return;
            }
        }

        // Nếu nhả chuột ra ngoài không gian UI -> Thực hiện VỨT ĐỒ ra đất
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.DropItem(slotUI);
            }
        }
    }

    // Hàm hoán đổi dữ liệu giữa 2 ô Slot
    private void SwapSlotItems(InventorySlotUI slotA, InventorySlotUI slotB)
    {
        ItemData tempItemA = slotA.item;
        int tempCountA = slotA.count;

        ItemData tempItemB = slotB.item;
        int tempCountB = slotB.count;

        // Đổi B sang A
        if (tempItemB != null) slotA.SetItem(tempItemB, tempCountB);
        else slotA.ClearSlot();

        // Đổi A sang B
        if (tempItemA != null) slotB.SetItem(tempItemA, tempCountA);
        else slotB.ClearSlot();

        // Cập nhật lại thanh độ bền nếu là vũ khí
        UpdateDurability(slotA);
        UpdateDurability(slotB);

        // Cất vũ khí đang cầm trên tay để người chơi bấm phím 1, 2, 3 chọn lại
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.HideAllWeaponsInHand();
        }
    }

    private void UpdateDurability(InventorySlotUI slot)
    {
        if (slot != null && slot.item != null && slot.item.itemType == ItemType.Weapon && InventoryManager.Instance != null)
        {
            GameObject weaponInHand = InventoryManager.Instance.FindWeaponInHand(slot.item.itemName);
            if (weaponInHand != null)
            {
                WeaponData wData = weaponInHand.GetComponent<WeaponData>();
                if (wData != null)
                {
                    slot.UpdateDurabilityUI(wData.currentDurability, wData.maxDurability);
                }
            }
        }
    }
}