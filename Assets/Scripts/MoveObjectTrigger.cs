using UnityEngine;
using DG.Tweening; // Cần thư viện này

[RequireComponent(typeof(Collider2D))]
public class MoveObjectTrigger : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Kéo object bạn muốn di chuyển vào đây")]
    [SerializeField] private GameObject objectToMove;

    [Header("Movement Settings")]
    [Tooltip("Khoảng cách di chuyển (X, Y, Z). Ví dụ: (0, 2, 0) là đi lên 2 đơn vị.")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 2f, 0f);

    [Tooltip("Thời gian di chuyển (giây)")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Kiểu chuyển động (OutQuad = nhanh lúc đầu, chậm dần; Linear = đều)")]
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Trigger Options")]
    [Tooltip("Nếu tích, chỉ kích hoạt 1 lần duy nhất.")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Có cần active object trước khi di chuyển không? (Dùng nếu object đang bị ẩn)")]
    [SerializeField] private bool activateObjectFirst = false;

    [Tooltip("Tag của người chơi")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;
    private Vector3 initialPosition;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        // Lưu vị trí ban đầu (đề phòng cần reset hoặc tính toán)
        if (objectToMove != null)
        {
            initialPosition = objectToMove.transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            MoveTheObject();
        }
    }

    private void MoveTheObject()
    {
        if (objectToMove == null) return;

        hasTriggered = true;

        // 1. Nếu cần active object lên trước (ví dụ vật tàng hình hiện ra rồi bay đi)
        if (activateObjectFirst)
        {
            objectToMove.SetActive(true);
        }

        // 2. Tính vị trí đích
        // Lưu ý: Dùng transform.position (World Space) để chính xác nhất
        Vector3 targetPos = objectToMove.transform.position + moveOffset;

        // 3. Thực hiện di chuyển bằng DOTween
        // DOKill để ngắt các chuyển động cũ nếu có, tránh xung đột
        objectToMove.transform.DOKill();
        
        objectToMove.transform.DOMove(targetPos, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                Debug.Log($"[Trigger] Đã di chuyển {objectToMove.name} tới {targetPos}");
            });

        // 4. Nếu chỉ dùng 1 lần thì tắt trigger đi để đỡ check lại
        if (triggerOnce)
        {
            // Tắt collider của trigger này
            GetComponent<Collider2D>().enabled = false;
        }
    }
    
    // Hàm Reset để test trong game nếu cần
    public void ResetPosition()
    {
        if (objectToMove != null)
        {
            objectToMove.transform.DOKill();
            objectToMove.transform.position = initialPosition;
            hasTriggered = false;
            GetComponent<Collider2D>().enabled = true;
        }
    }
}