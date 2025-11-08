using System.Collections;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Roam")]
    [SerializeField] private float roamChangeDirFloat = 2f;

    [Header("Attack (qua AttackZone)")]
    [SerializeField] private MonoBehaviour enemyType;   // IEnemy (ví dụ Grape)
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private bool stopMovingWhileAttacking = false;

    private bool canAttack = true;
    private bool inAttackZone = false;                  // <-- AttackZone sẽ bật/tắt cờ này

    private enum State { Roaming, Attacking }
    private State state;

    private Vector2 roamPosition;
    private float timeRoaming = 0f;

    private EnemyPathfinding enemyPathfinding;

    private void Awake()
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        state = State.Roaming;
    }

    private void Start()
    {
        roamPosition = GetRoamingPosition();
    }

    private void Update()
    {
        MovementStateControl();
    }

    private void MovementStateControl()
    {
        switch (state)
        {
            default:
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

        // di chuyển theo hướng roam hiện tại
        enemyPathfinding.MoveTo(roamPosition);

        // chuyển sang Attacking khi Player vào vùng (được AttackZone bật cờ)
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
        // nếu Player rời vùng, quay lại Roaming
        if (!inAttackZone)
        {
            state = State.Roaming;
            return;
        }

        // tấn công theo cooldown
        if (canAttack)
        {
            canAttack = false;

            // gọi skill/animation tấn công của enemy (IEnemy)
            (enemyType as IEnemy)?.Attack();

            if (stopMovingWhileAttacking)
            {
                enemyPathfinding.StopMoving();
            }
            else
            {
                // có thể chọn đuổi theo Player khi đang ở trạng thái tấn công
                // enemyPathfinding.MoveTo((PlayerController.Instance.transform.position - transform.position).normalized);
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

    // ==== API cho AttackZone gọi ====
    public void SetInAttackZone(bool value)
    {
        inAttackZone = value;
        if (inAttackZone) state = State.Attacking; else state = State.Roaming;
    }
}
