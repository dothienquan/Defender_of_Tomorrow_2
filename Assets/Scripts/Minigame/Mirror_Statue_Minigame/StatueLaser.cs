using UnityEngine;

public class StatueLaser : MonoBehaviour
{
    public LaserEmitter emitter;
    public float rotationStep = 90f;
    public bool isFinalStatue = false;  // 👈 đánh dấu tượng cuối
    private bool isActive = false;
    [HideInInspector] public bool isHit = false;

    void Start()
    {
        if (emitter == null)
            emitter = GetComponentInChildren<LaserEmitter>();

        if (emitter != null)
            emitter.isActive = false;
    }

    public void ActivateLaser()
    {
        if (!isActive)
        {
            isActive = true;

            // Nếu KHÔNG phải tượng cuối thì mới bật laser tiếp
            if (!isFinalStatue && emitter != null)
                emitter.isActive = true;

            FindFirstObjectByType<PuzzleManager>().StatueActivated(this);
        }
    }

    public void RotateStatue()
    {
        if (!isFinalStatue)  
            transform.Rotate(Vector3.forward, -rotationStep);
    }

    public void DeactivateLaser()
    {
        isActive = false;
        if (emitter != null)
            emitter.isActive = false;
    }

    public bool IsActive() => isActive;
}
