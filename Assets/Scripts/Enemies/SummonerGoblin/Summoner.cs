using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// TÁCH RIÊNG: KHÔNG sửa EnemyHealth.
/// Chỉ TRIỆU HỒI khi MÁU GIẢM từ >50% xuống <=50% (không gọi lúc start).
/// - Đọc health qua getter nếu có; nếu không có thì reflection.
/// - Có cờ "seenAboveHalf" để đảm bảo chỉ kích hoạt sau khi từng ở trên 50%.
/// </summary>
public class Summoner : MonoBehaviour
{
    [Header("Summon Config")]
    [Tooltip("Prefab(s) để triệu hồi")]
    [SerializeField] private GameObject[] summonPrefabs;
    [Tooltip("Tổng số enemy triệu hồi")]
    [SerializeField] private int summonCount = 3;
    [Tooltip("Bán kính ngẫu nhiên quanh enemy khi spawn")]
    [SerializeField] private float summonRadius = 2f;
    [Tooltip("Chỉ triệu hồi 1 lần khi qua ngưỡng")]
    [SerializeField] private bool summonOnce = true;

    [Header("Health Threshold")]
    [Tooltip("Triệu hồi khi HP <= % ngưỡng này (0.5 = 50%)")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float thresholdPercent = 0.5f;
    [Tooltip("Tần suất kiểm tra (giảm về 0.05-0.2 để tiết kiệm)")]
    [SerializeField] private float checkInterval = 0.1f;

    private EnemyHealth enemyHealth;
    private bool hasSummoned = false;
    private bool seenAboveHalf = false; // NEW: chỉ cho phép summon sau khi từng > 50%

    // Reflection cache
    private FieldInfo fiCurrentHealth;
    private FieldInfo fiStartingHealth;
    private MethodInfo miGetCurrentHealth;
    private MethodInfo miGetStartingHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("[Summoner] Không tìm thấy EnemyHealth trên GameObject.", this);
            enabled = false;
            return;
        }

        // Tìm method public/non-public nếu có
        Type t = enemyHealth.GetType();
        miGetCurrentHealth = t.GetMethod("GetCurrentHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        miGetStartingHealth = t.GetMethod("GetStartingHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // Fallback field private
        fiCurrentHealth = t.GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
        fiStartingHealth = t.GetField("startingHealth", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void OnEnable()
    {
        StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        // Đợi 1 frame để EnemyHealth.Start() kịp set currentHealth = startingHealth
        yield return null;

        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            TrySummonIfThresholdCrossed();
            yield return wait;
        }
    }

    private void TrySummonIfThresholdCrossed()
    {
        if (summonPrefabs == null || summonPrefabs.Length == 0) return;
        if (summonCount <= 0) return;
        if (summonOnce && hasSummoned) return;

        if (!TryGetHealth(out int current, out int starting)) return;
        if (starting <= 0) return;

        float hpPercent = (float)current / (float)starting;

        // Ghi nhận khi đã từng ở trên 50% (tránh summon sớm khi currentHealth chưa init)
        if (hpPercent > thresholdPercent)
        {
            seenAboveHalf = true;
            return;
        }

        // Chỉ summon khi đã từng >50% VÀ hiện tại <=50%
        if (seenAboveHalf && hpPercent <= thresholdPercent)
        {
            hasSummoned = true;
            for (int i = 0; i < summonCount; i++)
            {
                GameObject prefab = summonPrefabs[UnityEngine.Random.Range(0, summonPrefabs.Length)];
                Vector2 offset = UnityEngine.Random.insideUnitCircle * summonRadius;
                Vector3 pos = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, transform.position.z);
                Instantiate(prefab, pos, Quaternion.identity);
            }
            Debug.Log($"[Summoner] Summoned {summonCount} enemy at {hpPercent:P0} HP.", this);
        }
    }

    private bool TryGetHealth(out int current, out int starting)
    {
        current = 0;
        starting = 0;

        try
        {
            // Ưu tiên method
            if (miGetCurrentHealth != null)
                current = (int)miGetCurrentHealth.Invoke(enemyHealth, null);
            if (miGetStartingHealth != null)
                starting = (int)miGetStartingHealth.Invoke(enemyHealth, null);

            // Fallback field
            if (miGetCurrentHealth == null && fiCurrentHealth != null)
                current = (int)fiCurrentHealth.GetValue(enemyHealth);
            if (miGetStartingHealth == null && fiStartingHealth != null)
                starting = (int)fiStartingHealth.GetValue(enemyHealth);

            if (starting <= 0) return false;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Summoner] Lỗi đọc health: " + e.Message, this);
            return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}
