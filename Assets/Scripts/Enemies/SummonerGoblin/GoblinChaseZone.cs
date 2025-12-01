using UnityEngine;

public class GoblinChaseZone : MonoBehaviour
{
    private GoblinAI goblinAI;

    private void Awake()
    {
        goblinAI = GetComponentInParent<GoblinAI>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            goblinAI.SetInChaseZone(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            goblinAI.SetInChaseZone(false);
        }
    }
}
