using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAnim))]
public class FrozenSlimeController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseDistance = 5f;
    [SerializeField] private float attackDistance = 1.2f;
    [SerializeField] private float losePlayerDistance = 8f;

    [Header("Patrol")]
    [SerializeField] private MovementArea patrolArea;
    [SerializeField] private float patrolWaitTime = 1f;
    [SerializeField] private float patrolPointReachedDistance = 0.3f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private float postAttackStunDuration = 0.3f;
    
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpawnDelay = 0.2f;  // Delay để sync với animation
    [SerializeField] private Vector2 projectileSpawnOffset = Vector2.zero;

    private enum State
    {
        Patrolling,
        Chasing,
        Attacking
    }

    private State _currentState = State.Patrolling;
    private Rigidbody2D _rb;
    private EnemyAnim _enemyAnim;
    private EnemyHealth _enemyHealth;
    private Vector2 _patrolTarget;
    private float _patrolWaitTimer;
    private float _attackTimer;
    private float _attackAnimationTimer;
    private float _postAttackStunTimer;
    private bool _isDead;
    private Vector2 _spawnPosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _enemyAnim = GetComponent<EnemyAnim>();
        _enemyHealth = GetComponent<EnemyHealth>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        _spawnPosition = transform.position;
    }

    private void Start()
    {
        SetNewPatrolTarget();

        // Auto-detect attack animation duration from animator
        if (attackAnimationDuration <= 0f)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
                {
                    if (clip.name.Contains("Attack") || clip.name.Contains("attack"))
                    {
                        attackAnimationDuration = clip.length;
                        break;
                    }
                }
            }
            if (attackAnimationDuration <= 0f)
            {
                attackAnimationDuration = 0.5f;
            }
        }

        // Subscribe to death event
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDeath += OnEnemyDeath;
        }
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDeath -= OnEnemyDeath;
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            TryFindPlayer();
            if (player == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        _attackTimer -= Time.deltaTime;
        _attackAnimationTimer -= Time.deltaTime;
        _postAttackStunTimer -= Time.deltaTime;

        // Stop movement during attack animation or stun
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            if (_currentState != State.Attacking)
            {
                _currentState = State.Attacking;
            }
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInArea = patrolArea == null || patrolArea.IsInside(player.position);

        // Priority: if player in attack range, switch to attacking
        if (_attackAnimationTimer <= 0f && _postAttackStunTimer <= 0f)
        {
            if (playerInArea && distanceToPlayer <= attackDistance && _currentState != State.Attacking)
            {
                _currentState = State.Attacking;
            }
        }

        // State machine
        if (_attackAnimationTimer <= 0f && _postAttackStunTimer <= 0f)
        {
            switch (_currentState)
            {
                case State.Patrolling:
                    HandlePatrolling(distanceToPlayer, playerInArea);
                    break;

                case State.Chasing:
                    HandleChasing(distanceToPlayer, playerInArea);
                    break;

                case State.Attacking:
                    HandleAttacking(distanceToPlayer, playerInArea);
                    break;
            }
        }
    }

    private void FixedUpdate()
    {
        // Ensure velocity = 0 during attack animation and stun
        if (!_isDead && (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f))
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        // Clamp position to patrol area if it exists
        if (patrolArea != null && _currentState == State.Patrolling)
        {
            Vector2 clamped = patrolArea.ClampPoint(transform.position);
            transform.position = clamped;
        }
    }

    private void HandlePatrolling(float distanceToPlayer, bool playerInArea)
    {
        // Check if player is detected
        if (playerInArea && distanceToPlayer <= chaseDistance)
        {
            _currentState = State.Chasing;
            return;
        }

        // Move towards patrol target
        Vector2 dirToPatrol = (_patrolTarget - (Vector2)transform.position);
        float distToPatrol = dirToPatrol.magnitude;

        if (distToPatrol <= patrolPointReachedDistance)
        {
            // Reached patrol point, wait
            _rb.linearVelocity = Vector2.zero;
            _patrolWaitTimer += Time.deltaTime;
            if (_patrolWaitTimer >= patrolWaitTime)
            {
                SetNewPatrolTarget();
                _patrolWaitTimer = 0f;
            }
        }
        else
        {
            // Move towards patrol target
            Vector2 moveDir = dirToPatrol.normalized;
            _rb.linearVelocity = moveDir * patrolSpeed;
        }
    }

    private void HandleChasing(float distanceToPlayer, bool playerInArea)
    {
        // Stop if in attack animation
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Player ran away or left area
        if (!playerInArea || distanceToPlayer > losePlayerDistance)
        {
            _currentState = State.Patrolling;
            SetNewPatrolTarget();
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Check if close enough to attack
        if (distanceToPlayer <= attackDistance)
        {
            _currentState = State.Attacking;
            _rb.linearVelocity = Vector2.zero;
            TryAttack();
            return;
        }

        // Chase player
        Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dirToPlayer * chaseSpeed;
    }

    private void HandleAttacking(float distanceToPlayer, bool playerInArea)
    {
        // Stop if in attack animation or stun
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Player ran away
        if (!playerInArea || distanceToPlayer > losePlayerDistance)
        {
            _currentState = State.Patrolling;
            SetNewPatrolTarget();
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Still in attack range
        if (distanceToPlayer <= attackDistance)
        {
            _rb.linearVelocity = Vector2.zero;
            TryAttack();
        }
        else
        {
            // Player moved away but still in chase range
            _currentState = State.Chasing;
        }
    }

    private void SetNewPatrolTarget()
    {
        if (patrolArea == null)
        {
            // No patrol area, patrol around spawn position
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(1f, 3f);
            _patrolTarget = _spawnPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }
        else
        {
            // Get random point within patrol area
            Collider2D col = patrolArea.GetComponent<Collider2D>();
            if (col != null)
            {
                Bounds bounds = col.bounds;
                _patrolTarget = new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y)
                );
            }
            else
            {
                _patrolTarget = _spawnPosition;
            }
        }
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f) return;
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f) return;

        // Stop movement and trigger attack
        _rb.linearVelocity = Vector2.zero;
        _currentState = State.Attacking;

        _attackTimer = attackCooldown;
        _attackAnimationTimer = attackAnimationDuration;
        _postAttackStunTimer = postAttackStunDuration;

        // Set LastX/LastY dựa trên hướng tới player (không phải velocity)
        // Đảo ngược X để fix animation trái/phải bị ngược
        // KHÔNG đảo ngược Y vì attack animation cần đúng hướng
        if (player != null && _enemyAnim != null)
        {
            Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            Animator anim = _enemyAnim.GetComponent<Animator>();
            if (anim != null)
            {
                // Đảo ngược X: player ở bên phải → LastX âm (AttackLeft), player ở bên trái → LastX dương (AttackRight)
                // Giữ nguyên Y: player ở trên → LastY dương (AttackUp), player ở dưới → LastY âm (AttackDown)
                anim.SetFloat("LastX", -dirToPlayer.x);
                anim.SetFloat("LastY", dirToPlayer.y);
            }
        }

        // Use EnemyAnim to trigger attack animation
        if (_enemyAnim != null)
        {
            _enemyAnim.PlayAttack();
        }

        // Spawn projectile với delay để sync với animation
        if (projectilePrefab != null && player != null)
        {
            Invoke(nameof(SpawnProjectile), projectileSpawnDelay);
        }
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || player == null) return;

        // Tính hướng về player
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        // Vị trí spawn (vị trí slime + offset)
        Vector2 spawnPosition = (Vector2)transform.position + projectileSpawnOffset;
        
        // Spawn projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        
        // Launch projectile
        FrozenSlimeProjectile projScript = projectile.GetComponent<FrozenSlimeProjectile>();
        if (projScript != null)
        {
            projScript.Launch(direction);
        }
    }

    private void TryFindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void OnEnemyDeath()
    {
        if (_isDead) return;

        _isDead = true;
        _rb.linearVelocity = Vector2.zero;

        // Cancel pending projectile spawn
        CancelInvoke(nameof(SpawnProjectile));

        if (_enemyAnim != null)
        {
            _enemyAnim.Die();
        }
    }
}

