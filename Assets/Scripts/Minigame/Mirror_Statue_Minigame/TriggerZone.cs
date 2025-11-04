using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerZone : MonoBehaviour
{
    public MirrorPuzzleManager puzzle;
    public string playerTag = "Player";

    private int playerInsideCount = 0;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideCount++;
        if (playerInsideCount == 1 && puzzle != null)
        {
            puzzle.StartPuzzle();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideCount = Mathf.Max(0, playerInsideCount - 1);
        if (playerInsideCount == 0 && puzzle != null)
        {
            puzzle.ResetPuzzle();
        }
    }
}
