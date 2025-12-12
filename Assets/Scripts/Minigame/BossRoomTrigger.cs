using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossRoomTrigger : MonoBehaviour
{
    [Header("UI Reference")]
    public BossHPUI bossUI;

    [Header("Boss Reference (Choose One)")]
    [Tooltip("Assign boss prefab from folder - will find or spawn boss")]
    public GameObject bossPrefab;
    [Tooltip("Or assign boss directly if already in scene")]
    public EnemyHealth bossHealth;

    [Header("Spawn Settings (Only if using Prefab)")]
    [Tooltip("Where to spawn boss (if null, uses trigger position)")]
    public Transform bossSpawnPoint;
    [Tooltip("Tag to search for existing boss in scene")]
    public string bossTag = "Boss";
    [Tooltip("If true, will spawn new boss even if one exists")]
    public bool forceSpawnNew = false;

    private EnemyHealth currentBossInstance;
    private bool hasTriggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        if (bossUI == null)
        {
            Debug.LogWarning($"[BossRoomTrigger] bossUI is not assigned on {gameObject.name}", this);
            return;
        }

        EnemyHealth targetBoss = GetBossReference();
        if (targetBoss == null)
        {
            Debug.LogWarning($"[BossRoomTrigger] Could not find or create boss on {gameObject.name}", this);
            return;
        }

        bossUI.ShowBossHP(targetBoss);
    }

    private EnemyHealth GetBossReference()
    {
        // If direct reference is provided, use it
        if (bossHealth != null)
        {
            return bossHealth;
        }

        // If prefab is provided, find or spawn boss
        if (bossPrefab != null)
        {
            // Check if we already have a spawned instance
            if (currentBossInstance != null && currentBossInstance.gameObject != null)
            {
                return currentBossInstance;
            }

            // Try to find existing boss in scene
            if (!forceSpawnNew)
            {
                GameObject existingBoss = GameObject.FindGameObjectWithTag(bossTag);
                if (existingBoss != null)
                {
                    EnemyHealth existingHealth = existingBoss.GetComponent<EnemyHealth>();
                    if (existingHealth != null)
                    {
                        currentBossInstance = existingHealth;
                        return existingHealth;
                    }
                }

                // Also search by prefab name
                EnemyHealth[] allBosses = FindObjectsOfType<EnemyHealth>();
                foreach (var boss in allBosses)
                {
                    if (boss.gameObject.name.Contains(bossPrefab.name))
                    {
                        currentBossInstance = boss;
                        return boss;
                    }
                }
            }

            // Spawn new boss from prefab
            Vector3 spawnPos = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
            GameObject bossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            currentBossInstance = bossInstance.GetComponent<EnemyHealth>();

            if (currentBossInstance == null)
            {
                Debug.LogError($"[BossRoomTrigger] Boss prefab {bossPrefab.name} does not have EnemyHealth component!", this);
                Destroy(bossInstance);
                return null;
            }

            return currentBossInstance;
        }

        return null;
    }
}
