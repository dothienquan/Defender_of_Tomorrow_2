using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossPhase1Controller : MonoBehaviour, IEnemy, IBossOrbOwner
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Player")]
    [SerializeField] private Transform playerOverride;

    [Header("Vulnerable Window (Orbs)")]
    [SerializeField] private float vulnerableDuration = 5f;

    [Header("Orbs")]
    [SerializeField] private BossOrb orbPrefab;
    [SerializeField] private int orbCount = 3;
    [SerializeField] private float orbOrbitRadius = 2.5f;

    [Header("Lightning (Phase 1)")]
    [SerializeField] private LightningStrikeArea lightningStrikeAreaPrefab;
    [SerializeField] private LightningProjectile lightningProjectilePrefab;

    // ✅ KHÔNG Serialize nữa (prefab không kéo scene object được)
    private BossPhaseTransition phaseTransition;

    private Transform player;
    private readonly List<BossOrb> orbs = new List<BossOrb>();

    private int groundedOrbCount;
    private bool inVulnerableWindow;
    private bool isAttacking;

    private FieldInfo currentHealthField;
    private int lastRecordedHealth;

    private void Awake()
    {
        if (!enemyHealth)
            enemyHealth = GetComponent<EnemyHealth>();

        var type = typeof(EnemyHealth);
        currentHealthField = type.GetField(
            "currentHealth",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth != null)
            enemyHealth.OnDeath += OnBossDeath;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnDeath -= OnBossDeath;
    }

    private void Start()
    {
        // ✅ auto find transition trong scene (spawn prefab vẫn tìm được)
        phaseTransition = FindObjectOfType<BossPhaseTransition>(true);
        if (phaseTransition == null)
            Debug.LogError("[BossPhase1Controller] Không tìm thấy BossPhaseTransition trong scene!");

        if (playerOverride != null)
            player = playerOverride;
        else if (PlayerHealth.Instance != null)
            player = PlayerHealth.Instance.transform;

        if (enemyHealth != null && currentHealthField != null)
            lastRecordedHealth = (int)currentHealthField.GetValue(enemyHealth);

        SpawnOrbs();
        groundedOrbCount = 0;
        inVulnerableWindow = false;
    }

    private void LateUpdate()
    {
        if (enemyHealth == null || currentHealthField == null) return;

        int current = (int)currentHealthField.GetValue(enemyHealth);
        bool bossInvulnerable = !inVulnerableWindow;

        if (bossInvulnerable)
        {
            if (current < lastRecordedHealth)
            {
                currentHealthField.SetValue(enemyHealth, lastRecordedHealth);
                current = lastRecordedHealth;
            }
            else
            {
                lastRecordedHealth = current;
            }
        }
        else
        {
            lastRecordedHealth = current;
        }
    }

    public void Attack()
    {
        if (isAttacking) return;
        if (inVulnerableWindow) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        int skillIndex = Random.Range(0, 2);
        switch (skillIndex)
        {
            case 0:
                yield return CastRandomLightningStrikes();
                break;
            case 1:
                yield return CastSingleFastLightning();
                break;
        }

        isAttacking = false;
    }

    private void SpawnOrbs()
    {
        orbs.Clear();
        groundedOrbCount = 0;

        if (orbPrefab == null) return;

        float angleStep = 360f / Mathf.Max(1, orbCount);

        for (int i = 0; i < orbCount; i++)
        {
            float angle = i * angleStep;
            BossOrb orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
            orb.Setup(this, transform, player, angle, orbOrbitRadius);
            orbs.Add(orb);
        }
    }

    public void NotifyOrbGrounded(BossOrb orb)
    {
        groundedOrbCount++;

        if (!inVulnerableWindow && groundedOrbCount >= orbs.Count && orbs.Count > 0)
            StartCoroutine(VulnerableWindowRoutine());
    }

    private void ResetOrbs()
    {
        groundedOrbCount = 0;
        if (orbs.Count == 0) return;

        float angleStep = 360f / orbs.Count;

        for (int i = 0; i < orbs.Count; i++)
        {
            var orb = orbs[i];
            if (orb == null) continue;

            float startAngle = i * angleStep;
            orb.ResetToOrbit(startAngle);
        }

        inVulnerableWindow = false;
    }

    private IEnumerator VulnerableWindowRoutine()
    {
        if (inVulnerableWindow) yield break;

        inVulnerableWindow = true;

        yield return new WaitForSeconds(vulnerableDuration);

        ResetOrbs();
    }

    private void OnBossDeath()
    {
        StopAllCoroutines();

        foreach (var orb in orbs)
            if (orb != null)
                Destroy(orb.gameObject);

        orbs.Clear();

        // ✅ gọi transition (nếu scene có)
        if (phaseTransition != null)
            phaseTransition.StartPhaseTransition();

        Destroy(gameObject);
    }

    private IEnumerator CastRandomLightningStrikes()
    {
        if (!player || lightningStrikeAreaPrefab == null) yield break;

        int pattern = Random.Range(0, 4);
        Vector2 playerPos = player.position;

        Vector2[] offsets;
        switch (pattern)
        {
            case 0:
                offsets = new[]
                {
                    Vector2.zero,
                    new Vector2(2, 0),
                    new Vector2(-2, 0),
                    new Vector2(0, 2),
                    new Vector2(0, -2)
                };
                break;
            case 1:
                offsets = new[]
                {
                    new Vector2(1.5f, 0),
                    new Vector2(-1.5f, 0),
                    new Vector2(0, 1.5f),
                    new Vector2(0, -1.5f)
                };
                break;
            case 2:
                offsets = new[]
                {
                    new Vector2(1.5f, 1.5f),
                    new Vector2(-1.5f, 1.5f),
                    new Vector2(1.5f, -1.5f),
                    new Vector2(-1.5f, -1.5f)
                };
                break;
            default:
                offsets = new Vector2[6];
                for (int i = 0; i < offsets.Length; i++)
                    offsets[i] = Random.insideUnitCircle * 3f;
                break;
        }

        foreach (var off in offsets)
        {
            Vector2 pos = playerPos + off;
            Instantiate(lightningStrikeAreaPrefab, pos, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator CastSingleFastLightning()
    {
        if (!player || lightningProjectilePrefab == null) yield break;

        Vector2 startPos = transform.position;
        Vector2 dir = ((Vector2)player.position - startPos).normalized;

        LightningProjectile proj = Instantiate(lightningProjectilePrefab, startPos, Quaternion.identity);
        proj.Init(dir);

        yield return null;
    }
}
