using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class BossPhase2Controller : MonoBehaviour, IEnemy, IBossOrbOwner
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Player")]
    [SerializeField] private Transform playerOverride;

    [Header("Vulnerable Window (Orbs)")]
    [SerializeField] private float vulnerableDuration = 5f; // 5s khi tất cả orbs nằm đất

    [Header("Orbs")]
    [SerializeField] private BossOrb orbPrefab;
    [SerializeField] private int orbCount = 3;
    [SerializeField] private float orbOrbitRadius = 2.5f;

    [Header("Lightning (Phase 2)")]
    [SerializeField] private LightningStrikeArea lightningStrikeAreaPrefab;
    [SerializeField] private LightningProjectile lightningProjectilePrefab;

    [Header("Void Field")]
    [SerializeField] private SafeZone safeZonePrefab;
    [SerializeField] private GameObject voidBGPrefab;
    [SerializeField] private GameObject voidCastWavePrefab;
    [SerializeField] private float voidFieldDuration = 10f;
    [SerializeField] private float voidRadius = 7f; // 🔹 radius gameplay, inspector chỉnh

    private Transform player;
    private bool isAttacking;

    // Orbs data riêng cho Phase 2
    private readonly List<BossOrb> orbs = new List<BossOrb>();
    private int groundedOrbCount;      // bao nhiêu orb đã rơi xuống đất
    private bool isVulnerable;         // đang trong 5s boss ăn damage được

    // Hack invul: đọc/ghi currentHealth bằng reflection
    private FieldInfo currentHealthField;
    private int lastRecordedHealth;

    private void Awake()
    {
        if (!enemyHealth)
            enemyHealth = GetComponent<EnemyHealth>();

        // chuẩn bị reflection để chọc currentHealth trong EnemyHealth
        var type = typeof(EnemyHealth);
        currentHealthField = type.GetField("currentHealth",
            BindingFlags.NonPublic | BindingFlags.Instance);
    }

    // 🔹 ĐĂNG KÝ / HỦY ĐĂNG KÝ SỰ KIỆN CHẾT CỦA BOSS
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
        if (playerOverride != null)
            player = playerOverride;
        else if (PlayerHealth.Instance != null)
            player = PlayerHealth.Instance.transform;

        // lấy HP ban đầu làm mốc
        if (enemyHealth != null && currentHealthField != null)
        {
            lastRecordedHealth = (int)currentHealthField.GetValue(enemyHealth);
        }

        SpawnOrbs();
        groundedOrbCount = 0;
        isVulnerable = false; // ban đầu có orbs bay -> boss không ăn damage
    }

    private void LateUpdate()
    {
        if (enemyHealth == null || currentHealthField == null) return;

        int current = (int)currentHealthField.GetValue(enemyHealth);

        // Boss CHỈ vulnerable trong 5s khi TẤT CẢ orbs đã rơi (isVulnerable == true)
        bool bossInvulnerable = !isVulnerable;

        if (bossInvulnerable)
        {
            // Nếu máu tụt trong trạng thái invul -> rollback về lastRecordedHealth
            if (current < lastRecordedHealth)
            {
                currentHealthField.SetValue(enemyHealth, lastRecordedHealth);
                current = lastRecordedHealth;
            }
            else
            {
                // nếu được heal thì vẫn chấp nhận
                lastRecordedHealth = current;
            }
        }
        else
        {
            // đang vulnerable -> chấp nhận damage
            lastRecordedHealth = current;
        }
    }

    /// <summary>
    /// EnemyAI gọi khi ở trong AttackZone.
    /// </summary>
    public void Attack()
    {
        if (isAttacking) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (!player)
        {
            isAttacking = false;
            yield break;
        }

        int skill = Random.Range(0, 3);

        switch (skill)
        {
            case 0:
                yield return CastRandomLightningStrikes();
                break;
            case 1:
                yield return CastSingleFastLightning();
                break;
            case 2:
                yield return CastVoidField();
                break;
        }

        isAttacking = false;
    }

    #region ORBS + VULNERABLE (Phase 2)

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

            // owner = this (IBossOrbOwner), boss = transform, player, góc bắt đầu, bán kính
            orb.Setup(this, transform, player, angle, orbOrbitRadius);

            orbs.Add(orb);
        }
    }

    /// <summary>
    /// Gọi bởi BossOrb khi orb của phase 2 chạm đất (hết máu).
    /// </summary>
    public void NotifyOrbGrounded(BossOrb orb)
    {
        groundedOrbCount++;

        // CHỈ khi TẤT CẢ orbs đều đã rơi xuống đất -> boss bắt đầu nhận sát thương
        if (!isVulnerable && groundedOrbCount >= orbs.Count && orbs.Count > 0)
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

            float startAngle = i * angleStep;
            orb.ResetToOrbit(startAngle);
        }

        // Sau khi reset: orbs lại bay quanh boss -> boss invul trở lại
        isVulnerable = false;
    }

    private IEnumerator VulnerableWindowRoutine()
    {
        if (isVulnerable) yield break;

        // Bắt đầu 5s: all orbs nằm đất -> boss ăn sát thương
        isVulnerable = true;

        yield return new WaitForSeconds(vulnerableDuration);

        // Hết 5s: orbs hồi lại, bay quanh boss, boss lại không ăn sát thương
        ResetOrbs();
    }

    #endregion

    #region BOSS DEATH CLEANUP

    private void OnBossDeath()
    {
        // dừng toàn bộ skill/coroutine của boss
        StopAllCoroutines();

        // dọn toàn bộ orbs
        for (int i = 0; i < orbs.Count; i++)
            if (orbs[i] != null)
                Destroy(orbs[i].gameObject);
        orbs.Clear();

        // 🔥 DỌN VÙNG VOID CÒN TỒN TẠI 🔥

        // 1) Destroy tất cả VoidDamageController còn đang sống
        var voidControllers = FindObjectsOfType<VoidDamageController>();
        foreach (var v in voidControllers)
            Destroy(v.gameObject);

        // 2) Destroy Void BG (vì nó không tự tắt)
        var allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            // Nếu prefab đặt tên "VoidBG", "VoidBackground", hoặc tên tương tự thì chỉnh tại đây
            if (obj.name.Contains("VoidBG") || obj.name.Contains("VoidBackground"))
                Destroy(obj);
        }

        // 3) Destroy SafeZones còn sót
        foreach (var z in SafeZone.ActiveZones)
            if (z != null)
                Destroy(z.gameObject);

        SafeZone.ActiveZones.Clear();
    }


    #endregion

    #region LIGHTNING

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

    #region VOID FIELD

    private void SpawnLightningAroundSafeZone(SafeZone zone)
    {
        if (zone == null || lightningStrikeAreaPrefab == null) return;

        float r = zone.CurrentRadius;
        int count = 6;
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float ang = step * i * Mathf.Deg2Rad;
            Vector2 pos = (Vector2)zone.transform.position +
                          new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (r + 1f);

            Instantiate(lightningStrikeAreaPrefab, pos, Quaternion.identity);
        }
    }

    private IEnumerator CastVoidField()
    {
        if (safeZonePrefab == null || voidBGPrefab == null || voidCastWavePrefab == null)
            yield break;

        // object điều khiển damage void
        var voidObj = new GameObject("VoidDamageController");
        var voidDamage = voidObj.AddComponent<VoidDamageController>();

        // NỀN VOID (VISUAL)
        var bg = Instantiate(voidBGPrefab, transform.position, Quaternion.identity);
        voidObj.transform.position = bg.transform.position;

        // scale BG để vòng sprite đúng bằng voidRadius
        var sr = bg.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // bán kính sprite local khi scale = 1
            float spriteRadius = sr.sprite.bounds.extents.x;
            if (spriteRadius > 0.0001f)
            {
                float scaleFactor = voidRadius / spriteRadius;
                bg.transform.localScale = Vector3.one * scaleFactor;
            }
        }

        // gameplay radius cho damage + gizmo
        voidDamage.voidRadius = voidRadius;

        // ===== SPAWN SAFE ZONE TẠI VỊ TRÍ ORB =====
        List<SafeZone> zones = new List<SafeZone>();

        for (int i = 0; i < orbs.Count; i++)
        {
            if (orbs[i] == null) continue;

            Vector2 spawnPos = orbs[i].transform.position;

            SafeZone z = Instantiate(
                 safeZonePrefab,
                 spawnPos,
                 Quaternion.identity
            );

            // cho safezone sống đúng bằng thời gian void tồn tại
            z.Init(voidFieldDuration);

            zones.Add(z);
        }

        float timer = 0f;

        while (timer < voidFieldDuration)
        {
            foreach (var z in zones)
            {
                if (z != null && z.IsActive)
                    SpawnLightningAroundSafeZone(z);
            }

            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        // ===== DỌN DẸP =====
        Destroy(voidObj);

        foreach (var z in zones)
            if (z != null)
                Destroy(z.gameObject);

        Instantiate(voidCastWavePrefab, transform.position, Quaternion.identity);
        Destroy(bg);
    }

    #endregion
}
