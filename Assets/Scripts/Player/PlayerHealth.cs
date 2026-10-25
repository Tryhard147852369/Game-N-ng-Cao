using UnityEngine;
using UnityEngine.UI; // Thêm thư viện UI

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("--- UI THANH MÁU ---")]
    public Slider healthSlider; // Kéo HealthBar (Slider) vào đây

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth >= maxHealth) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateHealthUI();
        Debug.Log($"<color=green>[HEAL]</color> Hồi {amount} máu! Hiện tại: {Mathf.CeilToInt(currentHealth)}/{maxHealth}");
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthUI();
        Debug.Log("Player bị đánh! Máu còn lại: " + Mathf.CeilToInt(currentHealth));

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        Debug.Log("Player đã chết!");
        gameObject.SetActive(false);
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
}