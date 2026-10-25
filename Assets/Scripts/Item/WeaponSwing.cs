using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwing : MonoBehaviour
{
    [Header("--- KẾT NỐI UI KHO ĐỒ ---")]
    public GameObject inventoryPanel;

    [Header("--- CẤU HÌNH GÓC CHÉM ---")]
    public Vector3 slashRotation = new Vector3(-15f, 60f, 0f);

    [Header("--- CẤU HÌNH TỐC ĐỘ & TẦM ĐÁNH ---")]
    public float attackDuration = 0.2f;
    public float attackCooldown = 0.05f;

    [Header("--- VỊ TRÍ & BÁN KÍNH VÒNG QUÉT SÁT THƯƠNG ---")]
    public Vector3 hitOffset = new Vector3(0f, 0f, 0.5f);
    public float hitRange = 0.5f;
    public float damage = 25f;

    public bool IsAttacking { get; private set; } = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        ResetPosition();
        IsAttacking = false;
    }

    private void Update()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf) return;

        // Click chuột trái để chém
        if (Input.GetMouseButtonDown(0) && !IsAttacking)
        {
            StartCoroutine(SwingRoutine());
        }
    }

    private IEnumerator SwingRoutine()
    {
        IsAttacking = true;

        // 1. QUÉT SÁT THƯƠNG NGAY KHI VUNG VŨ KHÍ
        CheckHitEnemies();

        Quaternion targetRotation = originalRotation * Quaternion.Euler(slashRotation);

        float elapsed = 0f;
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / attackDuration;
            transform.localRotation = Quaternion.Slerp(originalRotation, targetRotation, t);
            yield return null;
        }

        yield return new WaitForSeconds(attackCooldown);

        ResetPosition();
        IsAttacking = false;
    }

    private void CheckHitEnemies()
    {
        Vector3 center = transform.position + transform.TransformDirection(hitOffset);
        Collider[] hitColliders = Physics.OverlapSphere(center, hitRange, ~0, QueryTriggerInteraction.Collide);

        HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Player") || col.transform.IsChildOf(transform.root)) continue;

            // 1. Kiểm tra EnemyHealth (Dành cho Quái Creep1)
            EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = col.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null && !hitEnemies.Contains(enemyHealth.gameObject))
            {
                hitEnemies.Add(enemyHealth.gameObject);
                enemyHealth.TakeDamage(damage);
                continue;
            }

            // 2. Kiểm tra NewEnemyHealth (Dành cho Boss Base_mesh_Skin1)
            NewEnemyHealth newEnemyHealth = col.GetComponent<NewEnemyHealth>();
            if (newEnemyHealth == null) newEnemyHealth = col.GetComponentInParent<NewEnemyHealth>();

            if (newEnemyHealth != null && !hitEnemies.Contains(newEnemyHealth.gameObject))
            {
                hitEnemies.Add(newEnemyHealth.gameObject);
                newEnemyHealth.TakeDamage(damage); // Hàm TakeDamage này sẽ tự kiểm tra idle1/Rage để chặn sát thương
                continue;
            }
        }
    }

    public void ResetPosition()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.TransformDirection(hitOffset);
        Gizmos.DrawWireSphere(center, hitRange);
    }
}