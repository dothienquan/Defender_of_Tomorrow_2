using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpawnOnPlayerEnter : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnOnce = true;
    [SerializeField] private bool destroyOnSpawn = false;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    private Collider2D _trigger;
    private bool _hasSpawned = false;

    private void Awake()
    {
        _trigger = GetComponent<Collider2D>();
        if (_trigger != null) _trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        SpawnNow();
    }

    // ✅ gọi bằng code sau dialog 2
    public void SpawnNow()
    {
        if (spawnOnce && _hasSpawned) return;

        SpawnPrefab();
        _hasSpawned = true;

        if (destroyOnSpawn)
        {
            Destroy(gameObject);
        }
    }

    private void SpawnPrefab()
    {
        if (prefabToSpawn == null) return;

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }
}
