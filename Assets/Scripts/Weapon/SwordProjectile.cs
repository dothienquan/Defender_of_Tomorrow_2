using UnityEngine;

public class SwordProjectile : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 4f;
    public LayerMask targetLayers;

    [SerializeField] private GameObject hitVFXPrefab;     // hiệu ứng khi projectile trúng enemy
    [SerializeField] private bool spawnProjectileOnHit;   // bật/tắt spawn projectile mới trên hit
    [SerializeField] private GameObject projectileOnHit;  // projectile mới muốn spawn

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check đối tượng có phải enemy không
        var enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage); // gây sát thương

            if (hitVFXPrefab != null)
                Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);

            if (spawnProjectileOnHit && projectileOnHit != null)
            {
                var newProj = Instantiate(projectileOnHit, transform.position, Quaternion.identity);

                // Hướng tiếp tục giống hướng viên cũ
                var rbNew = newProj.GetComponent<Rigidbody2D>();
                var rbOld = GetComponent<Rigidbody2D>();
                if (rbNew != null && rbOld != null)
                    rbNew.linearVelocity = rbOld.linearVelocity.normalized * rbOld.linearVelocity.magnitude;
            }

            Destroy(gameObject); // huỷ projectile cũ
        }
    }

}
