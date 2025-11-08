using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyStatusTint : MonoBehaviour
{
    [Header("Shader property (thường là _Color hoặc _TintColor)")]
    [SerializeField] string colorProp = "_Color";
    [SerializeField] Color slowTint = new Color(0.5f, 0.7f, 1f, 1f);
    [SerializeField] float fadeOutTime = 0.15f;

    SpriteRenderer sr;
    MaterialPropertyBlock mpb;
    Color baseColor;
    Coroutine tintCR;
    int colorID;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        colorID = Shader.PropertyToID(colorProp);

        // Lấy màu gốc hiện tại từ material (nếu shader không có prop, fallback sang sr.color)
        if (sr.sharedMaterial != null && sr.sharedMaterial.HasProperty(colorID))
            baseColor = sr.sharedMaterial.GetColor(colorID);
        else
            baseColor = sr.color;
    }

    public void ApplySlowTint(float duration)
    {
        if (tintCR != null) StopCoroutine(tintCR);
        tintCR = StartCoroutine(CoTint(duration));
    }

    IEnumerator CoTint(float duration)
    {
        // Set màu slow
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(colorID, slowTint);
        sr.SetPropertyBlock(mpb);

        // Giữ trong thời gian slow còn hiệu lực
        yield return new WaitForSeconds(duration);

        // Fade trả về màu gốc
        float t = 0f;
        Color from = slowTint;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            var c = Color.Lerp(from, baseColor, t / fadeOutTime);
            sr.GetPropertyBlock(mpb);
            mpb.SetColor(colorID, c);
            sr.SetPropertyBlock(mpb);
            yield return null;
        }

        // Chốt về màu gốc
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(colorID, baseColor);
        sr.SetPropertyBlock(mpb);
        tintCR = null;
    }

    void OnDisable() // quan trọng nếu dùng pooling
    {
        // Clear mọi tint khi enemy bị disable/return pool
        sr.GetPropertyBlock(mpb);
        mpb.SetColor(colorID, baseColor);
        sr.SetPropertyBlock(mpb);
        tintCR = null;
    }
}
