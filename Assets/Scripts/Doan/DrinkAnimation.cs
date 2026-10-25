using System.Collections;
using UnityEngine;

public class DrinkAnimation : MonoBehaviour
{
    [Header("--- CẤU HÌNH GÓC VÀ TỐC ĐỘ XOAY ---")]
    [Tooltip("Thời gian đít chai xoay tới điểm đích")]
    public float attackDuration = 0.2f;

    [Tooltip("Góc xoay dừng (chỉnh góc âm/dương để đít chai dừng đúng vị trí)")]
    public float maxFlipAngle = -120f;

    [Tooltip("Thời gian giữ đít chai đứng yên ngửa lên (Hold) trước khi biến mất")]
    public float holdDuration = 1.2f;

    [Header("--- VỊ TRÍ TÂM XOAY (ĐẦU CHAI) ---")]
    [Tooltip("Khoảng cách từ gốc chai lên nắp chai")]
    public Vector3 pivotOffset = new Vector3(0f, 0.4f, 0f);

    [Header("--- HIỆU ỨNG NƯỚC BẮN (PARTICLE SYSTEM) ---")]
    [Tooltip("Kéo GameObject Particle System ở dưới làm con vào đây")]
    public ParticleSystem waterParticles;

    [Header("--- CẤU HÌNH ÂM THANH (AUDIO) ---")]
    public AudioSource audioSource;
    public AudioClip drinkSound;

    [Tooltip("Thời gian trễ (giây) trước khi phát âm thanh tính từ lúc bấm uống. Để 0 nếu muốn phát ngay.")]
    public float audioStartDelay = 0f;

    [Tooltip("Mốc thời gian BẮT ĐẦU phát trong file audio (giây). Ví dụ: 0.5s để bỏ qua đoạn đầu.")]
    public float audioStartTime = 0f;

    [Tooltip("Mốc thời gian KẾT THÚC phát trong file audio (giây). Để 0 nếu muốn phát đến hết file.")]
    public float audioEndTime = 0f;

    [Header("--- VỊ TRÍ & BÁN KÍNH VÒNG QUÉT SÁT THƯƠNG ---")]
    public Vector3 hitOffset = new Vector3(0f, 0.3f, -0.15f);
    public float hitRange = 0.5f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Coroutine audioRoutine;

    public bool isAnimating { get; private set; } = false;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (waterParticles == null)
        {
            waterParticles = GetComponentInChildren<ParticleSystem>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        ResetToOriginalPosition();
        StopWaterEffect();
        isAnimating = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopWaterEffect();
        ResetToOriginalPosition();
        isAnimating = false;
    }

    public void ResetToOriginalPosition()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }

    public void PlayWaterEffect()
    {
        if (waterParticles != null)
        {
            waterParticles.gameObject.SetActive(true);
            waterParticles.Play();
        }
    }

    public void StopWaterEffect()
    {
        if (waterParticles != null)
        {
            waterParticles.Stop();
            waterParticles.gameObject.SetActive(false);
        }

        StopDrinkSound();
    }

    #region AUDIO CONTROL
    public void PlayDrinkSound()
    {
        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(PlayAudioCustomRangeRoutine());
    }

    public void StopDrinkSound()
    {
        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
            audioRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator PlayAudioCustomRangeRoutine()
    {
        // 1. Chờ khoảng thời gian trễ (nếu có)
        if (audioStartDelay > 0f)
        {
            yield return new WaitForSeconds(audioStartDelay);
        }

        if (audioSource != null && drinkSound != null)
        {
            audioSource.clip = drinkSound;

            // 2. Tua đến mốc thời gian bắt đầu trong file âm thanh
            if (audioStartTime > 0f && audioStartTime < drinkSound.length)
            {
                audioSource.time = audioStartTime;
            }
            else
            {
                audioSource.time = 0f;
            }

            audioSource.Play();

            // 3. Tính độ dài cần phát và dừng khi chạm mốc audioEndTime
            float effectiveEndTime = (audioEndTime > audioStartTime && audioEndTime <= drinkSound.length)
                ? audioEndTime
                : drinkSound.length;

            float playDuration = effectiveEndTime - audioSource.time;

            if (playDuration > 0f)
            {
                yield return new WaitForSeconds(playDuration);
                audioSource.Stop();
            }
        }
    }
    #endregion

    public void PlayDrinkAnimation(System.Action onReachPeak, System.Action onFinishAll)
    {
        if (!isAnimating && gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateFlip(onReachPeak, onFinishAll));
        }
    }

    private IEnumerator AnimateFlip(System.Action onReachPeak, System.Action onFinishAll)
    {
        isAnimating = true;

        // Kích hoạt phát audio theo đoạn đã thiết lập
        PlayDrinkSound();

        Vector3 localPivot = originalPosition + (originalRotation * pivotOffset);

        // 1. Xoay đít chai ngửa lên điểm cực đại
        float elapsed = 0f;
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / attackDuration;

            float currentAngle = Mathf.Lerp(0f, maxFlipAngle, progress);
            ApplyLocalRotationAroundPivot(localPivot, currentAngle);

            yield return null;
        }

        // --- BẮT ĐẦU HOLD: BẬT TIA NƯỚC TRÀO RA & CẤP BUFF ---
        PlayWaterEffect();
        onReachPeak?.Invoke();

        // 2. GIỮ NGUYÊN ĐÍT CHAI NGỬA VÀ NƯỚC TRÀO TRONG THỜI GIAN HOLD
        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        // --- KẾT THÚC HOLD: TẮT TIA NƯỚC, TẮT ÂM THANH & HOÀN TẤT ---
        StopWaterEffect();
        isAnimating = false;
        ResetToOriginalPosition();

        onFinishAll?.Invoke();
    }

    private void ApplyLocalRotationAroundPivot(Vector3 localPivot, float angle)
    {
        Quaternion rot = originalRotation * Quaternion.Euler(angle, 0f, 0f);
        transform.localRotation = rot;
        transform.localPosition = localPivot - (rot * pivotOffset);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = transform.position + transform.TransformDirection(hitOffset);

        int segments = 30;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + transform.up * hitRange;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + (transform.up * Mathf.Cos(angle) + transform.forward * Mathf.Sin(angle)) * hitRange;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(pivotOffset), 0.03f);
    }
}