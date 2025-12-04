using UnityEngine;
using TMPro;

/// <summary>
/// Script để tự động setup Quest Text để tự động xuống dòng và fit trong panel
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class QuestTextAutoFit : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool enableTextWrapping = true;
    [SerializeField] private bool rightAlign = true; // Canh phải

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (setupOnStart)
        {
            // Delay một frame để đảm bảo RectTransform đã được tính toán
            StartCoroutine(SetupTextDelayed());
        }
    }

    private System.Collections.IEnumerator SetupTextDelayed()
    {
        // Đợi end of frame để RectTransform được tính toán
        yield return new WaitForEndOfFrame();
        SetupText();
    }

    /// <summary>
    /// Setup text để tự động xuống dòng và fit trong panel
    /// </summary>
    public void SetupText()
    {
        if (textComponent == null)
        {
            Debug.LogError("[QuestTextAutoFit] TextMeshProUGUI component không tìm thấy!");
            return;
        }

        // Kiểm tra material và font asset trước khi setup
        if (textComponent.font == null)
        {
            Debug.LogWarning("[QuestTextAutoFit] Font asset chưa được gán! Không thể setup text.");
            return;
        }

        // Đảm bảo RectTransform có size phù hợp TRƯỚC khi setup text
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // KHÔNG force set anchors nếu đã có anchors khác (giữ nguyên setup hiện tại)
            // Chỉ kiểm tra và cảnh báo nếu width = 0
            
            // Force update RectTransform để tính toán size
            Canvas.ForceUpdateCanvases();
            
            // Đảm bảo có width > 0 (nếu width = 0 thì text sẽ wrap theo từng ký tự)
            float width = rectTransform.rect.width;
            if (width <= 0)
            {
                // Nếu width = 0, thử lấy từ sizeDelta hoặc parent
                width = rectTransform.sizeDelta.x;
                if (width <= 0 && rectTransform.parent != null)
                {
                    RectTransform parentRect = rectTransform.parent as RectTransform;
                    if (parentRect != null)
                    {
                        width = parentRect.rect.width;
                    }
                }
                
                if (width <= 0)
                {
                    Debug.LogError($"[QuestTextAutoFit] RectTransform width = 0! Text sẽ wrap theo từng ký tự. GameObject: {gameObject.name}");
                    return; // Không setup nếu width = 0
                }
            }
            
            Debug.Log($"[QuestTextAutoFit] RectTransform width: {width}");
        }

        // QUAN TRỌNG: Tắt word wrapping trước, sau đó mới bật lại
        textComponent.enableWordWrapping = false;
        
        // Enable text wrapping (tự động xuống dòng theo WORD, không phải character)
        if (enableTextWrapping)
        {
            // Set text wrapping mode = Normal TRƯỚC khi bật enableWordWrapping
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            
            // BẬT word wrapping - đây là cách đúng để wrap theo word
            textComponent.enableWordWrapping = true;
            
            // Điều chỉnh word wrapping ratios (0.4 = wrap khi còn 40% line width)
            textComponent.wordWrappingRatios = 0.4f;
        }
        else
        {
            textComponent.textWrappingMode = (TextWrappingModes)0; // NoWrapping
            textComponent.enableWordWrapping = false;
        }

        // Set overflow mode để text không bị cắt và có thể wrap
        textComponent.overflowMode = TextOverflowModes.Overflow;

        // Set alignment (canh phải) - nhưng vẫn wrap đúng cách
        if (rightAlign)
        {
            // Dùng TopRight thay vì MidlineRight để wrap tốt hơn
            textComponent.alignment = TextAlignmentOptions.TopRight;
        }
        else
        {
            textComponent.alignment = TextAlignmentOptions.TopLeft;
        }

        // Force update để áp dụng changes (chỉ khi material đã sẵn sàng)
        try
        {
            if (textComponent.font != null && textComponent.font.material != null)
            {
                textComponent.ForceMeshUpdate();
            }
            else
            {
                // Nếu material chưa sẵn sàng, đợi thêm một frame
                StartCoroutine(ForceUpdateDelayed());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QuestTextAutoFit] Không thể force update text ngay: {e.Message}. Sẽ thử lại sau.");
            StartCoroutine(ForceUpdateDelayed());
        }

        Debug.Log($"[QuestTextAutoFit] Đã setup text - Word Wrapping: {textComponent.enableWordWrapping}, Wrapping Mode: {textComponent.textWrappingMode}, Width: {rectTransform?.rect.width}");
    }

    /// <summary>
    /// Force update text layout với delay (tránh NullReferenceException)
    /// </summary>
    private System.Collections.IEnumerator ForceUpdateDelayed()
    {
        yield return new WaitForEndOfFrame();
        if (textComponent != null && textComponent.font != null)
        {
            try
            {
                textComponent.ForceMeshUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[QuestTextAutoFit] Không thể force update text: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Force update text layout (gọi khi text thay đổi)
    /// </summary>
    public void ForceUpdateText()
    {
        if (textComponent != null && textComponent.font != null)
        {
            try
            {
                textComponent.ForceMeshUpdate();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[QuestTextAutoFit] Không thể force update text: {e.Message}");
            }
        }
    }

    // Editor helper - có thể gọi từ Inspector
    [ContextMenu("Setup Text Now")]
    private void SetupTextNow()
    {
        SetupText();
    }
}

