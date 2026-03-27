using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Script tự động tạo loading effect khi load vào scene "Defender Of Tomorrow"
/// Màn hình tối hoàn toàn, đợi thời gian random, sau đó fade in (sáng dần)
/// </summary>
public class SceneFadeIn : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên scene cần loading effect (mặc định: 'Defender Of Tomorrow')")]
    [SerializeField] private string targetSceneName = "Defender Of Tomorrow";

    [Header("Loading Settings")]
    [Tooltip("Thời gian đợi tối thiểu (giây) - màn hình sẽ tối trong khoảng thời gian này")]
    [SerializeField] private float minWaitDuration = 1.5f;
    
    [Tooltip("Thời gian đợi tối đa (giây) - màn hình sẽ tối trong khoảng thời gian này")]
    [SerializeField] private float maxWaitDuration = 3.5f;

    [Header("Fade Settings")]
    [Tooltip("Thời gian fade in sau khi hết thời gian đợi (giây)")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Header("Loading Text")]
    [Tooltip("Text loading (TMP_Text) - sẽ hiện trong thời gian đợi và ẩn khi fade")]
    [SerializeField] private TMP_Text loadingText;

    [Header("Delay Settings")]
    [Tooltip("Delay trước khi bắt đầu (tạo cảm giác loading)")]
    [SerializeField] private float delayBeforeStart = 0.1f;

    private void Start()
    {
        // Đảm bảo loading text được ẩn ban đầu
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }

        // Kiểm tra xem có phải scene target không
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        if (currentSceneName == targetSceneName)
        {
            StartCoroutine(LoadingRoutine());
        }
    }

    private IEnumerator LoadingRoutine()
    {
        // Đợi một chút để đảm bảo scene đã load xong
        yield return new WaitForSeconds(delayBeforeStart);

        // Kiểm tra UIFade có tồn tại không
        if (UIFade.Instance == null)
        {
            Debug.LogWarning("[SceneFadeIn] UIFade.Instance không tồn tại. Không thể tạo loading effect.");
            yield break;
        }

        // Tối màn hình ngay lập tức
        UIFade.Instance.SetAlphaImmediate(1f);
        yield return null; // Đợi 1 frame để fade screen được set

        // Hiện loading text và đảm bảo nó nằm trên fade screen
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            EnsureLoadingTextOnTop();
        }

        // Random thời gian đợi (tạo cảm giác loading)
        float waitDuration = Random.Range(minWaitDuration, maxWaitDuration);
        
        Debug.Log($"[SceneFadeIn] Màn hình tối, đợi {waitDuration:F2} giây...");

        // Đợi trong khoảng thời gian random
        yield return new WaitForSeconds(waitDuration);

        // Ẩn loading text trước khi fade
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }

        // Fade in (sáng dần)
        Debug.Log($"[SceneFadeIn] Bắt đầu fade in với thời gian: {fadeInDuration:F2} giây");
        UIFade.Instance.FadeToClear(fadeInDuration);

        // Đợi fade hoàn thành
        yield return new WaitForSeconds(fadeInDuration);
        
        Debug.Log("[SceneFadeIn] Fade in hoàn thành, loading hoàn thành.");
    }

    /// <summary>
    /// Đảm bảo loading text luôn nằm trên fade screen
    /// </summary>
    private void EnsureLoadingTextOnTop()
    {
        if (loadingText == null || loadingText.transform == null) return;

        Transform textTransform = loadingText.transform;
        Transform parent = textTransform.parent;

        if (parent != null)
        {
            // Đặt loading text làm child cuối cùng (render trên cùng trong cùng Canvas)
            textTransform.SetAsLastSibling();

            // Đảm bảo Canvas chứa loading text có sort order cao hơn fade screen
            Canvas textCanvas = parent.GetComponentInParent<Canvas>();
            if (textCanvas != null)
            {
                // Tìm Canvas của fade screen (từ UIFade GameObject)
                Canvas fadeCanvas = null;
                if (UIFade.Instance != null)
                {
                    fadeCanvas = UIFade.Instance.GetComponentInParent<Canvas>();
                    if (fadeCanvas == null)
                    {
                        // Nếu UIFade không có Canvas, tìm fade screen Image
                        UnityEngine.UI.Image[] images = UIFade.Instance.GetComponentsInChildren<UnityEngine.UI.Image>();
                        foreach (var img in images)
                        {
                            Canvas canvas = img.GetComponentInParent<Canvas>();
                            if (canvas != null)
                            {
                                fadeCanvas = canvas;
                                break;
                            }
                        }
                    }
                }

                if (fadeCanvas != null && fadeCanvas != textCanvas)
                {
                    // Khác Canvas: đặt sort order của text Canvas cao hơn fade Canvas
                    textCanvas.sortingOrder = fadeCanvas.sortingOrder + 1;
                }
                else if (fadeCanvas == textCanvas)
                {
                    // Cùng Canvas: đảm bảo text có sibling index cao hơn fade screen
                    // Fade screen đã được SetAsLastSibling bởi UIFade, nên cần set lại text
                    textTransform.SetAsLastSibling();
                }
                else
                {
                    // Không tìm thấy fade Canvas, đảm bảo text Canvas có sort order cao nhất
                    Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    int maxSortOrder = int.MinValue;
                    
                    foreach (Canvas canvas in allCanvases)
                    {
                        if (canvas != textCanvas && canvas.sortingOrder > maxSortOrder)
                        {
                            maxSortOrder = canvas.sortingOrder;
                        }
                    }

                    // Đặt sort order của text Canvas cao nhất
                    if (textCanvas.sortingOrder <= maxSortOrder)
                    {
                        textCanvas.sortingOrder = maxSortOrder + 1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gọi từ code để trigger loading effect thủ công (nếu cần)
    /// </summary>
    public void TriggerLoading()
    {
        StartCoroutine(LoadingRoutine());
    }
}

