using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAnim))]
public class InfernoSlimeController : MonoBehaviour
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

    [Header("Jump Attack")]
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField] private float jumpArcHeight = 2f;
    [SerializeField] private float jumpLandDistance = 0.2f;  // Khoảng cách coi như đã đáp

    private enum State
    {
        Patrolling,
        Chasing,
        Jumping,
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
    private Coroutine _jumpCoroutine;

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

        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
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

        // Stop movement during jump, attack animation or stun
        if (_currentState == State.Jumping || _attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            if (_currentState != State.Jumping)
            {
                _rb.linearVelocity = Vector2.zero;
            }
            if (_currentState != State.Attacking && _currentState != State.Jumping)
            {
                _currentState = State.Attacking;
            }
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInArea = patrolArea == null || patrolArea.IsInside(player.position);

        // Priority: if player in attack range and not jumping, start jump attack
        if (_currentState != State.Jumping && playerInArea && distanceToPlayer <= attackDistance && _attackTimer <= 0f)
        {
            _currentState = State.Jumping;
            StartJumpAttack();
            return;
        }

        // State machine
        if (_currentState != State.Jumping)
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
        // Ensure velocity = 0 during attack animation and stun (but not during jump)
        if (!_isDead && _currentState != State.Jumping && (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f))
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

        // Check if close enough to attack (start jump)
        if (distanceToPlayer <= attackDistance && _attackTimer <= 0f)
        {
            _currentState = State.Jumping;
            StartJumpAttack();
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
        if (distanceToPlayer <= attackDistance && _attackTimer <= 0f)
        {
            _currentState = State.Jumping;
            StartJumpAttack();
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

    private void StartJumpAttack()
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

        // Start jump coroutine
        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
        }
        _jumpCoroutine = StartCoroutine(JumpToPlayer());
    }

    private IEnumerator JumpToPlayer()
    {
        if (player == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        targetPos.z = startPos.z; // Keep Z consistent

        float distance = Vector2.Distance(startPos, targetPos);
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float clampedT = Mathf.Clamp01(t);

            // Linear interpolation for X position
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, clampedT);

            // Parabolic arc for Y: y = startY + 4h * t * (1 - t)
            // This creates a smooth arc that starts and ends at ground level
            float arcOffset = 4f * jumpArcHeight * clampedT * (1f - clampedT);
            currentPos.y = Mathf.Lerp(startPos.y, targetPos.y, clampedT) + arcOffset;

            transform.position = currentPos;

            yield return null;
        }

        // Ensure we're at target position
        transform.position = targetPos;

        // Check if close enough to player to attack
        float finalDistance = Vector2.Distance(transform.position, targetPos);
        if (finalDistance <= jumpLandDistance)
        {
            // Landed, now play attack animation
            _currentState = State.Attacking;
            PlayAttackAnimation();
        }
        else
        {
            // Didn't land close enough, return to chasing
            _currentState = State.Chasing;
        }

        _jumpCoroutine = null;
    }

    private void PlayAttackAnimation()
    {
        if (_attackTimer > 0f) return;

        // Stop movement and trigger attack
        _rb.linearVelocity = Vector2.zero;

        _attackTimer = attackCooldown;
        _attackAnimationTimer = attackAnimationDuration;
        _postAttackStunTimer = postAttackStunDuration;

        // Use EnemyAnim to trigger attack animation
        if (_enemyAnim != null)
        {
            _enemyAnim.PlayAttack();
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

        // Cancel jump coroutine
        if (_jumpCoroutine != null)
        {
            StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = null;
        }

        if (_enemyAnim != null)
        {
            _enemyAnim.Die();
        }
    }
}

