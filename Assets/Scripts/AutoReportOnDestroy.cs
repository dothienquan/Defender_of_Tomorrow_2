using UnityEngine;

public class AutoReportOnDestroy : MonoBehaviour
{
    [HideInInspector] public TowerMinigame owner;

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.NotifyUnitDestroyed();
        }
    }
}