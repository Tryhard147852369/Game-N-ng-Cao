using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [Header("1. Cài đặt Sát thương Liên tục (Continuous)")]
    public float damage = 10f;             // Mức sát thương cho mỗi lần nhảy (tick)
    public string targetTag = "Player";

    [Tooltip("Khoảng thời gian (giây) giữa các lần trừ máu liên tục.")]
    public float damageInterval = 0.2f;

    private Collider col;
    private float lastDamageTime;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.enabled = false;
        }
    }

    private void OnDisable()
    {
        DisableHitbox();
    }

    public void EnableHitbox()
    {
        lastDamageTime = -damageInterval;
        if (col != null) col.enabled = true;
    }

    public void DisableHitbox()
    {
        if (col != null) col.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDealContinuousDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDealContinuousDamage(other);
    }

    private void TryDealContinuousDamage(Collider target)
    {
        if (target.CompareTag(targetTag))
        {
            if (Time.time >= lastDamageTime + damageInterval)
            {
                PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = target.GetComponent<PlayerHealth>();
                }

                if (playerHealth != null)
                {
                    lastDamageTime = Time.time;
                    playerHealth.TakeDamage(damage);
                    Debug.Log($"<color=red>[CREEP HITBOX]</color> Trừ {damage} máu Player!");
                }
            }
        }
    }
}