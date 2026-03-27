using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemPickupUIController : MonoBehaviour
{
    public static ItemPickupUIController Instance { get; private set; }

    [Header("Popup Settings")]
    [Tooltip("Prefab cho popup item")]
    public GameObject popupPrefab;
    
    [Tooltip("Số lượng popup tối đa hiển thị cùng lúc")]
    public int maxPopup = 5;
    
    [Tooltip("Thời gian hiển thị popup trước khi fade out (giây)")]
    public float popupDuration = 3f;

    [Header("Animation Settings")]
    [Tooltip("Thời gian di chuyển từ bottom lên top (giây)")]
    [SerializeField] private float moveUpDuration = 0.5f;
    
    [Tooltip("Khoảng cách di chuyển lên (pixels). Nếu = 0 sẽ tự động tính dựa trên container height")]
    [SerializeField] private float moveUpDistance = 0f;
    
    [Tooltip("Thời gian fade out (giây)")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    private readonly Queue<GameObject> activePopups = new();
    private RectTransform containerRectTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        // Cache container RectTransform
        containerRectTransform = GetComponent<RectTransform>();
        if (containerRectTransform == null)
        {
            Debug.LogError("[ItemPickupUIController] Container does not have RectTransform component!");
        }
    }

    public void ShowItemPopup(string itemName, Sprite gameIcon)
    {
        GameObject newPopup = Instantiate(popupPrefab, transform);
        newPopup.GetComponentInChildren<TMP_Text>().text = itemName;

        Image itemImage = newPopup.transform.Find("ItemIcon")?.GetComponent<Image>();

        if (itemImage)
        {
            itemImage.sprite = gameIcon;
        }

        activePopups.Enqueue(newPopup); 

        if(activePopups.Count > maxPopup)
        {
            GameObject oldPopup = activePopups.Dequeue();
            if (oldPopup != null)
            {
                // Kill tween nếu có
                oldPopup.transform.DOKill();
                Destroy(oldPopup);
            }
        }

        // Setup và animate popup
        SetupAndAnimatePopup(newPopup);
    }

    /// <summary>
    /// Setup popup ban đầu ở bottom và animate lên top rồi fade out
    /// </summary>
    private void SetupAndAnimatePopup(GameObject popup)
    {
        if (popup == null) return;

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        if (popupRect == null)
        {
            Debug.LogError("[ItemPickupUIController] Popup does not have RectTransform component!");
            return;
        }

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.AddComponent<CanvasGroup>();
        }

        // Tính toán vị trí bottom và top
        float containerHeight = containerRectTransform != null ? containerRectTransform.rect.height : 500f;
        float popupHeight = popupRect.rect.height;
        
        // Vị trí bottom: popup nằm dưới container (ẩn)
        float bottomY = -(containerHeight * 0.5f + popupHeight * 0.5f);
        
        // Vị trí top: popup ở top của container
        float topY = containerHeight * 0.5f - popupHeight * 0.5f;
        
        // Nếu có moveUpDistance được set, sử dụng nó
        if (moveUpDistance > 0f)
        {
            topY = bottomY + moveUpDistance;
        }

        // Set vị trí ban đầu ở bottom
        Vector2 startPos = popupRect.anchoredPosition;
        startPos.y = bottomY;
        popupRect.anchoredPosition = startPos;

        // Set alpha ban đầu = 0 (ẩn)
        canvasGroup.alpha = 0f;

        // Tạo sequence animation
        Sequence sequence = DOTween.Sequence();

        // Fade in và di chuyển lên top cùng lúc
        sequence.Append(canvasGroup.DOFade(1f, moveUpDuration * 0.3f).SetEase(Ease.OutQuad));
        sequence.Join(popupRect.DOAnchorPosY(topY, moveUpDuration).SetEase(Ease.OutCubic));

        // Giữ nguyên vị trí trong thời gian hiển thị
        sequence.AppendInterval(popupDuration);

        // Fade out
        sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

        // Destroy sau khi animation hoàn thành
        sequence.OnComplete(() =>
        {
            if (popup != null)
            {
                Destroy(popup);
            }
        });

        // Set target để có thể kill tween khi cần
        sequence.SetTarget(popup);
    }

    private void OnDestroy()
    {
        // Kill tất cả tweens khi destroy
        transform.DOKill();
    }
}
