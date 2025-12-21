using UnityEngine;
using DG.Tweening;
using System.Collections;

public class RitualSequence : MonoBehaviour
{
    [Header("--- OBJECTS SETUP ---")]
    [SerializeField] private GameObject object1;
    [SerializeField] private GameObject object2;
    [SerializeField] private GameObject object3;
    [Tooltip("Object sẽ bị tắt đi khi bắt đầu")]
    [SerializeField] private GameObject objectToHide;

    [Header("--- TIMING & SETTINGS ---")]
    [SerializeField] private float delayHideObject = 0.5f; // Delay trước khi tắt objectToHide
    [SerializeField] private float intervalBetweenActive = 1f; // Thời gian chờ giữa các lần active 1, 2, 3
    
    [Header("--- RITUAL MOVEMENT ---")]
    [SerializeField] private float rotateDuration = 3f; // Thời gian xoay vòng tròn trước khi nổ
    [SerializeField] private float startRotateSpeed = 5f; // Tốc độ xoay ban đầu
    [SerializeField] private float speedMultiplier = 2f; // Gia tốc làm tăng tốc độ xoay
    [SerializeField] private float mergeDuration = 2.5f; // Thời gian Obj1 và 2 hút vào nhau

    [Header("--- FINALE ---")]
    [SerializeField] private CanvasGroup fadePanel; // Panel màu trắng/đen để Fade (cần có CanvasGroup)
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private GameObject finalPrefab; // Prefab sinh ra cuối cùng
    [SerializeField] private Transform spawnPoint; // Vị trí sinh prefab

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;
        
        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(RunSequence());
        }
    }

    private IEnumerator RunSequence()
    {
        // 1. Xử lý Inactive Object (Chạy song song hoặc đợi một chút)
        DOVirtual.DelayedCall(delayHideObject, () => 
        {
            if(objectToHide != null) objectToHide.SetActive(false);
        });

        // 2. Lần lượt Active Object 1, 2, 3
        yield return new WaitForSeconds(0.2f); // Delay nhỏ lúc bắt đầu
        
        object1.SetActive(true);
        yield return new WaitForSeconds(intervalBetweenActive);

        object2.SetActive(true);
        yield return new WaitForSeconds(intervalBetweenActive);

        object3.SetActive(true);
        yield return new WaitForSeconds(intervalBetweenActive);

        // 3. Bắt đầu giai đoạn xoay và hợp thể
        yield return StartCoroutine(HandleRitualMovement());

        // 4. Kết thúc: Fade màn hình -> Tắt hết -> Spawn -> Fade lại
        yield return StartCoroutine(HandleFinale());
    }

    private IEnumerator HandleRitualMovement()
    {
        // --- Setup vị trí ---
        // Tính tâm điểm giữa Obj1 và Obj2
        Vector3 centerPos = (object1.transform.position + object2.transform.position) / 2f;
        
        // Tính bán kính xoay dựa trên khoảng cách hiện tại của Obj3 tới tâm
        float radius = Vector3.Distance(object3.transform.position, centerPos);
        float currentAngle = 0f;
        float currentSpeed = startRotateSpeed;
        float timer = 0f;

        // --- Hiệu ứng hợp thể: Obj1 và Obj2 di chuyển từ từ vào tâm ---
        object1.transform.DOMove(centerPos, mergeDuration).SetEase(Ease.InCubic);
        object2.transform.DOMove(centerPos, mergeDuration).SetEase(Ease.InCubic);

        // --- Vòng lặp di chuyển Obj3 quay quanh tâm ---
        while (timer < rotateDuration)
        {
            timer += Time.deltaTime;
            
            // Tăng tốc độ xoay theo thời gian
            currentSpeed += speedMultiplier * Time.deltaTime;
            currentAngle += currentSpeed * Time.deltaTime;

            // Tính toán vị trí mới theo hình tròn (Sin/Cos)
            float x = centerPos.x + Mathf.Cos(currentAngle) * radius;
            float y = centerPos.y + Mathf.Sin(currentAngle) * radius;
            
            // Cập nhật vị trí Obj3
            object3.transform.position = new Vector3(x, y, object3.transform.position.z);

            // Tùy chọn: Thu nhỏ bán kính dần để Obj3 cũng hút vào tâm (nếu muốn)
            // radius = Mathf.Lerp(radius, 0.5f, Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator HandleFinale()
    {
        // 1. Fade màn hình (trắng hoặc đen tùy màu Panel của bạn)
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 0f;
            yield return fadePanel.DOFade(1f, fadeDuration).WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // Fallback nếu không gán fadePanel
        }

        // 2. Lúc màn hình đang che hết: Tắt object cũ, Spawn object mới
        object1.SetActive(false);
        object2.SetActive(false);
        object3.SetActive(false);

        if (finalPrefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Instantiate(finalPrefab, pos, Quaternion.identity);
        }

        // 3. Fade màn hình trở lại bình thường
        if (fadePanel != null)
        {
            yield return fadePanel.DOFade(0f, fadeDuration).WaitForCompletion();
            fadePanel.gameObject.SetActive(false);
        }
        
        // Tự hủy script hoặc gameobject trigger này nếu không dùng nữa
        // Destroy(gameObject);
    }
}