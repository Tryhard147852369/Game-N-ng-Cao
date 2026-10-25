using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FoodConsumable : MonoBehaviour
{
    [Header("--- KẾT NỐI UI KHO ĐỒ ---")]
    public GameObject inventoryPanel;

    [Header("--- CẤU HÌNH HỒI MÁU ---")]
    public float healAmount = 10f;
    public float eatDuration = 2.0f;

    [Header("--- HIỆU ỨNG VỤN THỨC ĂN (PARTICLE SYSTEM) ---")]
    public ParticleSystem foodParticles;

    [Header("--- CẤU HÌNH ÂM THANH (AUDIO) ---")]
    public AudioSource audioSource;
    public AudioClip eatSound;

    [Tooltip("Thời gian trễ (giây) trước khi phát âm thanh tính từ lúc bấm ăn. Để 0 nếu muốn phát ngay.")]
    public float audioStartDelay = 0f;

    [Tooltip("Mốc thời gian BẮT ĐẦU phát trong file audio (giây). Để 0 nếu muốn phát từ đầu.")]
    public float audioStartTime = 0f;

    [Tooltip("Mốc thời gian KẾT THÚC phát trong file audio (giây). Để 0 nếu muốn phát đến hết file.")]
    public float audioEndTime = 0f;

    [Header("--- KẾT NỐI HỆ THỐNG ---")]
    public PlayerHealth playerHealth;
    public DrinkAnimation drinkAnimation;

    public bool IsEating { get; private set; } = false;

    private Coroutine eatAudioRoutine;

    private void Start()
    {
        if (drinkAnimation == null)
            drinkAnimation = GetComponent<DrinkAnimation>();

        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (foodParticles == null)
            foodParticles = GetComponentInChildren<ParticleSystem>(true);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        IsEating = false;
        StopFoodEffect();
        StopEatSound();

        if (drinkAnimation != null)
        {
            drinkAnimation.ResetToOriginalPosition();
            drinkAnimation.StopWaterEffect();
        }
    }

    private void Update()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(1) && !IsEating)
        {
            EatFood();
        }
    }

    public void EatFood()
    {
        if (IsEating) return;
        IsEating = true;

        // KÍCH HOẠT PHÁT ÂM THANH ĂN UỐNG
        PlayEatSound();

        InventorySlotUI currentSlot = null;
        if (InventoryManager.Instance != null)
        {
            currentSlot = InventoryManager.Instance.GetCurrentEquippedSlot();
        }

        // 1. ĐĂNG KÝ TIẾN TRÌNH ĂN VỚI INVENTORYMANAGER
        if (InventoryManager.Instance != null && currentSlot != null)
        {
            InventoryManager.Instance.StartConsumeItem(currentSlot, eatDuration, () =>
            {
                ApplyHealOnly();
                StopFoodEffect();
                StopEatSound();
                IsEating = false;
            });
        }

        // 2. CHẠY HOẠT ẢNH TRÊN TAY
        if (drinkAnimation != null)
        {
            drinkAnimation.holdDuration = eatDuration;
            drinkAnimation.PlayDrinkAnimation(
                onReachPeak: () => { PlayFoodEffect(); },
                onFinishAll: () =>
                {
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.FinishConsumeImmediately();
                    }
                    else
                    {
                        ApplyHealAndConsumeLocal();
                    }
                }
            );
        }
        else
        {
            StartCoroutine(EatRoutineWithoutAnimation());
        }
    }

    private IEnumerator EatRoutineWithoutAnimation()
    {
        PlayFoodEffect();
        yield return new WaitForSeconds(eatDuration);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.FinishConsumeImmediately();
        }
        else
        {
            ApplyHealAndConsumeLocal();
        }
    }

    #region AUDIO CONTROL
    public void PlayEatSound()
    {
        if (eatAudioRoutine != null) StopCoroutine(eatAudioRoutine);
        eatAudioRoutine = StartCoroutine(PlayAudioCustomRangeRoutine());
    }

    public void StopEatSound()
    {
        if (eatAudioRoutine != null)
        {
            StopCoroutine(eatAudioRoutine);
            eatAudioRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator PlayAudioCustomRangeRoutine()
    {
        // 1. Độ trễ phát âm thanh
        if (audioStartDelay > 0f)
        {
            yield return new WaitForSeconds(audioStartDelay);
        }

        if (audioSource != null && eatSound != null)
        {
            audioSource.clip = eatSound;

            // 2. Tua đến vị trí bắt đầu mong muốn trong file
            if (audioStartTime > 0f && audioStartTime < eatSound.length)
            {
                audioSource.time = audioStartTime;
            }
            else
            {
                audioSource.time = 0f;
            }

            audioSource.Play();

            // 3. Tính độ dài phát và tự ngắt khi chạm mốc audioEndTime
            float effectiveEndTime = (audioEndTime > audioStartTime && audioEndTime <= eatSound.length)
                ? audioEndTime
                : eatSound.length;

            float playDuration = effectiveEndTime - audioSource.time;

            if (playDuration > 0f)
            {
                yield return new WaitForSeconds(playDuration);
                audioSource.Stop();
            }
        }
    }
    #endregion

    private void ApplyHealOnly()
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }
    }

    private void ApplyHealAndConsumeLocal()
    {
        ApplyHealOnly();
        StopFoodEffect();
        StopEatSound();
        IsEating = false;

        if (InventoryManager.Instance != null)
        {
            InventorySlotUI currentSlot = InventoryManager.Instance.GetCurrentEquippedSlot();
            if (currentSlot != null)
            {
                currentSlot.DeductCount(1);
            }
        }
    }

    private void OnDisable()
    {
        StopFoodEffect();
        StopEatSound();
        IsEating = false;
    }

    private void PlayFoodEffect()
    {
        if (foodParticles != null)
        {
            foodParticles.gameObject.SetActive(true);
            foodParticles.Play();
        }
    }

    private void StopFoodEffect()
    {
        if (foodParticles != null)
        {
            foodParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}