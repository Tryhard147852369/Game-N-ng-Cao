using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 8f;
    public LayerMask enemyLayer;

    [Header("--- KẾT NỐI CONTROLLER & UI ---")]
    public HotbarWeaponController hotbarController;
    [Tooltip("Kéo InventoryPanel từ Canvas vào đây để khóa tấn công khi mở B")]
    public GameObject inventoryPanel;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        // Cập nhật thanh độ bền dưới các ô Slot UI
        UpdateAllSlotsDurabilityUI();

        // 1. Kiểm tra nếu Bảng kho đồ (B) đang BẬT -> KHÔNG cho tấn công
        if (inventoryPanel != null && inventoryPanel.activeSelf) return;

        // 2. Kiểm tra nếu chuột đang bấm lên UI / Ô Slot -> KHÔNG cho tấn công
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    void Attack()
    {
        if (mainCam == null) return;

        WeaponData activeWeapon = GetActiveWeaponData();
        int currentDamage = (activeWeapon != null) ? activeWeapon.damage : 0;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, 1f);

        // Biến đánh dấu xem cú chém này có trúng quái hay không
        bool hitEnemy = false;

        if (Physics.Raycast(ray, out hit, attackRange, enemyLayer))
        {
            Debug.Log("Đã bắn trúng vật thể: " + hit.collider.name);

            // 1. Kiểm tra NewEnemyHealth
            NewEnemyHealth newEnemyHealth = hit.collider.GetComponentInParent<NewEnemyHealth>();
            if (newEnemyHealth == null)
            {
                newEnemyHealth = hit.collider.GetComponent<NewEnemyHealth>();
            }

            if (newEnemyHealth != null)
            {
                if (currentDamage > 0) newEnemyHealth.TakeDamage(currentDamage);
                hitEnemy = true; // Xác nhận ĐÃ ĐÁNH TRÚNG QUÁI
                Debug.Log($"<color=green>ĐÃ ĐÁNH TRÚNG QUÁI (NewEnemyHealth)!</color> Gây {currentDamage} damage.");
            }
            else
            {
                // 2. Kiểm tra EnemyHealth
                EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null)
                {
                    enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                }

                if (enemyHealth != null)
                {
                    if (currentDamage > 0) enemyHealth.TakeDamage(currentDamage);
                    hitEnemy = true; // Xác nhận ĐÃ ĐÁNH TRÚNG QUÁI
                    Debug.Log($"<color=green>ĐÃ ĐÁNH TRÚNG QUÁI (EnemyHealth)!</color> Gây {currentDamage} damage.");
                }
            }
        }

        // --- XỬ LÝ TRỪ ĐỘ BỀN ---
        // CHỈ TRỪ ĐỘ BỀN KHI: Có cầm vũ khí VÀ ĐÃ ĐÁNH TRÚNG QUÁI (hitEnemy == true)
        if (activeWeapon != null && hitEnemy)
        {
            activeWeapon.currentDurability--;

            // Cập nhật UI ngay lập tức
            UpdateAllSlotsDurabilityUI();

            // Khi hết độ bền (<= 0) -> Vũ khí bị gãy
            if (activeWeapon.currentDurability <= 0)
            {
                Debug.Log($"<color=red>Vũ khí {activeWeapon.weaponName} đã bị gãy!</color>");
                activeWeapon.ResetDurability();
                activeWeapon.gameObject.SetActive(false);

                // Xóa icon vũ khí khỏi ô Slot UI
                if (hotbarController != null)
                {
                    hotbarController.ClearCurrentSlot();
                }
            }
        }
    }

    private WeaponData GetActiveWeaponData()
    {
        if (hotbarController == null || hotbarController.weapon3DModels == null) return null;

        foreach (GameObject weaponObj in hotbarController.weapon3DModels)
        {
            if (weaponObj != null && weaponObj.activeSelf)
            {
                return weaponObj.GetComponent<WeaponData>();
            }
        }
        return null;
    }

    private void UpdateAllSlotsDurabilityUI()
    {
        if (hotbarController == null || hotbarController.hotbarSlots == null) return;

        for (int i = 0; i < hotbarController.hotbarSlots.Length; i++)
        {
            InventorySlotUI slot = hotbarController.hotbarSlots[i];
            if (slot != null && slot.item != null)
            {
                WeaponData wData = GetWeaponDataByName(slot.item.name);
                if (wData != null)
                {
                    slot.UpdateDurabilityUI(wData.currentDurability, wData.maxDurability);
                }
                else
                {
                    slot.UpdateDurabilityUI(0, 0);
                }
            }
            else if (slot != null)
            {
                slot.UpdateDurabilityUI(0, 0);
            }
        }
    }

    private WeaponData GetWeaponDataByName(string itemName)
    {
        if (hotbarController == null || hotbarController.weapon3DModels == null) return null;

        string cleanItemName = itemName.Replace("_Data", "").Replace("_Item", "").Trim();

        foreach (GameObject weaponObj in hotbarController.weapon3DModels)
        {
            if (weaponObj == null) continue;

            string cleanWeaponName = weaponObj.name.Replace("_Data", "").Replace("_Item", "").Trim();

            if (weaponObj.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) ||
                cleanWeaponName.StartsWith(cleanItemName, System.StringComparison.OrdinalIgnoreCase) ||
                cleanItemName.StartsWith(cleanWeaponName, System.StringComparison.OrdinalIgnoreCase))
            {
                return weaponObj.GetComponent<WeaponData>();
            }
        }
        return null;
    }

    // ==========================================
    // HIỂN THỊ PHẠM VI ĐÁNH TRONG CỬA SỔ SCENE
    // ==========================================
    // ==========================================
    // HIỂN THỊ CHỈ 1 ĐƯỜNG TẦM ĐÁNH THẲNG (HƯỚNG Y/FORWARD)
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>();

        if (cam != null)
        {
            // Chỉ vẽ đúng 1 đường thẳng chỉ tầm đánh từ Camera ra trước
            Gizmos.color = Color.red;
            Gizmos.DrawRay(cam.transform.position, cam.transform.forward * attackRange);
        }
    }
}