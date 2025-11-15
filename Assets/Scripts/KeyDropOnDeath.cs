using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class KeyDropOnDeath : MonoBehaviour
{
    [SerializeField] private GameObject keyPrefab;

    private EnemyHealth enemyHealth;
    private bool hasDropped = false;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        enemyHealth.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        enemyHealth.OnDeath -= HandleDeath;
    }

    public void SetKeyPrefab(GameObject prefab)
    {
        keyPrefab = prefab;
    }

    private void HandleDeath()
    {
        if (hasDropped) return;
        hasDropped = true;

        if (keyPrefab == null)
        {
            Debug.LogWarning("[KeyDropOnDeath] keyPrefab is null on " + gameObject.name);
            return;
        }

        Instantiate(keyPrefab, transform.position, Quaternion.identity);
        Debug.Log("[KeyDropOnDeath] Dropped key from enemy: " + gameObject.name);
    }
}
