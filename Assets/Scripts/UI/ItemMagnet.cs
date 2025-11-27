using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Script để item bay vào player khi player đến đủ gần
/// Gắn vào các item trong game world
/// </summary>
public class ItemMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float magnetDistance = 3f; // Khoảng cách để bắt đầu hút
    [SerializeField] private float magnetSpeed = 8f; // Tốc độ bay vào player
    [SerializeField] private float accelerationRate = 2f; // Tốc độ tăng tốc
    [SerializeField] private float rotationSpeed = 360f; // Tốc độ xoay khi bay (độ/giây)

    [Header("Visual Effects")]
    [SerializeField] private bool enableRotation = true; // Có xoay khi bay không
    [SerializeField] private float scaleUpOnMagnet = 1.2f; // Phóng to khi bắt đầu hút
    [SerializeField] private float scaleDuration = 0.2f; // Thời gian phóng to

    private bool isMagnetized = false;
    private float currentSpeed = 0f;
    private Transform playerTransform;
    private Vector3 originalScale;
    private Tween scaleTween;
    private SpriteRenderer spriteRenderer;
    private Image itemImage;

    private void Awake()
    {
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemImage = GetComponent<Image>();
    }

    private void Start()
    {
        // Tìm player nếu chưa có
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            // Thử tìm lại player mỗi frame nếu chưa có
            if (PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Kiểm tra nếu player đến đủ gần
        if (distanceToPlayer <= magnetDistance && !isMagnetized)
        {
            StartMagnet();
        }

        // Di chuyển về phía player nếu đã được hút
        if (isMagnetized)
        {
            MoveTowardsPlayer();
            
            // Xoay item khi bay
            if (enableRotation)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void StartMagnet()
    {
        isMagnetized = true;
        currentSpeed = magnetSpeed * 0.5f; // Bắt đầu với tốc độ chậm

        // Hiệu ứng phóng to khi bắt đầu hút
        if (scaleTween != null) scaleTween.Kill();
        scaleTween = transform.DOScale(originalScale * scaleUpOnMagnet, scaleDuration)
            .SetEase(Ease.OutBack);
    }

    private void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        
        // Tăng tốc dần
        currentSpeed = Mathf.Min(currentSpeed + accelerationRate * Time.deltaTime, magnetSpeed * 2f);
        
        // Di chuyển
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }

    // Hàm để reset trạng thái (nếu cần)
    public void ResetMagnet()
    {
        isMagnetized = false;
        currentSpeed = 0f;
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
    }
}

