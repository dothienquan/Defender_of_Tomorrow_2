using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Malugaz Boss Controller với 2 phase:
/// Phase 1: Teleport liên tục mỗi 2 giây và bắn projectile 4 hướng
/// Phase 2: Đứng yên -> nhảy đến player -> bắn 8 hướng + để lại 1 projectile
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyAnim))]
[RequireComponent(typeof(Rigidbody2D))]
public class MalugazBossController : MonoBehaviour
{
    private enum BossPhase
    {
        Phase1,
        Transitioning,
        Phase2,
        Dead
    }

    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyAnim enemyAnim;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [Header("Death Activation")]
    [Tooltip("Object có sẵn trên map, sẽ được kích hoạt sau khi boss chết.")]
    [SerializeField] private GameObject deathSpawnObject;
    [Tooltip("Thời gian fade in (giây).")]
    [SerializeField] private float fadeInDuration = 1f;
    [Tooltip("Ease type cho fade animation.")]
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Death Movement")]
    [Tooltip("Object sẽ được di chuyển sau khi boss chết (delay 7s).")]
    [SerializeField] private GameObject moveObject;
    [Tooltip("Offset di chuyển từ vị trí ban đầu.")]
    [SerializeField] private Vector3 moveOffset = Vector3.zero;
    [Tooltip("Thời gian di chuyển (giây).")]
    [SerializeField] private float moveDuration = 2f;
    [Tooltip("Ease type cho movement animation.")]
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Phase 1 - Teleport")]
    [SerializeField] private float teleportInterval = 2f;
    [SerializeField] private float teleportRange = 5f;
    [SerializeField] private float teleportSpeed = 10f;
    [Tooltip("Transform để scale (thường là sprite transform). Nếu null sẽ dùng transform này.")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float zoomInDuration = 0.2f;
    [SerializeField] private float zoomOutDuration = 0.3f;
    [SerializeField] private float zoomInScale = 0f;

    [Header("Phase 1 - Teleport Phase")]
    [Tooltip("Thời gian thực hiện teleport và bắn (giây).")]
    [SerializeField] private float teleportPhaseDuration = 8f;
    [Tooltip("Thời gian nghỉ sau teleport phase (giây).")]
    [SerializeField] private float restAfterTeleportDuration = 2f;
    
    [Header("Phase 1 - Projectiles")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileSpawnDistance = 0.5f;
    [SerializeField] private Vector2 phase1ProjectileOffset = Vector2.zero;
    [Tooltip("Thời gian delay sau khi chạy Attack animation trước khi bắn đạn.")]
    [SerializeField] private float attackAnimationDelay = 0.2f;
    [Tooltip("Số đợt đạn bắn ra mỗi lần tấn công.")]
    [SerializeField] private int phase1BurstCount = 3;
    [Tooltip("Góc xoay giữa các đợt đạn (độ).")]
    [SerializeField] private float phase1BurstRotationAngle = 15f;
    [Tooltip("Thời gian delay giữa các đợt đạn.")]
    [SerializeField] private float phase1BurstDelay = 0.15f;

    [Header("Phase 1 - Attack Pattern 2 (Rectangle Projectiles)")]
    [Tooltip("Prefab projectile dùng cho attack pattern 2 (hình chữ nhật). Nếu null sẽ dùng projectilePrefab.")]
    [SerializeField] private GameObject rectangleProjectilePrefab;
    [Tooltip("Các vị trí boss sẽ di chuyển đến để bắn (tối thiểu 2 vị trí).")]
    [SerializeField] private Transform[] attackPositions = new Transform[2];
    [Tooltip("Tốc độ di chuyển đến vị trí attack.")]
    [SerializeField] private float moveToAttackPositionSpeed = 5f;
    [Tooltip("Khoảng cách coi như đã đến vị trí attack.")]
    [SerializeField] private float attackPositionReachDistance = 0.5f;
    [Tooltip("Số đợt projectile bắn ra tại mỗi vị trí.")]
    [SerializeField] private int rectangleBurstCount = 5;
    [Tooltip("Thời gian delay giữa các đợt projectile.")]
    [SerializeField] private float rectangleBurstDelay = 0.3f;
    [Tooltip("Kích thước projectile hình chữ nhật (width x height).")]
    [SerializeField] private Vector2 rectangleProjectileSize = new Vector2(4f, 6f);
    [Tooltip("Hướng bắn projectile (normalized vector).")]
    [SerializeField] private Vector2 rectangleShootDirection = Vector2.down;
    [Tooltip("Khoảng cách spawn projectile từ boss.")]
    [SerializeField] private float rectangleSpawnDistance = 1f;
    [Tooltip("Thời gian sống của projectile hình chữ nhật (giây).")]
    [SerializeField] private float rectangleProjectileLifetime = 5f;
    [Tooltip("Thời gian nghỉ sau khi hoàn tất attack pattern 2 (giây).")]
    [SerializeField] private float restAfterPattern2Duration = 2f;

    [Header("Phase Transition")]
    [SerializeField] private float transitionMoveSpeed = 2f;
    [SerializeField] private float spawnReachDistance = 0.5f;

    [Header("Phase 2 - Jump")]
    [SerializeField] private float phase2IdleTime = 2.5f;
    [SerializeField] private float jumpSpeed = 15f;
    [SerializeField] private float jumpReachDistance = 0.3f;

    [Header("Phase 2 - Projectiles")]
    [Tooltip("Số lượng projectile bắn ra (sẽ chia đều 360 độ).")]
    [SerializeField] private int phase2ProjectileCount = 8;
    [SerializeField] private float phase2ProjectileLifetime = 2f;
    [SerializeField] private Vector2 phase2ProjectileOffset = Vector2.zero;
    [SerializeField] private GameObject phase2PersistentProjectilePrefab;
    [SerializeField] private float phase2PersistentProjectileLifetime = 10f;
    [SerializeField] private Vector2 phase2PersistentProjectileOffset = Vector2.zero;

    private enum Phase1State
    {
        TeleportPhase,
        RestAfterTeleport,
        AttackPattern2,
        RestAfterPattern2
    }

    private BossPhase currentPhase = BossPhase.Phase1;
    private Phase1State phase1State = Phase1State.TeleportPhase;
    private Transform player;
    private Vector3 spawnPosition;
    private float teleportTimer;
    private float phase1StateTimer;
    private float phase2IdleTimer;
    private bool isTransitioning = false;
    private bool isJumping = false;
    private bool isMovingToAttackPosition = false;
    private Vector3 jumpTarget;
    private int lastHealth;
    private Vector3 originalScale;
    private Transform scaleTransform;

    private void Awake()
    {
        if (!enemyHealth) enemyHealth = GetComponent<EnemyHealth>();
        if (!enemyAnim) enemyAnim = GetComponent<EnemyAnim>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();

        // Xác định transform để scale
        scaleTransform = scaleTarget != null ? scaleTarget : transform;
        originalScale = scaleTransform.localScale;

        spawnPosition = transform.position;
        lastHealth = enemyHealth.GetCurrentHealth();

        // Đảm bảo object ban đầu tắt
        if (deathSpawnObject != null)
        {
            deathSpawnObject.SetActive(false);
        }

        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += HandleDeath;
        }
    }

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
        else
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // Bắt đầu Phase 1
        StartPhase1();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= HandleDeath;
        }

        // Dừng tất cả DOTween animations
        if (scaleTransform != null)
        {
            scaleTransform.DOKill();
        }
    }

    private void Update()
    {
        if (currentPhase == BossPhase.Dead) return;

        // Cập nhật animation direction liên tục để idle/run/walk đúng hướng
        // Chỉ cập nhật khi không đang trong attack animation
        if (!IsAttackAnimationPlaying())
        {
            UpdateAnimationDirection();
        }

        // Kiểm tra health để chuyển phase
        CheckHealthThreshold();

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                UpdatePhase1();
                break;
            case BossPhase.Transitioning:
                UpdateTransition();
                break;
            case BossPhase.Phase2:
                UpdatePhase2();
                break;
        }
    }

    private void CheckHealthThreshold()
    {
        if (currentPhase != BossPhase.Phase1) return;
        if (isTransitioning) return;

        int currentHealth = enemyHealth.GetCurrentHealth();
        float healthPercentage = enemyHealth.GetHealthPercentage();

        // Khi còn 50% máu, bắt đầu transition
        if (healthPercentage <= 0.5f && currentHealth < lastHealth)
        {
            StartTransition();
        }

        lastHealth = currentHealth;
    }

    private void StartPhase1()
    {
        currentPhase = BossPhase.Phase1;
        phase1State = Phase1State.TeleportPhase;
        teleportTimer = 0f;
        phase1StateTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        
        // Set hướng ban đầu để boss quay mặt về phía player khi spawn
        UpdateAnimationDirection();
        
        // Bắt đầu teleport phase
        StartCoroutine(Phase1TeleportLoop());
    }

    private void UpdatePhase1()
    {
        // Logic phase 1 được xử lý bởi các coroutines
        // Không cần update timer ở đây nữa
    }

    private IEnumerator Phase1TeleportLoop()
    {
        // Teleport phase: teleport và bắn trong 8 giây
        float elapsed = 0f;
        while (elapsed < teleportPhaseDuration && currentPhase == BossPhase.Phase1)
        {
            // Teleport và bắn
            yield return StartCoroutine(TeleportAndShoot());
            
            // Đợi đến lần teleport tiếp theo
            float waitTime = Mathf.Min(teleportInterval, teleportPhaseDuration - elapsed);
            elapsed += waitTime;
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
        }

        // Nghỉ 2 giây
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(restAfterTeleportDuration);

        // Bắt đầu Attack Pattern 2
        if (currentPhase == BossPhase.Phase1)
        {
            yield return StartCoroutine(Phase1AttackPattern2());
        }
    }

    private IEnumerator Phase1AttackPattern2()
    {
        if (attackPositions == null || attackPositions.Length < 2)
        {
            Debug.LogWarning("[MalugazBossController] Attack positions not set! Need at least 2 positions.");
            yield break;
        }

        // Di chuyển và bắn tại mỗi vị trí
        for (int posIndex = 0; posIndex < attackPositions.Length; posIndex++)
        {
            if (attackPositions[posIndex] == null) continue;
            if (currentPhase != BossPhase.Phase1) yield break;

            // Di chuyển đến vị trí attack
            yield return StartCoroutine(MoveToAttackPosition(attackPositions[posIndex].position));

            // Bắn 5 đợt projectile hình chữ nhật về phía player
            for (int burst = 0; burst < rectangleBurstCount; burst++)
            {
                if (currentPhase != BossPhase.Phase1) yield break;
                
                // Set attack animation direction về phía player trước khi bắn
                SetAttackAnimationDirection();
                
                // Chơi attack animation
                enemyAnim.PlayAttack();
                
                // Đợi một chút để animation Attack chạy
                yield return new WaitForSeconds(attackAnimationDelay);
                
                // Bắn về phía player
                ShootRectangleProjectile();
                
                if (burst < rectangleBurstCount - 1)
                {
                    yield return new WaitForSeconds(rectangleBurstDelay);
                }
            }
        }

        // Nghỉ 2 giây sau khi hoàn tất pattern 2
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(restAfterPattern2Duration);

        // Lặp lại từ đầu
        if (currentPhase == BossPhase.Phase1)
        {
            StartCoroutine(Phase1TeleportLoop());
        }
    }

    private IEnumerator MoveToAttackPosition(Vector3 targetPosition)
    {
        isMovingToAttackPosition = true;

        float distance = Vector3.Distance(transform.position, targetPosition);
        float reachDistanceSqr = attackPositionReachDistance * attackPositionReachDistance;

        // Di chuyển đến vị trí target bằng cách set velocity
        // EnemyAnim sẽ tự động detect velocity và set animation locomotion đúng hướng
        while (currentPhase == BossPhase.Phase1)
        {
            Vector2 directionToTarget = ((Vector2)targetPosition - (Vector2)transform.position);
            float distanceSqr = directionToTarget.sqrMagnitude;

            // Kiểm tra xem đã đến gần đủ chưa
            if (distanceSqr <= reachDistanceSqr)
            {
                break;
            }

            // Set velocity để di chuyển và EnemyAnim sẽ tự động set animation
            Vector2 moveDirection = directionToTarget.normalized;
            rb.linearVelocity = moveDirection * moveToAttackPositionSpeed;
            
            yield return null;
        }

        // Đã đến nơi, dừng lại
        transform.position = targetPosition;
        rb.linearVelocity = Vector2.zero;
        
        isMovingToAttackPosition = false;
    }

    private void ShootRectangleProjectile()
    {
        // Sử dụng rectangleProjectilePrefab nếu có, nếu không thì dùng projectilePrefab
        GameObject prefabToUse = rectangleProjectilePrefab != null ? rectangleProjectilePrefab : projectilePrefab;
        
        if (prefabToUse == null) return;

        // Tính hướng bắn về phía player
        Vector2 shootDirection;
        if (player != null)
        {
            shootDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        }
        else
        {
            // Fallback: dùng hướng mặc định nếu không có player
            shootDirection = rectangleShootDirection.normalized;
        }

        // Spawn projectile từ giữa boss với offset Y = 6
        Vector3 spawnPos = transform.position + new Vector3(0f, 6f, 0f);

        // Tạo projectile
        GameObject projectile = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
        
        // Set hướng
        projectile.transform.right = shootDirection;
        
        // Scale SpriteRenderer để tạo hình chữ nhật (4x6)
        // Lấy kích thước gốc của sprite để tính scale chính xác
        SpriteRenderer spriteRenderer = projectile.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Lấy kích thước gốc của sprite (trong world units)
            float spriteWidth = spriteRenderer.sprite.bounds.size.x;
            float spriteHeight = spriteRenderer.sprite.bounds.size.y;
            
            // Tính scale để đạt kích thước mong muốn
            float scaleX = rectangleProjectileSize.x / spriteWidth;
            float scaleY = rectangleProjectileSize.y / spriteHeight;
            
            projectile.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            // Fallback: scale trực tiếp nếu không có SpriteRenderer
            projectile.transform.localScale = new Vector3(
                rectangleProjectileSize.x,
                rectangleProjectileSize.y,
                1f
            );
        }

        // Cấu hình projectile
        if (projectile.TryGetComponent(out Projectile proj))
        {
            proj.UpdateMoveSpeed(projectileSpeed);
            proj.SetIsEnemyProjectile(true);
        }

        // Tự hủy sau vài giây
        StartCoroutine(DestroyProjectileAfterTime(projectile, rectangleProjectileLifetime));
    }

    private IEnumerator TeleportAndShoot()
    {
        // Zoom nhỏ lại trước khi teleport
        scaleTransform.DOKill();
        Tween zoomIn = scaleTransform.DOScale(zoomInScale, zoomInDuration).SetEase(Ease.InBack);
        yield return zoomIn.WaitForCompletion();

        // Teleport đến vị trí ngẫu nhiên trong phạm vi
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 targetPosition = spawnPosition + (Vector3)(randomDirection * Random.Range(2f, teleportRange));

        // Di chuyển đến vị trí mới (teleport effect)
        float distance = Vector3.Distance(transform.position, targetPosition);
        float travelTime = distance / teleportSpeed;

        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        // Zoom ra kích thước ban đầu sau khi teleport
        Tween zoomOut = scaleTransform.DOScale(originalScale, zoomOutDuration).SetEase(Ease.OutBack);
        yield return zoomOut.WaitForCompletion();

        // Set animation direction dựa trên hướng tới player trước khi chơi Attack
        SetAttackAnimationDirection();
        
        // Chạy animation Attack trước khi bắn
        enemyAnim.PlayAttack();
        
        // Đợi một chút để animation Attack chạy
        yield return new WaitForSeconds(attackAnimationDelay);

        // Bắn nhiều đợt projectile 4 hướng với góc khác nhau
        yield return StartCoroutine(ShootFourDirectionsBurst());
    }

    private IEnumerator ShootFourDirectionsBurst()
    {
        if (projectilePrefab == null) yield break;

        // Hướng cơ bản (4 hướng chính)
        Vector2[] baseDirections = new Vector2[]
        {
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left
        };

        // Bắn nhiều đợt với góc xoay khác nhau
        for (int burst = 0; burst < phase1BurstCount; burst++)
        {
            // Tính góc xoay cho đợt này
            float rotationAngle = burst * phase1BurstRotationAngle;
            float rotationRad = rotationAngle * Mathf.Deg2Rad;

            // Xoay các hướng
            for (int i = 0; i < baseDirections.Length; i++)
            {
                Vector2 baseDir = baseDirections[i];
                
                // Xoay vector bằng cách nhân với rotation matrix
                Vector2 rotatedDir = new Vector2(
                    baseDir.x * Mathf.Cos(rotationRad) - baseDir.y * Mathf.Sin(rotationRad),
                    baseDir.x * Mathf.Sin(rotationRad) + baseDir.y * Mathf.Cos(rotationRad)
                );

                // Spawn projectile từ giữa boss với offset Y = 6
                Vector3 spawnPos = transform.position + new Vector3(0f, 6f, 0f);

                GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                projectile.transform.right = rotatedDir;

                if (projectile.TryGetComponent(out Projectile proj))
                {
                    proj.UpdateMoveSpeed(projectileSpeed);
                    proj.SetIsEnemyProjectile(true);
                }
            }

            // Delay giữa các đợt (trừ đợt cuối)
            if (burst < phase1BurstCount - 1)
            {
                yield return new WaitForSeconds(phase1BurstDelay);
            }
        }
    }

    private void StartTransition()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        currentPhase = BossPhase.Transitioning;

        // Chơi animation Hurt
        enemyAnim.PlayHurt();

        // Vô hiệu hóa nhận sát thương
        enemyHealth.SetCanTakeDamage(false);

        // Dừng tất cả coroutines
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateTransition()
    {
        if (!isTransitioning) return;

        // Di chuyển từ từ về spawn position
        Vector3 direction = (spawnPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, spawnPosition);

        if (distance > spawnReachDistance)
        {
            rb.linearVelocity = direction * transitionMoveSpeed;
        }
        else
        {
            // Đã đến spawn position
            transform.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            isTransitioning = false;

            // Bắt đầu Phase 2
            StartPhase2();
        }
    }

    private void StartPhase2()
    {
        currentPhase = BossPhase.Phase2;
        phase2IdleTimer = 0f;
        isJumping = false;

        // Bật lại nhận sát thương
        enemyHealth.SetCanTakeDamage(true);
    }

    private void UpdatePhase2()
    {
        if (isJumping) return;

        phase2IdleTimer += Time.deltaTime;

        if (phase2IdleTimer >= phase2IdleTime)
        {
            phase2IdleTimer = 0f;
            StartCoroutine(Phase2JumpAndShoot());
        }
    }

    private IEnumerator Phase2JumpAndShoot()
    {
        if (player == null) yield break;

        isJumping = true;
        jumpTarget = player.position;
        rb.linearVelocity = Vector2.zero;

        // Nhảy đến player
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, jumpTarget);
        float jumpTime = distance / jumpSpeed;

        float elapsed = 0f;
        while (elapsed < jumpTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpTime;
            transform.position = Vector3.Lerp(startPos, jumpTarget, t);
            yield return null;
        }

        transform.position = jumpTarget;

        // Set animation direction dựa trên hướng tới player trước khi chơi Attack
        SetAttackAnimationDirection();
        
        // Chạy animation Attack trước khi bắn
        enemyAnim.PlayAttack();
        
        // Đợi một chút để animation Attack chạy
        yield return new WaitForSeconds(attackAnimationDelay);

        // Bắn 8 hướng
        ShootEightDirections();

        isJumping = false;
    }

    private void ShootEightDirections()
    {
        if (projectilePrefab == null) return;

        // Tính toán các hướng dựa trên số lượng projectile
        // Chia đều 360 độ
        float angleStep = 360f / phase2ProjectileCount;

        // Bắn các projectile (biến mất sau 2s hoặc va chạm player)
        for (int i = 0; i < phase2ProjectileCount; i++)
        {
            // Tính góc (bắt đầu từ hướng lên - 90 độ)
            float angle = (i * angleStep - 90f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            Vector3 spawnPos = transform.position + (Vector3)(direction * projectileSpawnDistance + phase2ProjectileOffset);

            GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            projectile.transform.right = direction;

            if (projectile.TryGetComponent(out Projectile proj))
            {
                proj.UpdateMoveSpeed(projectileSpeed);
                proj.SetIsEnemyProjectile(true);
            }

            // Tự hủy sau 2 giây
            StartCoroutine(DestroyProjectileAfterTime(projectile, phase2ProjectileLifetime));
        }

        // Để lại 1 projectile tồn tại 10 giây (sử dụng prefab riêng nếu có)
        GameObject persistentPrefab = phase2PersistentProjectilePrefab != null ? phase2PersistentProjectilePrefab : projectilePrefab;
        Vector3 persistentSpawnPos = transform.position + (Vector3)(Vector2.up * projectileSpawnDistance + phase2PersistentProjectileOffset);
        GameObject persistentProjectile = Instantiate(persistentPrefab, persistentSpawnPos, Quaternion.identity);
        persistentProjectile.transform.right = Vector2.up;

        if (persistentProjectile.TryGetComponent(out Projectile persistentProj))
        {
            persistentProj.UpdateMoveSpeed(projectileSpeed);
            persistentProj.SetIsEnemyProjectile(true);
        }

        // Tự hủy sau 10 giây
        StartCoroutine(DestroyProjectileAfterTime(persistentProjectile, phase2PersistentProjectileLifetime));
    }

    private IEnumerator DestroyProjectileAfterTime(GameObject projectile, float lifetime)
    {
        if (projectile == null) yield break;

        yield return new WaitForSeconds(lifetime);

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private void SetAttackAnimationDirection()
    {
        if (animator == null) return;

        Vector2 directionToPlayer;
        
        // Nếu có player, dùng hướng tới player
        if (player != null)
        {
            directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        }
        else
        {
            // Nếu không có player, dùng hướng di chuyển hiện tại hoặc hướng mặc định (xuống)
            Vector2 velocity = rb.linearVelocity;
            if (velocity.magnitude > 0.001f)
            {
                directionToPlayer = velocity.normalized;
            }
            else
            {
                directionToPlayer = Vector2.down; // Mặc định hướng xuống
            }
        }

        // Set LastX và LastY để animation biết hướng
        // Đảo ngược X để fix animation trái/phải bị ngược
        // KHÔNG đảo ngược Y cho attack animation (giống FrozenSlimeController và GolemController)
        animator.SetFloat("LastX", -directionToPlayer.x);
        animator.SetFloat("LastY", directionToPlayer.y);
    }

    private bool IsAttackAnimationPlaying()
    {
        if (animator == null) return false;
        
        // Kiểm tra xem có đang trong attack state không
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Kiểm tra bằng tag (nếu animator có tag "Attack")
        if (stateInfo.IsTag("Attack"))
            return true;
        
        // Kiểm tra bằng tên state - kiểm tra các tên phổ biến
        return stateInfo.IsName("Attack") || stateInfo.IsName("AttackUp") || 
               stateInfo.IsName("AttackDown") || stateInfo.IsName("AttackLeft") || 
               stateInfo.IsName("AttackRight") || stateInfo.IsName("Base Layer.Attack") ||
               stateInfo.IsName("Base Layer.AttackUp") || stateInfo.IsName("Base Layer.AttackDown") ||
               stateInfo.IsName("Base Layer.AttackLeft") || stateInfo.IsName("Base Layer.AttackRight");
    }

    private void UpdateAnimationDirection()
    {
        if (animator == null) return;

        Vector2 directionToPlayer;
        
        // Nếu có player, dùng hướng tới player
        if (player != null)
        {
            directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        }
        else
        {
            // Nếu không có player, dùng hướng di chuyển hiện tại hoặc hướng mặc định (xuống)
            Vector2 velocity = rb.linearVelocity;
            if (velocity.magnitude > 0.001f)
            {
                directionToPlayer = velocity.normalized;
            }
            else
            {
                directionToPlayer = Vector2.down; // Mặc định hướng xuống
            }
        }

        // Set LastX và LastY để animation biết hướng
        // Đảo ngược X để fix animation trái/phải bị ngược
        // KHÔNG đảo ngược Y để boss quay mặt về phía player (giống attack animation)
        animator.SetFloat("LastX", -directionToPlayer.x);
        animator.SetFloat("LastY", directionToPlayer.y);
    }

    private void HandleDeath()
    {
        currentPhase = BossPhase.Dead;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;

        // Kích hoạt object có sẵn khi boss chết với fade effect
        if (deathSpawnObject != null)
        {
            FadeInObject(deathSpawnObject, fadeInDuration);
        }

        // Di chuyển object khác sau 7 giây (sử dụng DOTween.Sequence để tránh bị StopAllCoroutines dừng)
        if (moveObject != null)
        {
            Debug.Log($"[MalugazBossController] Scheduling move for object '{moveObject.name}' after 7 seconds.");
            
            // Sử dụng DOTween.Sequence thay vì coroutine để tránh bị StopAllCoroutines dừng
            Sequence moveSequence = DOTween.Sequence();
            moveSequence.AppendInterval(7f);
            moveSequence.AppendCallback(() => MoveObjectDelayed());
        }
        else
        {
            Debug.LogWarning("[MalugazBossController] Move object is not assigned!");
        }
    }

    private void MoveObjectDelayed()
    {
        if (moveObject == null)
        {
            Debug.LogWarning("[MalugazBossController] Move object is null after delay!");
            return;
        }

        // Đảm bảo object được active
        if (!moveObject.activeInHierarchy)
        {
            moveObject.SetActive(true);
            Debug.Log($"[MalugazBossController] Activated move object '{moveObject.name}'.");
        }

        // Lưu vị trí ban đầu
        Vector3 startPosition = moveObject.transform.position;
        Vector3 targetPosition = startPosition + moveOffset;

        Debug.Log($"[MalugazBossController] Moving object '{moveObject.name}' from {startPosition} to {targetPosition} (duration: {moveDuration}s).");

        // Di chuyển object bằng DOTween
        moveObject.transform.DOMove(targetPosition, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() => 
            {
                Debug.Log($"[MalugazBossController] Move completed for '{moveObject.name}'.");
            });
    }

    private void FadeInObject(GameObject obj, float duration)
    {
        if (obj == null) return;

        obj.SetActive(true);

        // Tìm tất cả SpriteRenderer và CanvasGroup để fade in
        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        CanvasGroup[] canvasGroups = obj.GetComponentsInChildren<CanvasGroup>(true);

        // Fade in tất cả SpriteRenderer
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null)
            {
                Color originalColor = sr.color;
                originalColor.a = 0f;
                sr.color = originalColor;
                sr.DOFade(1f, duration).SetEase(fadeEase);
            }
        }

        // Fade in tất cả CanvasGroup
        foreach (CanvasGroup cg in canvasGroups)
        {
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.DOFade(1f, duration).SetEase(fadeEase);
            }
        }

        // Nếu không có SpriteRenderer hoặc CanvasGroup, log warning
        if (spriteRenderers.Length == 0 && canvasGroups.Length == 0)
        {
            Debug.LogWarning($"[MalugazBossController] Object '{obj.name}' has no SpriteRenderer or CanvasGroup for fade effect.");
        }
        else
        {
            Debug.Log($"[MalugazBossController] Object '{obj.name}' activated with fade effect.");
        }
    }
}

