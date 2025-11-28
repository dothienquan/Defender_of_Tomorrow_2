using UnityEngine;

public class RaftController : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform mountPoint;   // vị trí player đứng trên thuyền
    private PlayerController player;
    private bool playerOnRaft = false;

    [Header("Raft Settings")]
    public float raftMoveSpeed = 3f;

    // WIND: gió tác dụng lên thuyền
    private Vector2 currentWind = Vector2.zero;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !playerOnRaft)
        {
            player = null;
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Nhấn E để lên/xuống thuyền
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!playerOnRaft) AttachPlayer();
            else DetachPlayer();
        }

        // Nếu player đang ở trên thuyền -> điều khiển thuyền
        if (playerOnRaft)
        {
            MoveRaft();
        }
    }

    private void AttachPlayer()
    {
        playerOnRaft = true;

        // Tắt điều khiển player
        player.enabled = false;

        // Đặt player lên mount point
        player.transform.position = mountPoint.position;

        // Cho player làm con của thuyền để di chuyển chung
        player.transform.SetParent(transform);

        Debug.Log("Player đã lên thuyền!");
    }

    private void DetachPlayer()
    {
        playerOnRaft = false;

        // Bỏ parent
        player.transform.SetParent(null);

        // Bật điều khiển player lại
        player.enabled = true;

        Debug.Log("Player đã rời thuyền!");
    }

    private void MoveRaft()
    {
        // Input WASD
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(x, y).normalized;

        // Vận tốc do input
        Vector2 inputVelocity = inputDir * raftMoveSpeed;

        // Tổng: input + gió
        Vector2 finalVelocity = inputVelocity + currentWind;

        // Translate theo world-space
        transform.Translate(finalVelocity * Time.deltaTime, Space.World);
    }

    // WIND: hàm cho vùng gió gọi vào
    public void SetWind(Vector2 wind)
    {
        currentWind = wind;
    }
}
