using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 2 pha: Malugaz
/// Gắn script này lên prefab boss, cùng với:
/// - Rigidbody2D (Body Type: Dynamic hoặc Kinematic)
/// - Collider2D (IsTrigger = false)
/// - EnemyPathfinding
/// - EnemyHealth  (đã chỉnh sửa để hỗ trợ destroyOnDeath + OnDeath event)
/// - Knockback, Flash (giống enemy thường)
/// - Animator (tuỳ chọn)
/// </summary>
[RequireComponent(typeof(EnemyPathfinding))]
[RequireComponent(typeof(EnemyHealth))]
public class MalugazBoss : MonoBehaviour
{
    public enum BossPhase { Phase1, Phase2, Dead }

    [Header("General")]
    [SerializeField] private BossPhase startingPhase = BossPhase.Phase1;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 6f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private string phase1AnimTrigger = "Phase1";
    [SerializeField] private string phase2AnimTrigger = "Phase2";
    [SerializeField] private string teleportAnimTrigger = "Teleport";
    [SerializeField] private string slamAnimTrigger = "Slam";
    [SerializeField] private string chargeAnimTrigger = "Charge";

    [Header("Phase 1 - Fire Mage")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform fireballSpawnPoint;
    [SerializeField] private float fireballSpeed = 6f;
    [SerializeField] private int fireballDamage = 1;

    [SerializeField] private GameObject flameZonePrefab; // prefab vùng lửa có script MalugazFlameZone
    [SerializeField] private int randomFlameCount = 5;
    [SerializeField] private float telegraphFlameDelay = 0.75f;

    [SerializeField] private float phase1AttackInterval = 2.5f;
    [SerializeField] private float teleportIntervalMin = 3f;
    [SerializeField] private float teleportIntervalMax = 5f;

    [Header("Phase 2 - Melee")]
    [SerializeField] private float phase2MoveSpeed = 3f;
    [SerializeField] private float slamRange = 2f;
    [SerializeField] private int slamDamage = 2;
    [SerializeField] private float slamCooldown = 3f;

    [SerializeField] private float chargeRange = 6f;
    [SerializeField] private float chargeDuration = 0.6f;
    [SerializeField] private float chargeSpeedMultiplier = 4f;
    [SerializeField] private int chargeDamage = 2;

    [SerializeField] private GameObject chargeFlamePrefab; // có thể dùng lại prefab flameZone với telegraphTime = 0
    [SerializeField] private int chargeFlameCount = 8;
    [SerializeField] private float chargeFlameRadius = 2.5f;

    private BossPhase currentPhase;
    private EnemyPathfinding pathfinding;
    private EnemyHealth enemyHealth;

    private float baseMoveSpeed;
    private bool isSlamming;
    private bool isCharging;
    private bool canSlam = true;
    private bool hasTransformedOnce = false;

    private void Awake()
    {
        pathfinding = GetComponent<EnemyPathfinding>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (arenaCenter == null)
        {
            // nếu chưa set, lấy vị trí hiện tại làm tâm
            GameObject centerObj = new GameObject("MalugazArenaCenter");
            centerObj.transform.position = transform.position;
            arenaCenter = centerObj.transform;
        }

        Debug.Log("[MalugazBoss] Awake. ArenaCenter = " + arenaCenter.position);
    }

    private void Start()
    {
        currentPhase = startingPhase;

        // Lưu tốc chạy gốc để dùng cho phase 2 + charge
        baseMoveSpeed = pathfinding.GetMoveSpeed();

        Debug.Log("[MalugazBoss] Start. StartingPhase = " + currentPhase + ", baseMoveSpeed = " + baseMoveSpeed);

        // Phase 1: đứng yên, chỉ dịch chuyển = teleport
        if (currentPhase == BossPhase.Phase1)
        {
            pathfinding.StopMoving();
            pathfinding.SetMoveSpeed(0f);
            Debug.Log("[MalugazBoss] Enter Phase1: stop movement, speed = 0.");
            if (animator != null) animator.SetTrigger(phase1AnimTrigger);
        }

        // Là boss 2 phase => cho phép sống lại 1 lần
        enemyHealth.SetDestroyOnDeath(false);
        enemyHealth.OnDeath += OnEnemyHealthDepleted;
        Debug.Log("[MalugazBoss] Registered OnEnemyHealthDepleted. destroyOnDeath set to false.");

        // Bắt đầu vòng lặp hành vi
        StartPhaseBehaviour();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= OnEnemyHealthDepleted;
        }
    }

    private void Update()
    {
        if (currentPhase == BossPhase.Phase2 && !isSlamming && !isCharging)
        {
            // đuổi theo player
            if (PlayerController.Instance != null)
            {
                Transform player = PlayerController.Instance.transform;
                Vector2 dir = (player.position - transform.position).normalized;
                pathfinding.MoveTo(dir);
            }
        }
    }

    private void StartPhaseBehaviour()
    {
        StopAllCoroutines();
        Debug.Log("[MalugazBoss] StartPhaseBehaviour. CurrentPhase = " + currentPhase);

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

    #region Phase 1

    private IEnumerator Phase1Loop()
    {
        Debug.Log("[MalugazBoss] Phase1Loop started.");
        float nextTeleportTime = Time.time + Random.Range(teleportIntervalMin, teleportIntervalMax);

        while (currentPhase == BossPhase.Phase1)
        {
            // teleport định kỳ
            if (Time.time >= nextTeleportTime)
            {
                Debug.Log("[MalugazBoss] Phase1 Teleport triggered.");
                yield return StartCoroutine(TeleportAroundArena());
                nextTeleportTime = Time.time + Random.Range(teleportIntervalMin, teleportIntervalMax);
            }

            // tấn công (bắn cầu lửa hoặc gọi lửa random)
            yield return new WaitForSeconds(phase1AttackInterval);

            if (currentPhase != BossPhase.Phase1)
            {
                Debug.Log("[MalugazBoss] Phase1Loop ended because phase changed.");
                yield break;
            }

            if (Random.value < 0.5f)
            {
                Debug.Log("[MalugazBoss] Phase1 Attack: ShootFireballAtPlayer");
                ShootFireballAtPlayer();
            }
            else
            {
                Debug.Log("[MalugazBoss] Phase1 Attack: SpawnRandomFlames");
                SpawnRandomFlames();
            }
        }
    }

    private IEnumerator TeleportAroundArena()
    {
        if (animator != null) animator.SetTrigger(teleportAnimTrigger);

        // chờ 0.3s cho animation
        yield return new WaitForSeconds(0.3f);

        Vector2 randomPos = arenaCenter.position + (Vector3)(Random.insideUnitCircle.normalized * arenaRadius);
        Debug.Log("[MalugazBoss] Teleporting to " + randomPos);
        transform.position = randomPos;
    }

    private void ShootFireballAtPlayer()
    {
        if (fireballPrefab == null || fireballSpawnPoint == null)
        {
            Debug.LogWarning("[MalugazBoss] ShootFireballAtPlayer called but prefab or spawnPoint is null.");
            return;
        }

        if (PlayerController.Instance == null)
        {
            Debug.LogWarning("[MalugazBoss] ShootFireballAtPlayer: PlayerController.Instance is null.");
            return;
        }

        Transform player = PlayerController.Instance.transform;
        Vector2 dir = (player.position - fireballSpawnPoint.position).normalized;

        GameObject go = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
        MalugazFireball fb = go.GetComponent<MalugazFireball>();
        if (fb != null)
        {
            fb.Init(dir, fireballSpeed, fireballDamage);
        }
        else
        {
            Debug.LogWarning("[MalugazBoss] Fireball prefab missing MalugazFireball component.");
        }
    }

    private void SpawnRandomFlames()
    {
        if (flameZonePrefab == null)
        {
            Debug.LogWarning("[MalugazBoss] SpawnRandomFlames called but flameZonePrefab is null.");
            return;
        }

        for (int i = 0; i < randomFlameCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * arenaRadius;
            Vector2 spawnPos = (Vector2)arenaCenter.position + offset;

            GameObject go = Instantiate(flameZonePrefab, spawnPos, Quaternion.identity);
            MalugazFlameZone fz = go.GetComponent<MalugazFlameZone>();
            if (fz != null)
            {
                fz.SetTelegraphTime(telegraphFlameDelay);
            }
            else
            {
                Debug.LogWarning("[MalugazBoss] FlameZone prefab missing MalugazFlameZone component.");
            }
        }
    }

    #endregion

    #region Phase 2

    private IEnumerator Phase2Loop()
    {
        // chuẩn bị phase 2
        pathfinding.SetMoveSpeed(phase2MoveSpeed);
        if (animator != null) animator.SetTrigger(phase2AnimTrigger);
        Debug.Log("[MalugazBoss] Phase2Loop started. MoveSpeed = " + phase2MoveSpeed);

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
                Debug.Log("[MalugazBoss] Phase2: SlamAndMaybeCharge triggered. dist = " + dist);
                yield return StartCoroutine(SlamAndMaybeCharge());
            }
            else
            {
                yield return null;
            }
        }

        Debug.Log("[MalugazBoss] Phase2Loop ended. Phase = " + currentPhase);
    }

    private IEnumerator SlamAndMaybeCharge()
    {
        canSlam = false;
        isSlamming = true;
        pathfinding.StopMoving();

        if (animator != null) animator.SetTrigger(slamAnimTrigger);
        Debug.Log("[MalugazBoss] Slam started.");

        // chờ 0.4s cho animation "dậm tay"
        yield return new WaitForSeconds(0.4f);

        // gây dmg quanh boss
        DoRadialDamage(slamRange, slamDamage);
        Debug.Log("[MalugazBoss] Slam damage applied. Range = " + slamRange + ", Damage = " + slamDamage);

        // quyết định có charge không
        if (PlayerController.Instance != null)
        {
            Transform player = PlayerController.Instance.transform;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= chargeRange)
            {
                Debug.Log("[MalugazBoss] ChargeAttack will be executed. dist = " + dist);
                yield return StartCoroutine(ChargeAttack(player.position));
            }
        }

        isSlamming = false;

        // hồi cooldown
        yield return new WaitForSeconds(slamCooldown);
        canSlam = true;
        Debug.Log("[MalugazBoss] Slam cooldown finished. CanSlam = true.");
    }

    private IEnumerator ChargeAttack(Vector3 targetPos)
    {
        isCharging = true;

        if (animator != null) animator.SetTrigger(chargeAnimTrigger);
        Debug.Log("[MalugazBoss] ChargeAttack started. TargetPos = " + targetPos);

        // hướng đến vị trí target tại thời điểm bắt đầu charge
        Vector2 dir = (targetPos - transform.position).normalized;

        float oldSpeed = pathfinding.GetMoveSpeed();
        pathfinding.SetMoveSpeed(oldSpeed * chargeSpeedMultiplier);

        float timer = 0f;
        bool hasHitPlayer = false;

        while (timer < chargeDuration)
        {
            timer += Time.deltaTime;
            pathfinding.MoveTo(dir);

            // check va chạm player kiểu đơn giản bằng OverlapCircle
            if (!hasHitPlayer)
            {
                Collider2D hit = Physics2D.OverlapCircle(transform.position, 1f, LayerMask.GetMask("Player"));
                if (hit != null && hit.CompareTag("Player"))
                {
                    hasHitPlayer = true;
                    Debug.Log("[MalugazBoss] Charge hit player. Damage = " + chargeDamage);
                    PlayerHealth.Instance.TakeDamage(chargeDamage, transform);
                }
            }

            yield return null;
        }

        // dừng lại
        pathfinding.StopMoving();
        pathfinding.SetMoveSpeed(oldSpeed);

        // tạo lửa xung quanh vị trí lao tới
        Debug.Log("[MalugazBoss] Charge ended. Spawning charge flames.");
        SpawnChargeFlames(transform.position);

        isCharging = false;
    }

    private void SpawnChargeFlames(Vector3 center)
    {
        if (chargeFlamePrefab == null || chargeFlameCount <= 0)
        {
            Debug.LogWarning("[MalugazBoss] SpawnChargeFlames called but prefab is null or count <= 0.");
            return;
        }

        float angleStep = 360f / chargeFlameCount;
        for (int i = 0; i < chargeFlameCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 pos = (Vector2)center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * chargeFlameRadius;
            GameObject go = Instantiate(chargeFlamePrefab, pos, Quaternion.identity);

            // nếu dùng cùng prefab với flameZone => tắt telegraph, bật lửa ngay
            MalugazFlameZone fz = go.GetComponent<MalugazFlameZone>();
            if (fz != null)
            {
                fz.SetTelegraphTime(0f);
            }
            else
            {
                Debug.LogWarning("[MalugazBoss] ChargeFlame prefab missing MalugazFlameZone component.");
            }
        }
    }

    private void DoRadialDamage(float radius, int damage)
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Player"));
        if (hit != null && hit.CompareTag("Player"))
        {
            Debug.Log("[MalugazBoss] DoRadialDamage hit player. Radius = " + radius + ", Damage = " + damage);
            PlayerHealth.Instance.TakeDamage(damage, transform);
        }
        else
        {
            Debug.Log("[MalugazBoss] DoRadialDamage no player in range. Radius = " + radius);
        }
    }

    #endregion

    #region Health / Phase Change

    private void OnEnemyHealthDepleted()
    {
        Debug.Log("[MalugazBoss] OnEnemyHealthDepleted called. hasTransformedOnce = " + hasTransformedOnce + ", currentPhase = " + currentPhase);

        if (!hasTransformedOnce)
        {
            // Lần đầu chết => chuyển sang phase 2
            hasTransformedOnce = true;

            // Dừng toàn bộ hành vi phase 1 (loop, teleport, bắn…)
            StopAllCoroutines();
            Debug.Log("[MalugazBoss] First death detected. Stopping all coroutines and starting TransformToPhase2.");

            StartCoroutine(TransformToPhase2());
        }
        else
        {
            // Lần thứ 2 => cho phép EnemyHealth phá huỷ object
            currentPhase = BossPhase.Dead;
            enemyHealth.SetDestroyOnDeath(true);
            Debug.Log("[MalugazBoss] Second death detected. Set phase Dead and allow destroyOnDeath = true.");
            // không reset máu nữa, lần DetectDeath tiếp theo sẽ Destroy (do EnemyHealth xử lý)
        }
    }

    private IEnumerator TransformToPhase2()
    {
        Debug.Log("[MalugazBoss] TransformToPhase2 started.");

        // KHÔNG StopAllCoroutines ở đây nữa, tránh tự kill chính coroutine này

        // animation biến hình (tuỳ bạn set)
        if (animator != null) animator.SetTrigger(phase2AnimTrigger);

        // chờ 1s cho animation
        yield return new WaitForSeconds(1f);

        // hồi full máu
        enemyHealth.ResetHealthToMax();
        Debug.Log("[MalugazBoss] TransformToPhase2: Health reset to max.");

        // chuyển sang phase 2
        currentPhase = BossPhase.Phase2;
        Debug.Log("[MalugazBoss] TransformToPhase2: Switched to Phase2.");

        // tăng lại speed để di chuyển
        pathfinding.SetMoveSpeed(phase2MoveSpeed);

        StartPhaseBehaviour();
        Debug.Log("[MalugazBoss] TransformToPhase2 finished. Phase2 behaviour started.");
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }

        // vẽ slam range, charge flame
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeFlameRadius);
    }
}
