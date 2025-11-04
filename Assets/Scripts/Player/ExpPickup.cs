using UnityEngine;

/// <summary>
/// EXP Pickup (prefab): hút về phía Player khi ở gần, khi chạm Player -> AddXP(random 5..20) rồi tự hủy.
/// Không sửa vào Pickup.cs gốc để tránh xung đột.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ExpPickup : MonoBehaviour
{
    [Header("EXP")]
    [Tooltip("EXP rơi ra (min..max)")]
    public int minExp = 5;
    public int maxExp = 20;

    [Header("Hút về Player")]
    public float pickupDistance = 5f;
    public float acceleration = 0.2f;
    public float moveSpeed = 3f;

    [Header("Pop spawn")]
    public AnimationCurve spawnCurve;
    public float heightY = 1.5f;
    public float popDuration = 0.6f;

    private Vector3 _moveDir;
    private Rigidbody2D _rb;
    private bool _spawnDone = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Pop nhẹ khi spawn
        StartCoroutine(SpawnPopRoutine());
    }

    private void Update()
    {
        if (!_spawnDone) return;

        if (PlayerController.Instance == null) return;
        Vector3 playerPos = PlayerController.Instance.transform.position;

        if (Vector3.Distance(transform.position, playerPos) < pickupDistance)
        {
            _moveDir = (playerPos - transform.position).normalized;
            moveSpeed += acceleration;
        }
        else
        {
            _moveDir = Vector3.zero;
            moveSpeed = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!_spawnDone) return;
        _rb.linearVelocity = _moveDir * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            int exp = Random.Range(minExp, maxExp + 1);
            var lvl = other.GetComponentInChildren<PlayerLevelSystemLinear>();
            if (lvl == null) lvl = other.GetComponent<PlayerLevelSystemLinear>();
            if (lvl != null) lvl.AddXP(exp);

            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator SpawnPopRoutine()
    {
        Vector2 start = transform.position;
        float rx = transform.position.x + Random.Range(-1.5f, 1.5f);
        float ry = transform.position.y + Random.Range(-1f, 1f);
        Vector2 end = new Vector2(rx, ry);
        float t = 0f;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            float lin = Mathf.Clamp01(t / popDuration);
            float ht = (spawnCurve != null) ? spawnCurve.Evaluate(lin) : lin;
            float h = Mathf.Lerp(0f, heightY, ht);
            transform.position = Vector2.Lerp(start, end, lin) + new Vector2(0f, h);
            yield return null;
        }
        _spawnDone = true;
    }
}