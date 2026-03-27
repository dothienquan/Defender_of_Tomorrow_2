using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyPathfinding))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class GoblinAI : MonoBehaviour, IEnemy
{
    [Header("Animator parameter names")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTriggerParam = "Attack";
    [SerializeField] private string dieTriggerParam = "Die";

    [Header("Timing")]
    [Tooltip("Thời gian animation tấn công (xấp xỉ length clip Attack).")]
    [SerializeField] private float attackAnimDuration = 0.6f;
    [Tooltip("Thời gian cho anim chết chạy trước khi Destroy.")]
    [SerializeField] private float deathDelay = 1.0f;

    private Animator anim;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private EnemyAI enemyAI;
    private EnemyPathfinding pathfinding;
    private SpriteRenderer spriteRenderer;

    private Vector2 lastMoveDir = Vector2.down; // mặc định nhìn xuống
    private Vector2 lastPosition;

    private bool isDead = false;
    private bool isAttacking = false;
    private float attackAnimTimer = 0f;

    // == VÙNG CHASE RIÊNG ==
    private bool isInChaseZone = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyAI = GetComponent<EnemyAI>();
        pathfinding = GetComponent<EnemyPathfinding>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        enemyHealth.OnDeath += HandleDeath;
    }

    private void Start()
    {
        // Để GoblinAI tự Destroy sau khi chơi anim chết
        enemyHealth.SetDestroyOnDeath(false);
        lastPosition = rb.position;
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // ===== đếm thời gian attack =====
        if (isAttacking)
        {
            attackAnimTimer -= Time.deltaTime;
            if (attackAnimTimer <= 0f)
            {
                isAttacking = false;
            }
        }

        // ===== pseudo velocity từ MovePosition =====
        Vector2 currentPos = rb.position;
        Vector2 pseudoVelocity = (currentPos - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = currentPos;

        float speedSqr = pseudoVelocity.sqrMagnitude;
        anim.SetFloat(speedParam, speedSqr);

        bool inAttackZone = enemyAI != null && enemyAI.IsInAttackZone();

        if (!isAttacking)
        {
            // 1) Player trong AttackZone (vùng đánh)
            if (inAttackZone)
            {
                // Để EnemyAI lo phần tấn công/cooldown.
                // GoblinAI chỉ lo quay mặt về player.
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    Vector2 dirToPlayer = (playerObj.transform.position - transform.position);
                    if (dirToPlayer.sqrMagnitude > 0.0001f)
                    {
                        lastMoveDir = GetCardinalDirection(dirToPlayer.normalized);
                    }
                }
                else if (speedSqr > 0.0001f)
                {
                    lastMoveDir = GetCardinalDirection(pseudoVelocity.normalized);
                }
            }
            // 2) Player chỉ ở trong ChaseZone (đuổi theo, chưa tới tầm đánh)
            else if (isInChaseZone)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    Vector2 dirToPlayer = (playerObj.transform.position - transform.position);
                    if (dirToPlayer.sqrMagnitude > 0.0001f)
                    {
                        Vector2 chaseDir = GetCardinalDirection(dirToPlayer.normalized);
                        lastMoveDir = chaseDir;
                        // Đuổi theo player
                        pathfinding.MoveTo(chaseDir);
                    }
                }
            }
            // 3) Không trong chase cũng không trong attack → roam bình thường
            else
            {
                if (speedSqr > 0.0001f)
                {
                    lastMoveDir = GetCardinalDirection(pseudoVelocity.normalized);
                }
            }

            // Cập nhật hướng cho Animator (Idle / Walk / chuẩn bị Attack)
            anim.SetFloat(moveXParam, lastMoveDir.x);
            anim.SetFloat(moveYParam, lastMoveDir.y);
        }

        // Tắt flipX tự động để không phá animation 4 hướng
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
    }

    // EnemyAI gọi hàm này qua IEnemy khi tới lượt tấn công
    public void Attack()
    {
        if (isDead) return;

        isAttacking = true;
        attackAnimTimer = attackAnimDuration;

        // Chốt hướng về phía player tại thời điểm bắt đầu attack
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Vector2 dirToPlayer = (playerObj.transform.position - transform.position);
            if (dirToPlayer.sqrMagnitude > 0.0001f)
            {
                lastMoveDir = GetCardinalDirection(dirToPlayer.normalized);
            }
        }

        anim.SetFloat(moveXParam, lastMoveDir.x);
        anim.SetFloat(moveYParam, lastMoveDir.y);
        anim.SetTrigger(attackTriggerParam);
        // Gây damage thật: dùng Animation Event gọi DoAttackHit()
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        pathfinding.StopMoving();
        rb.linearVelocity = Vector2.zero;
        enemyAI.enabled = false;

        anim.SetFloat(speedParam, 0f);
        anim.SetTrigger(dieTriggerParam);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    // Gọi bằng Animation Event trong clip Attack
    public void DoAttackHit()
    {
        // Ví dụ:
        // GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        // if (playerObj != null)
        // {
        //     playerObj.GetComponent<PlayerHealth>()?.TakeDamage(1);
        // }
    }

    /// <summary>
    /// Snap vector bất kỳ về 4 hướng: Up, Down, Left, Right
    /// </summary>
    private Vector2 GetCardinalDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return new Vector2(Mathf.Sign(dir.x), 0f); // Left / Right
        }
        else
        {
            return new Vector2(0f, Mathf.Sign(dir.y)); // Up / Down
        }
    }

    // ===== API cho GoblinChaseZone gọi =====
    public void SetInChaseZone(bool value)
    {
        isInChaseZone = value;
    }

    public bool IsInChaseZone()
    {
        return isInChaseZone;
    }
}
