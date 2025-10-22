using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserEmitter : MonoBehaviour
{
    public float maxDistance = 50f;
    public LayerMask hitLayer;
    public bool isActive = false;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
    }

    void Update()
    {
        if (isActive)
        {
            FireLaser();
        }
        else
        {
            lr.enabled = false;
        }
    }

    void FireLaser()
    {
        lr.enabled = true;
        Vector2 dir = transform.right;
        Vector2 startPos = transform.position;

        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, maxDistance, hitLayer);
        if (hit.collider != null)
        {
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, hit.point);

            var statue = hit.collider.GetComponent<StatueLaser>();
            if (statue != null)
            {
                statue.ActivateLaser();
            }
        }
        else
        {
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, startPos + dir * maxDistance);
        }
    }
}
