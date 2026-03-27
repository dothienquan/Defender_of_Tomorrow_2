using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class AutoFitToSlot : MonoBehaviour
{
    public float padding = 4f;
    public bool preserveAspectForImage = true;

    RectTransform rt;
    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void OnTransformParentChanged() { Apply(); }

    void OnRectTransformDimensionsChange() { Apply(); }

    /// <summary>
    /// Public method để có thể gọi Apply() từ bên ngoài
    /// </summary>
    public void Apply()
    {
        if (rt == null || rt.parent == null) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);

        var img = GetComponent<Image>();
        if (img != null && preserveAspectForImage) img.preserveAspect = true;

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}
