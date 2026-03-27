using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HolyBeamScrollPulseURP : MonoBehaviour
{
    public LineRenderer lr;
    [Header("Scrolling")]
    public float scrollSpeed = 0.5f;     // forward flow speed
    public string baseMapName = "_BaseMap";
    public string baseColorName = "_BaseColor";

    [Header("Pulse (emission)")]
    public bool enablePulse = true;
    public string emissionColorName = "_EmissionColor";
    public float pulseSpeed = 1.2f;      // how fast it breathes
    public float pulseMin = 0.6f;        // min multiplier
    public float pulseMax = 1.2f;        // max multiplier

    private Material mat;
    private float scroll;

    void Reset() { lr = GetComponent<LineRenderer>(); }

    void Awake()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        // Use an instance so we don’t edit the shared material
        mat = lr.material = new Material(lr.material);
        // Ensure emission keyword enabled (URP unlit uses _EmissionColor)
        if (mat.HasProperty(emissionColorName))
            mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        if (mat == null) return;

        // --- Forward scroll of base map
        scroll += Time.deltaTime * scrollSpeed;
        if (mat.HasProperty(baseMapName))
        {
            // Shift X offset; Y stays 0
            var st = mat.GetTextureOffset(baseMapName);
            st.x = scroll;
            mat.SetTextureOffset(baseMapName, st);
        }

        // --- Soft brightness pulse via emission
        if (enablePulse && mat.HasProperty(emissionColorName))
        {
            // Medium-strength breathing
            float t = (Mathf.Sin(Time.time * pulseSpeed * 2f) + 1f) * 0.5f; // 0..1
            float k = Mathf.Lerp(pulseMin, pulseMax, t);

            // Base emission color (soft white-gold)
            Color baseEmit = new Color(1.0f, 0.95f, 0.75f, 1f);
            Color pulsed = baseEmit * k;

            // In URP Unlit, emission alpha is often ignored; color intensity matters
            mat.SetColor(emissionColorName, pulsed);
        }
    }
}
