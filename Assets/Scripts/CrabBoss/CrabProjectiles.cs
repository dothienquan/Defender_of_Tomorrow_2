using UnityEngine;

namespace CrabBoss
{
    /// <summary>
    /// Generic boss projectile / falling rock hitbox.
    /// Attach this to the HITBOX object (not the particle-only VFX).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CrabProjectiles : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private int damage = 10;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private bool destroyOnHit = true;

        [Header("Lifetime")]
        [SerializeField] private float lifetimeSeconds = 6f;

        [Header("Motion")]
        [SerializeField] private bool useRigidbody = true;
        [SerializeField] private float speed = 7f;

        private Rigidbody2D _rb;
        private float _despawnAt;
        private bool _launched;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _despawnAt = Time.time + lifetimeSeconds;
        }

        private void Update()
        {
            if (lifetimeSeconds > 0f && Time.time >= _despawnAt)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>For normal straight projectiles.</summary>
        public void Launch(Vector2 direction, float overrideSpeed = -1f)
        {
            _launched = true;
            float s = overrideSpeed > 0f ? overrideSpeed : speed;

            if (useRigidbody && _rb != null)
            {
                _rb.linearVelocity = direction.normalized * s;
            }
            else
            {
                // Fallback: manual move in FixedUpdate not implemented here; prefer Rigidbody2D.
                transform.right = direction.normalized;
            }
        }

        /// <summary>For falling rocks: force vertical downward velocity.</summary>
        public void SetFallingRock(float fallSpeed)
        {
            _launched = true;
            if (useRigidbody && _rb != null)
            {
                _rb.linearVelocity = Vector2.down * Mathf.Abs(fallSpeed);
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);

        private void TryHit(Collider2D other)
        {
            if (!enabled || other == null) return;

            // layer filter
            if (((1 << other.gameObject.layer) & hitMask.value) == 0) return;

            if (TryApplyDamage(other.gameObject))
            {
                if (destroyOnHit)
                    Destroy(gameObject);
            }
        }

        private bool TryApplyDamage(GameObject target)
        {
            // Preferred: your own interface/system
            if (target.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(damage);
                return true;
            }

            // Common alternative
            var ph = target.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage,transform);
                return true;
            }

            // Fallback
            target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            return true;
        }

        public void SetDamage(int value) => damage = value;
        public void SetHitMask(LayerMask mask) => hitMask = mask;
    }

    // If your project already has IDamageable, delete this interface from this file.
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}