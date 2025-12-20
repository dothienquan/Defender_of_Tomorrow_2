using System.Collections;
using UnityEngine;

namespace CrabBoss
{
    /// <summary>
    /// Trigger zone that spawns falling rocks when player stays inside.
    /// Recommended setup:
    /// - Zone object has BoxCollider2D (IsTrigger).
    /// - RockHitboxPrefab: a wrapper prefab with Rigidbody2D + Collider2D + CrabProjectiles.
    /// - RockVfxPrefab: your existing particle-only prefab (optional). It will be instantiated as a CHILD of hitbox.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RockZone : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Wrapper prefab that carries physics + collider + CrabProjectiles.")]
        [SerializeField] private GameObject rockHitboxPrefab;

        [Tooltip("Particle-only prefab. Will be spawned as child of hitbox (optional).")]
        [SerializeField] private GameObject rockVfxPrefab;

        [Header("Spawn")]
        [SerializeField] private int rockCountPerWave = 3;
        [SerializeField] private float timeBetweenRocks = 0.25f;
        [SerializeField] private float waveCooldown = 1.5f;

        [Tooltip("How high above zone bounds the rock spawns from.")]
        [SerializeField] private float spawnHeightAboveZone = 6f;

        [Tooltip("Randomize X inside the zone bounds.")]
        [SerializeField] private bool randomizeX = true;

        [Header("Falling Rock Motion")]
        [SerializeField] private float fallingSpeed = 12f;

        [Header("Damage")]
        [SerializeField] private int rockDamage = 10;
        [SerializeField] private LayerMask hitMask;

        [Header("Pooling (optional)")]
        [SerializeField] private bool usePooling = true;
        [SerializeField] private int poolWarmup = 10;

        private BoxCollider2D _box;
        private bool _playerInside;
        private Coroutine _routine;

        private readonly System.Collections.Generic.Queue<GameObject> _pool =
            new System.Collections.Generic.Queue<GameObject>(64);

        private void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _box.isTrigger = true;

            if (usePooling && rockHitboxPrefab != null)
            {
                for (int i = 0; i < poolWarmup; i++)
                {
                    var go = Instantiate(rockHitboxPrefab);
                    go.SetActive(false);
                    _pool.Enqueue(go);
                }
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            _playerInside = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInside = true;
            if (_routine == null)
                _routine = StartCoroutine(SpawnLoop());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInside = false;
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            // Repeat waves while player stays in zone
            while (_playerInside)
            {
                yield return SpawnWave();

                if (waveCooldown > 0f)
                {
                    float t = 0f;
                    while (t < waveCooldown && _playerInside)
                    {
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
            }
        }

        private IEnumerator SpawnWave()
        {
            if (rockHitboxPrefab == null) yield break;

            for (int i = 0; i < rockCountPerWave; i++)
            {
                SpawnOneRock();

                if (timeBetweenRocks > 0f)
                    yield return new WaitForSeconds(timeBetweenRocks);
                else
                    yield return null;
            }
        }

        private void SpawnOneRock()
        {
            Bounds b = _box.bounds;

            float x = randomizeX ? Random.Range(b.min.x, b.max.x) : b.center.x;
            float y = b.max.y + spawnHeightAboveZone;

            var hitbox = GetFromPool();
            hitbox.transform.position = new Vector3(x, y, 0f);
            hitbox.transform.rotation = Quaternion.identity;
            hitbox.SetActive(true);

            // Ensure projectile config
            var proj = hitbox.GetComponent<CrabProjectiles>();
            if (proj != null)
            {
                proj.SetDamage(rockDamage);
                proj.SetHitMask(hitMask);
                proj.SetFallingRock(fallingSpeed);
            }
            else
            {
                // If prefab forgot this component, still try to push rigidbody down
                var rb = hitbox.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.down * Mathf.Abs(fallingSpeed);
            }

            // Spawn particle as child (VFX only)
            if (rockVfxPrefab != null)
            {
                var vfx = Instantiate(rockVfxPrefab, hitbox.transform);
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = Quaternion.identity;
            }

            // If pooling: auto return to pool after a safe time (since DestroyOnHit would kill it)
            // For pooling mode, set CrabProjectiles.destroyOnHit = false and rely on this timer (recommended).
            if (usePooling)
            {
                StartCoroutine(ReturnToPoolAfter(hitbox, 8f));
            }
        }

        private GameObject GetFromPool()
        {
            if (!usePooling) return Instantiate(rockHitboxPrefab);

            while (_pool.Count > 0)
            {
                var go = _pool.Dequeue();
                if (go != null) return go;
            }

            var created = Instantiate(rockHitboxPrefab);
            created.SetActive(false);
            return created;
        }

        private IEnumerator ReturnToPoolAfter(GameObject go, float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (go == null) yield break;

            // Clear children VFX to avoid accumulation
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(go.transform.GetChild(i).gameObject);
            }

            go.SetActive(false);
            _pool.Enqueue(go);
        }

        // Allow boss to hard-stop spawning when dead
        public void StopAll()
        {
            _playerInside = false;
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }
    }
}