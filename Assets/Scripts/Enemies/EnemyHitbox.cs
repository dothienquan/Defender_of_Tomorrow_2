using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Dùng để tính hướng knockback lên player, mặc định lấy root của enemy")]
    [SerializeField] private Transform attacker;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (attacker == null)
            attacker = transform.root;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Lấy PlayerHealth trên player
        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // Gọi đúng hàm có sẵn, để giữ nguyên knockback + invulnerability, vv.
        playerHealth.TakeDamage(damage, attacker != null ? attacker : transform);
    }
}
