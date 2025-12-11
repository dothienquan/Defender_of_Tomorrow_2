using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAnim))]
public class GolemController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 2.5f;
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private float chaseDistance = 6f;
    [SerializeField] private float attackDistance = 4f;  // Khoảng cách để bắt đầu tấn công
    [SerializeField] private float losePlayerDistance = 10f;

    [Header("Patrol")]
    [SerializeField] private MovementArea patrolArea;
    [SerializeField] private float patrolWaitTime = 1f;
    [SerializeField] private float patrolPointReachedDistance = 0.3f;

    [Header("Dash Attack")]
    [SerializeField] private float attackWindupDuration = 0.5f;  // Thời gian đứng yên trước khi dash
    [SerializeField] private float dashSpeed = 8f;  // Tốc độ dash
    [SerializeField] private float dashDuration = 0.4f;  // Thời gian dash
    [SerializeField] private float attackAnimationDuration = 0.6f;
    [SerializeField] private float postAttackIdleDuration = 1f;  // Thời gian idle sau khi tấn công
    [SerializeField] private float attackCooldown = 2f;  // Cooldown giữa các lần tấn công

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpawnDelay = 0.3f;  // Delay để sync với animation
    [SerializeField] private Vector2 projectileSpawnOffset = Vector2.zero;

    private enum State
    {
        Patrolling,
        Chasing,
        Windup,  // Đứng yên chuẩn bị dash
        Dashing,  // Đang lao về phía player
        Attacking,  // Đang trong attack animation
        PostAttackIdle  // Idle sau khi tấn công
    }

    private State _currentState = State.Patrolling;
    private Rigidbody2D _rb;
    private EnemyAnim _enemyAnim;
    private EnemyHealth _enemyHealth;
    private Vector2 _patrolTarget;
    private float _patrolWaitTimer;
    private float _attackTimer;
    private float _windupTimer;
    private float _dashTimer;
    private float _attackAnimationTimer;
    private float _postAttackIdleTimer;
    private bool _isDead;
    private Vector2 _spawnPosition;
    private Coroutine _dashCoroutine;
    private Vector2 _dashDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _enemyAnim = GetComponent<EnemyAnim>();
        _enemyHealth = GetComponent<EnemyHealth>();

        if (player == null)
        {
            // Try to find via PlayerController singleton first
            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance.transform;
            }
            else
            {
                // Fallback to tag search
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
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
                attackAnimationDuration = 0.6f;
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

        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
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
        _windupTimer -= Time.deltaTime;
        _dashTimer -= Time.deltaTime;
        _attackAnimationTimer -= Time.deltaTime;
        _postAttackIdleTimer -= Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInArea = patrolArea == null || patrolArea.IsInside(player.position);

        // State machine
        switch (_currentState)
        {
            case State.Patrolling:
                HandlePatrolling(distanceToPlayer, playerInArea);
                break;

            case State.Chasing:
                HandleChasing(distanceToPlayer, playerInArea);
                break;

            case State.Windup:
                // Stop movement during windup
                _rb.linearVelocity = Vector2.zero;
                HandleWindup(distanceToPlayer, playerInArea);
                break;

            case State.Dashing:
                // Handled by coroutine - movement is set in coroutine
                break;

            case State.Attacking:
                // Stop movement during attack animation
                _rb.linearVelocity = Vector2.zero;
                HandleAttacking(distanceToPlayer, playerInArea);
                break;

            case State.PostAttackIdle:
                // Stop movement during post-attack idle
                _rb.linearVelocity = Vector2.zero;
                HandlePostAttackIdle(distanceToPlayer, playerInArea);
                break;
        }
    }

    private void FixedUpdate()
    {
        // Ensure velocity = 0 during windup, attack animation and post-attack idle (but not during dash)
        if (!_isDead && _currentState != State.Dashing && 
            (_windupTimer > 0f || _attackAnimationTimer > 0f || _postAttackIdleTimer > 0f))
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
        // Player ran away or left area
        if (!playerInArea || distanceToPlayer > losePlayerDistance)
        {
            _currentState = State.Patrolling;
            SetNewPatrolTarget();
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Check if close enough to attack (start windup)
        if (distanceToPlayer <= attackDistance && _attackTimer <= 0f)
        {
            _currentState = State.Windup;
            StartWindup();
            return;
        }

        // Chase player
        Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        // If too close but on cooldown, stand still and wait
        if (distanceToPlayer <= attackDistance && _attackTimer > 0f)
        {
            // Stand still while waiting for cooldown
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // Normal chase
            _rb.linearVelocity = dirToPlayer * chaseSpeed;
        }
    }

    private void HandleWindup(float distanceToPlayer, bool playerInArea)
    {
        // Player ran away during windup
        if (!playerInArea || distanceToPlayer > losePlayerDistance)
        {
            _currentState = State.Patrolling;
            SetNewPatrolTarget();
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Windup finished, start dash
        if (_windupTimer <= 0f)
        {
            _currentState = State.Dashing;
            StartDash();
        }
    }

    private void HandleAttacking(float distanceToPlayer, bool playerInArea)
    {
        // Attack animation finished
        if (_attackAnimationTimer <= 0f)
        {
            _currentState = State.PostAttackIdle;
            _postAttackIdleTimer = postAttackIdleDuration;
            return;
        }
    }

    private void HandlePostAttackIdle(float distanceToPlayer, bool playerInArea)
    {
        // Post-attack idle finished
        if (_postAttackIdleTimer <= 0f)
        {
            // Player still in range, start new attack cycle
            if (playerInArea && distanceToPlayer <= chaseDistance)
            {
                _currentState = State.Chasing;
            }
            else
            {
                // Player ran away, return to patrolling
                _currentState = State.Patrolling;
                SetNewPatrolTarget();
            }
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

    private void StartWindup()
    {
        if (player == null) return;

        // Stop movement
        _rb.linearVelocity = Vector2.zero;

        // Set LastX/LastY dựa trên hướng tới player (không phải velocity)
        // Đảo ngược X để fix animation trái/phải bị ngược
        // KHÔNG đảo ngược Y vì attack animation cần đúng hướng
        if (_enemyAnim != null)
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

        _windupTimer = attackWindupDuration;
    }

    private void StartDash()
    {
        if (player == null) return;

        // Calculate dash direction towards player
        _dashDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // Start dash coroutine
        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
        }
        _dashCoroutine = StartCoroutine(DashToPlayer());
    }

    private IEnumerator DashToPlayer()
    {
        if (player == null) yield break;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            _dashTimer = dashDuration - elapsed;

            // Move towards player at dash speed
            _rb.linearVelocity = _dashDirection * dashSpeed;

            yield return null;
        }

        // Dash finished, play attack animation
        _currentState = State.Attacking;
        PlayAttackAnimation();

        _dashCoroutine = null;
    }

    private void PlayAttackAnimation()
    {
        if (_attackTimer > 0f) return;

        // Stop movement and trigger attack
        _rb.linearVelocity = Vector2.zero;

        _attackTimer = attackCooldown;
        _attackAnimationTimer = attackAnimationDuration;

        // Use EnemyAnim to trigger attack animation
        if (_enemyAnim != null)
        {
            _enemyAnim.PlayAttack();
        }

        // Spawn 8 projectiles xung quanh với delay để sync với animation
        if (projectilePrefab != null)
        {
            Invoke(nameof(SpawnEightDirectionProjectiles), projectileSpawnDelay);
        }
    }

    private void SpawnEightDirectionProjectiles()
    {
        if (projectilePrefab == null) return;

        // 8 hướng: trên, dưới, trái, phải, và 4 hướng chéo
        Vector2[] directions = new Vector2[]
        {
            Vector2.up,                      // Trên
            new Vector2(1f, 1f).normalized,  // Trên phải (chéo)
            Vector2.right,                   // Phải
            new Vector2(1f, -1f).normalized, // Dưới phải (chéo)
            Vector2.down,                    // Dưới
            new Vector2(-1f, -1f).normalized, // Dưới trái (chéo)
            Vector2.left,                    // Trái
            new Vector2(-1f, 1f).normalized  // Trên trái (chéo)
        };

        // Vị trí spawn (vị trí golem + offset)
        Vector2 spawnPosition = (Vector2)transform.position + projectileSpawnOffset;

        // Spawn 8 projectile cùng lúc
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 direction = directions[i];
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

            // Launch projectile
            FrozenSlimeProjectile projScript = projectile.GetComponent<FrozenSlimeProjectile>();
            if (projScript != null)
            {
                projScript.Launch(direction);
            }
            else
            {
                // Fallback: nếu không có FrozenSlimeProjectile, thử dùng Rigidbody2D để set velocity
                Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
                if (projRb != null)
                {
                    // Giả sử projectile có moveSpeed trong component khác hoặc dùng giá trị mặc định
                    float moveSpeed = 5f; // Có thể lấy từ component khác nếu cần
                    projRb.linearVelocity = direction * moveSpeed;
                }
            }
        }
    }

    private void TryFindPlayer()
    {
        // Try to find via PlayerController singleton first
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
        else
        {
            // Fallback to tag search
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void OnEnemyDeath()
    {
        if (_isDead) return;

        _isDead = true;
        _rb.linearVelocity = Vector2.zero;

        // Cancel dash coroutine
        if (_dashCoroutine != null)
        {
            StopCoroutine(_dashCoroutine);
            _dashCoroutine = null;
        }

        // Cancel pending projectile spawn
        CancelInvoke(nameof(SpawnEightDirectionProjectiles));

        if (_enemyAnim != null)
        {
            _enemyAnim.Die();
        }
    }
}

