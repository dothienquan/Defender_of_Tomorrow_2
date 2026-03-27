using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script đơn giản để toggle panel (hiển thị/ẩn)
/// Có thể gắn vào Button hoặc gọi từ code khác
/// </summary>
public class PanelToggle : MonoBehaviour
{
    [Header("Panel Settings")]
    [Tooltip("Panel cần toggle (GameObject cần bật/tắt)")]
    [SerializeField] private GameObject panel;
    
    [Tooltip("Trạng thái ban đầu của panel (true = hiển thị, false = ẩn)")]
    [SerializeField] private bool startVisible = false;
    
    [Tooltip("Nếu true, sẽ tự động gán panel từ parent hoặc tìm trong scene")]
    [SerializeField] private bool autoFindPanel = false;
    
    [Header("Optional: Multiple Panels")]
    [Tooltip("Nếu muốn toggle nhiều panel cùng lúc")]
    [SerializeField] private GameObject[] additionalPanels;
    
    [Header("Optional: Close Other Panels")]
    [Tooltip("Các panel khác sẽ bị đóng khi panel này mở")]
    [SerializeField] private GameObject[] panelsToCloseOnOpen;
    
    private bool isPanelOpen = false;

    private void Awake()
    {
        // Tự động tìm panel nếu chưa gán
        if (autoFindPanel && panel == null)
        {
            // Thử tìm panel trong parent
            panel = transform.parent?.gameObject;
            
            // Nếu không có, thử tìm trong scene
            if (panel == null)
            {
                // Tìm GameObject có tên chứa "Panel"
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Panel") && obj.activeInHierarchy)
                    {
                        panel = obj;
                        break;
                    }
                }
            }
        }
        
        // Set trạng thái ban đầu
        if (panel != null)
        {
            isPanelOpen = panel.activeSelf;
            if (!startVisible)
            {
                panel.SetActive(false);
                isPanelOpen = false;
            }
        }
    }

    /// <summary>
    /// Toggle panel (bật nếu đang tắt, tắt nếu đang bật)
    /// </summary>
    public void TogglePanel()
    {
        if (panel == null)
        {
            Debug.LogWarning($"[PanelToggle] Panel is null on {gameObject.name}!");
            return;
        }

        isPanelOpen = !isPanelOpen;
        
        // Đóng các panel khác nếu cần
        if (isPanelOpen && panelsToCloseOnOpen != null)
        {
            foreach (GameObject otherPanel in panelsToCloseOnOpen)
            {
                if (otherPanel != null && otherPanel != panel)
                {
                    otherPanel.SetActive(false);
                }
            }
        }
        
        // Toggle panel chính
        panel.SetActive(isPanelOpen);
        
        // Toggle các panel phụ nếu có
        if (additionalPanels != null)
        {
            foreach (GameObject additionalPanel in additionalPanels)
            {
                if (additionalPanel != null)
                {
                    additionalPanel.SetActive(isPanelOpen);
                }
            }
        }
    }

    /// <summary>
    /// Mở panel
    /// </summary>
    public void OpenPanel()
    {
        if (panel == null) return;
        
        if (!isPanelOpen)
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// Đóng panel
    /// </summary>
    public void ClosePanel()
    {
        if (panel == null) return;
        
        if (isPanelOpen)
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// Set panel state trực tiếp
    /// </summary>
    public void SetPanelState(bool isOpen)
    {
        if (panel == null) return;
        
        if (isPanelOpen != isOpen)
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// Kiểm tra panel có đang mở không
    /// </summary>
    public bool IsPanelOpen()
    {
        return isPanelOpen && panel != null && panel.activeSelf;
    }

    /// <summary>
    /// Gán panel từ code
    /// </summary>
    public void SetPanel(GameObject newPanel)
    {
        panel = newPanel;
        if (panel != null)
        {
            isPanelOpen = panel.activeSelf;
        }
    }
}

