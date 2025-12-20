using UnityEngine;

namespace CrabBoss
{
    /// <summary>
    /// Simple boss controller inspired by FrozenSlimeController-style:
    /// patrol/chase/attack with Animator parameters (Attack + LastX/LastY).
    /// RockZone is independent; this controller only optionally triggers attack projectile.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CrabBossController : MonoBehaviour
    {
        private enum State { Patrol, Chase, Attack, Stunned, Dead }

        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private Animator animator;
        [SerializeField] private RockZone rockZone;
        [SerializeField] private EnemyHealth enemyHealth; // if you have it

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 3.25f;
        [SerializeField] private float chaseRange = 6f;
        [SerializeField] private float attackRange = 2.2f;

        [Header("Attack (optional projectile)")]
        [SerializeField] private bool useProjectileAttack = false;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform projectileSpawn;
        [SerializeField] private float projectileSpeed = 7f;

        [Header("Attack Timing")]
        [SerializeField] private float attackCooldown = 2.0f;
        [SerializeField] private float attackAnimDuration = 0.55f;
        [SerializeField] private float postAttackStun = 0.2f;

        private Rigidbody2D _rb;
        private State _state = State.Patrol;

        private float _nextAttackTime;
        private float _stunUntil;

        private Vector2 _moveDir = Vector2.left;
        private Vector2 _lastFacing = Vector2.down;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            if (rockZone == null) rockZone = GetComponentInChildren<RockZone>();
            if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        }

        private void Update()
        {
            if (_state == State.Dead) return;

            if (enemyHealth != null && enemyHealth.IsDead)
            {
                Die();
                return;
            }

            if (player == null)
            {
                _state = State.Patrol;
                return;
            }

            if (Time.time < _stunUntil)
            {
                _state = State.Stunned;
                SetMove(Vector2.zero);
                return;
            }

            float dist = Vector2.Distance(transform.position, player.position);

            if (dist <= attackRange && Time.time >= _nextAttackTime)
            {
                StartAttack();
            }
            else if (dist <= chaseRange)
            {
                _state = State.Chase;
            }
            else
            {
                _state = State.Patrol;
            }
        }

        private void FixedUpdate()
        {
            if (_state == State.Dead) return;

            switch (_state)
            {
                case State.Patrol:
                    PatrolMove();
                    break;
                case State.Chase:
                    ChaseMove();
                    break;
                case State.Attack:
                case State.Stunned:
                    SetMove(Vector2.zero);
                    break;
            }
        }

        private void PatrolMove()
        {
            // Simple back-and-forth using current moveDir
            SetMove(_moveDir * moveSpeed);
        }

        private void ChaseMove()
        {
            Vector2 toPlayer = (player.position - transform.position);
            Vector2 dir = toPlayer.normalized;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                _lastFacing = new Vector2(Mathf.Sign(dir.x), 0f);
            else
                _lastFacing = new Vector2(0f, Mathf.Sign(dir.y));

            SetAnimatorFacing(_lastFacing);

            SetMove(dir * chaseSpeed);
        }

        private void SetMove(Vector2 velocity)
        {
            _rb.linearVelocity = velocity;

            if (velocity.sqrMagnitude > 0.001f)
            {
                Vector2 dir = velocity.normalized;
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                    _lastFacing = new Vector2(Mathf.Sign(dir.x), 0f);
                else
                    _lastFacing = new Vector2(0f, Mathf.Sign(dir.y));

                SetAnimatorFacing(_lastFacing);
            }
        }

        private void StartAttack()
        {
            _state = State.Attack;
            _nextAttackTime = Time.time + attackCooldown;
            _stunUntil = Time.time + attackAnimDuration + postAttackStun;

            if (animator != null)
            {
                SetAnimatorFacing(_lastFacing);
                animator.SetTrigger("Attack");
            }

            if (useProjectileAttack && projectilePrefab != null)
            {
                FireProjectile();
            }
        }

        private void FireProjectile()
        {
            Vector3 spawnPos = projectileSpawn != null ? projectileSpawn.position : transform.position;
            var go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            var proj = go.GetComponent<CrabProjectiles>();
            if (proj != null)
            {
                proj.Launch(_lastFacing, projectileSpeed);
            }
            else
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = _lastFacing.normalized * projectileSpeed;
            }
        }

        private void SetAnimatorFacing(Vector2 face)
        {
            if (animator == null) return;
            animator.SetFloat("LastX", face.x);
            animator.SetFloat("LastY", face.y);
        }

        private void Die()
        {
            _state = State.Dead;
            _rb.linearVelocity = Vector2.zero;

            if (animator != null) animator.SetBool("Dead", true);
            if (rockZone != null) rockZone.StopAll();
        }

        // Optional: called by animation event
        public void AnimEvent_AttackFire()
        {
            if (!useProjectileAttack) return;
            if (projectilePrefab == null) return;
            FireProjectile();
        }

        // Simple direction flip helper (call from trigger or timer if you want)
        public void FlipPatrolDirection()
        {
            _moveDir = -_moveDir;
        }
    }

    // Minimal placeholder for compilation; remove if you already have EnemyHealth in project.
    public class EnemyHealth : MonoBehaviour
    {
        public bool IsDead => false;
    }
}