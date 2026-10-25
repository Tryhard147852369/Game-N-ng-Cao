using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("--- CẤU HÌNH MÁU CREEP ---")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;

    [Header("--- TRẠNG THÁI BẤT TỬ ---")]
    [Tooltip("Bật true thủ công hoặc tự động kiểm tra qua Animator / Animation Event")]
    public bool isInvulnerable = false;

    private bool isDead = false;
    private Animator animator;

    private void Awake()
    {
        // Tự động tìm Animator trên quái hoặc các object con/cha
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponentInParent<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    private void OnEnable()
    {
        // Reset trạng thái khi Creep được bật lại (tái sinh / spawn lại)
        isDead = false;
        currentHealth = maxHealth;

        // Bật lại Collider khi tái sinh
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        // 1. TỰ ĐỘNG KIỂM TRA HOẠT ẢNH TRONG ANIMATOR (Sniff / Roar)
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Creep|Sniff_Action") || stateInfo.IsName("Creep|Roar_Action"))
            {
                Debug.Log("<color=yellow>[BẤT TỬ CREEP] Quái đang Sniff/Roar -> Bỏ qua sát thương!</color>");
                return;
            }
        }

        // 2. KIỂM TRA BIẾN isInvulnerable (Trường hợp bật thủ công bằng Code/Event)
        if (isInvulnerable)
        {
            Debug.Log("<color=yellow>[BẤT TỬ CREEP] isInvulnerable = true -> Bỏ qua sát thương!</color>");
            return;
        }

        currentHealth -= damageAmount;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"<color=green>[CREEP HIT]</color> Creep bị đánh! Máu còn: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        // Kích hoạt xử lý chết trên script EnemyFollow
        EnemyFollow followScript = GetComponent<EnemyFollow>();
        if (followScript == null) followScript = GetComponentInParent<EnemyFollow>();

        if (followScript != null)
        {
            followScript.TriggerDeath();
        }

        // Tắt toàn bộ Collider trên Creep để không cản đường Player khi đã chết
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }
}