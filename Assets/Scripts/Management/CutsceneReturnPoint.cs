using UnityEngine;

/// <summary>
/// Component để định nghĩa vị trí spawn của player khi quay lại từ cutscene
/// Đặt component này vào GameObject trong scene để chỉ định vị trí player sẽ xuất hiện
/// </summary>
public class CutsceneReturnPoint : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string returnPointID = ""; // ID để match với cutscene (optional)
    [SerializeField] private bool useThisPosition = true; // Dùng vị trí của GameObject này
    [SerializeField] private Vector2 customPosition = Vector2.zero; // Hoặc dùng vị trí custom

    [Header("Camera Confiner Settings")]
    [SerializeField] private PolygonCollider2D mapBoundary; // Bounding shape cho camera confiner tại vị trí này
    [SerializeField] private bool autoFindBoundary = true; // Tự động tìm boundary gần nhất nếu không gán

    /// <summary>
    /// Lấy vị trí spawn
    /// </summary>
    public Vector2 GetSpawnPosition()
    {
        if (useThisPosition)
        {
            return transform.position;
        }
        else
        {
            return customPosition;
        }
    }

    /// <summary>
    /// Set vị trí spawn
    /// </summary>
    public void SetSpawnPosition(Vector2 position)
    {
        customPosition = position;
        useThisPosition = false;
    }

    /// <summary>
    /// Set vị trí spawn từ Transform
    /// </summary>
    public void SetSpawnPosition(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position;
            useThisPosition = true;
        }
    }

    /// <summary>
    /// Get return point ID
    /// </summary>
    public string GetReturnPointID()
    {
        return returnPointID;
    }

    /// <summary>
    /// Get map boundary cho camera confiner
    /// </summary>
    public PolygonCollider2D GetMapBoundary()
    {
        if (mapBoundary != null)
        {
            return mapBoundary;
        }

        if (autoFindBoundary)
        {
            // Tìm boundary gần nhất
            PolygonCollider2D[] allBoundaries = FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None);
            PolygonCollider2D closest = null;
            float closestDistance = float.MaxValue;
            Vector2 spawnPos = GetSpawnPosition();

            foreach (var boundary in allBoundaries)
            {
                // Kiểm tra xem boundary có chứa vị trí spawn không
                if (boundary.bounds.Contains(spawnPos))
                {
                    return boundary;
                }

                // Hoặc tìm boundary gần nhất
                float distance = Vector2.Distance(boundary.bounds.center, spawnPos);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = boundary;
                }
            }

            return closest;
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        // Vẽ gizmo để dễ thấy vị trí trong Scene view
        Gizmos.color = Color.green;
        Vector2 pos = GetSpawnPosition();
        Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), 0.5f);
        Gizmos.DrawLine(new Vector3(pos.x - 0.5f, pos.y, 0), new Vector3(pos.x + 0.5f, pos.y, 0));
        Gizmos.DrawLine(new Vector3(pos.x, pos.y - 0.5f, 0), new Vector3(pos.x, pos.y + 0.5f, 0));
    }
}

