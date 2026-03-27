using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float chaseDistance = 6f;   // khoảng bắt đầu đuổi
    public float attackDistance = 1.5f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    public int[] attackPattern = new int[] { 1, 2, 3 };   // 1 -> 2 -> 3 lặp

    [Header("Area (chase + giới hạn)")]
    public MovementArea movementArea;  // kéo vùng vào đây, có thể để trống nếu không muốn giới hạn

    private int _attackStep = 0;
    private float _attackTimer;

    private Rigidbody2D _rb;
    private Animator _anim;
    private bool _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
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
            _rb.linearVelocity = Vector2.zero;
            SetIdleAnim();
            return;
        }

        _attackTimer -= Time.deltaTime;

        // Nếu có vùng thì chỉ chase khi PLAYER ở trong vùng đó
        bool playerInArea = movementArea == null || movementArea.IsInside(player.position);
        if (!playerInArea)
        {
            _rb.linearVelocity = Vector2.zero;
            SetIdleAnim();
            HandleFlip();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackDistance)
        {
            // Tấn công
            _rb.linearVelocity = Vector2.zero;
            SetMoveAnim(false);
            TryAttack();
        }
        else if (dist <= chaseDistance)
        {
            // Đuổi theo
            MoveTowards(player.position);
            SetMoveAnim(true);
        }
        else
        {
            // Trong vùng nhưng player quá xa -> idle
            _rb.linearVelocity = Vector2.zero;
            SetIdleAnim();
        }

        HandleFlip();
    }

    private void LateUpdate()
    {
        // Luôn giữ enemy bên trong vùng
        if (movementArea != null)
        {
            Vector2 clamped = movementArea.ClampPoint(transform.position);
            transform.position = clamped;
        }
    }

    private void MoveTowards(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }

    private void SetMoveAnim(bool isMoving)
    {
        _anim.SetBool("isFlying", isMoving);
        _anim.SetBool("idle", !isMoving);
    }

    private void SetIdleAnim()
    {
        _anim.SetBool("isFlying", false);
        _anim.SetBool("idle", true);
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f) return;
        if (attackPattern == null || attackPattern.Length == 0) return;

        _attackTimer = attackCooldown;

        int atk = attackPattern[_attackStep];
        _attackStep++;
        if (_attackStep >= attackPattern.Length)
            _attackStep = 0;

        PlayAttack(atk);
    }

    private void PlayAttack(int attackIndex)
    {
        _anim.SetBool("attack1", false);
        _anim.SetBool("attack2", false);
        _anim.SetBool("attack3", false);

        switch (attackIndex)
        {
            case 1: _anim.SetBool("attack1", true); break;
            case 2: _anim.SetBool("attack2", true); break;
            case 3: _anim.SetBool("attack3", true); break;
        }
    }

    private void HandleFlip()
    {
        if (player == null) return;

        var scale = transform.localScale;
        if (player.position.x > transform.position.x)
            scale.x = Mathf.Abs(scale.x);
        else
            scale.x = -Mathf.Abs(scale.x);

        transform.localScale = scale;
    }

    public void Die()
    {
        if (_isDead) return;

        _isDead = true;
        _rb.linearVelocity = Vector2.zero;

        _anim.SetBool("attack1", false);
        _anim.SetBool("attack2", false);
        _anim.SetBool("attack3", false);
        _anim.SetBool("isFlying", false);
        _anim.SetBool("idle", false);
        _anim.SetBool("Die", true);
    }
}
