using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MovementArea : MonoBehaviour
{
    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    // Player/enemy đang ở trong vùng?
    public bool IsInside(Vector2 point)
    {
        return _col != null && _col.OverlapPoint(point);
    }

    // Ép 1 điểm nằm trong vùng (dùng để clamp enemy)
    public Vector2 ClampPoint(Vector2 point)
    {
        if (_col is BoxCollider2D box)
        {
            Bounds b = box.bounds;
            float x = Mathf.Clamp(point.x, b.min.x, b.max.x);
            float y = Mathf.Clamp(point.y, b.min.y, b.max.y);
            return new Vector2(x, y);
        }

        if (_col is CircleCollider2D circle)
        {
            Vector2 center = circle.bounds.center;
            float radius = circle.radius * Mathf.Max(circle.transform.lossyScale.x, circle.transform.lossyScale.y);

            Vector2 dir = point - center;
            if (dir.sqrMagnitude > radius * radius)
                dir = dir.normalized * radius;

            return center + dir;
        }

        return point;
    }
}
