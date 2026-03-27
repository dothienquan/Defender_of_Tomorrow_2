using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script để chỉnh gold coin dễ dàng trong Editor
/// Gắn vào bất kỳ GameObject nào trong scene (hoặc tạo GameObject riêng)
/// </summary>
public class GoldCoinEditor : MonoBehaviour
{
    [Header("Gold Coin Control")]
    [Tooltip("Số lượng gold muốn set")]
    [SerializeField] private int goldAmount = 0;

    [Header("Quick Actions")]
    [Tooltip("Set gold ngay khi click button")]
    [SerializeField] private bool setGoldOnStart = false;

    private void Start()
    {
        if (setGoldOnStart && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetGold(goldAmount);
            Debug.Log($"[GoldCoinEditor] Set gold to {goldAmount} on Start.");
        }
    }

    /// <summary>
    /// Set gold coin (có thể gọi từ Inspector button hoặc code)
    /// </summary>
    public void SetGold()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetGold(goldAmount);
            Debug.Log($"[GoldCoinEditor] Set gold to {goldAmount}.");
        }
        else
        {
            Debug.LogError("[GoldCoinEditor] EconomyManager.Instance is null!");
        }
    }

    /// <summary>
    /// Set gold với số lượng tùy chỉnh
    /// </summary>
    public void SetGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetGold(amount);
            Debug.Log($"[GoldCoinEditor] Set gold to {amount}.");
        }
        else
        {
            Debug.LogError("[GoldCoinEditor] EconomyManager.Instance is null!");
        }
    }

    /// <summary>
    /// Thêm gold
    /// </summary>
    public void AddGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(amount);
            Debug.Log($"[GoldCoinEditor] Added {amount} gold. Total: {EconomyManager.Instance.CurrentGold}.");
        }
        else
        {
            Debug.LogError("[GoldCoinEditor] EconomyManager.Instance is null!");
        }
    }

    /// <summary>
    /// Trừ gold
    /// </summary>
    public void SpendGold(int amount)
    {
        if (EconomyManager.Instance != null)
        {
            bool success = EconomyManager.Instance.SpendGold(amount);
            if (success)
            {
                Debug.Log($"[GoldCoinEditor] Spent {amount} gold. Remaining: {EconomyManager.Instance.CurrentGold}.");
            }
            else
            {
                Debug.LogWarning($"[GoldCoinEditor] Cannot spend {amount} gold. Current: {EconomyManager.Instance.CurrentGold}.");
            }
        }
        else
        {
            Debug.LogError("[GoldCoinEditor] EconomyManager.Instance is null!");
        }
    }

    /// <summary>
    /// Hiển thị gold hiện tại
    /// </summary>
    public void ShowCurrentGold()
    {
        if (EconomyManager.Instance != null)
        {
            Debug.Log($"[GoldCoinEditor] Current gold: {EconomyManager.Instance.CurrentGold}");
        }
        else
        {
            Debug.LogError("[GoldCoinEditor] EconomyManager.Instance is null!");
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom Editor cho GoldCoinEditor để có buttons trong Inspector
/// </summary>
[CustomEditor(typeof(GoldCoinEditor))]
public class GoldCoinEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GoldCoinEditor goldEditor = (GoldCoinEditor)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Gold"))
        {
            goldEditor.SetGold();
        }
        if (GUILayout.Button("Add 100 Gold"))
        {
            goldEditor.AddGold(100);
        }
        if (GUILayout.Button("Add 500 Gold"))
        {
            goldEditor.AddGold(500);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spend 50 Gold"))
        {
            goldEditor.SpendGold(50);
        }
        if (GUILayout.Button("Spend 100 Gold"))
        {
            goldEditor.SpendGold(100);
        }
        if (GUILayout.Button("Show Current Gold"))
        {
            goldEditor.ShowCurrentGold();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Các buttons trên chỉ hoạt động trong Play Mode.", MessageType.Info);
    }
}
#endif
