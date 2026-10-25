using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; // Sửa triệt để lỗi CS0104 xung đột Random

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI Grid Parent (Kho đồ B)")]
    public Transform itemGrid;

    [Header("UI Hotbar Grid Parent (Thanh 1, 2, 3)")]
    public Transform hotbarGrid;

    [Header("Trang bị Vũ khí")]
    public Transform rightHand;

    public List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private int currentEquippedIndex = -1;

    // --- CÁC BIẾN QUẢN LÝ QUÁ TRÌNH ĂN / UỐNG ---
    private Coroutine activeConsumeCoroutine;
    private InventorySlotUI activeConsumeSlot;
    private ItemData activeConsumeItem;
    private GameObject activeConsumableInHand;
    private System.Action onActiveConsumeBuff;

    public bool IsConsuming => activeConsumeCoroutine != null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RefreshSlotList();
        HideAllWeaponsInHand();
        UpdateAllSlotDurability();
    }

    private void Update()
    {
        // Cho phép bấm phím 1, 2, 3 đổi Slot thoải mái kể cả khi đang ăn/uống
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeaponFromHotbar(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeaponFromHotbar(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipWeaponFromHotbar(2);
    }

    // =========================================================================
    // 1. SỬA LỖI CS1061: HÀM ApplyConsumableEffect
    // =========================================================================
    public void ApplyConsumableEffect(ItemData item)
    {
        if (item == null) return;

        // Nếu đang trong tiến trình ăn/uống dở dang -> Ép hoàn thành ngay lập tức
        if (activeConsumeCoroutine != null)
        {
            FinishConsumeImmediately();
        }
        else
        {
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Heal(20f);
                Debug.Log($"<color=green>[CONSUME]</color> Đã sử dụng {item.itemName}!");
            }
        }
    }

    // =========================================================================
    // 2. BẮT ĐẦU QUÁ TRÌNH ĂN / UỐNG (LƯU VỊ TRÍ Ô BAN ĐẦU)
    // =========================================================================
    public void StartConsumeItem(InventorySlotUI sourceSlot, float duration = 1.5f, System.Action customBuffAction = null)
    {
        if (sourceSlot == null || sourceSlot.item == null) return;

        // Nếu đang ăn món trước mà bấm ăn món mới -> Ép dứt điểm món cũ ngay
        if (activeConsumeCoroutine != null)
        {
            FinishConsumeImmediately();
        }

        activeConsumeSlot = sourceSlot;
        activeConsumeItem = sourceSlot.item;
        onActiveConsumeBuff = customBuffAction;

        // Bật mô hình vật phẩm tiêu thụ trên tay
        activeConsumableInHand = FindWeaponInHand(activeConsumeItem.itemName);
        if (activeConsumableInHand != null) activeConsumableInHand.SetActive(true);

        activeConsumeCoroutine = StartCoroutine(ConsumeRoutine(duration));
    }

    private IEnumerator ConsumeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Chạy đủ thời gian -> Hoàn thành nhận Buff và trừ đồ
        FinishConsumeImmediately();
    }

    // =========================================================================
    // 3. ÉP HOÀN THÀNH NGAY (DÙNG KHI HẾT THỜI GIAN HOẶC BẤM ĐỔI SLOT 1, 2, 3)
    // =========================================================================
    public void FinishConsumeImmediately()
    {
        if (activeConsumeCoroutine != null)
        {
            StopCoroutine(activeConsumeCoroutine);
            activeConsumeCoroutine = null;
        }

        if (activeConsumeItem != null)
        {
            // A. KÍCH HOẠT BUFF / HỒI MÁU
            if (onActiveConsumeBuff != null)
            {
                onActiveConsumeBuff.Invoke();
                onActiveConsumeBuff = null;
            }
            else
            {
                PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.Heal(20f);
                }
            }

            // B. TRỪ SỐ LƯỢNG VẬT PHẨM Ở ĐÚNG Ô UI BAN ĐẦU (ĐÃ ĐƯỢC LƯU CỐ ĐỊNH)
            if (activeConsumeSlot != null)
            {
                activeConsumeSlot.DeductCount(1);
                activeConsumeSlot = null;
            }

            // C. TẮT MÔ HÌNH ĂN/UỐNG TRÊN TAY
            if (activeConsumableInHand != null)
            {
                DrinkAnimation drinkAnim = activeConsumableInHand.GetComponent<DrinkAnimation>();
                if (drinkAnim != null) drinkAnim.ResetToOriginalPosition();

                activeConsumableInHand.SetActive(false);
                activeConsumableInHand = null;
            }

            activeConsumeItem = null;
        }
    }

    // =========================================================================
    // 4. ĐỔI SLOT TRÊN HOTBAR (PHÍM 1, 2, 3)
    // =========================================================================
    public void EquipWeaponFromHotbar(int hotbarIndex)
    {
        if (hotbarGrid == null) return;

        // NẾU ĐANG ĂN/UỐNG MÀ BẤM ĐỔI SLOT -> CỘNG BUFF NGAY & TRỪ ĐỒ Ô BAN ĐẦU NGAY LẬP TỨC
        if (activeConsumeCoroutine != null)
        {
            FinishConsumeImmediately();
        }

        InventorySlotUI[] hotbarSlots = hotbarGrid.GetComponentsInChildren<InventorySlotUI>(true);
        if (hotbarIndex < 0 || hotbarIndex >= hotbarSlots.Length) return;

        if (currentEquippedIndex == hotbarIndex)
        {
            HideAllWeaponsInHand();
            return;
        }

        HideAllWeaponsInHand();

        InventorySlotUI selectedSlot = hotbarSlots[hotbarIndex];
        if (selectedSlot != null && selectedSlot.item != null)
        {
            ItemData item = selectedSlot.item;

            GameObject itemInHand = FindWeaponInHand(item.itemName);
            if (itemInHand != null)
            {
                itemInHand.SetActive(true);
                currentEquippedIndex = hotbarIndex;

                UpdateAllSlotDurability();
                return;
            }
        }
    }

    public void RefreshSlotList()
    {
        slots.Clear();

        if (hotbarGrid != null)
        {
            foreach (InventorySlotUI slot in hotbarGrid.GetComponentsInChildren<InventorySlotUI>(true))
            {
                if (!slots.Contains(slot)) slots.Add(slot);
            }
        }

        if (itemGrid != null)
        {
            foreach (InventorySlotUI slot in itemGrid.GetComponentsInChildren<InventorySlotUI>(true))
            {
                if (!slots.Contains(slot)) slots.Add(slot);
            }
        }

        if (slots.Count == 0)
        {
            InventorySlotUI[] allSlots = FindObjectsOfType<InventorySlotUI>(true);
            slots.AddRange(allSlots);
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        RefreshSlotList();

        int remainingAmount = amount;

        if (item.maxStack > 1)
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.item != null && IsNameMatching(slot.item.itemName, item.itemName))
                {
                    if (slot.count < item.maxStack)
                    {
                        int spaceLeft = item.maxStack - slot.count;

                        if (remainingAmount <= spaceLeft)
                        {
                            slot.SetItem(slot.item, slot.count + remainingAmount);
                            remainingAmount = 0;
                            break;
                        }
                        else
                        {
                            slot.SetItem(slot.item, item.maxStack);
                            remainingAmount -= spaceLeft;
                        }
                    }
                }
            }
        }

        if (remainingAmount > 0)
        {
            foreach (var slot in slots)
            {
                if (slot != null && (slot.item == null || (slot.icon != null && !slot.icon.enabled)))
                {
                    int addAmount = Mathf.Min(remainingAmount, item.maxStack);
                    slot.SetItem(item, addAmount);
                    remainingAmount -= addAmount;

                    if (item.itemType == ItemType.Weapon)
                    {
                        GameObject weaponInHand = FindWeaponInHand(item.itemName);
                        if (weaponInHand != null)
                        {
                            WeaponData wData = weaponInHand.GetComponent<WeaponData>();
                            if (wData != null) wData.ResetDurability();
                        }
                    }

                    if (remainingAmount <= 0) break;
                }
            }
        }

        UpdateAllSlotDurability();
        CheckAutoEquip();

        return remainingAmount < amount;
    }

    public void UpdateAllSlotDurability()
    {
        RefreshSlotList();
        foreach (var slot in slots)
        {
            if (slot != null && slot.item != null && slot.item.itemType == ItemType.Weapon)
            {
                slot.ShowWeaponDurability();
            }
        }
    }

    public InventorySlotUI GetCurrentEquippedSlot()
    {
        if (hotbarGrid == null || currentEquippedIndex < 0) return null;

        InventorySlotUI[] hotbarSlots = hotbarGrid.GetComponentsInChildren<InventorySlotUI>(true);
        if (currentEquippedIndex < hotbarSlots.Length)
        {
            return hotbarSlots[currentEquippedIndex];
        }
        return null;
    }

    public void UpdateWeaponDurabilityUI(string weaponName, int currentDurability, int maxDurability)
    {
        RefreshSlotList();
        foreach (var slot in slots)
        {
            if (slot != null && slot.item != null && slot.item.itemType == ItemType.Weapon)
            {
                if (IsNameMatching(slot.item.itemName, weaponName))
                {
                    slot.UpdateDurabilityUI(currentDurability, maxDurability);
                }
            }
        }
    }

    public void RemoveWeaponFromInventory(string weaponName)
    {
        RefreshSlotList();
        foreach (var slot in slots)
        {
            if (slot != null && slot.item != null && slot.item.itemType == ItemType.Weapon)
            {
                if (IsNameMatching(slot.item.itemName, weaponName))
                {
                    slot.ClearSlot();
                    break;
                }
            }
        }
        HideAllWeaponsInHand();
    }

    private bool IsNameMatching(string name1, string name2)
    {
        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2)) return false;
        string clean1 = name1.ToLower().Replace("_data", "").Replace("data", "").Replace("_", "").Replace("01", "").Replace("05", "").Trim();
        string clean2 = name2.ToLower().Replace("_data", "").Replace("data", "").Replace("_", "").Replace("01", "").Replace("05", "").Trim();
        return clean1.Contains(clean2) || clean2.Contains(clean1);
    }

    public void TransferItemSlot(InventorySlotUI sourceSlot)
    {
        if (sourceSlot == null || sourceSlot.item == null) return;

        if (activeConsumeCoroutine != null)
        {
            FinishConsumeImmediately();
        }

        bool isFromHotbar = sourceSlot.transform.IsChildOf(hotbarGrid);
        Transform targetGrid = isFromHotbar ? itemGrid : hotbarGrid;

        if (targetGrid == null) return;

        InventorySlotUI[] targetSlots = targetGrid.GetComponentsInChildren<InventorySlotUI>(true);

        foreach (var targetSlot in targetSlots)
        {
            if (targetSlot.item == null || (targetSlot.icon != null && !targetSlot.icon.enabled))
            {
                ItemData tempItem = sourceSlot.item;
                int tempCount = sourceSlot.count;

                targetSlot.SetItem(tempItem, tempCount);
                sourceSlot.ClearSlot();
                HideAllWeaponsInHand();

                UpdateAllSlotDurability();
                return;
            }
        }
    }

    public void HideAllWeaponsInHand()
    {
        if (rightHand == null) return;
        foreach (Transform child in rightHand)
        {
            child.gameObject.SetActive(false);
        }
        currentEquippedIndex = -1;
    }

    public GameObject FindWeaponInHand(string itemName)
    {
        if (rightHand == null || string.IsNullOrEmpty(itemName)) return null;

        foreach (Transform child in rightHand)
        {
            WeaponData wData = child.GetComponent<WeaponData>();
            if (wData != null && IsNameMatching(wData.weaponName, itemName))
            {
                return child.gameObject;
            }

            if (IsNameMatching(child.name, itemName))
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private void CheckAutoEquip()
    {
        if (currentEquippedIndex == -1)
        {
            EquipWeaponFromHotbar(0);
        }
    }

    public void DropItem(InventorySlotUI slot)
    {
        if (slot == null || slot.item == null) return;

        if (activeConsumeCoroutine != null)
        {
            FinishConsumeImmediately();
        }

        ItemData itemToDrop = slot.item;
        int dropAmount = slot.count;

        if (itemToDrop.itemPrefab == null)
        {
            Debug.LogError($"<color=red>[DROP ERROR]</color> File ItemData '{itemToDrop.itemName}' chưa được kéo Prefab vào ô 'Item Prefab'!");
            return;
        }

        Transform referenceTransform = Camera.main != null ? Camera.main.transform : transform;
        Vector3 baseSpawnPosition = referenceTransform.position + referenceTransform.forward * 1.0f;

        RaycastHit hit;
        if (Physics.Raycast(referenceTransform.position, referenceTransform.forward, out hit, 1.2f))
        {
            baseSpawnPosition = hit.point - referenceTransform.forward * 0.2f;
        }

        for (int i = 0; i < dropAmount; i++)
        {
            Vector3 spawnOffset = new Vector3(
                Random.Range(-0.15f, 0.15f),
                Random.Range(0f, 0.1f),
                Random.Range(-0.15f, 0.15f)
            );

            Vector3 spawnPosition = baseSpawnPosition + spawnOffset;

            GameObject droppedObj = Instantiate(itemToDrop.itemPrefab, spawnPosition, Quaternion.identity);
            droppedObj.SetActive(true);

            WaterConsumable waterScript = droppedObj.GetComponent<WaterConsumable>();
            if (waterScript != null) Destroy(waterScript);

            FoodConsumable foodScript = droppedObj.GetComponent<FoodConsumable>();
            if (foodScript != null) Destroy(foodScript);

            DrinkAnimation drinkScript = droppedObj.GetComponent<DrinkAnimation>();
            if (drinkScript != null) Destroy(drinkScript);

            int itemLayer = LayerMask.NameToLayer("Item");
            foreach (Transform trans in droppedObj.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.tag = "Item";
                if (itemLayer != -1) trans.gameObject.layer = itemLayer;
            }

            Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
            if (rb == null) rb = droppedObj.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 randomDir = (referenceTransform.forward + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.1f, 0.3f), Random.Range(-0.2f, 0.2f))).normalized;
            rb.AddForce(randomDir * 0.8f, ForceMode.Impulse);

            Collider col = droppedObj.GetComponentInChildren<Collider>();
            if (col == null)
            {
                col = droppedObj.AddComponent<BoxCollider>();
            }
            col.enabled = true;
            col.isTrigger = false;

            if (col is MeshCollider meshCol)
            {
                meshCol.convex = true;
            }

            ItemPickup pickup = droppedObj.GetComponent<ItemPickup>();
            if (pickup == null) pickup = droppedObj.AddComponent<ItemPickup>();

            pickup.itemData = itemToDrop;
            pickup.amount = 1;
        }

        slot.ClearSlot();
        HideAllWeaponsInHand();
    }
}