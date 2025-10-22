using UnityEngine;

public class PressureSwitch : MonoBehaviour
{
    public LaserEmitter laserSource;
    public PuzzleManager puzzleManager; // Gán trong Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            laserSource.isActive = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            laserSource.isActive = false;
            puzzleManager.ResetAllStatues();  // 👈 Reset toàn bộ tượng
        }
    }
}
