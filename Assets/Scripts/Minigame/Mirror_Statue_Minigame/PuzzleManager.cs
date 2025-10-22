using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public StatueLaser[] statues;
    private int activatedCount = 0;
    private bool puzzleComplete = false;

    public void StatueActivated(StatueLaser statue)
    {
        if (!puzzleComplete)
        {
            activatedCount = 0;
            foreach (var s in statues)
            {
                if (s.IsActive()) activatedCount++;
            }

            if (activatedCount == statues.Length)
            {
                puzzleComplete = true;
                OnPuzzleComplete();
            }
        }
    }

    void OnPuzzleComplete()
    {
        Debug.Log("Puzzle Complete!");
        // TODO: Mở cổng / Kích hoạt sự kiện tiếp theo
    }

    public void ResetAllStatues()
    {
        foreach (var s in statues)
        {
            s.DeactivateLaser();
        }
    }


}
