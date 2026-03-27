using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Fixes IndexOutOfRangeException in TMP_MaterialManager.GetFallbackMaterial
/// This error occurs when font assets have mismatched material arrays and atlas counts
/// </summary>
public class TMPAtlasIndexFixer
{
    [MenuItem("Tools/Fix TMP Atlas Index Error")]
    public static void FixAtlasIndexError()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        int fixedCount = 0;
        int checkedCount = 0;
        List<string> issues = new List<string>();
        List<string> fixedIssues = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            
            if (font == null) continue;
            
            checkedCount++;
            bool wasFixed = false;

            // Use reflection to safely access TMP properties (varies by version)
            var fontType = typeof(TMP_FontAsset);
            
            // Get material references property
            var materialReferencesProp = fontType.GetProperty("materialReferences", 
                BindingFlags.Public | BindingFlags.Instance);
            
            // Get atlas-related properties
            var atlasTextureCountProp = fontType.GetProperty("atlasTextureCount", 
                BindingFlags.Public | BindingFlags.Instance);
            var atlasTexturesProp = fontType.GetProperty("atlasTextures", 
                BindingFlags.Public | BindingFlags.Instance);

            Material[] materialRefs = null;
            if (materialReferencesProp != null)
            {
                materialRefs = materialReferencesProp.GetValue(font) as Material[];
            }

            // Check if font has materials array
            if (materialRefs == null || materialRefs.Length == 0)
            {
                issues.Add($"{font.name}: Missing material references array");
                
                // Try to get the default material
                Material defaultMaterial = font.material;
                if (defaultMaterial != null && materialReferencesProp != null)
                {
                    // Create a material reference array with the default material
                    Material[] newMaterials = new Material[] { defaultMaterial };
                    materialReferencesProp.SetValue(font, newMaterials);
                    EditorUtility.SetDirty(font);
                    wasFixed = true;
                    fixedIssues.Add($"{font.name}: Created material references array");
                }
            }
            else
            {
                // Check if material references match atlas textures
                int atlasCount = 1; // Default to 1 atlas
                
                if (atlasTextureCountProp != null)
                {
                    object atlasCountObj = atlasTextureCountProp.GetValue(font);
                    if (atlasCountObj != null)
                    {
                        atlasCount = (int)atlasCountObj;
                    }
                }
                else if (atlasTexturesProp != null)
                {
                    // Try to get atlas count from atlasTextures array
                    Texture2D[] atlasTextures = atlasTexturesProp.GetValue(font) as Texture2D[];
                    if (atlasTextures != null && atlasTextures.Length > 0)
                    {
                        atlasCount = atlasTextures.Length;
                    }
                }

                int materialCount = materialRefs.Length;

                if (materialCount < atlasCount)
                {
                    issues.Add($"{font.name}: Material count ({materialCount}) < Atlas count ({atlasCount})");
                    
                    // Extend material array to match atlas count
                    Material[] newMaterials = new Material[atlasCount];
                    for (int i = 0; i < atlasCount; i++)
                    {
                        if (i < materialCount && materialRefs[i] != null)
                        {
                            newMaterials[i] = materialRefs[i];
                        }
                        else
                        {
                            // Use default material or first available material
                            Material fallbackMat = font.material;
                            if (fallbackMat == null && materialRefs.Length > 0)
                            {
                                fallbackMat = materialRefs[0];
                            }
                            newMaterials[i] = fallbackMat;
                        }
                    }
                    
                    if (materialReferencesProp != null)
                    {
                        materialReferencesProp.SetValue(font, newMaterials);
                        EditorUtility.SetDirty(font);
                        wasFixed = true;
                        fixedIssues.Add($"{font.name}: Extended material array from {materialCount} to {atlasCount}");
                    }
                }
            }

            // Check fallback fonts - these are often the source of the error
            if (font.fallbackFontAssetTable != null && font.fallbackFontAssetTable.Count > 0)
            {
                foreach (var fallback in font.fallbackFontAssetTable)
                {
                    if (fallback != null)
                    {
                        // Ensure fallback has proper material setup
                        var fallbackType = typeof(TMP_FontAsset);
                        var fallbackMaterialRefsProp = fallbackType.GetProperty("materialReferences", 
                            BindingFlags.Public | BindingFlags.Instance);
                        
                        if (fallbackMaterialRefsProp != null)
                        {
                            Material[] fallbackMaterials = fallbackMaterialRefsProp.GetValue(fallback) as Material[];
                            
                            if (fallbackMaterials == null || fallbackMaterials.Length == 0)
                            {
                                Material fallbackDefault = fallback.material;
                                if (fallbackDefault != null)
                                {
                                    fallbackMaterials = new Material[] { fallbackDefault };
                                    fallbackMaterialRefsProp.SetValue(fallback, fallbackMaterials);
                                    EditorUtility.SetDirty(fallback);
                                    wasFixed = true;
                                    fixedIssues.Add($"  → Fixed fallback font: {fallback.name}");
                                }
                            }
                            else
                            {
                                // Check if fallback also has atlas mismatch
                                var fallbackAtlasCountProp = fallbackType.GetProperty("atlasTextureCount", 
                                    BindingFlags.Public | BindingFlags.Instance);
                                
                                int fallbackAtlasCount = 1;
                                if (fallbackAtlasCountProp != null)
                                {
                                    object fallbackAtlasCountObj = fallbackAtlasCountProp.GetValue(fallback);
                                    if (fallbackAtlasCountObj != null)
                                    {
                                        fallbackAtlasCount = (int)fallbackAtlasCountObj;
                                    }
                                }
                                
                                if (fallbackMaterials.Length < fallbackAtlasCount)
                                {
                                    Material[] newFallbackMaterials = new Material[fallbackAtlasCount];
                                    for (int i = 0; i < fallbackAtlasCount; i++)
                                    {
                                        if (i < fallbackMaterials.Length && fallbackMaterials[i] != null)
                                        {
                                            newFallbackMaterials[i] = fallbackMaterials[i];
                                        }
                                        else
                                        {
                                            newFallbackMaterials[i] = fallback.material ?? fallbackMaterials[0];
                                        }
                                    }
                                    fallbackMaterialRefsProp.SetValue(fallback, newFallbackMaterials);
                                    EditorUtility.SetDirty(fallback);
                                    wasFixed = true;
                                    fixedIssues.Add($"  → Fixed fallback font {fallback.name}: Extended materials to match atlas count");
                                }
                            }
                        }
                    }
                }
            }

            if (wasFixed)
            {
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Log results
        Debug.Log("=== TMP Font Atlas Index Fix ===");
        Debug.Log($"Checked: {checkedCount} font assets");
        Debug.Log($"Fixed: {fixedCount} font assets");
        
        if (issues.Count > 0)
        {
            Debug.Log("Issues found:");
            foreach (string issue in issues)
            {
                Debug.Log($"  {issue}");
            }
        }
        
        if (fixedIssues.Count > 0)
        {
            Debug.Log("Fixed issues:");
            foreach (string fix in fixedIssues)
            {
                Debug.Log($"  ✓ {fix}");
            }
        }

        string message = $"Checked: {checkedCount} font assets\n" +
                        $"Fixed: {fixedCount} font assets";
        
        if (fixedIssues.Count > 0)
        {
            message += $"\n\nFixed {fixedIssues.Count} issue(s). See Console for details.";
        }
        else if (issues.Count > 0)
        {
            message += $"\n\nFound {issues.Count} issue(s) but couldn't auto-fix. See Console.";
        }
        else
        {
            message += "\n\nNo issues found!";
        }

        EditorUtility.DisplayDialog("Atlas Index Fix Complete", message, "OK");
    }
}

