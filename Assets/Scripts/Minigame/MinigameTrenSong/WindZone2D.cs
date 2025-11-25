using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WindZone2D : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = new Vector2(1f, 0f); // hướng gió (mặc định sang phải)
    public float windStrength = 3f;                      // độ mạnh (units/second)

    private void Reset()
    {
        // đảm bảo collider là trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector2 wind = windDirection.normalized * windStrength;
            player.SetWind(wind);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetWind(Vector2.zero);
        }
    }
}
