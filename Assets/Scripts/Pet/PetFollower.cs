using UnityEngine;

public class PetFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private Vector2 baseOffset = new Vector2(-0.7f, 0.7f);
    [SerializeField] private float followSpeed = 3f; // Tốc độ follow (cao hơn = nhanh hơn)
    [SerializeField] private float followLag = 0.3f; // Độ trễ khi follow (tạo cảm giác tự nhiên)
    [SerializeField] private float followDelay = 0.5f; // Delay trước khi bắt đầu follow (giây)

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

    [Header("Trail Effect")]
    [Tooltip("Trail Renderer component (tự động tìm hoặc tạo nếu để trống)")]
    [SerializeField] private TrailRenderer trailRenderer;
    
    [Tooltip("Bật/tắt trail effect")]
    [SerializeField] private bool enableTrail = true;
    
    [Tooltip("Màu sắc của trail (gradient từ đầu đến cuối)")]
    [SerializeField] private Gradient trailGradient;
    
    [Tooltip("Độ dài trail (thời gian trail tồn tại)")]
    [SerializeField] private float trailTime = 0.5f;
    
    [Tooltip("Độ rộng trail tại đầu")]
    [SerializeField] private float trailStartWidth = 0.3f;
    
    [Tooltip("Độ rộng trail tại cuối")]
    [SerializeField] private float trailEndWidth = 0.1f;
    
    [Tooltip("Material cho trail (có thể để trống để dùng default)")]
    [SerializeField] private Material trailMaterial;

    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private float floatTimer;
    private Vector3 lastPlayerPosition;
    private float currentRotation;
    private float followDelayTimer; // Timer cho delay follow
    private bool isFollowing; // Trạng thái đang follow hay chưa
    private bool playerWasMoving; // Trạng thái player có đang di chuyển không
    private Vector3 playerStartPosition; // Vị trí player khi bắt đầu di chuyển

    private void Start()
    {
        // Tìm player nếu chưa được gán
        if (player == null)
        {
            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance.transform;
            }
            else
            {
                // Thử tìm bằng tag hoặc name
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
        }

        if (player == null)
        {
            Debug.LogWarning($"[PetFollower] {gameObject.name}: Player not found! Pet will not follow.");
            return;
        }

        // Khởi tạo các giá trị
        targetPosition = transform.position;
        wanderTarget = Vector3.zero;
        wanderTimer = Random.Range(0f, wanderChangeInterval);
        floatTimer = floatOffset; // Mỗi pet có offset khác nhau
        lastPlayerPosition = player.position;
        playerStartPosition = lastPlayerPosition;
        
        // Khởi tạo delay follow
        followDelayTimer = 0f;
        isFollowing = false;
        playerWasMoving = false;

        // Setup Trail Renderer
        SetupTrailRenderer();
        
        Debug.Log($"[PetFollower] {gameObject.name}: Initialized. Player: {player.name}, Follow Delay: {followDelay}s");
    }

    private void SetupTrailRenderer()
    {
        if (!enableTrail) return;

        // Tìm hoặc tạo Trail Renderer
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
        }

        if (trailRenderer == null) return;

        // Cấu hình Trail Renderer
        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailStartWidth;
        trailRenderer.endWidth = trailEndWidth;
        trailRenderer.minVertexDistance = 0.1f; // Khoảng cách tối thiểu giữa các điểm
        trailRenderer.textureMode = LineTextureMode.Tile;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        // Set gradient màu
        if (trailGradient != null && trailGradient.colorKeys.Length > 0)
        {
            trailRenderer.colorGradient = trailGradient;
        }
        else
        {
            // Tạo gradient mặc định nếu chưa có
            Gradient defaultGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.5f), 0.5f), // Vàng nhạt
                new GradientColorKey(new Color(1f, 0.5f, 0f), 1f) // Cam
            };
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f) // Fade out
            };
            defaultGradient.SetKeys(colorKeys, alphaKeys);
            trailRenderer.colorGradient = defaultGradient;
        }

        // Set material nếu có
        if (trailMaterial != null)
        {
            trailRenderer.material = trailMaterial;
        }
        else
        {
            // Tạo material mặc định với shader phù hợp
            if (trailRenderer.material == null || trailRenderer.material.shader.name == "Sprites/Default")
            {
                Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
                trailRenderer.material = defaultMaterial;
            }
        }

        // Đảm bảo trail được bật
        trailRenderer.enabled = true;
    }

    private void Update()
    {
        // Tìm player lại nếu bị mất reference
        if (player == null)
        {
            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance.transform;
            }
            if (player == null) return;
        }

        // Kiểm tra xem player có đang di chuyển không
        Vector3 playerMovement = player.position - lastPlayerPosition;
        float movementDistance = playerMovement.magnitude;
        bool playerIsMoving = movementDistance > 0.01f; // Ngưỡng nhỏ để detect movement

        // Xử lý delay follow dựa trên player movement
        // Nếu followDelay = 0, bỏ qua delay và follow ngay
        if (followDelay <= 0f)
        {
            isFollowing = true;
        }
        else if (!isFollowing)
        {
            if (playerIsMoving && !playerWasMoving)
            {
                // Player vừa bắt đầu di chuyển
                playerWasMoving = true;
                playerStartPosition = player.position;
                followDelayTimer = followDelay; // Bắt đầu đếm delay
                Debug.Log($"[PetFollower] {gameObject.name}: Player started moving. Starting delay timer: {followDelay}s");
            }
            else if (!playerIsMoving && playerWasMoving)
            {
                // Player dừng lại, reset delay timer
                playerWasMoving = false;
                followDelayTimer = 0f;
            }

            // Đếm ngược delay timer
            if (followDelayTimer > 0f)
            {
                followDelayTimer -= Time.deltaTime;
                if (followDelayTimer <= 0f)
                {
                    // Delay xong, bắt đầu follow
                    isFollowing = true;
                    lastPlayerPosition = player.position;
                    currentVelocity = Vector3.zero;
                    Debug.Log($"[PetFollower] {gameObject.name}: Delay finished. Starting to follow player.");
                }
            }

            // Trong thời gian delay, pet đứng yên
            if (!isFollowing)
            {
                // Chỉ cập nhật sprite flip dựa trên vị trí player
                UpdateSpriteFlip();
                
                // Từ từ reset rotation về 0 trong thời gian delay
                if (currentRotation != 0f)
                {
                    currentRotation = Mathf.LerpAngle(currentRotation, 0f, Time.deltaTime * rotationSmoothness);
                    transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
                }
                
                // Cập nhật lastPlayerPosition để detect movement tiếp theo
                lastPlayerPosition = player.position;
                return; // Không di chuyển trong thời gian delay
            }
        }
        else
        {
            // Đã bắt đầu follow, cập nhật trạng thái player movement
            playerWasMoving = playerIsMoving;
        }

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

        // Cập nhật trail effect
        UpdateTrailEffect();
    }

    private void UpdateTrailEffect()
    {
        if (!enableTrail || trailRenderer == null) return;

        // Điều chỉnh trail dựa trên tốc độ di chuyển
        float speed = currentVelocity.magnitude;
        
        // Trail dài hơn khi di chuyển nhanh
        float speedMultiplier = Mathf.Clamp01(speed / 2f); // Normalize với tốc độ tối đa ~2
        trailRenderer.time = trailTime * (0.5f + speedMultiplier * 0.5f); // Từ 0.5x đến 1x trailTime
        
        // Trail rộng hơn khi di chuyển nhanh
        trailRenderer.startWidth = trailStartWidth * (0.7f + speedMultiplier * 0.3f);
        trailRenderer.endWidth = trailEndWidth * (0.7f + speedMultiplier * 0.3f);
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
