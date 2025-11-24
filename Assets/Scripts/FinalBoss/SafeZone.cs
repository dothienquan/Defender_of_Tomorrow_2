using UnityEngine;

public class SafeZone : MonoBehaviour
{
    [SerializeField] private float startRadius = 3f;
    [SerializeField] private float shrinkRate = 0.4f; // giảm kích thước mỗi giây

    private CircleCollider2D col;
    private float radius;

    public GameObject glow;

    public bool IsActive { get; private set; } = true;

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();
        radius = startRadius;
        col.radius = startRadius;
        transform.localScale = Vector3.one * startRadius;
    }

    private void Update()
    {
        if (!IsActive) return;

        radius -= shrinkRate * Time.deltaTime;
        col.radius = radius;
        transform.localScale = Vector3.one * radius;

        if (radius <= 0.3f)
        {
            IsActive = false;
            Destroy(gameObject);
        }

        if (glow != null)
            glow.transform.localScale = transform.localScale * 1.1f;
    }


    public float CurrentRadius => radius;
}
