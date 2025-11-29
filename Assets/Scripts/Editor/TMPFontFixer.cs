using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script để fix lỗi TextMeshPro Font Asset missing characters
/// Chạy script này từ menu: Tools > Fix TMP Font Assets
/// </summary>
public class TMPFontFixer : EditorWindow
{
    [MenuItem("Tools/Fix TMP Font Assets")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontFixer>("TMP Font Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("TextMeshPro Font Asset Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Lỗi KeyNotFoundException xảy ra khi Font Asset thiếu ký tự.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Reimport All TMP Font Assets", GUILayout.Height(30)))
        {
            ReimportAllTMPFonts();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Find and List All TMP Font Assets", GUILayout.Height(30)))
        {
            FindAllTMPFonts();
        }

        GUILayout.Space(20);
        GUILayout.Label("Hướng dẫn thủ công:", EditorStyles.boldLabel);
        GUILayout.Label("1. Mở Window > TextMeshPro > Font Asset Creator", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2. Chọn font asset bị lỗi", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3. Chọn Character Set: ASCII", EditorStyles.wordWrappedLabel);
        GUILayout.Label("4. Nhấn Generate Font Atlas", EditorStyles.wordWrappedLabel);
        GUILayout.Label("5. Save font asset", EditorStyles.wordWrappedLabel);
    }

    private void ReimportAllTMPFonts()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            count++;
        }

        Debug.Log($"Đã reimport {count} TMP Font Assets. Nếu vẫn lỗi, hãy regenerate font atlas thủ công.");
        EditorUtility.DisplayDialog("Hoàn thành", 
            $"Đã reimport {count} TMP Font Assets.\n\nNếu vẫn lỗi, hãy:\n1. Mở Window > TextMeshPro > Font Asset Creator\n2. Chọn font và Generate Font Atlas với Character Set: ASCII", 
            "OK");
    }

    private void FindAllTMPFonts()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        Debug.Log($"Tìm thấy {guids.Length} TMP Font Assets:");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                Debug.Log($"  - {font.name} tại {path}");
            }
        }

        EditorUtility.DisplayDialog("Hoàn thành", 
            $"Đã tìm thấy {guids.Length} TMP Font Assets.\nXem Console để xem danh sách chi tiết.", 
            "OK");
    }
}

