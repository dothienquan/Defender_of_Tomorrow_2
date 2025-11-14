
using UnityEngine;

/// <summary>
/// Cầu lửa bắn thẳng, chạm Player thì gây dmg rồi huỷ.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MalugazFireball : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private LayerMask hitMask;

    private Rigidbody2D rb;
    private int damage;

    public void Init(Vector2 direction, float speed, int damageAmount)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;
        damage = damageAmount;
        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitMask) != 0)
        {
            if (other.CompareTag("Player"))
            {
                PlayerHealth.Instance.TakeDamage(damage, transform);
            }

            Destroy(gameObject);
        }
    }
}
