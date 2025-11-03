using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FireballSkill : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab của quả cầu lửa. Prefab nên có Rigidbody2D + Collider2D (IsTrigger) + FireballProjectile.")]
    public GameObject fireballPrefab;

    [Tooltip("Điểm bắn ra cầu lửa (empty Transform ở trước nhân vật). Nếu để trống sẽ dùng vị trí của Player.")]
    public Transform firePoint;

    [Header("Cấu hình kỹ năng")]
    [Tooltip("Tốc độ bay của cầu lửa.")]
    public float fireballSpeed = 14f;

    [Tooltip("Sát thương gây ra.")]
    public int damage = 1;

    [Tooltip("Hồi chiêu bấm X (giây).")]
    public float cooldown = 0.25f;

    [Tooltip("Tên trigger animation khi thi triển kỹ năng.")]
    public string castTrigger = "CastFireball";

    [Tooltip("Thời gian bất động nhẹ khi thi triển (để hòa nhập animation). Đặt 0 nếu không cần.")]
    public float castLockDuration = 0.1f;

    [Header("Điều khiển hướng bắn")]
    [Tooltip("Bắn theo hướng trỏ chuột. Nếu false, bắn theo hướng di chuyển gần nhất (WASD).")]
    public bool aimAtMouse = true;

    [Tooltip("Hướng mặc định nếu không có input di chuyển. True = sang trái.")]
    public bool defaultFaceLeft = false;

    private Animator _anim;
    private float _nextCastTime;
    private Rigidbody2D _rb;
    private Vector2 _lastMoveDir = Vector2.right;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Ưu tiên Input System cũ để đơn giản theo yêu cầu: phím X kích hoạt
        if (Input.GetKeyDown(KeyCode.X))
        {
            TryCast();
        }

        // Ghi nhớ hướng di chuyển gần nhất (để bắn khi không aim chuột)
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (move.sqrMagnitude > 0.001f)
        {
            _lastMoveDir = move.normalized;
        }
        else
        {
            _lastMoveDir = defaultFaceLeft ? Vector2.left : Vector2.right;
        }
    }

    public void TryCast()
    {
        if (Time.time < _nextCastTime) return;
        if (Stamina.Instance == null) return;

        // Cần 2 stamina
        if (Stamina.Instance.CurrentStamina < 2) return;

        // Tiêu stamina 2 đơn vị (gọi 2 lần vì UseStamina() của bạn giảm 1)
        Stamina.Instance.UseStamina();
        Stamina.Instance.UseStamina();

        // Đặt hồi chiêu
        _nextCastTime = Time.time + cooldown;

        // Gọi animation
        if (_anim != null && !string.IsNullOrEmpty(castTrigger))
        {
            _anim.SetTrigger(castTrigger);
        }

        // Khóa rất ngắn để đồng bộ động tác (tùy chọn)
        if (castLockDuration > 0f && _rb != null)
        {
            StartCoroutine(CastLockCoroutine());
        }

        // Tính hướng bắn
        Vector2 dir = ComputeAimDirection();

        // Bắn
        SpawnFireball(dir);
    }

    private System.Collections.IEnumerator CastLockCoroutine()
    {
        var originalConstraints = _rb.constraints;
        _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        yield return new WaitForSeconds(castLockDuration);
        _rb.constraints = originalConstraints;
    }

    private Vector2 ComputeAimDirection()
    {
        if (aimAtMouse && Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorld - transform.position);
            dir.y = Mathf.Abs(dir.y) < 0.0001f ? 0f : dir.y; // tránh NaN
            dir.x = Mathf.Abs(dir.x) < 0.0001f ? (defaultFaceLeft ? -1f : 1f) : dir.x;
            return dir.normalized;
        }
        return _lastMoveDir.sqrMagnitude > 0.001f ? _lastMoveDir.normalized : (defaultFaceLeft ? Vector2.left : Vector2.right);
    }

    private void SpawnFireball(Vector2 dir)
    {
        if (fireballPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, new Vector3(dir.x, dir.y, 0f));

        GameObject go = Instantiate(fireballPrefab, spawnPos, rot);

        // Thiết lập vận tốc
        var prj = go.GetComponent<FireballProjectile>();
        if (prj != null)
        {
            prj.Launch(dir, fireballSpeed, damage, gameObject);
        }
        else
        {
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null) rb2d.linearVelocity = dir * fireballSpeed;
        }
    }
}