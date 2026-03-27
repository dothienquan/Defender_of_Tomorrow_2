using UnityEngine;
using DG.Tweening; // Cần thư viện này

[RequireComponent(typeof(Collider2D))]
public class ProximityPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Kéo Panel UI bạn muốn hiện vào đây")]
    [SerializeField] private GameObject uiPanel;
    
    [Tooltip("Thời gian hiệu ứng (giây)")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("Tag của người chơi")]
    [SerializeField] private string playerTag = "Player";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Đảm bảo Trigger được bật
        GetComponent<Collider2D>().isTrigger = true;

        if (uiPanel != null)
        {
            // Tự động thêm CanvasGroup nếu chưa có (để chỉnh Alpha)
            canvasGroup = uiPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = uiPanel.AddComponent<CanvasGroup>();
            }

            // Ẩn panel ngay từ đầu
            HidePanelImmediate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            ShowPanel();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            HidePanel();
        }
    }

    private void ShowPanel()
    {
        if (uiPanel == null) return;

        // 1. Ngắt tween cũ (nếu đang chạy dở)
        canvasGroup.DOKill();

        // 2. Bật GameObject lên trước
        uiPanel.SetActive(true);

        // 3. Fade In (Alpha -> 1)
        canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        
        // 4. Cho phép tương tác chuột (nếu có nút bấm)
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        if (uiPanel == null) return;

        // 1. Ngắt tween cũ
        canvasGroup.DOKill();

        // 2. Tắt tương tác ngay lập tức để không bấm nhầm lúc đang fade
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 3. Fade Out (Alpha -> 0)
        canvasGroup.DOFade(0f, fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => 
            {
                // 4. Chỉ tắt GameObject khi đã mờ hẳn (để tối ưu game)
                uiPanel.SetActive(false);
            });
    }

    private void HidePanelImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        uiPanel.SetActive(false);
    }
}