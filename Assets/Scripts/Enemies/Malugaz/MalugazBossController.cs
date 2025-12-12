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

    [Header("Phase 1 - Teleport")]
    [SerializeField] private float teleportInterval = 2f;
    [SerializeField] private float teleportRange = 5f;
    [SerializeField] private float teleportSpeed = 10f;
    [Tooltip("Transform để scale (thường là sprite transform). Nếu null sẽ dùng transform này.")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float zoomInDuration = 0.2f;
    [SerializeField] private float zoomOutDuration = 0.3f;
    [SerializeField] private float zoomInScale = 0f;

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

    private BossPhase currentPhase = BossPhase.Phase1;
    private Transform player;
    private Vector3 spawnPosition;
    private float teleportTimer;
    private float phase2IdleTimer;
    private bool isTransitioning = false;
    private bool isJumping = false;
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
        UpdateAnimationDirection();

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
        teleportTimer = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdatePhase1()
    {
        teleportTimer += Time.deltaTime;

        if (teleportTimer >= teleportInterval)
        {
            teleportTimer = 0f;
            StartCoroutine(TeleportAndShoot());
        }
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

                Vector3 spawnPos = transform.position + (Vector3)(rotatedDir * projectileSpawnDistance + phase1ProjectileOffset);

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
        UpdateAnimationDirection();
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
        // Đảo ngược Y để đồng bộ với EnemyAnim (giống như trong EnemyAnim.cs)
        animator.SetFloat("LastX", -directionToPlayer.x);
        animator.SetFloat("LastY", -directionToPlayer.y);
    }

    private void HandleDeath()
    {
        currentPhase = BossPhase.Dead;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
    }
}

