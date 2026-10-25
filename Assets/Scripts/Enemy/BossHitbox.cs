using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    [Header("Cấu hình Sát thương")]
    public float damage = 30f;
    public string targetTag = "Player";

    [Header("Tốc độ gây sát thương liên tục")]
    [Tooltip("Khoảng thời gian (giây) giữa các lần trừ máu khi Player ở trong hoặc chạm rìa Hitbox")]
    public float damageInterval = 0.4f;

    private float nextDamageTime = 0f;

    // Reset mốc thời gian ngay khi Hitbox được bật lên theo hoạt ảnh tấn công
    private void OnEnable()
    {
        nextDamageTime = 0f;
    }

    // 1. Khi Player vừa va chạm/chạm vào rìa Hitbox
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag) && Time.time >= nextDamageTime)
        {
            TryApplyDamage(other);
        }
    }

    // 2. Khi Player ĐÃ ĐỨNG SẴN ở trong hoặc phần rìa Hitbox lúc hoạt ảnh đang chạy
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(targetTag) && Time.time >= nextDamageTime)
        {
            TryApplyDamage(other);
        }
    }

    private void TryApplyDamage(Collider targetCollider)
    {
        // Tìm component PlayerHealth ở cả Object va chạm hoặc Object cha (tránh bị sót Collider con của Player)
        PlayerHealth playerHealth = targetCollider.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = targetCollider.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            nextDamageTime = Time.time + damageInterval; // Đặt mốc thời gian trừ máu tiếp theo
            Debug.Log($"<color=green>[BOSS HITBOX]</color> Đánh trúng Player trong/rìa Hitbox! Gây {damage} sát thương.");
        }
    }
}