using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(EnemyPathfinding))]
[RequireComponent(typeof(EnemyHealth))]
public class MalugazAI : MonoBehaviour
{
    public enum BossPhase { Phase1, Phase2, Dead }

    private enum FirePattern
    {
        Straight,
        Spread,
        Circle
    }

    [Header("General")]
    [SerializeField] private BossPhase startingPhase = BossPhase.Phase1;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 6f;

    [Header("Visual / Animator")]
    [SerializeField] private MalugazAnimator malugazAnimator;   // <- script chuyên animator

    // ================== PHASE 1 (FIRE MAGE) ==================

    [Header("Phase 1 - Fire Mage")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform fireballSpawnPoint;

    [Header("Fireball Patterns")]
    [Tooltip("Chu kỳ pattern bắn. Boss sẽ bắn theo thứ tự mảng này: 0 -> 1 -> 2 -> ... -> 0.")]
    [SerializeField]
    private FirePattern[] firePatterns = new FirePattern[]
    {
        FirePattern.Straight,
        FirePattern.Spread,
        FirePattern.Circle
    };

    [SerializeField] private float fireballSpeed = 6f;
    [SerializeField] private int fireballDamage = 1;

    [Header("Straight Settings")]
    [SerializeField] private int straightCount = 1;

    [Header("Spread Settings")]
    [SerializeField] private int fireballCount = 3;
    [SerializeField] private float fireballSpreadAngle = 30;

    [Header("Circle Settings")]
    [SerializeField] private int circleCount = 8;

    [Header("Flame Zone")]
    [SerializeField] private GameObject flameZonePrefab;
    [SerializeField] private int randomFlameCount = 5;
    [SerializeField] private float telegraphFlameDelay = 0.75f;

    [Header("Phase 1 Timers")]
    [SerializeField] private float phase1AttackInterval = 2.5f;
    [SerializeField] private float teleportIntervalMin = 3f;
    [SerializeField] private float teleportIntervalMax = 5f;

    // ================== PHASE 2 (MELEE) ==================

    [Header("Phase 2 - Melee")]
    [SerializeField] private float phase2MoveSpeed = 3f;
    [SerializeField] private float slamRange = 2f;
    [SerializeField] private int slamDamage = 2;
    [SerializeField] private float slamCooldown = 3f;

    [SerializeField] private float chargeRange = 6f;
    [SerializeField] private float chargeDuration = 0.6f;
    [SerializeField] private float chargeSpeedMultiplier = 4f;
    [SerializeField] private int chargeDamage = 2;

    [SerializeField] private GameObject chargeFlamePrefab;
    [SerializeField] private int chargeFlameCount = 8;
    [SerializeField] private float chargeFlameRadius = 2.5f;

    [Header("Phase Change")]
    [Tooltip("Tỉ lệ máu còn lại để chuyển sang Phase 2 (0.5 = 50%).")]
    [SerializeField] private float phase2HpPercent = 0.5f;

    // ================== STATE ==================

    private BossPhase currentPhase;
    private EnemyPathfinding pathfinding;
    private EnemyHealth enemyHealth;

    private float baseMoveSpeed;
    private bool isSlamming;
    private bool isCharging;
    private bool canSlam = true;

    private bool hasTransformedOnce = false;   // đã sang phase2 chưa
    private bool _isDead;
    private int firePatternIndex = 0;

    // ====== đọc máu từ EnemyHealth bằng reflection (không sửa EnemyHealth) ======
    private FieldInfo startingHealthField;
    private FieldInfo currentHealthField;
    private bool healthReflectionReady = false;
    private int maxHealthCached = 0;

    // ================== UNITY ==================

    private void Awake()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (malugazAnimator == null)
            malugazAnimator = GetComponentInChildren<MalugazAnimator>();

        if (arenaCenter == null)
        {
            GameObject centerObj = new GameObject("MalugazArenaCenter");
            centerObj.transform.position = transform.position;
            arenaCenter = centerObj.transform;
        }

        Debug.Log("[MalugazAI] Awake. ArenaCenter = " + arenaCenter.position);
    }

    private void Start()
    {
        currentPhase = startingPhase;

        baseMoveSpeed = pathfinding.GetMoveSpeed();
        Debug.Log("[MalugazAI] Start. StartingPhase = " + currentPhase + ", baseMoveSpeed = " + baseMoveSpeed);

        if (currentPhase == BossPhase.Phase1)
        {
            // Phase1: đứng yên, bắn skill
            pathfinding.StopMoving();
            pathfinding.SetMoveSpeed(0f);

            // cho anim phase1 intro nếu cần
            malugazAnimator?.PlayPhase1Intro();
        }

        enemyHealth.SetDestroyOnDeath(false);
        enemyHealth.OnDeath += OnEnemyHealthDepleted;

        StartPhaseBehaviour();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnEnemyHealthDepleted;
    }

    private void Update()
    {
        // Check đổi phase theo % máu
        TrySwitchToPhase2ByHealth();

        // ---------------- MOVE LOGIC ----------------
        if (currentPhase == BossPhase.Phase2 && !isSlamming && !isCharging)
        {
            if (PlayerController.Instance != null)
            {
                Transform player = PlayerController.Instance.transform;
                Vector2 dir = (player.position - transform.position).normalized;
                pathfinding.MoveTo(dir);
            }
        }

        // ---------------- ANIM LOGIC ----------------
        if (malugazAnimator != null)
        {
            if (currentPhase == BossPhase.Phase1)
            {
                // luôn idle ở Phase1
                malugazAnimator.ForceIdleFront();
            }
            else if (currentPhase == BossPhase.Phase2)
            {
                Vector2 vel = pathfinding.GetCurrentVelocity();
                malugazAnimator.SetMoveDirection(vel);
            }
        }
    }

    // ================== PHASE CONTROL ==================

    private void StartPhaseBehaviour()
    {
        StopAllCoroutines();
        Debug.Log("[MalugazAI] StartPhaseBehaviour. CurrentPhase = " + currentPhase);

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                StartCoroutine(Phase1Loop());
                break;
            case BossPhase.Phase2:
                StartCoroutine(Phase2Loop());
                break;
        }
    }

    private void InitHealthReflection()
    {
        if (healthReflectionReady) return;
        if (enemyHealth == null) return;

        var t = typeof(EnemyHealth);

        startingHealthField = t.GetField("startingHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        currentHealthField = t.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);

        if (startingHealthField == null || currentHealthField == null)
        {
            Debug.LogWarning("[MalugazAI] Không tìm thấy startingHealth / currentHealth trong EnemyHealth.");
            return;
        }

        maxHealthCached = (int)startingHealthField.GetValue(enemyHealth);
        if (maxHealthCached <= 0)
        {
            Debug.LogWarning("[MalugazAI] startingHealth <= 0, bỏ qua chuyển phase theo % máu.");
            return;
        }

        healthReflectionReady = true;
        Debug.Log("[MalugazAI] Health reflection init OK. MaxHp = " + maxHealthCached);
    }

    /// <summary>
    /// Nếu đang Phase1 và % máu <= phase2HpPercent → chuyển sang Phase2.
    /// </summary>
    private void TrySwitchToPhase2ByHealth()
    {
        if (hasTransformedOnce) return;
        if (currentPhase != BossPhase.Phase1) return;
        if (enemyHealth == null) return;

        InitHealthReflection();
        if (!healthReflectionReady) return;

        int curHp = (int)currentHealthField.GetValue(enemyHealth);
        if (curHp <= 0) return;

        float ratio = (float)curHp / maxHealthCached;

        if (ratio <= phase2HpPercent)
        {
            hasTransformedOnce = true;
            StopAllCoroutines();
            Debug.Log("[MalugazAI] HP ratio = " + ratio + " <= " + phase2HpPercent + ", transform to Phase2.");
            StartCoroutine(TransformToPhase2());
        }
    }

    // ---------- PHASE 1 (FIRE MAGE) ----------

    private IEnumerator Phase1Loop()
    {
        Debug.Log("[MalugazAI] Phase1Loop started.");
        float nextTeleportTime = Time.time + Random.Range(teleportIntervalMin, teleportIntervalMax);

        while (currentPhase == BossPhase.Phase1)
        {
            if (Time.time >= nextTeleportTime)
            {
                Debug.Log("[MalugazAI] Phase1 Teleport triggered.");
                malugazAnimator?.PlayTeleport();
                nextTeleportTime = Time.time + Random.Range(teleportIntervalMin, teleportIntervalMax);
            }

            yield return new WaitForSeconds(phase1AttackInterval);

            if (currentPhase != BossPhase.Phase1)
            {
                Debug.Log("[MalugazAI] Phase1Loop ended because phase changed.");
                yield break;
            }

            if (Random.value < 0.5f)
            {
                Debug.Log("[MalugazAI] Phase1 Attack: Fireball Pattern Cycle");
                ShootFireballPatternCycle();
            }
            else
            {
                Debug.Log("[MalugazAI] Phase1 Attack: SpawnRandomFlames");
                SpawnRandomFlames();
            }
        }
    }

    // GỌI TỪ ANIM EVENT teleport: Teleport và để lại flame zone
    public void Phase1_AnimEvent_TeleportAndLeaveFlame()
    {
        if (arenaCenter == null) return;

        Vector3 oldPos = transform.position;

        Vector2 newPos = (Vector2)arenaCenter.position +
                         Random.insideUnitCircle.normalized * arenaRadius;
        transform.position = newPos;

        if (flameZonePrefab != null)
        {
            GameObject go = Instantiate(flameZonePrefab, oldPos, Quaternion.identity);
            MalugazFlameZone fz = go.GetComponent<MalugazFlameZone>();
            if (fz != null)
                fz.SetTelegraphTime(telegraphFlameDelay);
        }

        Debug.Log("[MalugazAI] Teleported from " + oldPos + " to " + newPos + " and left a flame zone.");
    }

    // ====== FIREBALL PATTERN CYCLE ======

    private void ShootFireballPatternCycle()
    {
        if (fireballPrefab == null || fireballSpawnPoint == null)
            return;

        if (PlayerController.Instance == null)
            return;

        if (firePatterns == null || firePatterns.Length == 0)
        {
            ShootStraight();
            return;
        }

        FirePattern pattern = firePatterns[firePatternIndex];
        firePatternIndex = (firePatternIndex + 1) % firePatterns.Length;

        switch (pattern)
        {
            case FirePattern.Straight:
                ShootStraight();
                break;
            case FirePattern.Spread:
                ShootSpread();
                break;
            case FirePattern.Circle:
                ShootCircle();
                break;
        }
    }

    private void ShootStraight()
    {
        Transform player = PlayerController.Instance.transform;
        Vector2 baseDir = (player.position - fireballSpawnPoint.position).normalized;

        int count = Mathf.Max(1, straightCount);
        for (int i = 0; i < count; i++)
        {
            SpawnFireball(baseDir);
        }
    }

    private void ShootSpread()
    {
        Transform player = PlayerController.Instance.transform;
        Vector2 baseDir = (player.position - fireballSpawnPoint.position).normalized;

        int count = Mathf.Max(2, fireballCount);
        float angleStep = fireballSpreadAngle / (count - 1);
        float startAngle = -fireballSpreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 dir = RotateVector(baseDir, angle);
            SpawnFireball(dir);
        }
    }

    private void ShootCircle()
    {
        int count = Mathf.Max(3, circleCount);
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = step * i;
            Vector2 dir = AngleToDir(angle);
            SpawnFireball(dir);
        }
    }

    private void SpawnFireball(Vector2 dir)
    {
        GameObject go = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
        MalugazFireball fb = go.GetComponent<MalugazFireball>();

        if (fb != null)
            fb.Init(dir, fireballSpeed, fireballDamage, transform);
    }

    private void SpawnRandomFlames()
    {
        if (flameZonePrefab == null)
        {
            Debug.LogWarning("[MalugazAI] SpawnRandomFlames called but flameZonePrefab is null.");
            return;
        }

        for (int i = 0; i < randomFlameCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * arenaRadius;
            Vector2 spawnPos = (Vector2)arenaCenter.position + offset;

            GameObject go = Instantiate(flameZonePrefab, spawnPos, Quaternion.identity);
            MalugazFlameZone fz = go.GetComponent<MalugazFlameZone>();
            if (fz != null)
                fz.SetTelegraphTime(telegraphFlameDelay);
        }
    }

    // ---------- PHASE 2 (MELEE) ----------

    private IEnumerator Phase2Loop()
    {
        pathfinding.SetMoveSpeed(phase2MoveSpeed);
        Debug.Log("[MalugazAI] Phase2Loop started. MoveSpeed = " + phase2MoveSpeed);

        while (currentPhase == BossPhase.Phase2)
        {
            if (!canSlam || isSlamming || isCharging)
            {
                yield return null;
                continue;
            }

            if (PlayerController.Instance == null)
            {
                yield return null;
                continue;
            }

            Transform player = PlayerController.Instance.transform;
            float dist = Vector2.Distance(transform.position, player.position);

            if (dist <= slamRange + 0.1f)
            {
                Debug.Log("[MalugazAI] Phase2: SlamAndMaybeCharge triggered. dist = " + dist);
                yield return StartCoroutine(SlamAndMaybeCharge());
            }
            else
            {
                yield return null;
            }
        }

        Debug.Log("[MalugazAI] Phase2Loop ended. Phase = " + currentPhase);
    }

    private IEnumerator SlamAndMaybeCharge()
    {
        canSlam = false;
        isSlamming = true;
        pathfinding.StopMoving();

        malugazAnimator?.PlaySlam();
        Debug.Log("[MalugazAI] Slam started.");

        yield return new WaitForSeconds(0.4f);

        DoRadialDamage(slamRange, slamDamage);
        Debug.Log("[MalugazAI] Slam damage applied. Range = " + slamRange + ", Damage = " + slamDamage);

        if (PlayerController.Instance != null)
        {
            Transform player = PlayerController.Instance.transform;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= chargeRange)
            {
                Debug.Log("[MalugazAI] ChargeAttack will be executed. dist = " + dist);
                yield return StartCoroutine(ChargeAttack(player.position));
            }
        }

        isSlamming = false;

        yield return new WaitForSeconds(slamCooldown);
        canSlam = true;
        Debug.Log("[MalugazAI] Slam cooldown finished. CanSlam = true.");
    }

    private IEnumerator ChargeAttack(Vector3 targetPos)
    {
        isCharging = true;

        malugazAnimator?.PlayCharge();
        Debug.Log("[MalugazAI] ChargeAttack started. TargetPos = " + targetPos);

        Vector2 dir = (targetPos - transform.position).normalized;

        float oldSpeed = pathfinding.GetMoveSpeed();
        pathfinding.SetMoveSpeed(oldSpeed * chargeSpeedMultiplier);

        float timer = 0f;
        bool hasHitPlayer = false;

        while (timer < chargeDuration)
        {
            timer += Time.deltaTime;
            pathfinding.MoveTo(dir);

            if (!hasHitPlayer)
            {
                Collider2D hit = Physics2D.OverlapCircle(transform.position, 1f, LayerMask.GetMask("Player"));
                if (hit != null && hit.CompareTag("Player"))
                {
                    hasHitPlayer = true;
                    Debug.Log("[MalugazAI] Charge hit player. Damage = " + chargeDamage);
                    PlayerHealth.Instance.TakeDamage(chargeDamage, transform);
                }
            }

            yield return null;
        }

        pathfinding.StopMoving();
        pathfinding.SetMoveSpeed(oldSpeed);

        Debug.Log("[MalugazAI] Charge ended. Spawning charge flames.");
        SpawnChargeFlames(transform.position);

        isCharging = false;
    }

    private void SpawnChargeFlames(Vector3 center)
    {
        if (chargeFlamePrefab == null || chargeFlameCount <= 0)
        {
            Debug.LogWarning("[MalugazAI] SpawnChargeFlames called but prefab is null hoặc count <= 0.");
            return;
        }

        float angleStep = 360f / chargeFlameCount;
        for (int i = 0; i < chargeFlameCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 pos = (Vector2)center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * chargeFlameRadius;
            GameObject go = Instantiate(chargeFlamePrefab, pos, Quaternion.identity);

            MalugazFlameZone fz = go.GetComponent<MalugazFlameZone>();
            if (fz != null)
                fz.SetTelegraphTime(0f);
        }
    }

    private void DoRadialDamage(float radius, int damage)
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Player"));
        if (hit != null && hit.CompareTag("Player"))
        {
            Debug.Log("[MalugazAI] DoRadialDamage hit player. Radius = " + radius + ", Damage = " + damage);
            PlayerHealth.Instance.TakeDamage(damage, transform);
        }
    }

    // ================== HEALTH / PHASE CHANGE ==================

    private void OnEnemyHealthDepleted()
    {
        Debug.Log("[MalugazAI] OnEnemyHealthDepleted called. currentPhase = " + currentPhase);

        currentPhase = BossPhase.Dead;
        _isDead = true;
        enemyHealth.SetDestroyOnDeath(true);
        pathfinding.StopMoving();
        StopAllCoroutines();

        malugazAnimator?.PlayDeath();
        Debug.Log("[MalugazAI] Boss dead. Allow destroyOnDeath = true.");
    }

    private IEnumerator TransformToPhase2()
    {
        Debug.Log("[MalugazAI] TransformToPhase2 started.");

        currentPhase = BossPhase.Phase2;

        // chơi anim transform sang phase2
        malugazAnimator?.PlayPhase2Transform();

        // cho animation transform chơi 1s rồi mới chạy logic phase 2
        yield return new WaitForSeconds(1f);

        pathfinding.SetMoveSpeed(phase2MoveSpeed);
        StartPhaseBehaviour();

        Debug.Log("[MalugazAI] TransformToPhase2 finished. Phase2 behaviour started.");
    }

    // ================== DEBUG GIZMOS ==================

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeFlameRadius);
    }

    private Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        float tx = v.x;
        float ty = v.y;

        return new Vector2(
            cos * tx - sin * ty,
            sin * tx + cos * ty
        );
    }

    private Vector2 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
