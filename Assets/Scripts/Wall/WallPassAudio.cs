using System.Collections;
using UnityEngine;

public class WallPassAudio : MonoBehaviour
{
    [Header("--- CẤU HÌNH ÂM THANH (AUDIO) ---")]
    public AudioSource audioSource;
    public AudioClip passSound;

    [Tooltip("Thời gian trễ (giây) trước khi phát âm thanh khi đi xuyên qua tường.")]
    public float audioStartDelay = 0f;

    [Tooltip("Mốc thời gian BẮT ĐẦU phát trong file audio (giây). Để 0 nếu muốn phát từ đầu.")]
    public float audioStartTime = 0f;

    [Tooltip("Mốc thời gian KẾT THÚC phát trong file audio (giây). Để 0 nếu muốn phát đến hết file.")]
    public float audioEndTime = 0f;

    [Header("--- CẤU HÌNH TÍNH NĂNG ---")]
    [Tooltip("Tích chọn nếu chỉ muốn phát âm thanh 1 lần duy nhất trong cả trận game.")]
    public bool playOnlyOnce = false;

    private bool hasTriggered = false;
    private Coroutine audioRoutine;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng va chạm có phải là Player không
        if (other.CompareTag("Player"))
        {
            if (playOnlyOnce && hasTriggered) return;

            hasTriggered = true;
            PlayPassSound();
        }
    }

    #region AUDIO CONTROL
    public void PlayPassSound()
    {
        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(PlayAudioCustomRangeRoutine());
    }

    public void StopPassSound()
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
        // 1. Độ trễ phát âm thanh
        if (audioStartDelay > 0f)
        {
            yield return new WaitForSeconds(audioStartDelay);
        }

        if (audioSource != null && passSound != null)
        {
            audioSource.clip = passSound;

            // 2. Tua đến mốc thời gian bắt đầu trong file audio
            if (audioStartTime > 0f && audioStartTime < passSound.length)
            {
                audioSource.time = audioStartTime;
            }
            else
            {
                audioSource.time = 0f;
            }

            audioSource.Play();

            // 3. Tính độ dài phát và tự ngắt khi chạm mốc audioEndTime
            float effectiveEndTime = (audioEndTime > audioStartTime && audioEndTime <= passSound.length)
                ? audioEndTime
                : passSound.length;

            float playDuration = effectiveEndTime - audioSource.time;

            if (playDuration > 0f)
            {
                yield return new WaitForSeconds(playDuration);
                audioSource.Stop();
            }
        }
    }
    #endregion
}