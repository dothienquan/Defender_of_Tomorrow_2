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

    private enum State { Roaming, Attacking }
    private State state;

    private Vector2 roamPosition;
    private float timeRoaming = 0f;

    private EnemyPathfinding enemyPathfinding;

    // 👇 THÊM
    private EnemyAnimator enemyAnimator;

    private void Awake()
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        enemyAnimator = GetComponentInChildren<EnemyAnimator>();   // <-- thêm animator
        state = State.Roaming;
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

        // đổi hướng roam định kỳ
        if (timeRoaming > roamChangeDirFloat)
        {
            roamPosition = GetRoamingPosition();
        }
    }

    private void Attacking()
    {
        if (!inAttackZone)
        {
            state = State.Roaming;
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
        state = value ? State.Attacking : State.Roaming;
    }
    public bool IsInAttackZone() => inAttackZone;
}
