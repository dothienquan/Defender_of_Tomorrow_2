using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script để debug và fix vấn đề item bị snap vào Canvas root
/// Gắn vào item trong inventory để kiểm tra và fix
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InventoryItemDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool autoFixOnStart = true;

    private RectTransform rectTransform;
    private Transform expectedParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (autoFixOnStart)
        {
            FixItemPosition();
        }
    }

    private void Update()
    {
        if (showDebugInfo)
        {
            DebugItemInfo();
        }
    }

    /// <summary>
    /// Fix item position nếu bị snap vào Canvas root
    /// </summary>
    [ContextMenu("Fix Item Position")]
    public void FixItemPosition()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // Kiểm tra parent
        Transform parent = rectTransform.parent;
        if (parent == null)
        {
            Debug.LogError($"[InventoryItemDebugger] {gameObject.name} has no parent!");
            return;
        }

        // Kiểm tra xem parent có phải là Slot không
        Slot slot = parent.GetComponent<Slot>();
        if (slot == null)
        {
            Debug.LogWarning($"[InventoryItemDebugger] {gameObject.name} parent '{parent.name}' is not a Slot!");
            
            // Tìm slot gần nhất
            Transform slotParent = FindNearestSlot();
            if (slotParent != null)
            {
                Debug.Log($"[InventoryItemDebugger] Moving {gameObject.name} to slot '{slotParent.name}'");
                rectTransform.SetParent(slotParent, false);
                parent = slotParent;
            }
        }

        // Fix RectTransform
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero;

        // Force update Canvas
        Canvas.ForceUpdateCanvases();

        Debug.Log($"[InventoryItemDebugger] Fixed position for {gameObject.name}");
    }

    /// <summary>
    /// Tìm slot gần nhất (nếu item bị mất parent)
    /// </summary>
    private Transform FindNearestSlot()
    {
        // Tìm InventoryController
        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController == null || inventoryController.inventoryPanel == null)
        {
            return null;
        }

        // Tìm slot trống đầu tiên
        foreach (Transform slotTransform in inventoryController.inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                return slotTransform;
            }
        }

        return null;
    }

    /// <summary>
    /// Debug thông tin item
    /// </summary>
    private void DebugItemInfo()
    {
        if (rectTransform == null) return;

        string parentName = rectTransform.parent != null ? rectTransform.parent.name : "NULL";
        string canvasName = "NULL";
        
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasName = canvas.name;
        }

        Debug.Log($"[InventoryItemDebugger] {gameObject.name} - Parent: {parentName}, Canvas: {canvasName}, " +
                  $"Position: {rectTransform.anchoredPosition}, Size: {rectTransform.sizeDelta}, " +
                  $"Anchors: ({rectTransform.anchorMin}, {rectTransform.anchorMax})");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom Inspector để dễ debug
    /// </summary>
    [CustomEditor(typeof(InventoryItemDebugger))]
    public class InventoryItemDebuggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            InventoryItemDebugger debugger = (InventoryItemDebugger)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Fix Item Position"))
            {
                debugger.FixItemPosition();
            }

            if (GUILayout.Button("Debug Item Info"))
            {
                debugger.DebugItemInfo();
            }
        }
    }
#endif
}

