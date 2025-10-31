using Cinemachine;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D mapBoundry;
    [SerializeField] private Direction direction;
    [SerializeField] private Transform teleportTargetPosition;

    // LƯU Ý: với Cinemachine 2D mới -> CinemachineConfiner2D
    [SerializeField] private CinemachineConfiner2D confiner;

    private enum Direction { Up, Down, Left, Right, Teleport }

    private void Start()
    {
        if (confiner != null && mapBoundry != null)
        {
            confiner.m_BoundingShape2D = mapBoundry;
            confiner.InvalidateCache();
        }
    }

    private void Awake()
    {
        // Dự phòng nếu quên gán trong Inspector
        if (confiner == null)
            confiner = FindFirstObjectByType<CinemachineConfiner2D>(); // hoặc FindAnyObjectByType trên Unity cũ

        if (confiner == null)
            Debug.LogError("Không tìm thấy CinemachineConfiner2D trong scene.");

        if (mapBoundry == null)
            Debug.LogError("Chưa gán mapBoundry (PolygonCollider2D) trong Inspector.");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (confiner != null && mapBoundry != null)
        {
            confiner.m_BoundingShape2D = mapBoundry;
            confiner.InvalidateCache();
        }

        UpdatePlayerPosition(collision.gameObject);
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        if (direction == Direction.Teleport)
        {
            if (teleportTargetPosition == null)
            {
                Debug.LogError("Teleport mode nhưng chưa gán teleportTargetPosition.");
                return;
            }
            player.transform.position = teleportTargetPosition.position;
            return;
        }

        var pos = player.transform.position;
        switch (direction)
        {
            case Direction.Up: pos.y += 2f; break;
            case Direction.Down: pos.y -= 2f; break;
            case Direction.Left: pos.x -= 2f; break;
            case Direction.Right: pos.x += 2f; break;
        }
        player.transform.position = pos;
    }
}
