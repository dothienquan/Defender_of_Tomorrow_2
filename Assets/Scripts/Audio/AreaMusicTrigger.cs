using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AreaMusicTrigger : MonoBehaviour
{
    [Header("Area Settings")]
    [Tooltip("Nhạc nền của khu vực này")]
    [SerializeField] private AudioClip areaMusic;

    [Tooltip("Tên khu vực (để debug)")]
    [SerializeField] private string areaName = "Zone Name";

    private void Start()
    {
        // Tự động set trigger để tránh quên
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra đúng tag Player
        if (other.CompareTag("Player"))
        {
            // Kiểm tra Manager và AudioClip có tồn tại không
            if (BGMManager.Instance != null && areaMusic != null)
            {
                // Chỉ cần gọi hàm này. 
                // BGMManager sẽ tự kiểm tra: nếu nhạc này đang phát rồi thì thôi,
                // nếu chưa phát thì sẽ Fade Out bài cũ -> Fade In bài này.
                BGMManager.Instance.PlayMainBGM(areaMusic);
                
                Debug.Log($"[AreaMusic] Đã chuyển sang khu vực: {areaName}");
            }
        }
    }
}