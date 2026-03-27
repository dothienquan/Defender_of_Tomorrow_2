using System.Collections;
using UnityEngine;

public class EnemyDash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 10f;       // tốc độ dash
    [SerializeField] private float dashInterval = 2f;     // dash mỗi 2s
    [SerializeField] private Transform dashTarget;        // <-- kéo thả GameObject vào đây
    private Animator myAnimator;
    private SpriteRenderer spriteRenderer;
    private EnemyAI enemyAI;   // <-- thêm dòng này
    readonly int ATTACK_HASH = Animator.StringToHash("Attack");

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();   // <-- thêm dòng này
        StartCoroutine(DashLoop());
    }

    IEnumerator DashLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(dashInterval);

            // CHỈ DASH KHI ĐANG TRONG ATTACK ZONE
            if (enemyAI != null && enemyAI.IsInAttackZone() && dashTarget != null)
            {
                StartCoroutine(DashToPoint(dashTarget.position));
            }
        }
    }
    public void Attack()
    {
        myAnimator.SetTrigger(ATTACK_HASH);

        if (transform.position.x - PlayerController.Instance.transform.position.x < 0)
        {
            spriteRenderer.flipX = false;
        }
        else
        {
            spriteRenderer.flipX = true;
        }

    }

    IEnumerator DashToPoint(Vector3 targetPos)
    {
        targetPos.z = transform.position.z; // giữ Z để không lệch (2D)

        while (transform.position != targetPos)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                dashSpeed * Time.deltaTime
            );

            yield return null;
        }
    }
}
