using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    public BossHPUI bossUI;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            bossUI.ShowBossHP();
        }
    }
}
