using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Malugaz 2-phase boss AI (no minigame logic, no scene references required on prefab).
/// Spawner chịu trách nhiệm Instantiate boss đúng thời điểm.
/// Boss khi được spawn sẽ:
/// - Tự tìm arena root / center / teleport points bằng tag (nếu chưa gán tay).
/// - Vào Phase 1 ngay: đứng yên, bắn cầu lửa, tele, tạo vùng lửa ngẫu nhiên.
/// - Khi EnemyHealth "chết" lần 1 -> reset máu, chuyển Phase 2 (melee + charge để lại lửa).
/// - Khi "chết" lần 2 -> thật sự chết, tắt arena.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyPathfinding))]
public class MalugazAI : MonoBehaviour, IEnemy
{
    // ================= CORE & PHASE STATE =================

    private enum BossPhase
    {
        Inactive,
        Phase1,
        Phase2,
        Dead
    }

    private BossPhase currentPhase = BossPhase.Inactive;

    [Header("Core References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyPathfinding pathfinding;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Root chứa sprite/animator của boss. Được bật khi encounter bắt đầu.")]
    [SerializeField] private GameObject visualRoot;

    private Transform player;
    private float baseMoveSpeed;

    [Header("Animator Parameters")]
    [SerializeField] private string animParamPhase = "Phase";          // int
    [SerializeField] private string animParamIsMoving = "IsMoving";    // bool
    [SerializeField] private string animParamMoveX = "MoveX";          // float
    [SerializeField] private string animParamMoveY = "MoveY";          // float
    [SerializeField] private string animTriggerSpawn = "Spawn";        // trigger
    [SerializeField] private string animTriggerPhaseChange = "PhaseChange";   // trigger
    [SerializeField] private string animTriggerPhase1Attack = "Phase1Attack"; // trigger
    [SerializeField] private string animTriggerPhase2Charge = "Phase2Charge"; // trigger

    // ================= ARENA =================

    [Header("Arena & Spawn")]
    [Tooltip("Tâm đấu trường. Nếu null, sẽ tự tìm theo arenaCenterTag.")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private string arenaCenterTag = "BossArenaCenter";

    [Tooltip("Root đấu trường (tường, camera bounds...). Nếu null, sẽ tự tìm theo arenaRootTag.")]
    [SerializeField] private GameObject arenaRoot;
    [SerializeField] private string arenaRootTag = "BossArenaRoot";

    [Tooltip("VFX xuất hiện khi boss spawn vào trung tâm đấu trường.")]
    [SerializeField] private GameObject spawnVfxPrefab;

    private bool arenaInitialized = false;

    // ================= TELEPORT (PHASE 1) =================

    [Header("Teleport (Phase 1)")]
    [Tooltip("Điểm teleport. Nếu rỗng, sẽ tự tìm trong teleportPointsRoot hoặc theo tag.")]
    [SerializeField] private Transform[] teleportPoints;
    [Tooltip("Parent chứa các điểm teleport. Nếu null, dùng tag.")]
    [SerializeField] private Transform teleportPointsRoot;
    [SerializeField] private string teleportPointTag = "BossTeleportPoint";
    [SerializeField] private float minTeleportInterval = 4f;
    [SerializeField] private float maxTeleportInterval = 7f;



    private float teleportTimer;

    // ================= PHASE 1 – FIREBALLS & FIRE ZONES =================

    public enum FirePatternType
    {
        Single,
        TripleSpread,
        Radial8
    }

    [System.Serializable]
    public class Phase1FirePattern
    {
        public string patternName = "Pattern";
        public FirePatternType patternType = FirePatternType.Single;
        [Tooltip("Thời gian giữa 2 lần bắn pattern này.")]
        public float interval = 3f;
    }

    [Header("Phase 1 – Fireballs")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform fireballSpawnPoint;
    [SerializeField] private float fireballSpeed = 6f;
    [SerializeField] private List<Phase1FirePattern> phase1Patterns = new List<Phase1FirePattern>();

    private int currentPatternIndex = 0;
    private float phase1AttackTimer;

    [Header("Phase 1 – Fire Zones")]
    [Tooltip("Prefab telegraph (vòng tròn đỏ) sẽ spawn trên sàn.")]
    [SerializeField] private GameObject fireZoneTelegraphPrefab;
    [Tooltip("Bán kính random xung quanh arenaCenter để spawn vùng lửa.")]
    [SerializeField] private float fireZoneRadius = 5f;

    // ================= PHASE 2 – CHASE & CHARGE =================

    [Header("Phase 2 – Chase")]
    [SerializeField] private float phase2MoveSpeed = 3f;

    [Header("Phase 2 – Charge")]
    [SerializeField] private float chargeRange = 4f;
    [SerializeField] private float chargeCooldown = 4f;
    [SerializeField] private float chargeWindupTime = 0.5f;
    [SerializeField] private float chargeDuration = 0.4f;
    [SerializeField] private float chargeSpeedMultiplier = 3f;
    [Tooltip("Prefab vùng lửa spawn tại điểm charge impact.")]
    [SerializeField] private GameObject chargeImpactFireZonePrefab;

    private float chargeTimer;
    private bool isCharging = false;
    private Vector2 chargeDirection;

    // ================= MULTI-PHASE HEALTH =================

    private bool hasEnteredPhase2 = false;

    // ================= UNITY LIFECYCLE =================

    private void Reset()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        pathfinding = GetComponent<EnemyPathfinding>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (!enemyHealth) enemyHealth = GetComponent<EnemyHealth>();
        if (!pathfinding) pathfinding = GetComponent<EnemyPathfinding>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (enemyHealth != null)
        {
            // Lần chết đầu: chuyển phase, không destroy.
            enemyHealth.SetDestroyOnDeath(false);
            enemyHealth.OnDeath += HandleEnemyHealthDeath;
        }
    }

    private void Start()
    {
        if (pathfinding != null)
        {
            baseMoveSpeed = pathfinding.GetMoveSpeed();
        }

        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }

        currentPhase = BossPhase.Inactive;

        // Encounter bắt đầu ngay khi prefab được spawn.
        InitializeArenaIfNeeded();
        ActivateBossPhase1();
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= HandleEnemyHealthDeath;
        }
    }

    private void Update()
    {
        if (currentPhase == BossPhase.Inactive || currentPhase == BossPhase.Dead)
            return;

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                UpdatePhase1();
                break;
            case BossPhase.Phase2:
                UpdatePhase2();
                break;
        }

        UpdateAnimatorMovement();
    }

    // ================= ARENA INIT =================

    private void InitializeArenaIfNeeded()
    {
        if (arenaInitialized)
            return;

        // Tìm arenaRoot nếu cần
        if (arenaRoot == null && !string.IsNullOrEmpty(arenaRootTag))
        {
            GameObject rootObj = GameObject.FindWithTag(arenaRootTag);
            if (rootObj != null)
                arenaRoot = rootObj;
        }

        if (arenaRoot != null)
            arenaRoot.SetActive(true);

        // Tìm arenaCenter nếu cần
        if (arenaCenter == null && !string.IsNullOrEmpty(arenaCenterTag))
        {
            GameObject centerObj = GameObject.FindWithTag(arenaCenterTag);
            if (centerObj != null)
                arenaCenter = centerObj.transform;
        }

        // VFX spawn
        if (arenaCenter != null && spawnVfxPrefab != null)
        {
            Instantiate(spawnVfxPrefab, arenaCenter.position, Quaternion.identity);
        }

        // Cache teleport points
        CacheTeleportPoints();

        arenaInitialized = true;
    }

    private void CacheTeleportPoints()
    {
        if (teleportPoints != null && teleportPoints.Length > 0)
            return;

        var found = new System.Collections.Generic.List<Transform>();

        if (teleportPointsRoot != null)
        {
            foreach (Transform child in teleportPointsRoot)
            {
                if (child != null) found.Add(child);
            }
        }

        if (found.Count == 0 && !string.IsNullOrEmpty(teleportPointTag))
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(teleportPointTag);
            foreach (var go in tagged)
            {
                if (go != null) found.Add(go.transform);
            }
        }

        if (found.Count > 0)
        {
            teleportPoints = found.ToArray();
        }
        else
        {
            Debug.LogWarning("[MalugazAI] Không tìm thấy teleport points. Boss sẽ không teleport trong Phase 1.");
        }
    }

    // ================= PHASE 1 =================

    private void ActivateBossPhase1()
    {
        InitializeArenaIfNeeded();

        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (arenaCenter != null)
            transform.position = arenaCenter.position;

        currentPhase = BossPhase.Phase1;

        teleportTimer = Random.Range(minTeleportInterval, maxTeleportInterval);

        if (phase1Patterns != null && phase1Patterns.Count > 0)
        {
            currentPatternIndex = 0;
            phase1AttackTimer = phase1Patterns[0].interval;
        }

        if (animator != null)
        {
            animator.SetInteger(animParamPhase, 1);
            if (!string.IsNullOrEmpty(animTriggerSpawn))
                animator.SetTrigger(animTriggerSpawn);
        }
    }

    private void UpdatePhase1()
    {
        if (pathfinding != null)
            pathfinding.StopMoving();

        // Teleport
        if (teleportPoints != null && teleportPoints.Length > 0)
        {
            teleportTimer -= Time.deltaTime;
            if (teleportTimer <= 0f)
            {
                TeleportToRandomPoint();
                teleportTimer = Random.Range(minTeleportInterval, maxTeleportInterval);
            }
        }

        // Fire patterns
        if (phase1Patterns != null && phase1Patterns.Count > 0)
        {
            phase1AttackTimer -= Time.deltaTime;
            if (phase1AttackTimer <= 0f)
            {
                var pattern = phase1Patterns[currentPatternIndex];
                currentPatternIndex = (currentPatternIndex + 1) % phase1Patterns.Count;
                phase1AttackTimer = pattern.interval;

                if (animator != null && !string.IsNullOrEmpty(animTriggerPhase1Attack))
                    animator.SetTrigger(animTriggerPhase1Attack);
                // Nếu muốn bắn thuần code, có thể gọi:
                // ExecuteFirePattern(pattern.patternType);
            }
        }
    }

    private void TeleportToRandomPoint()
    {
        if (teleportPoints == null || teleportPoints.Length == 0)
            return;

        int index = Random.Range(0, teleportPoints.Length);
        Transform tp = teleportPoints[index];
        if (tp != null)
        {
            transform.position = tp.position;
        }
    }

    private void ExecuteFirePattern(FirePatternType patternType)
    {
        switch (patternType)
        {
            case FirePatternType.Single:
                SpawnFireballSingle();
                break;
            case FirePatternType.TripleSpread:
                SpawnFireballTripleSpread();
                break;
            case FirePatternType.Radial8:
                SpawnFireballRadial(8);
                break;
        }

        SpawnRandomFireZone();
    }

    // ----- Fireball helpers -----

    private Vector2 GetDirectionToPlayerOrDefault()
    {
        if (player != null)
        {
            Vector2 dir = (player.position - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }
        return Vector2.right;
    }

    private Transform GetFireballSpawnOrigin()
    {
        return fireballSpawnPoint != null ? fireballSpawnPoint : transform;
    }

    private void SpawnFireballSingle()
    {
        if (fireballPrefab == null) return;

        var origin = GetFireballSpawnOrigin();
        Vector2 dir = GetDirectionToPlayerOrDefault();

        GameObject proj = Instantiate(fireballPrefab, origin.position, Quaternion.identity);
        var rb2d = proj.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = dir * fireballSpeed;
        }
    }

    private void SpawnFireballTripleSpread()
    {
        if (fireballPrefab == null) return;

        var origin = GetFireballSpawnOrigin();
        Vector2 centerDir = GetDirectionToPlayerOrDefault();

        float angleOffset = 15f;
        SpawnFireballWithAngle(origin.position, centerDir, 0f);
        SpawnFireballWithAngle(origin.position, centerDir, angleOffset);
        SpawnFireballWithAngle(origin.position, centerDir, -angleOffset);
    }

    private void SpawnFireballRadial(int count)
    {
        if (fireballPrefab == null || count <= 0) return;

        var origin = GetFireballSpawnOrigin();
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = step * i;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            SpawnFireballWithDir(origin.position, dir);
        }
    }

    private void SpawnFireballWithAngle(Vector2 origin, Vector2 baseDir, float angleDeg)
    {
        Vector2 dir = Quaternion.Euler(0f, 0f, angleDeg) * baseDir;
        SpawnFireballWithDir(origin, dir.normalized);
    }

    private void SpawnFireballWithDir(Vector2 origin, Vector2 dir)
    {
        if (fireballPrefab == null) return;

        GameObject proj = Instantiate(fireballPrefab, origin, Quaternion.identity);
        var rb2d = proj.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = dir * fireballSpeed;
        }
    }

    private void SpawnRandomFireZone()
    {
        if (fireZoneTelegraphPrefab == null || arenaCenter == null)
            return;

        Vector2 randomOffset = Random.insideUnitCircle * fireZoneRadius;
        Vector3 spawnPos = arenaCenter.position + (Vector3)randomOffset;
        Instantiate(fireZoneTelegraphPrefab, spawnPos, Quaternion.identity);
    }

    // ================= PHASE 2 =================

    private void EnterPhase2()
    {
        currentPhase = BossPhase.Phase2;

        if (pathfinding != null)
        {
            pathfinding.SetMoveSpeed(phase2MoveSpeed);
        }

        chargeTimer = chargeCooldown;
        isCharging = false;

        if (animator != null)
        {
            animator.SetInteger(animParamPhase, 2);
            if (!string.IsNullOrEmpty(animTriggerPhaseChange))
                animator.SetTrigger(animTriggerPhaseChange);
        }
    }

    private void UpdatePhase2()
    {
        if (player == null || pathfinding == null)
            return;

        if (!isCharging)
        {
            Vector2 toPlayer = (player.position - transform.position);
            Vector2 dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.zero;
            pathfinding.MoveTo(dir);
        }

        chargeTimer -= Time.deltaTime;
        if (!isCharging && chargeTimer <= 0f)
        {
            float distToPlayer = Vector2.Distance(transform.position, player.position);
            if (distToPlayer <= chargeRange)
            {
                StartCoroutine(ChargeRoutine());
                chargeTimer = chargeCooldown;
            }
        }
    }

    private IEnumerator ChargeRoutine()
    {
        if (player == null || pathfinding == null)
            yield break;

        isCharging = true;

        Vector2 toPlayer = (player.position - transform.position);
        chargeDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.right;

        pathfinding.StopMoving();

        if (animator != null && !string.IsNullOrEmpty(animTriggerPhase2Charge))
            animator.SetTrigger(animTriggerPhase2Charge);

        yield return new WaitForSeconds(chargeWindupTime);

        float originalSpeed = pathfinding.GetMoveSpeed();
        pathfinding.SetMoveSpeed(originalSpeed * chargeSpeedMultiplier);

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            pathfinding.MoveTo(chargeDirection);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pathfinding.StopMoving();
        pathfinding.SetMoveSpeed(phase2MoveSpeed);

        SpawnChargeImpactFireZone();

        isCharging = false;
    }

    private void SpawnChargeImpactFireZone()
    {
        if (chargeImpactFireZonePrefab == null)
            return;

        Instantiate(chargeImpactFireZonePrefab, transform.position, Quaternion.identity);
    }

    // ================= HEALTH MULTI-PHASE =================

    private void HandleEnemyHealthDeath()
    {
        if (!hasEnteredPhase2)
        {
            hasEnteredPhase2 = true;

            if (enemyHealth != null)
            {
                enemyHealth.ResetHealthToMax();
                enemyHealth.SetDestroyOnDeath(true); // lần chết sau sẽ destroy thật
            }

            EnterPhase2();
        }
        else
        {
            currentPhase = BossPhase.Dead;

            if (arenaRoot != null)
                arenaRoot.SetActive(false);
        }
    }

    // ================= IEnemy =================

    public void Attack()
    {
        if (currentPhase == BossPhase.Phase1)
        {
            if (animator != null && !string.IsNullOrEmpty(animTriggerPhase1Attack))
                animator.SetTrigger(animTriggerPhase1Attack);
        }
        else if (currentPhase == BossPhase.Phase2)
        {
            if (!isCharging && chargeTimer <= 0f && player != null)
            {
                StartCoroutine(ChargeRoutine());
                chargeTimer = chargeCooldown;
            }
        }
    }

    // ================= ANIMATOR MOVEMENT =================

    private void UpdateAnimatorMovement()
    {
        if (animator == null || pathfinding == null)
            return;

        Vector2 velocity = pathfinding.GetCurrentVelocity();
        bool isMoving = velocity.sqrMagnitude > 0.001f;

        animator.SetBool(animParamIsMoving, isMoving);
        if (isMoving)
        {
            animator.SetFloat(animParamMoveX, velocity.x);
            animator.SetFloat(animParamMoveY, velocity.y);
        }
    }

    // ================= ANIMATION EVENTS =================

    // ---- Phase 1 ----
    public void AnimEvent_Phase1_FireballSingle() => SpawnFireballSingle();
    public void AnimEvent_Phase1_FireballTriple() => SpawnFireballTripleSpread();
    public void AnimEvent_Phase1_FireballRadial8() => SpawnFireballRadial(8);
    public void AnimEvent_Phase1_RandomFireZone() => SpawnRandomFireZone();

    // ---- Phase 2 ----
    public void AnimEvent_Phase2_ChargeImpactFire() => SpawnChargeImpactFireZone();

    /// <summary>
    /// Nếu bạn muốn đồng bộ chuyển Phase 2 đúng frame trong animation PhaseChange,
    /// có thể gọi event này trong clip đó. (Không bắt buộc vì logic đã gọi EnterPhase2 trong HandleEnemyHealthDeath).
    /// </summary>
    public void AnimEvent_EnterPhase2FromAnim()
    {
        if (!hasEnteredPhase2)
        {
            hasEnteredPhase2 = true;
            if (enemyHealth != null)
            {
                enemyHealth.ResetHealthToMax();
                enemyHealth.SetDestroyOnDeath(true);
            }
        }
        EnterPhase2();
    }
}
