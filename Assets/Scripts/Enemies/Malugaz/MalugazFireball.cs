using UnityEngine;

/// <summary>
/// Cầu lửa bắn thẳng, chạm Player thì gây dmg rồi huỷ.
/// Dùng trực tiếp cho MalugazAI.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MalugazFireball : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;
    private int damage;
    private Transform owner;   // Boss – dùng làm hướng knockback (tuỳ chọn)

    /// <summary>
    /// direction: hướng bắn
    /// speed: tốc độ bay
    /// damageAmount: sát thương
    /// owner: transform boss (tuỳ chọn, để knockback đúng hướng)
    /// </summary>
    public void Init(Vector2 direction, float speed, int damageAmount, Transform owner = null)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = direction.normalized * speed;
        damage = damageAmount;
        this.owner = owner;

        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // đảm bảo collider là trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // không bị gravity kéo
        rb.gravityScale = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ xử lý Player
        if (!other.CompareTag("Player")) return;

        if (PlayerHealth.Instance != null)
        {
            Transform hitFrom = owner != null ? owner : transform;
            PlayerHealth.Instance.TakeDamage(damage, hitFrom);
        }

        Destroy(gameObject);
    }
}
