using UnityEngine;

public class VoidCastWave : MonoBehaviour
{
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float maxScale = 6f;
    [SerializeField] private SpriteRenderer sr;

    private float t;

    private void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        t += Time.deltaTime;
        float p = t / duration;

        transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * maxScale, p);
        sr.color = new Color(0, 0, 0, 1f - p);  // fade đen mờ

        if (p >= 1f) Destroy(gameObject);
    }
}
