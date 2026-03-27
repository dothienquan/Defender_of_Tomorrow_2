using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FireballProjectile : MonoBehaviour
{
    [Tooltip("Th?i gian t?n t?i t?i ?a (gi�y).")]
    public float lifeTime = 3f;

    [Tooltip("Ph� h?y khi ch?m b?t c? th? g� (k? c? t??ng).")]
    public bool destroyOnAnyHit = true;

    [Tooltip("L?p m?c ti�u nh?n s�t th??ng.")]
    public LayerMask damageableLayers;

    private Rigidbody2D _rb;
    private int _damage = 1;
    private GameObject _owner;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 dir, float speed, int damage, GameObject owner)
    {
        _damage = damage;
        _owner = owner;
        _rb.linearVelocity = dir.normalized * speed;
        Invoke(nameof(SelfDestruct), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kh�ng t? g�y damage l�n ch�nh ng??i b?n
        if (_owner != null && other.gameObject == _owner) return;

        // Ki?m tra enemy c� EnemyHealth hay kh�ng (y nh? DamageSource ?ang l�m)
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(_damage);   // <-- gi?ng DamageSource
            SelfDestruct();
            return;
        }

        // N?u ch?m t??ng ho?c v?t th? kh�c -> ph� h?y (tu? config)
        if (destroyOnAnyHit)
            SelfDestruct();
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }
}