using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum DamageMode { Direct, DamageOverTime }

    [Header("Damage")]
    [SerializeField] private DamageMode damageMode = DamageMode.Direct;
    [SerializeField] private int directDamage = 1;

    [Header("DoT Settings")]
    [Tooltip("Sát thương mỗi tick")]
    [SerializeField] private int dotDamagePerTick = 1;
    [Tooltip("Khoảng thời gian giữa các tick (giây)")]
    [SerializeField] private float dotTickInterval = 0.5f;
    [Tooltip("Tổng thời gian gây hiệu ứng (giây)")]
    [SerializeField] private float dotDuration = 3f;
    [Tooltip("Hiệu ứng DoT có cộng dồn không")]
    [SerializeField] private bool dotStackable = false;

    [Header("On-Hit Slow")]
    [Tooltip("Bật làm chậm khi trúng mục tiêu")]
    [SerializeField] private bool applySlowOnHit = false;
    [Tooltip("Hệ số tốc độ khi bị slow (0.5 = còn 50%)")]
    [Range(0.05f, 1f)][SerializeField] private float slowMultiplier = 0.5f;
    [Tooltip("Thời gian slow (giây)")]
    [SerializeField] private float slowDuration = 2f;
    [Tooltip("Slow có cộng dồn không (nhiều nguồn)")]
    [SerializeField] private bool slowStackable = false;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private float projectileRange = 10f;

    [Header("Homing")]
    [Tooltip("Có tự bám về điểm click không")]
    [SerializeField] private bool useHoming = true;
    [Tooltip("Độ xoay tối đa (độ/giây) khi đã ramp lên tối đa")]
    [SerializeField] private float turnRateDegPerSec = 360f;
    [Tooltip("Trễ trước khi bắt đầu homing (giây). Sẽ cộng thêm theo từng mũi tên từ Bow.")]
    [SerializeField] private float homingStartDelay = 0.05f;
    [Tooltip("Thời gian ramp từ 0 -> full steering (giây)")]
    [SerializeField] private float homingRampTime = 0.25f;
    [Tooltip("Khoảng cách coi như đã chạm điểm target")]
    [SerializeField] private float targetHitRadius = 0.2f;
    [SerializeField] private bool destroyOnReachTarget = true;

    [Header("Safety / Despawn")]
    [Tooltip("Bật tự hủy sau một thời gian tối đa")]
    [SerializeField] private bool useMaxLifetime = true;
    [Tooltip("Thời gian sống tối đa (giây). Nếu = 0 sẽ tính theo tầm bắn.")]
    [SerializeField] private float maxLifetimeSeconds = 0f;
    [Tooltip("Nếu ở gần mục tiêu quá lâu (quay vòng), sẽ tự hủy.")]
    [SerializeField] private float orbitGuardRadius = 0.4f;
    [SerializeField] private float orbitGuardTime = 0.5f;

    [Header("Hit/VFX")]
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    [SerializeField] private bool isEnemyProjectile = false;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private float lifeTime = 0f;
    private float orbitTimer = 0f;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;
        MoveProjectile();
        DetectFireDistance();
        DetectReachTarget();
        DespawnSafety();
    }

    public void SetTarget(Vector3 targetWorldPosition)
    {
        targetPosition = targetWorldPosition;
        targetPosition.z = 0f;
        hasTarget = true;
    }

    public void SetHomingDelay(float extraDelay)
    {
        homingStartDelay += Mathf.Max(0f, extraDelay);
    }

    public void UpdateProjectileRange(float projectileRange)
    {
        this.projectileRange = projectileRange;
    }

    public void UpdateMoveSpeed(float moveSpeed)
    {
        this.moveSpeed = moveSpeed;
    }

    // NEW: cho phép code ngoài set đạn là của enemy (boss) hay không
    public void SetIsEnemyProjectile(bool value)
    {
        isEnemyProjectile = value;
    }

    private void ApplyDamageTo(EnemyHealth enemyHealth)
    {
        if (damageMode == DamageMode.Direct)
        {
            enemyHealth.TakeDamage(directDamage);
        }
        else // DamageOverTime
        {
            enemyHealth.ApplyDot(dotDamagePerTick, dotTickInterval, dotDuration, dotStackable);
        }
    }

    private void ApplySlowTo(EnemyHealth enemyHealth)
    {
        if (!applySlowOnHit) return;
        enemyHealth.ApplySlow(slowMultiplier, slowDuration, slowStackable);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // --- New: check boss redirector first ---
        BossHitRedirector redirector = other.gameObject.GetComponent<BossHitRedirector>();
        if (redirector != null)
        {
            redirector.HandleProjectileHit(this, directDamage, damageMode == DamageMode.Direct,
                dotDamagePerTick, dotTickInterval, dotDuration, dotStackable,
                applySlowOnHit, slowMultiplier, slowDuration, slowStackable,
                particleOnHitPrefabVFX);
            Destroy(gameObject);
            return;
        }

        // --- Original behavior (non-boss or not handled by redirector) ---
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        Indestructible indestructible = other.gameObject.GetComponent<Indestructible>();
        PlayerHealth player = other.gameObject.GetComponent<PlayerHealth>();

        if (!other.isTrigger && (enemyHealth || indestructible || player))
        {
            if ((player && isEnemyProjectile) || (enemyHealth && !isEnemyProjectile))
            {
                if (player && isEnemyProjectile)
                {
                    player.TakeDamage(1, transform);
                }
                if (enemyHealth && !isEnemyProjectile)
                {
                    ApplyDamageTo(enemyHealth);
                    ApplySlowTo(enemyHealth);
                }

                if (particleOnHitPrefabVFX) Instantiate(particleOnHitPrefabVFX, transform.position, transform.rotation);
                Destroy(gameObject);
            }
            else if (!other.isTrigger && indestructible)
            {
                if (particleOnHitPrefabVFX) Instantiate(particleOnHitPrefabVFX, transform.position, transform.rotation);
                Destroy(gameObject);
            }
        }
    }

    private void DetectFireDistance()
    {
        float sqr = (transform.position - startPosition).sqrMagnitude;
        if (sqr > projectileRange * projectileRange)
        {
            Destroy(gameObject);
        }
    }

    private void DetectReachTarget()
    {
        if (!hasTarget) return;

        float dist = Vector3.Distance(transform.position, targetPosition);

        if (dist <= targetHitRadius)
        {
            if (destroyOnReachTarget)
            {
                if (particleOnHitPrefabVFX) Instantiate(particleOnHitPrefabVFX, transform.position, transform.rotation);
                Destroy(gameObject);
                return;
            }
            else
            {
                hasTarget = false;
            }
        }

        if (dist <= orbitGuardRadius)
        {
            orbitTimer += Time.deltaTime;
            if (orbitTimer >= orbitGuardTime)
            {
                if (particleOnHitPrefabVFX) Instantiate(particleOnHitPrefabVFX, transform.position, transform.rotation);
                Destroy(gameObject);
            }
        }
        else
        {
            orbitTimer = 0f;
        }
    }

    private void DespawnSafety()
    {
        if (!useMaxLifetime) return;

        float fallbackMax = (projectileRange > 0f && moveSpeed > 0f) ? (projectileRange / moveSpeed) * 1.75f : 6f;
        float lifetimeCap = (maxLifetimeSeconds > 0f) ? maxLifetimeSeconds : fallbackMax;

        if (lifeTime >= lifetimeCap)
        {
            Destroy(gameObject);
        }
    }

    private void MoveProjectile()
    {
        Vector3 dir = transform.right;

        if (useHoming && hasTarget)
        {
            float t = Mathf.Clamp01((lifeTime - homingStartDelay) / Mathf.Max(0.0001f, homingRampTime));
            if (t > 0f)
            {
                float ease = t * t;
                Vector3 toTarget = (targetPosition - transform.position);
                Vector3 desiredDir = toTarget.sqrMagnitude > 0.000001f ? toTarget.normalized : dir;

                float maxRadiansDelta = Mathf.Deg2Rad * turnRateDegPerSec * ease * Time.deltaTime;
                Vector3 newDir = Vector3.RotateTowards(dir, desiredDir, maxRadiansDelta, 0f);

                if (newDir.sqrMagnitude > 0.000001f)
                {
                    transform.right = newDir;
                    dir = newDir;
                }
            }
        }

        transform.position += dir * moveSpeed * Time.deltaTime;
    }
}
