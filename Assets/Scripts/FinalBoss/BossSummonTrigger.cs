using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossSummonTrigger : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private GameObject bossPrefab;       // Prefab boss
    [SerializeField] private Transform bossSpawnPoint;    // Điểm spawn boss (nếu null sẽ dùng vị trí trigger)

    [Header("Summon VFX")]
    [SerializeField] private GameObject summonVfxPrefab;  // Hiệu ứng triệu hồi
    [SerializeField] private float bossSpawnDelay = 0.5f; // Delay sau VFX mới spawn boss

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;     // Chỉ chạy 1 lần

    private bool hasTriggered = false;

    private void Reset()
    {
        // Auto set collider thành trigger cho đỡ quên
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        StartCoroutine(SummonRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        // Vị trí triệu hồi: ưu tiên bossSpawnPoint, nếu không có thì ngay chỗ trigger
        Vector3 spawnPos = bossSpawnPoint ? bossSpawnPoint.position : transform.position;

        // Spawn hiệu ứng triệu hồi
        if (summonVfxPrefab != null)
        {
            Instantiate(summonVfxPrefab, spawnPos, Quaternion.identity);
        }

        // Đợi 1 chút rồi mới xuất hiện boss (cho cảm giác "triệu hồi" đúng nghĩa)
        if (bossSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(bossSpawnDelay);
        }

        // Spawn boss
        if (bossPrefab != null)
        {
            Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[BossSummonTrigger] bossPrefab chưa được gán!");
        }
    }
}
