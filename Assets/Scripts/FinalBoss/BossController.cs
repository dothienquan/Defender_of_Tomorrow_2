using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Health / Phase")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private float phase2ThresholdPercent = 0.5f;
    [SerializeField] private float vulnerableDuration = 6f;

    [Header("Orbs")]
    [SerializeField] private BossOrb orbPrefab;
    [SerializeField] private int orbCount = 3;
    [SerializeField] private float orbOrbitRadius = 2.5f;

    private readonly List<BossOrb> orbs = new List<BossOrb>();
    private int groundedOrbCount = 0;
    private bool inVulnerableWindow = false;

    [Header("Attack timing")]
    [SerializeField] private float minTimeBetweenAttacks = 1.5f;
    [SerializeField] private float maxTimeBetweenAttacks = 3.0f;

    [Header("Lightning prefabs (Phase 1)")]
    [SerializeField] private LightningStrikeArea lightningStrikeAreaPrefab;
    [SerializeField] private LightningProjectile lightningProjectilePrefab;

    private Transform player;
    private bool isPhase2 = false;
    private bool fightActive = false;

    private enum BossState { Waiting, Phase1, Phase2, Dead }
    private BossState state = BossState.Waiting;

    private void Awake()
    {
        if (!bossHealth) bossHealth = GetComponent<BossHealth>();
    }

    private void Start()
    {
        player = PlayerHealth.Instance.transform;

        bossHealth.IsInvulnerable = true;

        SpawnOrbs();

        state = BossState.Phase1;
        fightActive = true;
        StartCoroutine(Phase1Loop());
    }

    private void Update()
    {
        if (!fightActive) return;

        if (!isPhase2 && bossHealth.HealthPercent <= phase2ThresholdPercent)
        {
            isPhase2 = true;
            state = BossState.Phase2;
            // TODO: dừng Phase1Loop và start Phase2Loop
        }
    }

    #region ORBS

    private void SpawnOrbs()
    {
        orbs.Clear();
        if (orbPrefab == null) return;

        float angleStep = 360f / orbCount;

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

        if (groundedOrbCount >= orbs.Count)
        {
            StartCoroutine(VulnerableWindowRoutine());
        }
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

            // nếu muốn vị trí cố định:
            float startAngle = i * angleStep;

            // nếu muốn random hoàn toàn:
            // float startAngle = Random.Range(0f, 360f);

            orb.ResetToOrbit(startAngle);
        }
    }


    private IEnumerator VulnerableWindowRoutine()
    {
        if (inVulnerableWindow) yield break;

        inVulnerableWindow = true;

        bossHealth.IsInvulnerable = false;
        // TODO: animation stun

        yield return new WaitForSeconds(vulnerableDuration);

        bossHealth.IsInvulnerable = true;
        ResetOrbs();

        inVulnerableWindow = false;
    }

    #endregion

    #region PHASE 1 LOOP

    private IEnumerator Phase1Loop()
    {
        while (state == BossState.Phase1)
        {
            float wait = Random.Range(minTimeBetweenAttacks, maxTimeBetweenAttacks);
            yield return new WaitForSeconds(wait);

            if (inVulnerableWindow) continue;

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
        }
    }

    #endregion

    #region SKILLS

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

    #endregion
}
