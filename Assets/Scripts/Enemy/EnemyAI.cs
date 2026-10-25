using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Components")]
    private Animator animator;

    [Header("Stats & States")]
    public int maxHealth = 100;
    public int currentHealth;

    private bool isRaged = false;
    private bool isDead = false;
    private Coroutine randomMoveCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        // Bắt đầu vòng lặp di chuyển ngẫu nhiên ban đầu
        randomMoveCoroutine = StartCoroutine(RandomMovementRoutine());
    }

    // ==========================================
    // 1. NGẪU NHIÊN DI CHUYỂN BAN ĐẦU
    // ==========================================
    private IEnumerator RandomMovementRoutine()
    {
        while (!isRaged && !isDead)
        {
            // Random từ 0 đến 4 (0: Run, 1: walk1, 2: walkBack, 3: StrafeLeft, 4: StrafeRight)
            int randomMove = Random.Range(0, 5);
            animator.SetInteger("MovementState", randomMove);

            // Chờ từ 3 đến 5 giây để đảm bảo animation hiện tại chạy xong 100% rồi mới đổi sang dạng mới
            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }
    }

    // ==========================================
    // 2. KÍCH HOẠT RAGE (GỒNG / ĐỨNG YÊN)
    // ==========================================
    public void TriggerRageState()
    {
        if (isDead || isRaged) return;

        isRaged = true;

        // Dừng vòng lặp di chuyển ngẫu nhiên
        if (randomMoveCoroutine != null)
        {
            StopCoroutine(randomMoveCoroutine);
        }

        // Kích hoạt animation Rage
        animator.SetTrigger("Rage");
    }

    // ==========================================
    // 3. TẤN CÔNG NGẪU NHIÊN (1, 2, 3, 4)
    // ==========================================
    public void PerformRandomAttack()
    {
        if (isDead) return;

        // Random số nguyên từ 1 đến 4
        int randomAttackIndex = Random.Range(1, 5);

        animator.SetInteger("AttackIndex", randomAttackIndex);
        animator.SetTrigger("Attack");
    }

    // ==========================================
    // 4. NHẬN SÁT THƯƠNG & BỊ ĐÁNH NGẪU NHIÊN (1, 2, 3)
    // ==========================================
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        // Kiểm tra điều kiện chết
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Nếu chưa chết thì chạy animation bị đánh ngẫu nhiên (1, 2, 3)
        int randomHitIndex = Random.Range(1, 4);
        animator.SetInteger("GetHitIndex", randomHitIndex);
        animator.SetTrigger("GetHit");
    }

    // ==========================================
    // 5. CHẾT (CHỈ KÍCH HOẠT SAU KHI ĐÃ RAGE)
    // ==========================================
    public void Die()
    {
        // Điều kiện: Bắt buộc đã Rage mới được phép kích hoạt Animation Chết
        if (isRaged)
        {
            isDead = true;

            // Dừng mọi coroutine đang chạy
            StopAllCoroutines();

            // Kích hoạt trigger Die/Death
            animator.SetTrigger("Die");

            // Vô hiệu hóa Collider hoặc các component xử lý vật lý/AI nếu cần
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            this.enabled = false; // Tắt script này
            Debug.Log("Quái đã chết sau khi Rage!");
        }
        else
        {
            // Nếu chưa Rage mà hết máu, giữ quái ở 1 máu tối thiểu để đợi Rage
            currentHealth = 1;
            Debug.Log("Quái chưa gồng Rage nên chưa thể chết! Hãy kích hoạt Rage trước.");
        }
    }
}