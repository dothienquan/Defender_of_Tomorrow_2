using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WindZone2D : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = new Vector2(1f, 0f);
    public float windStrength = 3f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 wind = windDirection.normalized * windStrength;

        // Player
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetWind(wind);
        }

        // Raft
        var raft = other.GetComponent<RaftController>();
        if (raft != null)
        {
            raft.SetWind(wind);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Player
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetWind(Vector2.zero);
        }

        // Raft
        var raft = other.GetComponent<RaftController>();
        if (raft != null)
        {
            raft.SetWind(Vector2.zero);
        }
    }
}
