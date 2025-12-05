using UnityEngine;

public class EnemyHitbox1 : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform enemyRoot; // dùng cho knockback hướng từ enemy

    private void Reset()
    {
        // auto gán enemyRoot = root
        if (enemyRoot == null)
            enemyRoot = transform.root;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player phải được gán tag "Player"
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // Gọi đúng hàm bạn đã có
        playerHealth.TakeDamage(damage, enemyRoot != null ? enemyRoot : transform);
    }
}
