using UnityEngine;

public class MiniGameMover : MonoBehaviour
{
    [Header("Điểm đích sau khi mini-game hoàn thành")]
    public Transform targetPosition;

    [Header("Tốc độ di chuyển")]
    public float moveSpeed = 3f;

    private bool shouldMove = false;

    void Update()
    {
        if (shouldMove && targetPosition != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition.position,
                moveSpeed * Time.deltaTime
            );

            // Nếu đã đến nơi → ngưng di chuyển
            if (Vector3.Distance(transform.position, targetPosition.position) < 0.01f)
            {
                shouldMove = false;
            }
        }
    }

    // Gọi hàm này khi mini-game hoàn thành
    public void OnMiniGameComplete()
    {
        shouldMove = true;
    }
}
