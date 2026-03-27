using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Roam")]
    [SerializeField] private float roamChangeDirFloat = 2f;

    [Header("Attack (qua AttackZone)")]
    [SerializeField] private MonoBehaviour enemyType;   // IEnemy
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private bool stopMovingWhileAttacking = false;

    private bool canAttack = true;
    public bool inAttackZone = false;
    public bool inChaseZone = false;

    private enum State { Roaming, Chasing, Attacking }
    private State state;

    private Vector2 roamPosition;
    private float timeRoaming = 0f;
    private Transform playerTarget;

    private EnemyPathfinding enemyPathfinding;

    // 👇 THÊM
    private EnemyAnimator enemyAnimator;

    private void Awake()
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        enemyAnimator = GetComponentInChildren<EnemyAnimator>();   // <-- thêm animator
        state = State.Roaming;
        
        // Cache player reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void Start()
    {
        roamPosition = GetRoamingPosition();
    }

    private void Update()
    {
        MovementStateControl();
        UpdateAnimationDirection();   // 👈 luôn cập nhật hướng di chuyển cho animator
    }

    private void MovementStateControl()
    {
        switch (state)
        {
            case State.Roaming:
                Roaming();
                break;

            case State.Chasing:
                Chasing();
                break;

            case State.Attacking:
                Attacking();
                break;
        }
    }

    private void Roaming()
    {
        timeRoaming += Time.deltaTime;

        // di chuyển theo hướng roam
        enemyPathfinding.MoveTo(roamPosition);

        if (inAttackZone)
        {
            state = State.Attacking;
        }
        else if (inChaseZone)
        {
            state = State.Chasing;
            return;
        }

        // đổi hướng roam định kỳ
        if (timeRoaming > roamChangeDirFloat)
        {
            roamPosition = GetRoamingPosition();
        }
    }

    private void Chasing()
    {
        // Nếu vào attack zone, chuyển sang attack
        if (inAttackZone)
        {
            state = State.Attacking;
            return;
        }

        // Nếu ra khỏi chase zone, quay về roam
        if (!inChaseZone)
        {
            state = State.Roaming;
            roamPosition = GetRoamingPosition();
            return;
        }

        // Đuổi theo player
        if (playerTarget != null)
        {
            Vector2 dirToPlayer = (playerTarget.position - transform.position);
            if (dirToPlayer.sqrMagnitude > 0.0001f)
            {
                enemyPathfinding.MoveTo(dirToPlayer.normalized);
            }
        }
        else
        {
            // Nếu không tìm thấy player, quay về roam
            state = State.Roaming;
        }
    }

    private void Attacking()
    {
        if (!inAttackZone)
        {
            // Nếu vẫn trong chase zone thì tiếp tục chase, không thì roam
            state = inChaseZone ? State.Chasing : State.Roaming;
            return;
        }

        if (canAttack)
        {
            canAttack = false;

            // gọi skill/anim tấn công
            (enemyType as IEnemy)?.Attack();

            // 👇 GỌI ANIMATION ATTACK
            enemyAnimator?.PlayAttack();

            if (stopMovingWhileAttacking)
            {
                enemyPathfinding.StopMoving();
            }
            else
            {
                // vẫn di chuyển (nếu muốn)
                enemyPathfinding.MoveTo(roamPosition);
            }

            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private Vector2 GetRoamingPosition()
    {
        timeRoaming = 0f;
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }

    // ========= THÊM: Gửi hướng di chuyển -> animator =========
    private void UpdateAnimationDirection()
    {
        if (!enemyAnimator) return;

        Vector2 currentVelocity = enemyPathfinding.GetCurrentVelocity();
        // NOTE: bạn cần hàm này trong EnemyPathfinding:
        // public Vector2 GetCurrentVelocity() { return moveDir; }

        enemyAnimator.SetMoveDirection(currentVelocity);
    }

    // ==== AttackZone API ====
    public void SetInAttackZone(bool value)
    {
        inAttackZone = value;
        if (value)
        {
            state = State.Attacking;
        }
        else
        {
            // Nếu đang attack và ra khỏi attack zone, chuyển về chase hoặc roam
            if (state == State.Attacking)
            {
                state = inChaseZone ? State.Chasing : State.Roaming;
            }
        }
    }
    public bool IsInAttackZone() => inAttackZone;

    // ==== ChaseZone API ====
    public void SetInChaseZone(bool value)
    {
        inChaseZone = value;
        if (value && state == State.Roaming)
        {
            state = State.Chasing;
        }
        else if (!value && state == State.Chasing)
        {
            state = State.Roaming;
            roamPosition = GetRoamingPosition();
        }
    }
    public bool IsInChaseZone() => inChaseZone;
}
