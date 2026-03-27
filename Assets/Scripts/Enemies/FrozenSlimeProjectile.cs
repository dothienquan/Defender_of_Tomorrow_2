using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FrozenSlimeProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int damage = 1;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f;  // Độ xoay mỗi giây

    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;

    private Rigidbody2D _rb;
    private Vector2 _direction;
    private float _lifeTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Tự hủy sau lifetime
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        _direction = direction.normalized;
        _rb.linearVelocity = _direction * moveSpeed;
        
        // Xoay sprite theo hướng bay
        if (_direction.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= lifetime)
        {
            DestroyProjectile();
        }

        // Tự xoay projectile
        if (rotationSpeed != 0f)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra va chạm với Player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform);
            }
            DestroyProjectile();
            return;
        }

        // Va chạm với tường hoặc vật thể khác (không phải enemy)
        if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        if (hitVFXPrefab != null)
        {
            Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}

