using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [Header("Chìa khóa yêu cầu (trên tay Player)")]
    public GameObject requiredKeyInHand;

    private void OnMouseDown()
    {
        // Kiểm tra xem chìa khóa trên tay đã được bật (đã nhặt) chưa
        if (requiredKeyInHand != null && requiredKeyInHand.activeSelf)
        {
            // Ẩn cửa đi khi dùng chìa khóa mở
            gameObject.SetActive(false);
        }
    }
}