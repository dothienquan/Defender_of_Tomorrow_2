using UnityEngine;

/// <summary>
/// Script để quản lý line renderer trong cutscene
/// Line renderer sẽ luôn hiển thị mà không cần activate như minigame
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class CutsceneLineRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("Điểm bắt đầu của line (nếu null sẽ dùng transform.position)")]
    [SerializeField] private Transform startPoint;
    
    [Tooltip("Điểm kết thúc của line")]
    [SerializeField] private Transform endPoint;
    
    [Tooltip("Hoặc dùng vị trí tĩnh thay vì Transform")]
    [SerializeField] private bool useStaticPositions = false;
    
    [Tooltip("Vị trí bắt đầu (nếu useStaticPositions = true)")]
    [SerializeField] private Vector3 staticStartPosition;
    
    [Tooltip("Vị trí kết thúc (nếu useStaticPositions = true)")]
    [SerializeField] private Vector3 staticEndPosition;
    
    [Tooltip("Tự động enable line renderer khi Start")]
    [SerializeField] private bool autoEnableOnStart = true;
    
    [Header("Update Settings")]
    [Tooltip("Cập nhật line mỗi frame (nếu startPoint/endPoint có thể di chuyển)")]
    [SerializeField] private bool updateEveryFrame = true;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        if (lineRenderer == null)
        {
            Debug.LogError($"[CutsceneLineRenderer] {gameObject.name} does not have LineRenderer component!");
            return;
        }
        
        // Setup line renderer
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }

    private void Start()
    {
        if (autoEnableOnStart)
        {
            lineRenderer.enabled = true;
        }
        
        UpdateLinePositions();
    }

    private void Update()
    {
        if (updateEveryFrame && lineRenderer.enabled)
        {
            UpdateLinePositions();
        }
    }

    /// <summary>
    /// Cập nhật vị trí của line
    /// </summary>
    public void UpdateLinePositions()
    {
        if (lineRenderer == null) return;

        Vector3 startPos, endPos;

        if (useStaticPositions)
        {
            startPos = staticStartPosition;
            endPos = staticEndPosition;
        }
        else
        {
            startPos = startPoint != null ? startPoint.position : transform.position;
            endPos = endPoint != null ? endPoint.position : transform.position;
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    /// <summary>
    /// Set điểm bắt đầu
    /// </summary>
    public void SetStartPoint(Transform point)
    {
        startPoint = point;
        useStaticPositions = false;
        UpdateLinePositions();
    }

    /// <summary>
    /// Set điểm kết thúc
    /// </summary>
    public void SetEndPoint(Transform point)
    {
        endPoint = point;
        useStaticPositions = false;
        UpdateLinePositions();
    }

    /// <summary>
    /// Set vị trí tĩnh
    /// </summary>
    public void SetStaticPositions(Vector3 start, Vector3 end)
    {
        staticStartPosition = start;
        staticEndPosition = end;
        useStaticPositions = true;
        UpdateLinePositions();
    }

    /// <summary>
    /// Enable/disable line renderer
    /// </summary>
    public void SetLineEnabled(bool enabled)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = enabled;
        }
    }

    /// <summary>
    /// Kiểm tra xem line có đang enable không
    /// </summary>
    public bool IsLineEnabled()
    {
        return lineRenderer != null && lineRenderer.enabled;
    }

    private void OnDrawGizmos()
    {
        // Vẽ gizmo để preview line trong editor
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null) return;

        Vector3 startPos, endPos;

        if (useStaticPositions)
        {
            startPos = staticStartPosition;
            endPos = staticEndPosition;
        }
        else
        {
            startPos = startPoint != null ? startPoint.position : transform.position;
            endPos = endPoint != null ? endPoint.position : transform.position;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(startPos, 0.1f);
        Gizmos.DrawWireSphere(endPos, 0.1f);
    }
}

