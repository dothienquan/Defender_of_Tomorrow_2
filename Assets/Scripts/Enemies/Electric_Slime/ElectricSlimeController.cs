using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class ElectricSlimeController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float runSpeed = 4f;          // Tốc độ khi chase player (run)
    [SerializeField] private float patrolSpeed = 1.5f;      // Tốc độ khi patrol
    [SerializeField] private float chaseDistance = 5f;
    [SerializeField] private float attackDistance = 1.2f;
    [SerializeField] private float losePlayerDistance = 8f;

    [Header("Patrol")]
    [SerializeField] private MovementArea patrolArea;
    [SerializeField] private float patrolWaitTime = 1f;
    [SerializeField] private float patrolPointReachedDistance = 0.3f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;  // Thời gian attack animation (tự động lấy từ animator)
    [SerializeField] private float postAttackStunDuration = 0.3f;  // Thời gian đứng yên sau khi attack
    
    [Header("Electric Shock Wave")]
    [SerializeField] private GameObject shockWavePrefab;  // Prefab của vòng điện
    [SerializeField] private float shockWaveSpawnDelay = 0.1f;  // Delay trước khi spawn wave (để sync với animation)
    [SerializeField] private Vector2 shockWaveOffset = Vector2.zero;  // Offset để điều chỉnh vị trí spawn (Y dương = lên trên, Y âm = xuống dưới)

    private enum State
    {
        Patrolling,
        Chasing,
        Attacking
    }

    private State _currentState = State.Patrolling;
    private Rigidbody2D _rb;
    private Animator _anim;
    private Vector2 _patrolTarget;
    private float _patrolWaitTimer;
    private float _attackTimer;
    private float _attackAnimationTimer;  // Timer cho attack animation
    private float _postAttackStunTimer;  // Timer cho post-attack stun
    private bool _isDead;
    private Vector2 _lastVelocity;
    private Vector2 _spawnPosition;

    // Animator parameter hashes (cached for performance)
    private static readonly int MoveXHash = Animator.StringToHash("moveX");
    private static readonly int MoveYHash = Animator.StringToHash("moveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

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

        // Tự động lấy thời gian attack animation từ animator
        if (_anim != null && attackAnimationDuration <= 0f)
        {
            RuntimeAnimatorController ac = _anim.runtimeAnimatorController;
            if (ac != null)
            {
                foreach (AnimationClip clip in ac.animationClips)
                {
                    if (clip.name.Contains("Attack") || clip.name.Contains("attack"))
                    {
                        attackAnimationDuration = clip.length;
                        break;
                    }
                }
            }
            // Fallback nếu không tìm thấy
            if (attackAnimationDuration <= 0f)
            {
                attackAnimationDuration = 0.5f;
            }
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            return;
        }

        if (player == null)
        {
            TryFindPlayer();
            if (player == null)
            {
                _rb.linearVelocity = Vector2.zero;
                UpdateAnimator(Vector2.zero, false);
                return;
            }
        }

        _attackTimer -= Time.deltaTime;
        _attackAnimationTimer -= Time.deltaTime;
        _postAttackStunTimer -= Time.deltaTime;

        // Nếu đang trong attack animation hoặc stun, đứng yên và không xử lý logic di chuyển
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            // Vẫn giữ state Attacking để không chuyển state
            if (_currentState != State.Attacking)
            {
                _currentState = State.Attacking;
            }
            return;  // Không xử lý logic di chuyển khi đang attack/stun
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInArea = patrolArea == null || patrolArea.IsInside(player.position);

        // Priority check: Nếu player trong attack distance, chuyển sang Attacking ngay (bất kể hướng nào)
        if (_attackAnimationTimer <= 0f && _postAttackStunTimer <= 0f)
        {
            if (playerInArea && distanceToPlayer <= attackDistance && _currentState != State.Attacking)
            {
                _currentState = State.Attacking;
            }
        }

        // State machine - chỉ chạy nếu không đang attack/stun
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

        _lastVelocity = _rb.linearVelocity;
    }

    private void FixedUpdate()
    {
        // Đảm bảo velocity = 0 trong suốt thời gian attack animation và stun
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
            UpdateAnimator(Vector2.zero, false);

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
            UpdateAnimator(moveDir, false);
        }
    }

    private void HandleChasing(float distanceToPlayer, bool playerInArea)
    {
        // Nếu đang trong attack animation, không được chase
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            return;
        }

        // Player ran away or left area
        if (!playerInArea || distanceToPlayer > losePlayerDistance)
        {
            _currentState = State.Patrolling;
            SetNewPatrolTarget();
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            return;
        }

        // Check if close enough to attack - ưu tiên attack ở mọi hướng
        if (distanceToPlayer <= attackDistance)
        {
            _currentState = State.Attacking;
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            TryAttack();
            return;
        }

        // Chase player - run với tốc độ cao (chỉ khi không trong attack range)
        Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dirToPlayer * runSpeed;
        UpdateAnimator(dirToPlayer, true);  // isRunning = true để trigger run animation
    }

    private void HandleAttacking(float distanceToPlayer, bool playerInArea)
    {
        // Nếu đang trong attack animation hoặc post-attack stun, giữ đứng yên và KHÔNG chuyển state
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            // Không check distance hay chuyển state khi đang attack/stun
            return;
        }

        // Player ran away
        if (!playerInArea || distanceToPlayer > losePlayerDistance)
        {
            _currentState = State.Patrolling;
            SetNewPatrolTarget();
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            return;
        }

        // Still in attack range - attack ở mọi hướng
        if (distanceToPlayer <= attackDistance)
        {
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimator(Vector2.zero, false);
            TryAttack();
        }
        else
        {
            // Player moved away but still in chase range
            // Chỉ chuyển sang chase nếu không còn trong attack/stun
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
        // Nếu đang trong attack animation hoặc stun, không attack lại
        if (_attackAnimationTimer > 0f || _postAttackStunTimer > 0f) return;

        // Đảm bảo đứng yên ngay khi attack được trigger
        _rb.linearVelocity = Vector2.zero;
        _currentState = State.Attacking;  // Chuyển sang Attacking state ngay lập tức
        
        _attackTimer = attackCooldown;
        _attackAnimationTimer = attackAnimationDuration;  // Bắt đầu attack animation timer
        _postAttackStunTimer = postAttackStunDuration;  // Bắt đầu post-attack stun
        _anim.SetTrigger(AttackHash);
        
        // Spawn electric shock wave
        SpawnShockWave();
    }

    private void SpawnShockWave()
    {
        if (shockWavePrefab == null) return;

        // Delay spawn để sync với animation
        if (shockWaveSpawnDelay > 0f)
        {
            Invoke(nameof(SpawnShockWaveDelayed), shockWaveSpawnDelay);
        }
        else
        {
            SpawnShockWaveDelayed();
        }
    }

    private void SpawnShockWaveDelayed()
    {
        if (shockWavePrefab == null) return;

        // Lấy vị trí hiện tại của slime + offset để điều chỉnh vị trí spawn
        Vector3 spawnPos = transform.position + (Vector3)shockWaveOffset;
        
        // Spawn wave tại vị trí slime với offset
        GameObject wave = Instantiate(shockWavePrefab, spawnPos, Quaternion.identity);
        
        // Đảm bảo position được set đúng ngay lập tức
        wave.transform.position = spawnPos;
        
        // Set slime transform NGAY LẬP TỨC để wave follow slime (với offset)
        var shockWave = wave.GetComponent<ElectricShockWave>();
        if (shockWave != null)
        {
            shockWave.SetSlimeTransform(transform, shockWaveOffset);
        }
        
        // Wave sẽ tự động play effect khi Start()
    }

    private void UpdateAnimator(Vector2 moveDirection, bool isRunning)
    {
        if (_anim == null) return;

        float speed = moveDirection.magnitude;
        bool isMoving = speed > 0.01f;

        _anim.SetFloat(MoveXHash, moveDirection.x);
        _anim.SetFloat(MoveYHash, moveDirection.y);
        _anim.SetBool(IsMovingHash, isMoving);
        _anim.SetBool(IsRunningHash, isRunning && isMoving);
    }

    private void TryFindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        _anim.SetBool(IsDeadHash, true);
    }

    public void TakeDamage()
    {
        if (_isDead) return;
        _anim.SetTrigger(Animator.StringToHash("Hurt"));
    }

    // Called by animation events or external systems
    public void OnAttackAnimationEnd()
    {
        // Can be used to reset attack state if needed
    }
}

