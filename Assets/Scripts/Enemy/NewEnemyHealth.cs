using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NewEnemyHealth : MonoBehaviour
{
    [Header("--- MÁU CỦA BOSS (ĐỘC LẬP) ---")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("--- CẤU HÌNH PHASE 2 ---")]
    public bool hasPhaseTwo = true;

    [Tooltip("Kéo các con Creep ở Scene vào đây (Nhớ để Creep ngoài Hierarchy, không để trong Boss)")]
    public GameObject[] creepsInScene;

    [Tooltip("HOẶC Prefab Creep để sinh mới nếu danh sách trên trống")]
    public GameObject creepPrefab;
    public int creepSpawnAmount = 3;
    public float spawnRadius = 5f;

    private bool isPhaseTwo = false;
    private List<GameObject> spawnedCreeps = new List<GameObject>();

    [Header("--- UI THANH MÁU BOSS ---")]
    public Slider healthSlider;

    [Header("--- KHOẢNG CÁCH NHẬN DIỆN ---")]
    public Transform player;
    public float detectionRadius = 15f;

    [Header("--- TRẠNG THÁI BẤT TỬ THỦ CÔNG ---")]
    public bool isInvulnerable = false;

    [Header("--- CỬA MỞ KHI BOSS CHẾT ---")]
    public GameObject doorToDestroy;

    [Header("--- THAM CHIẾU COMPONENT ---")]
    public Animator animator;
    public NavMeshAgent agent;
    public EnemyCombat enemyCombat;

    [Header("--- CẤU HÌNH ÂM THANH (AUDIO) ---")]
    public AudioSource audioSource;
    public AudioClip healthBarAppearSound; // Âm thanh phát khi hiện thanh máu Boss

    [Header("--- VICTORY UI ---")]
    public CanvasGroup victoryCanvasGroup;
    public RectTransform victoryTextRect;
    public float delayBeforeShowVictory = 5.0f;
    public float textFadeDuration = 1.5f;

    private bool isDead = false;
    private bool hasTriggeredRage = false;

    void Start()
    {
        currentHealth = maxHealth;
        isPhaseTwo = false;
        isInvulnerable = false;

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (enemyCombat == null) enemyCombat = GetComponent<EnemyCombat>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        HideVictoryUI();

        // Ẩn toàn bộ Creep ở Phase 1
        if (creepsInScene != null && creepsInScene.Length > 0)
        {
            foreach (GameObject creep in creepsInScene)
            {
                if (creep != null) creep.SetActive(false);
            }
        }

        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
            UpdateHealthUI();
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool isPlayerInRadius = distance <= detectionRadius;

        if (isPlayerInRadius)
        {
            if (!hasTriggeredRage && animator != null)
            {
                hasTriggeredRage = true;
                animator.SetTrigger("Rage");
            }

            // KHI BẮT ĐẦU HIỆN THANH MÁU -> PHÁT ÂM THANH
            if (healthSlider != null && !healthSlider.gameObject.activeSelf)
            {
                healthSlider.gameObject.SetActive(true);
                UpdateHealthUI();

                PlayHealthBarAppearSound();
            }
        }
        else if (!hasTriggeredRage)
        {
            if (healthSlider != null && healthSlider.gameObject.activeSelf)
            {
                healthSlider.gameObject.SetActive(false);
            }
        }
    }

    public void PlayHealthBarAppearSound()
    {
        if (audioSource != null && healthBarAppearSound != null && !isDead)
        {
            audioSource.PlayOneShot(healthBarAppearSound);
        }
    }

    // =========================================================================
    // HÀM NHẬN SÁT THƯƠNG CHỈ TRỪ MÁU BOSS
    // =========================================================================
    public void TakeDamage(float amount)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthUI();

        Debug.Log($"<color=red>[BOSS TAKEDAMAGE]</color> Máu Boss còn: {currentHealth}/{maxHealth} (Phase {(isPhaseTwo ? 2 : 1)})");

        if (currentHealth <= 0)
        {
            if (hasPhaseTwo && !isPhaseTwo)
            {
                TriggerPhaseTwo();
            }
            else
            {
                Die();
            }
        }
    }

    void TriggerPhaseTwo()
    {
        isPhaseTwo = true;
        currentHealth = maxHealth; // Boss hồi đầy 100% máu ở Phase 2
        UpdateHealthUI();

        Debug.Log("<color=cyan>[BOSS PHASE 2]</color> Boss hồi đầy máu và gọi Creep xuất hiện!");

        if (animator != null)
        {
            animator.SetTrigger("Rage");
        }

        // Bật Creep trong Scene
        if (creepsInScene != null && creepsInScene.Length > 0)
        {
            foreach (GameObject creep in creepsInScene)
            {
                if (creep != null)
                {
                    creep.SetActive(true);
                    BuffCreepDetectionRange(creep);
                }
            }
        }
        else if (creepPrefab != null)
        {
            spawnedCreeps.Clear();
            for (int i = 0; i < creepSpawnAmount; i++)
            {
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * spawnRadius;
                randomOffset.y = 0;
                GameObject newCreep = Instantiate(creepPrefab, transform.position + randomOffset, Quaternion.identity);
                spawnedCreeps.Add(newCreep);
                BuffCreepDetectionRange(newCreep);
            }
        }
    }

    void BuffCreepDetectionRange(GameObject creepObj)
    {
        if (creepObj == null) return;

        var followScript = creepObj.GetComponent("EnemyFollow");
        if (followScript != null)
        {
            System.Type type = followScript.GetType();
            var f1 = type.GetField("detectAndChaseRadius");
            if (f1 != null) f1.SetValue(followScript, 1000f);

            var f2 = type.GetField("detectAndChaseRange");
            if (f2 != null) f2.SetValue(followScript, 1000f);
        }

        EnemyCombat combat = creepObj.GetComponent<EnemyCombat>();
        if (combat != null)
        {
            combat.detectRadius = 1000f;
        }
    }

    public void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    private void HideVictoryUI()
    {
        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.alpha = 0f;
            victoryCanvasGroup.interactable = false;
            victoryCanvasGroup.blocksRaycasts = false;
            victoryCanvasGroup.gameObject.SetActive(false);
        }

        if (victoryTextRect != null)
        {
            victoryTextRect.gameObject.SetActive(false);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;
        UpdateHealthUI();

        // Kéo toàn bộ Creep chết theo khi Boss cạn máu Phase 2
        KillAllCreeps();

        if (doorToDestroy != null) doorToDestroy.SetActive(false);
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        if (enemyCombat != null) enemyCombat.TriggerDeath();

        if (agent != null)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = false;

        StartCoroutine(ShowVictoryRoutine());
    }

    private void KillAllCreeps()
    {
        if (creepsInScene != null)
        {
            foreach (GameObject creep in creepsInScene)
            {
                if (creep != null && creep.activeInHierarchy)
                {
                    TriggerCreepDeath(creep);
                }
            }
        }

        if (spawnedCreeps != null)
        {
            foreach (GameObject creep in spawnedCreeps)
            {
                if (creep != null && creep.activeInHierarchy)
                {
                    TriggerCreepDeath(creep);
                }
            }
        }
    }

    private void TriggerCreepDeath(GameObject creepObj)
    {
        if (creepObj == null) return;

        NavMeshAgent creepAgent = creepObj.GetComponent<NavMeshAgent>();
        if (creepAgent != null)
        {
            if (creepAgent.isActiveAndEnabled && creepAgent.isOnNavMesh)
            {
                creepAgent.isStopped = true;
                creepAgent.velocity = Vector3.zero;
                creepAgent.ResetPath();
            }
            creepAgent.enabled = false;
        }

        MonoBehaviour[] scripts = creepObj.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != null && script != this)
            {
                script.enabled = false;
            }
        }

        Collider[] colliders = creepObj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Animator creepAnim = creepObj.GetComponentInChildren<Animator>();
        if (creepAnim != null)
        {
            creepAnim.SetBool("isChasing", false);
            creepAnim.SetBool("Attack", false);
            creepAnim.SetBool("IsSensing", false);

            creepAnim.SetTrigger("Death");
            creepAnim.Play("Creep|Death_Action", 0, 0f);
        }
    }

    private IEnumerator ShowVictoryRoutine()
    {
        yield return new WaitForSeconds(delayBeforeShowVictory);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.gameObject.SetActive(true);
            victoryCanvasGroup.alpha = 1f;
            victoryCanvasGroup.interactable = true;
            victoryCanvasGroup.blocksRaycasts = true;
        }

        if (victoryTextRect != null)
        {
            victoryTextRect.gameObject.SetActive(true);

            CanvasGroup textCanvasGroup = victoryTextRect.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null)
            {
                textCanvasGroup = victoryTextRect.gameObject.AddComponent<CanvasGroup>();
            }

            textCanvasGroup.alpha = 0f;
            Vector3 initialScale = Vector3.one * 0.1f;
            Vector3 targetScale = Vector3.one;
            victoryTextRect.localScale = initialScale;

            float timer = 0f;
            while (timer < textFadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / textFadeDuration;

                textCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                victoryTextRect.localScale = Vector3.Lerp(initialScale, targetScale, progress);

                yield return null;
            }

            textCanvasGroup.alpha = 1f;
            victoryTextRect.localScale = targetScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}