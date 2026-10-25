using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyCombat : MonoBehaviour
{
    [Header("Tham chiếu")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;

    [Header("Cấu hình Phạm vi")]
    public float detectRadius = 10f;
    public float loseRadiusMultiplier = 1.3f;
    public float attackRange = 3.5f;

    [Header("Cấu hình Tốc độ")]
    public float patrolSpeed = 2.0f;
    public float runSpeed = 4.5f;

    [Header("Cấu hình Damage & Hitbox Tấn công")]
    public float attackDamage = 20f;
    public float damageInterval = 0.5f; // Thời gian (giây) giữa các lần gây sát thương liên tục
    public Collider attackHitbox;
    private float nextDamageTime = 0f;

    [Header("Cấu hình Âm thanh (Audio)")]
    public AudioSource audioSource;
    public AudioClip rageSound;

    [Header("Cấu hình Gizmos")]
    public bool showGizmosAlways = true;
    public Color detectRangeColor = new Color(1f, 0.92f, 0.016f, 1f);
    public Color loseRangeColor = new Color(1f, 0.6f, 0f, 0.8f);
    public Color attackRangeColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float gizmoYOffset = 0.05f;

    private NewEnemyHealth healthScript;

    private bool hasTriggeredRageOnce = false;
    private bool isDead = false;
    private bool isCombatActive = false;
    private bool isPlayerInZone = false;

    public bool IsCombatActive => isCombatActive;

    #region AUDIO CONTROL
    public void PlayRageSound()
    {
        if (audioSource != null && rageSound != null && !isDead)
        {
            audioSource.clip = rageSound;
            audioSource.Play();
        }
    }

    public void StopRageSound()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    #endregion

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        healthScript = GetComponent<NewEnemyHealth>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Đảm bảo Hitbox luôn tắt lúc bắt đầu
        if (attackHitbox != null) attackHitbox.enabled = false;

        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 0.5f;
        }

        StartCoroutine(MainStateController());
    }

    void Update()
    {
        if (isDead) return;
        CheckPlayerDistance();
    }

    // Kiểm tra xem Animator có đang thực sự ở trạng thái tấn công (attack1 hoặc attack2) hay không
    public bool IsAttacking()
    {
        if (animator == null) return false;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName("attack1") || info.IsName("attack2");
    }

    // ================= 1. LUỒNG ĐIỀU KHIỂN CHÍNH =================
    IEnumerator MainStateController()
    {
        while (!isDead)
        {
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                FreezeAgentOnce();
                yield return null;
                continue;
            }

            if (!isPlayerInZone)
            {
                isCombatActive = false;
                yield return StartCoroutine(PatrolOutsideRoutine());
            }
            else
            {
                yield return StartCoroutine(FullCombatSequence());
            }

            yield return null;
        }
    }

    // ================= 2. TUẦN TRA =================
    IEnumerator PatrolOutsideRoutine()
    {
        SetMovementState(1); // Walk1
        if (animator != null) animator.SetBool("isRunning", false);

        UnfreezeAgent(patrolSpeed);

        Vector3 patrolPoint = GetOptimalPatrolPoint();
        SafeSetDestination(patrolPoint);

        while (!isPlayerInZone && !isDead && player != null)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.pathPending)
            {
                if (agent.remainingDistance <= 0.4f || agent.velocity.sqrMagnitude < 0.05f)
                {
                    SetMovementState(0); // Idle1
                    yield return new WaitForSeconds(1.5f);

                    if (isPlayerInZone || isDead) break;

                    SetMovementState(1);
                    patrolPoint = GetOptimalPatrolPoint();
                    SafeSetDestination(patrolPoint);
                }
            }

            yield return null;
        }
    }

    // ================= 3. CHU KỲ COMBAT =================
    IEnumerator FullCombatSequence()
    {
        isCombatActive = true;

        if (!hasTriggeredRageOnce)
        {
            hasTriggeredRageOnce = true;
            FreezeAgentOnce();

            if (animator != null)
            {
                animator.SetBool("isRunning", false);
                SetMovementState(0);
                animator.SetTrigger("RageTrigger");
            }

            // 1. Phát âm thanh gầm (Rage/Roar)
            PlayRageSound();

            yield return null;
            if (animator != null) animator.ResetTrigger("RageTrigger");

            // Chờ chạy HẾT hoạt ảnh Rage
            yield return StartCoroutine(WaitRageComplete("Rage"));

            // 2. Ngắt âm thanh ngay khi hết hoạt ảnh Rage
            StopRageSound();

            if (!isPlayerInZone || isDead)
            {
                isCombatActive = false;
                yield break;
            }
        }

        while (isPlayerInZone && player != null && !isDead)
        {
            float currentDistance = Vector3.Distance(transform.position, player.position);

            if (currentDistance > attackRange)
            {
                bool isRun = Random.value > 0.5f;

                UnfreezeAgent(isRun ? runSpeed : patrolSpeed);

                if (animator != null)
                {
                    SetMovementState(1);
                    animator.SetBool("isRunning", isRun);
                }

                float approachTimer = 0f;
                while (Vector3.Distance(transform.position, player.position) > attackRange && isPlayerInZone && !isDead)
                {
                    SafeSetDestination(player.position);
                    approachTimer += Time.deltaTime;
                    if (approachTimer > 3.0f) break;
                    yield return null;
                }

                if (!isPlayerInZone || isDead) break;
            }

            FreezeAgentOnce();
            SmoothLookAtPlayer(15f);

            int attackIndex = Random.Range(1, 3);
            string attackStateName = "attack" + attackIndex;

            if (animator != null)
            {
                animator.SetBool("isRunning", false);
                SetMovementState(0);
                animator.SetInteger("AttackIndex", attackIndex);
                animator.SetTrigger("Attack");
            }

            // Chờ hoạt ảnh bắt đầu -> Bật Hitbox -> Chờ hoạt ảnh kết thúc -> Tắt Hitbox
            yield return StartCoroutine(WaitAndInstantlyTransition(attackStateName));

            yield return new WaitForSeconds(0.2f);
        }

        isCombatActive = false;
    }

    // ================= 4. XỬ LÝ KHI BỊ ĐÁNH =================
    public void OnGetHit()
    {
        if (isDead) return;

        if (!isPlayerInZone)
        {
            isPlayerInZone = true;
        }
    }

    // ================= 5. GÂY SÁT THƯƠNG LIÊN TỤC KHI ĐANG TRONG HOẠT ẢNH ATTACK =================
    private void OnTriggerStay(Collider other)
    {
        if (attackHitbox != null && attackHitbox.enabled && IsAttacking() && other.CompareTag("Player"))
        {
            if (Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + damageInterval;

                var playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"<color=red>[BOSS HITBOX]</color> Trừ {attackDamage} máu! (Mốc thời gian: {Time.time:F2}s)");
                }
            }
        }
    }

    // ================= 6. HÀM XỬ LÝ CHẾT =================
    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;

        // Dừng âm thanh gầm ngay lập tức nếu chết giữa chừng
        StopRageSound();

        if (healthScript != null) healthScript.isInvulnerable = false;

        StopAllCoroutines();

        if (attackHitbox != null) attackHitbox.enabled = false;

        if (agent != null)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetBool("isRunning", false);
            animator.SetInteger("MovementState", 0);
            animator.SetInteger("AttackIndex", 0);
            animator.SetInteger("GetHitIndex", 0);

            animator.ResetTrigger("Attack");
            animator.ResetTrigger("GetHit");
            animator.ResetTrigger("RageTrigger");

            animator.SetTrigger("Die");
            animator.Play("Death2", 0, 0f);
        }

        this.enabled = false;
    }

    private IEnumerator WaitRageComplete(string stateName, int layer = 0)
    {
        if (animator == null) yield break;

        yield return null;

        float timeout = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName) && timeout < 0.5f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        timeout = 0f;
        while (timeout < 4.0f)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
            if (!info.IsName(stateName) || info.normalizedTime >= 0.98f)
            {
                break;
            }
            timeout += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitAndInstantlyTransition(string stateName, int layer = 0)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) yield break;

        yield return null;

        float timeout = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName) && timeout < 0.4f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        if (attackHitbox != null && !isDead) attackHitbox.enabled = true;

        timeout = 0f;
        while (timeout < 3.0f)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
            if (!info.IsName(stateName) || info.normalizedTime >= 0.95f)
            {
                break;
            }
            timeout += Time.deltaTime;
            yield return null;
        }

        if (attackHitbox != null) attackHitbox.enabled = false;
    }

    void CheckPlayerDistance()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            if (isPlayerInZone)
            {
                isPlayerInZone = false;
                hasTriggeredRageOnce = false;
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isPlayerInZone && distance <= detectRadius)
        {
            isPlayerInZone = true;
        }
        else if (isPlayerInZone && distance > detectRadius * loseRadiusMultiplier)
        {
            isPlayerInZone = false;
            hasTriggeredRageOnce = false;
        }
    }

    Vector3 GetOptimalPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 12f;
        randomDirection += transform.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, 12f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position;
    }

    public void SmoothLookAtPlayer(float turnSpeed)
    {
        if (player == null) return;
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
    }

    void SetMovementState(int state)
    {
        if (animator != null) animator.SetInteger("MovementState", state);
    }

    void FreezeAgentOnce()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (agent.hasPath) agent.ResetPath();
        }
    }

    void UnfreezeAgent(float moveSpeed)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = moveSpeed;
        }
    }

    void SafeSetDestination(Vector3 destination)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    // ================= GIZMOS =================
    private void OnDrawGizmos()
    {
        if (showGizmosAlways) DrawGroundRanges();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmosAlways) DrawGroundRanges();
    }

    private void DrawGroundRanges()
    {
        Vector3 groundCenter = transform.position + Vector3.up * gizmoYOffset;

        Gizmos.color = detectRangeColor;
        DrawWireDisk(groundCenter, detectRadius);

        Gizmos.color = loseRangeColor;
        DrawWireDisk(groundCenter, detectRadius * loseRadiusMultiplier);

        Gizmos.color = attackRangeColor;
        DrawWireDisk(groundCenter, attackRange);
    }

    private void DrawWireDisk(Vector3 center, float radius)
    {
        int segments = 48;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    // ================= HÀM HỖ TRỢ BẬT/TẮT BẰNG ANIMATION EVENT (NẾU DÙNG) =================
    public void EnableAttackHitbox()
    {
        if (attackHitbox != null && !isDead) attackHitbox.enabled = true;
    }

    public void DisableAttackHitbox()
    {
        if (attackHitbox != null) attackHitbox.enabled = false;
    }
}