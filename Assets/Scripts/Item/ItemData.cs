using UnityEngine;

public enum ItemType
{
    Consumable, // Đồ ăn, thuốc máu (xếp chồng nhiều cái)
    Weapon,     // Vũ khí, trang bị (chỉ 1 cái / 1 slot)
    Other       // Vật phẩm khác
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;

    [Header("Số lượng xếp chồng tối đa")]
    public int maxStack = 64;

    [Header("Prefab 3D vứt ra đất")]
    public GameObject itemPrefab; // Prefab 3D con dao / cái rìu vứt ra đất

    [Header("Vũ khí trên tay (Nếu itemType == Weapon)")]
    public GameObject weaponInHand; // Kéo Object con nằm trong RightHand vào đây
}