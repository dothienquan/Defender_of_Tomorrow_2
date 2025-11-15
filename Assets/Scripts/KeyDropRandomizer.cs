using UnityEngine;

public class KeyDropRandomizer : MonoBehaviour
{
    [Header("Prefab chìa khóa sẽ spawn")]
    [SerializeField] private GameObject keyPrefab;

    private void Start()
    {
        if (keyPrefab == null)
        {
            Debug.LogWarning("[KeyDropRandomizer] keyPrefab is null.");
            return;
        }

        // Tìm tất cả EnemyHealth trong nhóm (children)
        EnemyHealth[] enemies = GetComponentsInChildren<EnemyHealth>();

        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("[KeyDropRandomizer] No EnemyHealth found in children of " + gameObject.name);
            return;
        }

        int randomIndex = Random.Range(0, enemies.Length);
        EnemyHealth chosen = enemies[randomIndex];

        // Gắn/kiếm component KeyDropOnDeath trên enemy được chọn
        KeyDropOnDeath keyDrop = chosen.GetComponent<KeyDropOnDeath>();
        if (keyDrop == null)
        {
            keyDrop = chosen.gameObject.AddComponent<KeyDropOnDeath>();
        }

        keyDrop.SetKeyPrefab(keyPrefab);

        Debug.Log("[KeyDropRandomizer] Enemy chosen to drop key: " + chosen.gameObject.name);
    }
}
