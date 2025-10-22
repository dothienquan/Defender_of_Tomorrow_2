using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRadius = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))  // Phím xoay tượng
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRadius);
            if (hit != null)
            {
                var statue = hit.GetComponent<StatueLaser>();
                if (statue != null)
                {
                    statue.RotateStatue();
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
