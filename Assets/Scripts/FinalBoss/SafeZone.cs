using UnityEngine;
using System.Collections.Generic;

public class SafeZone : MonoBehaviour
{
    [Header("Radius (world units)")]
    [SerializeField] private float startRadius = 3f;

    [Header("Visual")]
    [Tooltip("Transform chứa sprite vòng safe zone (child).")]
    [SerializeField] private Transform visualRoot;

    private CircleCollider2D col;
    private float radius;
    private float spriteLocalRadius; // bán kính sprite khi scale = 1

    private float lifeTime;  // thời gian tồn tại (sẽ set từ boss)
    private float elapsed;

    public bool IsActive { get; private set; } = true;

    // GLOBAL LIST các SafeZone đang tồn tại
    public static readonly List<SafeZone> ActiveZones = new List<SafeZone>();

    private void OnEnable()
    {
        if (!ActiveZones.Contains(this))
            ActiveZones.Add(this);
    }

    private void OnDisable()
    {
        ActiveZones.Remove(this);
    }

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();

        if (visualRoot != null)
        {
            var sr = visualRoot.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // bán kính sprite ở local khi scale = 1
                spriteLocalRadius = sr.sprite.bounds.extents.x;
            }
        }

        // nếu boss không gọi Init() thì cho 1 lifeTime mặc định
        lifeTime = Mathf.Max(0.01f, lifeTime);
        radius = startRadius;
        ApplyRadiusToColliderAndVisual();
    }

    /// <summary>
    /// Boss gọi ngay sau khi Instantiate: set thời gian tồn tại = thời gian của Void.
    /// </summary>
    public void Init(float lifeTimeSeconds)
    {
        lifeTime = Mathf.Max(0.01f, lifeTimeSeconds);
        elapsed = 0f;
        radius = startRadius;
        IsActive = true;
        ApplyRadiusToColliderAndVisual();
    }

    private void Update()
    {
        if (!IsActive) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifeTime);

        // thu nhỏ từ startRadius -> 0 trong suốt lifeTime
        radius = Mathf.Lerp(startRadius, 0f, t);
        ApplyRadiusToColliderAndVisual();

        if (elapsed >= lifeTime)
        {
            IsActive = false;
            Destroy(gameObject); // OnDisable sẽ remove khỏi ActiveZones
        }
    }

    private void ApplyRadiusToColliderAndVisual()
    {
        if (col != null)
            col.radius = radius;

        if (visualRoot != null && spriteLocalRadius > 0f)
        {
            float scaleFactor = radius / spriteLocalRadius;
            visualRoot.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
    }

    public float CurrentRadius => radius;

    /* ───────────────────────── GIZMOS ───────────────────────── */

    private void OnDrawGizmos()
    {
        float r = Application.isPlaying ? radius : startRadius;

        Gizmos.color = IsActive
            ? new Color(0f, 1f, 0.3f, 0.8f)
            : new Color(1f, 0.2f, 0.2f, 0.6f);

        DrawCircle(transform.position, r, 40);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), Mathf.Sin(0)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float rad = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
