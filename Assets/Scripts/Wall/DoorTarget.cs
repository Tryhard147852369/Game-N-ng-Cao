using UnityEngine;

public class DoorTarget : MonoBehaviour
{
    [Header("Mã chìa khóa cần thiết để mở cửa này")]
    public string requiredKeyID = "Key_01";

    public void OpenDoor()
    {
        // Cửa ẩn/biến mất
        gameObject.SetActive(false);
    }
}