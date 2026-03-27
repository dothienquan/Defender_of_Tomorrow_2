using UnityEngine;

/// Put this on each boss prefab.
/// Assign a BossUIProfile per boss type to decide which UI prefab appears.
public class BossSpawnAnnouncer : MonoBehaviour
{
    [Tooltip("If empty, will auto-find EnemyHealth on this GameObject.")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Boss UI Variant")]
    [SerializeField] private BossUIProfile uiProfile;

    private void Awake()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        if (enemyHealth == null) return;

        var mgr = BossHealthUIManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[BossSpawnAnnouncer] No BossHealthUIManager in scene.");
            return;
        }

        if (uiProfile != null)
            mgr.ShowFor(enemyHealth, uiProfile);
        else
            mgr.ShowFor(enemyHealth); // fallback to default UI
    }
}
