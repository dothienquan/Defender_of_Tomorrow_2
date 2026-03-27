#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script để tạo font asset TextMeshPro với hỗ trợ tiếng Việt
/// </summary>
public class VietnameseFontGenerator : EditorWindow
{
    private Font sourceFont;
    private int fontSize = 72;
    private int padding = 9;
    private int packingMethod = 0;
    private int atlasWidth = 1024;
    private int atlasHeight = 1024;
    private string assetName = "LiberationSans Vietnamese";

    [MenuItem("Tools/TextMeshPro/Generate Vietnamese Font")]
    public static void ShowWindow()
    {
        GetWindow<VietnameseFontGenerator>("Vietnamese Font Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Vietnamese Font Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceFont = (Font)EditorGUILayout.ObjectField("Source Font", sourceFont, typeof(Font), false);
        
        if (sourceFont == null)
        {
            EditorGUILayout.HelpBox("Vui lòng chọn font nguồn (ví dụ: LiberationSans từ TextMesh Pro/Fonts)", MessageType.Warning);
        }

        EditorGUILayout.Space();
        fontSize = EditorGUILayout.IntField("Font Size", fontSize);
        padding = EditorGUILayout.IntField("Padding", padding);
        packingMethod = EditorGUILayout.Popup("Packing Method", packingMethod, new string[] { "Fast", "Optimal" });
        atlasWidth = EditorGUILayout.IntField("Atlas Width", atlasWidth);
        atlasHeight = EditorGUILayout.IntField("Atlas Height", atlasHeight);
        assetName = EditorGUILayout.TextField("Asset Name", assetName);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Font này sẽ bao gồm:\n" +
            "- Tất cả ký tự ASCII (32-126)\n" +
            "- Ký tự tiếng Việt (Unicode: 00C0-00FF, 0100-017F, 1EA0-1EFF)\n" +
            "- Số và ký tự đặc biệt",
            MessageType.Info);

        EditorGUILayout.Space();

        GUI.enabled = sourceFont != null;
        if (GUILayout.Button("Generate Font Asset", GUILayout.Height(30)))
        {
            GenerateFontAsset();
        }
        GUI.enabled = true;
    }

    private void GenerateFontAsset()
    {
        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("Error", "Vui lòng chọn font nguồn!", "OK");
            return;
        }

        // Tạo character set cho tiếng Việt
        string characterSet = GenerateVietnameseCharacterSet();

        // Copy character set vào clipboard
        EditorGUIUtility.systemCopyBuffer = characterSet;

        // Mở Font Asset Creator window
        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Font Asset Creator");

        EditorUtility.DisplayDialog("Hướng dẫn", 
            "Đã copy character set tiếng Việt vào clipboard!\n\n" +
            "Các bước tiếp theo:\n" +
            "1. Trong Font Asset Creator window vừa mở:\n" +
            "   - Chọn Source Font: " + sourceFont.name + "\n" +
            "   - Font Size: " + fontSize + "\n" +
            "   - Padding: " + padding + "\n" +
            "   - Atlas Resolution: " + atlasWidth + "x" + atlasHeight + "\n\n" +
            "2. Trong phần 'Character Set', chọn 'Custom Characters'\n" +
            "3. Paste character set đã copy (Ctrl+V)\n" +
            "4. Click 'Generate Font Atlas'\n" +
            "5. Click 'Save' hoặc 'Save as...' và đặt tên: " + assetName + " SDF\n\n" +
            "Character set đã được copy vào clipboard!",
            "OK");
    }

    private string GenerateVietnameseCharacterSet()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // ASCII characters (32-126)
        for (int i = 32; i <= 126; i++)
        {
            sb.Append((char)i);
        }

        // Vietnamese characters - Basic Latin Extended (00C0-00FF)
        // ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơ
        for (int i = 0x00C0; i <= 0x00FF; i++)
        {
            sb.Append((char)i);
        }

        // Vietnamese characters - Latin Extended-A (0100-017F)
        // Ăă, Ąą, Ćć, etc.
        for (int i = 0x0100; i <= 0x017F; i++)
        {
            sb.Append((char)i);
        }

        // Vietnamese characters - Latin Extended Additional (1E00-1EFF)
        // Ạạ, Ảả, Ấấ, Ầầ, Ẩẩ, Ẫẫ, Ậậ, Ắắ, Ằằ, Ẳẳ, Ẵẵ, Ặặ, etc.
        for (int i = 0x1E00; i <= 0x1EFF; i++)
        {
            sb.Append((char)i);
        }

        // Common Vietnamese combinations
        string[] commonVietnamese = {
            "À", "Á", "Â", "Ã", "È", "É", "Ê", "Ì", "Í", "Ò", "Ó", "Ô", "Õ", "Ù", "Ú", "Ỳ", "Ý",
            "à", "á", "â", "ã", "è", "é", "ê", "ì", "í", "ò", "ó", "ô", "õ", "ù", "ú", "ỳ", "ý",
            "Ă", "ă", "Đ", "đ", "Ĩ", "ĩ", "Ũ", "ũ", "Ơ", "ơ", "Ư", "ư",
            "Ạ", "ạ", "Ả", "ả", "Ấ", "ấ", "Ầ", "ầ", "Ẩ", "ẩ", "Ẫ", "ẫ", "Ậ", "ậ",
            "Ắ", "ắ", "Ằ", "ằ", "Ẳ", "ẳ", "Ẵ", "ẵ", "Ặ", "ặ",
            "Ẹ", "ẹ", "Ẻ", "ẻ", "Ẽ", "ẽ", "Ế", "ế", "Ề", "ề", "Ể", "ể", "Ễ", "ễ", "Ệ", "ệ",
            "Ỉ", "ỉ", "Ị", "ị",
            "Ọ", "ọ", "Ỏ", "ỏ", "Ố", "ố", "Ồ", "ồ", "Ổ", "ổ", "Ỗ", "ỗ", "Ộ", "ộ", "Ớ", "ớ", "Ờ", "ờ", "Ở", "ở", "Ỡ", "ỡ", "Ợ", "ợ",
            "Ụ", "ụ", "Ủ", "ủ", "Ứ", "ứ", "Ừ", "ừ", "Ử", "ử", "Ữ", "ữ", "Ự", "ự",
            "Ỳ", "ỳ", "Ỵ", "ỵ", "Ỷ", "ỷ", "Ỹ", "ỹ"
        };

        foreach (string ch in commonVietnamese)
        {
            if (!sb.ToString().Contains(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
#endif

