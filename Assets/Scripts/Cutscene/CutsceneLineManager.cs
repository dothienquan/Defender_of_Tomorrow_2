using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager để quản lý tất cả line renderer trong cutscene
/// Tự động enable tất cả line renderer khi scene load
/// </summary>
public class CutsceneLineManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tự động tìm và enable tất cả CutsceneLineRenderer trong scene")]
    [SerializeField] private bool autoFindAndEnable = true;
    
    [Tooltip("Danh sách line renderer để quản lý (có thể để trống nếu autoFindAndEnable = true)")]
    [SerializeField] private List<CutsceneLineRenderer> lineRenderers = new List<CutsceneLineRenderer>();

    private void Start()
    {
        if (autoFindAndEnable)
        {
            // Tự động tìm tất cả CutsceneLineRenderer trong scene
            CutsceneLineRenderer[] foundLines = FindObjectsByType<CutsceneLineRenderer>(FindObjectsSortMode.None);
            lineRenderers.Clear();
            lineRenderers.AddRange(foundLines);
            
            Debug.Log($"[CutsceneLineManager] Found {lineRenderers.Count} line renderers in scene.");
        }

        // Enable tất cả line renderer
        EnableAllLines();
    }

    /// <summary>
    /// Enable tất cả line renderer
    /// </summary>
    public void EnableAllLines()
    {
        foreach (var line in lineRenderers)
        {
            if (line != null)
            {
                line.SetLineEnabled(true);
            }
        }
    }

    /// <summary>
    /// Disable tất cả line renderer
    /// </summary>
    public void DisableAllLines()
    {
        foreach (var line in lineRenderers)
        {
            if (line != null)
            {
                line.SetLineEnabled(false);
            }
        }
    }

    /// <summary>
    /// Thêm line renderer vào danh sách
    /// </summary>
    public void AddLineRenderer(CutsceneLineRenderer line)
    {
        if (line != null && !lineRenderers.Contains(line))
        {
            lineRenderers.Add(line);
        }
    }

    /// <summary>
    /// Xóa line renderer khỏi danh sách
    /// </summary>
    public void RemoveLineRenderer(CutsceneLineRenderer line)
    {
        if (lineRenderers.Contains(line))
        {
            lineRenderers.Remove(line);
        }
    }

    /// <summary>
    /// Lấy số lượng line renderer
    /// </summary>
    public int GetLineCount()
    {
        return lineRenderers.Count;
    }
}

