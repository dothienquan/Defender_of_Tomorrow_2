using System.Collections;
using UnityEngine;
using Cinemachine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class CutsceneCameraPan : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;
    
    [Header("Camera Pan Settings")]
    [Tooltip("Vị trí camera sẽ di chuyển đến (world position). Nếu null sẽ dùng transform.position")]
    [SerializeField] private Transform targetPosition;
    
    [Tooltip("Thời gian camera di chuyển đến vị trí target (giây)")]
    [SerializeField] private float panToTargetDuration = 2f;
    
    [Tooltip("Thời gian camera dừng lại ở vị trí target (giây)")]
    [SerializeField] private float holdDuration = 1.5f;
    
    [Tooltip("Thời gian camera di chuyển về lại player (giây)")]
    [SerializeField] private float panBackDuration = 2f;
    
    [Header("Easing")]
    [SerializeField] private Ease panEase = Ease.InOutQuad;
    
    private bool hasTriggered = false;
    private Collider2D triggerCollider;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineBrain cinemachineBrain;
    private Transform originalFollowTarget;
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    
    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        
        // Tìm Cinemachine Virtual Camera
        virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("[CutsceneCameraPan] Không tìm thấy CinemachineVirtualCamera!");
        }
        
        // Tìm Cinemachine Brain
        cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
        if (cinemachineBrain == null)
        {
            Debug.LogError("[CutsceneCameraPan] Không tìm thấy CinemachineBrain!");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CutsceneCameraPan] Không tìm thấy Main Camera!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;
        if (virtualCamera == null || mainCamera == null) return;
        
        hasTriggered = true;
        StartCoroutine(CutsceneRoutine());
    }
    
    private IEnumerator CutsceneRoutine()
    {
        // Lưu trạng thái ban đầu
        originalFollowTarget = virtualCamera.Follow;
        originalCameraPosition = mainCamera.transform.position;
        
        // Tạm thời disable Cinemachine Brain để điều khiển camera trực tiếp
        bool brainWasEnabled = false;
        if (cinemachineBrain != null)
        {
            brainWasEnabled = cinemachineBrain.enabled;
            cinemachineBrain.enabled = false;
        }
        
        // Đợi một frame để đảm bảo Cinemachine đã dừng
        yield return null;
        
        // Xác định vị trí target
        Vector3 targetPos = targetPosition != null ? targetPosition.position : transform.position;
        // Giữ nguyên Z của camera
        targetPos.z = originalCameraPosition.z;
        
        // Di chuyển camera đến vị trí target
        Sequence panSequence = DOTween.Sequence();
        
        panSequence.Append(mainCamera.transform.DOMove(targetPos, panToTargetDuration)
            .SetEase(panEase));
        
        // Giữ camera ở vị trí target
        panSequence.AppendInterval(holdDuration);
        
        // Di chuyển camera về lại player
        Vector3 playerPos = originalFollowTarget != null ? originalFollowTarget.position : originalCameraPosition;
        playerPos.z = originalCameraPosition.z;
        
        panSequence.Append(mainCamera.transform.DOMove(playerPos, panBackDuration)
            .SetEase(panEase));
        
        // Đợi sequence hoàn thành
        yield return panSequence.WaitForCompletion();
        
        // Khôi phục Cinemachine Brain
        if (cinemachineBrain != null && brainWasEnabled)
        {
            cinemachineBrain.enabled = true;
        }
        
        // Đảm bảo camera follow được khôi phục
        if (originalFollowTarget != null)
        {
            virtualCamera.Follow = originalFollowTarget;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Vẽ gizmo để dễ visualize vị trí target trong editor
        Vector3 targetPos = targetPosition != null ? targetPosition.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, 0.5f);
        Gizmos.DrawLine(transform.position, targetPos);
    }
}

