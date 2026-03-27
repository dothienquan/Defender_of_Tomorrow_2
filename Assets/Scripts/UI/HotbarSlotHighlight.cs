using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component để quản lý highlight của hotbar slot
/// Đặt component này vào mỗi slot trong hotbar để có thể highlight khi được chọn
/// </summary>
[RequireComponent(typeof(Slot))]
public class HotbarSlotHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Image component dùng để highlight slot (nếu để trống sẽ tự động tìm)")]
    [SerializeField] private Image highlightImage;

    [Tooltip("Tự động tìm highlight image trong children nếu chưa gán")]
    [SerializeField] private bool autoFindHighlight = true;

    [Tooltip("Tên của GameObject chứa highlight (nếu để trống sẽ tìm tất cả Image)")]
    [SerializeField] private string highlightObjectName = "";

    private void Awake()
    {
        if (highlightImage == null && autoFindHighlight)
        {
            FindHighlightImage();
        }
    }

    /// <summary>
    /// Tự động tìm highlight image
    /// </summary>
    private void FindHighlightImage()
    {
        // Nếu có tên cụ thể, tìm theo tên
        if (!string.IsNullOrEmpty(highlightObjectName))
        {
            Transform highlightTransform = transform.Find(highlightObjectName);
            if (highlightTransform != null)
            {
                highlightImage = highlightTransform.GetComponent<Image>();
            }
        }

        // Nếu chưa tìm thấy, tìm tất cả Image trong children
        if (highlightImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();
            foreach (Image img in images)
            {
                // Bỏ qua image của chính slot và của item
                if (img.transform == transform || img.GetComponent<Item>() != null)
                    continue;

                // Ưu tiên image có tên chứa "highlight", "active", "selected"
                string imgName = img.name.ToLower();
                if (imgName.Contains("highlight") || 
                    imgName.Contains("active") || 
                    imgName.Contains("selected"))
                {
                    highlightImage = img;
                    break;
                }
            }

            // Nếu vẫn chưa tìm thấy, dùng child đầu tiên (nếu không phải item)
            if (highlightImage == null && transform.childCount > 0)
            {
                Transform firstChild = transform.GetChild(0);
                if (firstChild.GetComponent<Item>() == null)
                {
                    highlightImage = firstChild.GetComponent<Image>();
                }
            }
        }

        // Mặc định tắt highlight
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Bật highlight cho slot này
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (highlightImage == null)
        {
            if (autoFindHighlight)
            {
                FindHighlightImage();
            }
            
            if (highlightImage == null)
            {
                Debug.LogWarning($"[HotbarSlotHighlight] Không tìm thấy highlight image cho slot: {gameObject.name}");
                return;
            }
        }

        highlightImage.gameObject.SetActive(active);
    }

    /// <summary>
    /// Kiểm tra xem highlight có đang bật không
    /// </summary>
    public bool IsHighlighted()
    {
        return highlightImage != null && highlightImage.gameObject.activeSelf;
    }

    /// <summary>
    /// Gán highlight image thủ công (dùng trong Inspector hoặc code)
    /// </summary>
    public void SetHighlightImage(Image image)
    {
        highlightImage = image;
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }
}

