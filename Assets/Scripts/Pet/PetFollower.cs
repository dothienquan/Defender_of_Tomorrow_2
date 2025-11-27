using UnityEngine;

public class PetFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private Vector2 baseOffset = new Vector2(-0.7f, 0.7f);
    [SerializeField] private float followSpeed = 3f; // Tốc độ follow (cao hơn = nhanh hơn)
    [SerializeField] private float followLag = 0.3f; // Độ trễ khi follow (tạo cảm giác tự nhiên)

    [Header("Floating Motion")]
    [SerializeField] private float floatAmplitude = 0.2f; // Biên độ floating (lên xuống)
    [SerializeField] private float floatSpeed = 2f; // Tốc độ floating
    [SerializeField] private float floatOffset = 0f; // Offset để mỗi pet có timing khác nhau

    [Header("Free Movement")]
    [SerializeField] private float wanderRadius = 0.3f; // Bán kính tự do di chuyển quanh vị trí target
    [SerializeField] private float wanderSpeed = 1.5f; // Tốc độ wander
    [SerializeField] private float wanderChangeInterval = 2f; // Thời gian đổi hướng wander

    [Header("Rotation")]
    [SerializeField] private float rotationSmoothness = 5f; // Độ mượt khi xoay
    [SerializeField] private float maxRotationAngle = 15f; // Góc xoay tối đa khi di chuyển

    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private float floatTimer;
    private Vector3 lastPlayerPosition;
    private float currentRotation;

    private void Start()
    {
        if (player == null)
        {
            player = PlayerController.Instance?.transform;
        }

        // Khởi tạo các giá trị
        targetPosition = transform.position;
        wanderTarget = Vector3.zero;
        wanderTimer = Random.Range(0f, wanderChangeInterval);
        floatTimer = floatOffset; // Mỗi pet có offset khác nhau
        lastPlayerPosition = player != null ? player.position : transform.position;
    }

    private void Update()
    {
        if (player == null) return;

        // Tính toán vị trí target cơ bản
        Vector3 baseTargetPos = player.position + (Vector3)baseOffset;

        // Cập nhật wander target định kỳ
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            // Tạo điểm wander mới trong phạm vi
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = baseTargetPos + (Vector3)randomOffset;
            wanderTimer = wanderChangeInterval + Random.Range(-0.5f, 0.5f); // Thêm random để tự nhiên hơn
        }

        // Smooth wander về target
        wanderTarget = Vector3.Lerp(wanderTarget, baseTargetPos, Time.deltaTime * wanderSpeed * 0.5f);

        // Tính toán floating effect (sin wave)
        floatTimer += Time.deltaTime * floatSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;

        // Kết hợp target position với floating
        targetPosition = wanderTarget + Vector3.up * floatY;

        // Smooth follow với độ trễ
        Vector3 playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
        Vector3 predictedPosition = targetPosition + (Vector3)playerVelocity * followLag;
        
        transform.position = Vector3.SmoothDamp(
            transform.position,
            predictedPosition,
            ref currentVelocity,
            1f / followSpeed,
            Mathf.Infinity,
            Time.deltaTime
        );

        // Cập nhật last position
        lastPlayerPosition = player.position;

        // Xoay sprite theo hướng di chuyển
        UpdateRotation();

        // Flip sprite theo hướng player
        UpdateSpriteFlip();
    }

    private void UpdateRotation()
    {
        // Tính góc xoay dựa trên velocity
        if (currentVelocity.magnitude > 0.01f)
        {
            float targetRotation = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
            // Giới hạn góc xoay
            targetRotation = Mathf.Clamp(targetRotation, -maxRotationAngle, maxRotationAngle);
            currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSmoothness);
        }
        else
        {
            // Từ từ về 0 khi không di chuyển
            currentRotation = Mathf.LerpAngle(currentRotation, 0f, Time.deltaTime * rotationSmoothness);
        }

        // Áp dụng rotation (chỉ xoay nhẹ, không quá nhiều)
        transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
    }

    private void UpdateSpriteFlip()
    {
        // Flip sprite dựa trên vị trí so với player
        Vector3 scale = transform.localScale;
        if (transform.position.x < player.position.x)
        {
            scale.x = Mathf.Abs(scale.x); // Face right
        }
        else
        {
            scale.x = -Mathf.Abs(scale.x); // Face left
        }
        transform.localScale = scale;
    }

    // Hàm để set player từ bên ngoài nếu cần
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }
}
