using UnityEngine;

public class PetFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow Settings")]
    [SerializeField] private Vector2 baseOffset = new Vector2(-0.7f, 0.7f);
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float followLag = 0.3f;
    [SerializeField] private float followDelay = 0.5f;

    [Header("Floating Motion")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatOffset = 0f;

    [Header("Free Movement")]
    [SerializeField] private float wanderRadius = 0.3f;
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderChangeInterval = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSmoothness = 5f;
    [SerializeField] private float maxRotationAngle = 15f;

    [Header("Trail Effect")]
    [Tooltip("Trail Renderer component")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private Gradient trailGradient;
    [SerializeField] private float trailTime = 0.5f;
    [SerializeField] private float trailStartWidth = 0.3f;
    [SerializeField] private float trailEndWidth = 0.1f;
    [SerializeField] private Material trailMaterial;

    private Vector3 currentVelocity;
    private Vector3 targetPosition;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private float floatTimer;
    private Vector3 lastPlayerPosition;
    private float currentRotation;
    private float followDelayTimer;
    private bool isFollowing;
    private bool playerWasMoving;
    private Vector3 playerStartPosition;

    private void Start()
    {
        if (player == null)
        {
            if (PlayerController.Instance != null) player = PlayerController.Instance.transform;
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }
        }

        if (player == null)
        {
            Debug.LogWarning($"[PetFollower] {gameObject.name}: Player not found!");
            return;
        }

        targetPosition = transform.position;
        wanderTarget = Vector3.zero;
        wanderTimer = Random.Range(0f, wanderChangeInterval);
        floatTimer = floatOffset;
        lastPlayerPosition = player.position;
        playerStartPosition = lastPlayerPosition;
        
        followDelayTimer = 0f;
        isFollowing = false;
        playerWasMoving = false;

        SetupTrailRenderer();
    }

    private void SetupTrailRenderer()
    {
        if (!enableTrail) return;
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null) trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailStartWidth;
        trailRenderer.endWidth = trailEndWidth;
        trailRenderer.minVertexDistance = 0.1f;
        trailRenderer.textureMode = LineTextureMode.Tile;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        
        if (trailGradient != null) trailRenderer.colorGradient = trailGradient;
        if (trailMaterial != null) trailRenderer.material = trailMaterial;
        
        trailRenderer.enabled = true;
    }

    private void Update()
    {
        // --- FIX QUAN TRỌNG: Nếu đang Pause game, không làm gì cả ---
        if (Time.deltaTime <= Mathf.Epsilon) return; 

        if (player == null)
        {
            if (PlayerController.Instance != null) player = PlayerController.Instance.transform;
            if (player == null) return;
        }

        Vector3 playerMovement = player.position - lastPlayerPosition;
        float movementDistance = playerMovement.magnitude;
        bool playerIsMoving = movementDistance > 0.01f;

        if (followDelay <= 0f)
        {
            isFollowing = true;
        }
        else if (!isFollowing)
        {
            if (playerIsMoving && !playerWasMoving)
            {
                playerWasMoving = true;
                playerStartPosition = player.position;
                followDelayTimer = followDelay;
            }
            else if (!playerIsMoving && playerWasMoving)
            {
                playerWasMoving = false;
                followDelayTimer = 0f;
            }

            if (followDelayTimer > 0f)
            {
                followDelayTimer -= Time.deltaTime;
                if (followDelayTimer <= 0f)
                {
                    isFollowing = true;
                    lastPlayerPosition = player.position;
                    currentVelocity = Vector3.zero;
                }
            }

            if (!isFollowing)
            {
                UpdateSpriteFlip();
                if (currentRotation != 0f)
                {
                    currentRotation = Mathf.LerpAngle(currentRotation, 0f, Time.deltaTime * rotationSmoothness);
                    transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
                }
                lastPlayerPosition = player.position;
                return;
            }
        }
        else
        {
            playerWasMoving = playerIsMoving;
        }

        Vector3 baseTargetPos = player.position + (Vector3)baseOffset;

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = baseTargetPos + (Vector3)randomOffset;
            wanderTimer = wanderChangeInterval + Random.Range(-0.5f, 0.5f);
        }

        wanderTarget = Vector3.Lerp(wanderTarget, baseTargetPos, Time.deltaTime * wanderSpeed * 0.5f);

        floatTimer += Time.deltaTime * floatSpeed;
        float floatY = Mathf.Sin(floatTimer) * floatAmplitude;
        targetPosition = wanderTarget + Vector3.up * floatY;

        // --- FIX QUAN TRỌNG: Kiểm tra chia cho 0 ---
        Vector3 playerVelocity = Vector3.zero;
        if (Time.deltaTime > 0.0001f) // Chỉ tính vận tốc nếu thời gian trôi qua đủ lớn
        {
            playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
        }

        Vector3 predictedPosition = targetPosition + (Vector3)playerVelocity * followLag;
        
        transform.position = Vector3.SmoothDamp(
            transform.position,
            predictedPosition,
            ref currentVelocity,
            1f / followSpeed,
            Mathf.Infinity,
            Time.deltaTime
        );

        lastPlayerPosition = player.position;

        UpdateRotation();
        UpdateSpriteFlip();
        UpdateTrailEffect();
    }

    private void UpdateTrailEffect()
    {
        if (!enableTrail || trailRenderer == null) return;
        float speed = currentVelocity.magnitude;
        float speedMultiplier = Mathf.Clamp01(speed / 2f);
        trailRenderer.time = trailTime * (0.5f + speedMultiplier * 0.5f);
        trailRenderer.startWidth = trailStartWidth * (0.7f + speedMultiplier * 0.3f);
        trailRenderer.endWidth = trailEndWidth * (0.7f + speedMultiplier * 0.3f);
    }

    private void UpdateRotation()
    {
        if (currentVelocity.magnitude > 0.01f)
        {
            float targetRotation = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
            targetRotation = Mathf.Clamp(targetRotation, -maxRotationAngle, maxRotationAngle);
            currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSmoothness);
        }
        else
        {
            currentRotation = Mathf.LerpAngle(currentRotation, 0f, Time.deltaTime * rotationSmoothness);
        }
        transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);
    }

    private void UpdateSpriteFlip()
    {
        Vector3 scale = transform.localScale;
        if (transform.position.x < player.position.x) scale.x = Mathf.Abs(scale.x);
        else scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        if (player != null) lastPlayerPosition = player.position;
    }
}