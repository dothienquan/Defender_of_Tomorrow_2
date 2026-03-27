using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Trigger để thực hiện sequence merge: active 3 objects, di chuyển, xoay vòng và merge
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MergeSequenceTrigger : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject object1;
    [SerializeField] private GameObject object2;
    [SerializeField] private GameObject object3;
    [SerializeField] private GameObject objectToDeactivate;

    [Header("Activation Settings")]
    [SerializeField] private float activationDelay1 = 0f;
    [SerializeField] private float activationDelay2 = 0.5f;
    [SerializeField] private float activationDelay3 = 1f;
    [SerializeField] private float fadeInDuration = 0.5f; // Thời gian fade in cho objects
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;

    [Header("Movement Settings")]
    [SerializeField] private Vector2 object1And2Offset = new Vector2(0f, 2f);
    [SerializeField] private float moveUpDuration = 1f;
    [SerializeField] private Ease moveUpEase = Ease.OutQuad;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationDelay = 2f; // Delay sau khi object 1 và 2 di chuyển lên
    [SerializeField] private float moveToRotationStartDuration = 1f; // Thời gian object 3 di chuyển đến vị trí bắt đầu xoay
    [SerializeField] private Ease moveToRotationStartEase = Ease.InOutQuad; // Easing cho di chuyển đến vị trí bắt đầu
    [SerializeField] private float rotationDuration = 5f; // Thời gian object 3 xoay
    [SerializeField] private float initialRotationSpeed = 90f; // Độ/giây
    [SerializeField] private float finalRotationSpeed = 360f; // Độ/giây
    [SerializeField] private float rotationRadius = 2f; // Bán kính quỹ đạo

    [Header("Merge Settings")]
    [SerializeField] private float mergeDuration = 5f; // Thời gian merge (chạy đồng thời với rotation)
    [SerializeField] private float mergeDistance = 1f; // Khoảng cách cuối cùng giữa các object
    [SerializeField] private Ease mergeEase = Ease.InOutQuad;

    [Header("Final Flash & Spawn")]
    [SerializeField] private float flashFadeInDuration = 0.5f; // Thời gian fade in trắng
    [SerializeField] private float flashHoldDuration = 0.3f; // Thời gian giữ trắng
    [SerializeField] private float flashFadeOutDuration = 0.5f; // Thời gian fade out
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private float prefabFadeInDuration = 0.5f; // Thời gian fade in cho prefab
    [SerializeField] private Ease prefabFadeInEase = Ease.OutQuad;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;
    private bool isSequenceRunning = false;
    private Coroutine sequenceCoroutine;
    private Vector3 object1StartPos;
    private Vector3 object2StartPos;
    private Vector3 object3StartPos;
    private Vector3 mergeCenter;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Lưu vị trí ban đầu
        if (object1 != null) object1StartPos = object1.transform.position;
        if (object2 != null) object2StartPos = object2.transform.position;
        if (object3 != null) object3StartPos = object3.transform.position;
    }

    private void Start()
    {
        // Đảm bảo các object bắt đầu inactive
        if (object1 != null) object1.SetActive(false);
        if (object2 != null) object2.SetActive(false);
        if (object3 != null) object3.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;
        if (isSequenceRunning) return;

        hasTriggered = true;
        isSequenceRunning = true;
        
        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);
        
        sequenceCoroutine = StartCoroutine(MergeSequenceCoroutine());
    }

    private IEnumerator MergeSequenceCoroutine()
    {
        // Phase 1: Active lần lượt 3 objects với fade in và deactive một object
        yield return new WaitForSeconds(activationDelay1);
        if (object1 != null)
        {
            yield return StartCoroutine(FadeInObjectCoroutine(object1));
        }

        yield return new WaitForSeconds(activationDelay2);
        if (object2 != null)
        {
            yield return StartCoroutine(FadeInObjectCoroutine(object2));
        }

        yield return new WaitForSeconds(activationDelay3);
        if (object3 != null)
        {
            yield return StartCoroutine(FadeInObjectCoroutine(object3));
        }

        if (objectToDeactivate != null)
            objectToDeactivate.SetActive(false);

        // Đợi 3 giây sau khi cả 3 object hiện ra
        yield return new WaitForSeconds(3f);

        // Phase 2: Object 1 và 2 di chuyển lên
        if (object1 != null && object2 != null)
        {
            Vector3 target1 = object1StartPos + (Vector3)object1And2Offset;
            Vector3 target2 = object2StartPos + (Vector3)object1And2Offset;

            object1.transform.DOMove(target1, moveUpDuration).SetEase(moveUpEase);
            object2.transform.DOMove(target2, moveUpDuration).SetEase(moveUpEase);

            yield return new WaitForSeconds(moveUpDuration);
        }

        // Đợi thêm rotationDelay trước khi object 3 bắt đầu xoay
        yield return new WaitForSeconds(rotationDelay);

        // Tính toán center point giữa object 1 và 2
        if (object1 != null && object2 != null)
        {
            mergeCenter = (object1.transform.position + object2.transform.position) * 0.5f;
        }
        else if (object1 != null)
        {
            mergeCenter = object1.transform.position;
        }
        else if (object2 != null)
        {
            mergeCenter = object2.transform.position;
        }
        else
        {
            mergeCenter = object3StartPos;
        }

        // Phase 3: Object 3 di chuyển đến vị trí bắt đầu xoay, sau đó xoay vòng
        if (object3 != null)
        {
            // Tính toán vị trí bắt đầu xoay (một điểm trên quỹ đạo tròn)
            Vector3 currentPos = object3.transform.position;
            Vector3 directionToCenter = (mergeCenter - currentPos).normalized;
            float initialDistance = Vector3.Distance(currentPos, mergeCenter);
            float startRadius = Mathf.Max(rotationRadius, initialDistance);
            
            // Vị trí bắt đầu xoay: một điểm trên quỹ đạo tròn với radius ban đầu
            // Sử dụng góc từ vị trí hiện tại đến center
            float initialAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x);
            Vector3 rotationStartPos = mergeCenter + new Vector3(
                Mathf.Cos(initialAngle) * startRadius,
                Mathf.Sin(initialAngle) * startRadius,
                0f
            );

            // Di chuyển mượt mà đến vị trí bắt đầu xoay
            yield return object3.transform.DOMove(rotationStartPos, moveToRotationStartDuration)
                .SetEase(moveToRotationStartEase)
                .WaitForCompletion();

            // Bắt đầu rotation với tốc độ tăng dần
            StartCoroutine(RotateObject3Coroutine());
        }

        // Bắt đầu merge effect cho object 1 và 2
        StartCoroutine(MergeObjectsCoroutine());

        // Đợi rotation gần hoàn thành (sớm hơn 0.2 giây để flash bắt đầu sớm)
        yield return new WaitForSeconds(rotationDuration - 0.2f);

        // Phase 4: Flash toàn màn hình và spawn prefab (bắt đầu sớm hơn 0.2 giây)
        yield return StartCoroutine(FlashScreenCoroutine());

        // Đợi 0.2 giây còn lại để rotation hoàn thành (nếu flash đã hoàn thành trước)
        yield return new WaitForSeconds(0.2f);

        // Đảm bảo object 3 ở đúng vị trí cuối cùng
        if (object3 != null)
        {
            float angleStep = 120f * Mathf.Deg2Rad;
            Vector3 target3Pos = mergeCenter + new Vector3(Mathf.Cos(angleStep * 2f), Mathf.Sin(angleStep * 2f), 0f) * mergeDistance;
            object3.transform.position = target3Pos;
        }

        if (prefabToSpawn != null)
        {
            Vector3 spawnPosition = mergeCenter + spawnOffset;
            GameObject spawnedPrefab = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            
            // Fade in cho prefab
            if (spawnedPrefab != null)
            {
                // Tạm thời disable Particle Systems để tránh lỗi shader compiler
                ParticleSystem[] particleSystems = spawnedPrefab.GetComponentsInChildren<ParticleSystem>(true);
                bool[] wasPlaying = new bool[particleSystems.Length];
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    wasPlaying[i] = particleSystems[i].isPlaying;
                    particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                
                // Đợi một frame để đảm bảo prefab được khởi tạo hoàn toàn (tránh lỗi TextMeshPro/Shader)
                yield return null;
                
                // Khôi phục Particle Systems sau khi đã khởi tạo xong
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (wasPlaying[i] && particleSystems[i] != null)
                    {
                        particleSystems[i].Play();
                    }
                }
                
                StartCoroutine(FadeInObjectCoroutine(spawnedPrefab, prefabFadeInDuration, prefabFadeInEase));
            }
        }

        // Objects đã được destroy trong lúc flash hold, không cần deactivate nữa

        isSequenceRunning = false;
    }

    private IEnumerator RotateObject3Coroutine()
    {
        if (object3 == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = object3.transform.position;
        
        // Tính toán góc ban đầu từ vị trí hiện tại
        Vector3 directionToCenter = (mergeCenter - startPos).normalized;
        float initialAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x);
        float currentAngle = initialAngle * Mathf.Rad2Deg; // Chuyển sang độ

        // Tính toán radius ban đầu
        float initialDistance = Vector3.Distance(startPos, mergeCenter);
        float startRadius = Mathf.Max(rotationRadius, initialDistance);
        float targetRadius = mergeDistance; // Radius cuối cùng khi merge

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / rotationDuration;

            // Tốc độ quay tăng dần đều
            float currentSpeed = Mathf.Lerp(initialRotationSpeed, finalRotationSpeed, progress);
            currentAngle += currentSpeed * Time.deltaTime;

            // Radius giảm dần về targetRadius (merge effect)
            float currentRadius = Mathf.Lerp(startRadius, targetRadius, progress);

            // Tính toán vị trí trên quỹ đạo tròn
            float angleRad = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * currentRadius,
                Mathf.Sin(angleRad) * currentRadius,
                0f
            );

            object3.transform.position = mergeCenter + offset;

            yield return null;
        }
    }

    private IEnumerator MergeObjectsCoroutine()
    {
        // Object 1 và 2 giữ nguyên vị trí sau khi di chuyển lên (không merge)
        // Chỉ object 3 merge về center trong quá trình xoay
        // Coroutine này chỉ đợi merge duration để đồng bộ với rotation
        yield return new WaitForSeconds(mergeDuration);
    }

    private IEnumerator FlashScreenCoroutine()
    {
        if (UIFade.Instance == null)
        {
            Debug.LogWarning("[MergeSequenceTrigger] UIFade.Instance không tồn tại. Không thể flash màn hình.");
            yield break;
        }

        // Lấy fadeScreen Image thông qua reflection
        var fadeType = typeof(UIFade);
        var fadeScreenField = fadeType.GetField("fadeScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (fadeScreenField == null)
        {
            Debug.LogWarning("[MergeSequenceTrigger] Không thể truy cập fadeScreen từ UIFade.");
            yield break;
        }

        UnityEngine.UI.Image fadeScreen = fadeScreenField.GetValue(UIFade.Instance) as UnityEngine.UI.Image;
        
        if (fadeScreen == null)
        {
            Debug.LogWarning("[MergeSequenceTrigger] fadeScreen Image không tồn tại.");
            yield break;
        }

        // Lưu màu gốc
        Color originalColor = fadeScreen.color;
        Color whiteColor = Color.white;

        // Đảm bảo fadeScreen ở trên cùng
        UIFade.Instance.SetAlphaImmediate(0f); // Reset alpha về 0 trước
        fadeScreen.color = whiteColor; // Set màu trắng

        // Fade in trắng (alpha từ 0 -> 1)
        float elapsed = 0f;
        while (elapsed < flashFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / flashFadeInDuration);
            fadeScreen.color = new Color(whiteColor.r, whiteColor.g, whiteColor.b, alpha);
            yield return null;
        }

        // Đảm bảo alpha = 1
        fadeScreen.color = new Color(whiteColor.r, whiteColor.g, whiteColor.b, 1f);

        // Giữ trắng một khoảng thời gian - destroy objects trong lúc này
        yield return new WaitForSeconds(flashHoldDuration * 0.5f); // Đợi một nửa thời gian hold

        // Destroy 3 objects trong lúc flash đang hold (trước khi fade out)
        if (object1 != null)
        {
            Destroy(object1);
            object1 = null;
        }
        if (object2 != null)
        {
            Destroy(object2);
            object2 = null;
        }
        if (object3 != null)
        {
            Destroy(object3);
            object3 = null;
        }

        // Đợi nửa thời gian hold còn lại
        yield return new WaitForSeconds(flashHoldDuration * 0.5f);

        // Fade out trắng (alpha từ 1 -> 0)
        elapsed = 0f;
        while (elapsed < flashFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / flashFadeOutDuration);
            fadeScreen.color = new Color(whiteColor.r, whiteColor.g, whiteColor.b, alpha);
            yield return null;
        }

        // Đảm bảo alpha = 0 và khôi phục màu gốc
        fadeScreen.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    /// <summary>
    /// Fade in effect cho object (hỗ trợ SpriteRenderer và CanvasGroup)
    /// </summary>
    private IEnumerator FadeInObjectCoroutine(GameObject obj, float duration = -1f, Ease ease = Ease.OutQuad)
    {
        if (obj == null) yield break;

        // Sử dụng duration mặc định nếu không được chỉ định
        float fadeDuration = duration > 0f ? duration : fadeInDuration;

        // Active object trước
        obj.SetActive(true);

        // Lấy SpriteRenderer hoặc CanvasGroup
        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();

        // Lưu alpha gốc
        float originalAlpha = 1f;
        if (spriteRenderer != null)
        {
            originalAlpha = spriteRenderer.color.a;
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }
        else if (canvasGroup != null)
        {
            originalAlpha = canvasGroup.alpha;
            canvasGroup.alpha = 0f;
        }
        else
        {
            // Nếu không có SpriteRenderer hoặc CanvasGroup, không fade
            yield break;
        }

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            
            // Apply easing
            float easedT = GetEasedValue(t, ease);
            float alpha = Mathf.Lerp(0f, originalAlpha, easedT);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }

            yield return null;
        }

        // Đảm bảo alpha = originalAlpha
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = originalAlpha;
            spriteRenderer.color = color;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = originalAlpha;
        }
    }

    /// <summary>
    /// Convert Ease enum thành giá trị eased (0-1)
    /// </summary>
    private float GetEasedValue(float t, Ease ease)
    {
        // Sử dụng DOTween để tính toán eased value
        return DG.Tweening.DOVirtual.EasedValue(0f, 1f, t, ease);
    }

    private void OnDestroy()
    {
        // Dừng tất cả tweens khi destroy
        if (object1 != null) object1.transform.DOKill();
        if (object2 != null) object2.transform.DOKill();
        if (object3 != null) object3.transform.DOKill();

        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);
    }
}

