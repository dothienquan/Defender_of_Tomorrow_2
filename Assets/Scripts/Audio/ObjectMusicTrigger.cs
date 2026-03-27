using UnityEngine;

public class ObjectMusicTrigger : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("Nhạc sẽ phát khi object này xuất hiện")]
    [SerializeField] private AudioClip overrideMusic;

    [Tooltip("Thời gian delay trước khi nhạc nổi lên (tùy chọn)")]
    [SerializeField] private float delayStart = 0f;

    private void OnEnable()
    {
        if (overrideMusic != null && BGMManager.Instance != null)
        {
            if (delayStart > 0)
            {
                Invoke(nameof(TriggerMusic), delayStart);
            }
            else
            {
                TriggerMusic();
            }
        }
    }

    private void TriggerMusic()
    {
        BGMManager.Instance.StartOverrideMusic(overrideMusic);
    }

    private void OnDisable()
    {
        // Hủy lệnh invoke nếu object bị tắt quá nhanh trước khi nhạc kịp chạy
        CancelInvoke(nameof(TriggerMusic));

        if (BGMManager.Instance != null)
        {
            // Trả lại nhạc nền chính
            BGMManager.Instance.StopOverrideMusic();
        }
    }

    private void OnDestroy()
    {
        // Đảm bảo trả lại nhạc nếu object bị Destroy
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.StopOverrideMusic();
        }
    }
}