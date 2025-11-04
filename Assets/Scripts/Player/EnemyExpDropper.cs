using UnityEngine;

public class EnemyExpDropper : MonoBehaviour
{
    [Header("Prefab EXP")]
    public GameObject expPrefab;   // <- Prefab, không phải script!

    [Tooltip("Offset khi spawn (tùy chọn)")]
    public Vector2 spawnOffset = Vector2.zero;

    public void DropExp()
    {
        if (expPrefab == null) return;
        Instantiate(expPrefab, (Vector2)transform.position + spawnOffset, Quaternion.identity);
    }
}
