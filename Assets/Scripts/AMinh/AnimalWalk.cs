using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class AnimalWalk: MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float wanderRadius = 4f;        // bán kính đi dạo
    public Vector2 centerOffset = Vector2.zero; // tâm dạo (offset so với vị trí spawn)
    public float arriveThreshold = 0.1f;

    [Header("Idle timing (seconds)")]
    public Vector2 idleWaitRange = new Vector2(0.8f, 2.0f); // khoảng nghỉ giữa 2 lần di chuyển

    [Header("Obstacle avoid")]
    public LayerMask obstacleMask;
    public float lookAhead = 0.6f;         // raycast trước mặt
    public float repickAfter = 1.5f;       // nếu kẹt > thời gian này thì chọn điểm mới

    [Header("Visuals")]
    public bool flipByVelocityX = true;    // lật sprite theo hướng đi (trái/phải)

    Rigidbody2D rb;
    Animator anim;

    Vector2 spawnPos;
    Vector2 targetPos;
    float stuckTimer = 0f;
    Vector2 lastPos;

    Coroutine routine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spawnPos = rb.position;
        lastPos = rb.position;
    }

    void OnEnable()
    {
        routine = StartCoroutine(WanderLoop());
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }

    IEnumerator WanderLoop()
    {
        while (true)
        {
            // chọn điểm mới trong vòng tròn
            targetPos = PickRandomPoint();

            // nếu đường thẳng đến điểm bị chặn nặng, chọn lại vài lần
            int attempts = 0;
            while (BlockedLine(rb.position, targetPos) && attempts < 6)
            {
                targetPos = PickRandomPoint();
                attempts++;
                yield return null;
            }

            // đi tới đó
            stuckTimer = 0f;
            while (Vector2.Distance(rb.position, targetPos) > arriveThreshold)
            {
                Vector2 dir = (targetPos - rb.position).normalized;

                // né chướng ngại đơn giản bằng raycast phía trước
                if (Physics2D.Raycast(rb.position, dir, lookAhead, obstacleMask))
                {
                    targetPos = PickRandomPoint();
                }

                Vector2 newPos = rb.position + dir * moveSpeed * Time.deltaTime;
                rb.MovePosition(newPos);

                // animator
                float speed = (newPos - lastPos).magnitude / Time.deltaTime;
                anim.SetFloat("Speed", speed);
                bool moving = speed > 0.02f;
                anim.SetBool("IsMoving", moving);

                // lật sprite theo hướng X (cho side-view hoặc top-down đơn giản)
                if (flipByVelocityX && Mathf.Abs(dir.x) > 0.02f)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Sign(dir.x) * Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }

                // phát hiện kẹt
                if (Vector2.Distance(rb.position, lastPos) < 0.005f) stuckTimer += Time.deltaTime;
                else stuckTimer = 0f;

                if (stuckTimer >= repickAfter)
                {
                    targetPos = PickRandomPoint();
                    stuckTimer = 0f;
                }

                lastPos = rb.position;
                yield return null;
            }

            // tới nơi → Idle 1 khoảng
            anim.SetBool("IsMoving", false);
            anim.SetFloat("Speed", 0f);
            yield return new WaitForSeconds(Random.Range(idleWaitRange.x, idleWaitRange.y));
        }
    }

    Vector2 PickRandomPoint()
    {
        // chọn ngẫu nhiên trong hình tròn, tâm = spawnPos + centerOffset
        Vector2 center = spawnPos + centerOffset;
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        return center + rnd;
    }

    bool BlockedLine(Vector2 from, Vector2 to)
    {
        Vector2 dir = (to - from);
        float dist = dir.magnitude;
        dir /= dist == 0 ? 1 : dist;
        return Physics2D.Raycast(from, dir, dist, obstacleMask);
    }

    // vẽ gizmo cho dễ chỉnh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 c = Application.isPlaying ? (Vector3)(spawnPos + centerOffset)
                                          : (Vector3)((Vector2)transform.position + centerOffset);
        Gizmos.DrawWireSphere(c, wanderRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere((Vector2)transform.position, 0.05f);
    }
}
