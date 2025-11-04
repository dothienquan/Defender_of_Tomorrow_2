using UnityEngine;

[RequireComponent(typeof(Statue))]
public class LineRotator : MonoBehaviour
{
    [Header("Control")]
    [Tooltip("If true, user must click near the line to start dragging; otherwise click anywhere.")]
    public bool clickOnLineOnly = true;

    [Tooltip("World-units tolerance to consider a click 'on the line'.")]
    public float clickLineTolerance = 0.25f;

    [Tooltip("Override ray length; <= 0 uses Statue.maxRayDistance")]
    public float maxRayDistanceOverride = -1f;

    private Statue statue;
    private MirrorPuzzleManager manager;

    private bool controlEnabled = false;
    private bool isDragging = false;
    private Camera cam;

    // Persisted direction (keeps angle on release)
    private Vector2 currentDir = Vector2.right;

    // Freeze mode after snapping to correct statue
    private bool isFrozen = false;
    private Vector2 frozenEndPoint;

    // ---------- Wiring ----------
    public void Bind(Statue s, MirrorPuzzleManager m)
    {
        statue = s;
        manager = m;
        cam = Camera.main;
    }

    // Enable/disable rotation control (does not alter frozen state)
    public void EnableControl(bool enable)
    {
        controlEnabled = enable;
        if (!enable) isDragging = false;
        UpdateLineImmediate();
    }

    // Called when a statue becomes the active one
    public void ResetForActivation()
    {
        isFrozen = false;
        // Keep currentDir if you want continuity; or uncomment to reset:
        // currentDir = Vector2.right;
        UpdateLineImmediate();
    }

    // Fully reset (used by ResetStatue)
    public void ResetAll()
    {
        isDragging = false;
        isFrozen = false;
        currentDir = Vector2.right;
        UpdateLineImmediate();
    }

    public void ResetAngle()
    {
        currentDir = Vector2.right;
        UpdateLineImmediate();
    }

    public void FreezeLineAtSnap(Vector2 hitPoint)
    {
        isFrozen = true;
        frozenEndPoint = hitPoint;
        UpdateLineImmediate();
    }

    // ---------- Unity Loop ----------
    void Update()
    {
        // Always keep the line drawn (frozen or not) if renderer is enabled
        if (statue == null || statue.lineRenderer == null)
            return;

        // Draw line immediately to reflect any transform movement
        // (e.g., if muzzle moves on animation)
        if (!controlEnabled || manager == null || !manager.IsCurrentStatue(statue))
        {
            UpdateLineImmediate();
            return;
        }

        HandleInput();
        CastAndDraw();
    }

    private void HandleInput()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || isFrozen) return;

        Vector2 origin = statue.GetMuzzlePosition();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            if (clickOnLineOnly)
            {
                if (IsMouseNearCurrentLine(origin, currentDir, mouseWorld, out _))
                    isDragging = true;
            }
            else
            {
                isDragging = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            currentDir = (mouseWorld - origin).normalized;
        }
    }

    private void CastAndDraw()
    {
        Vector2 origin = statue.GetMuzzlePosition();

        // If frozen, keep the snapped line in place exactly
        if (isFrozen)
        {
            statue.lineRenderer.SetPosition(0, origin);
            statue.lineRenderer.SetPosition(1, frozenEndPoint);
            return;
        }

        float dist = (maxRayDistanceOverride > 0f) ? maxRayDistanceOverride : statue.maxRayDistance;

        // Cast the ray
        RaycastHit2D hit = Physics2D.Raycast(origin, currentDir, dist, statue.hitMask);

        // Default end is full length
        Vector3 endPoint = origin + currentDir * dist;

        if (hit.collider != null)
        {
            var hitStatue = hit.collider.GetComponent<Statue>();

            // Ignore hitting ourselves
            if (hitStatue == null || hitStatue == statue)
            {
                // Do nothing; keep default endPoint
            }
            else
            {
                endPoint = hit.point;

                // Draw to the hit point
                statue.lineRenderer.SetPosition(0, origin);
                statue.lineRenderer.SetPosition(1, endPoint);

                // Notify the shooter statue of the hit
                statue.HandleHit(hitStatue, hit.point);
                return;
            }
        }

        // No valid hit (or hit self): draw full-length
        statue.lineRenderer.SetPosition(0, origin);
        statue.lineRenderer.SetPosition(1, endPoint);
    }

    private void UpdateLineImmediate()
    {
        if (statue == null || statue.lineRenderer == null) return;

        Vector2 origin = statue.GetMuzzlePosition();

        if (isFrozen)
        {
            statue.lineRenderer.SetPosition(0, origin);
            statue.lineRenderer.SetPosition(1, frozenEndPoint);
            return;
        }

        float dist = (maxRayDistanceOverride > 0f) ? maxRayDistanceOverride : statue.maxRayDistance;
        Vector3 end = origin + currentDir * dist;

        statue.lineRenderer.SetPosition(0, origin);
        statue.lineRenderer.SetPosition(1, end);
    }

    // ---------- Utilities ----------
    private bool IsMouseNearCurrentLine(Vector2 origin, Vector2 dir, Vector2 mouse, out float distance)
    {
        float dist = (maxRayDistanceOverride > 0f) ? maxRayDistanceOverride : statue.maxRayDistance;
        Vector2 a = origin;
        Vector2 b = origin + dir.normalized * dist;

        distance = DistancePointToSegment(mouse, a, b);
        return distance <= clickLineTolerance;
    }

    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float ab2 = Vector2.SqrMagnitude(ab);
        if (ab2 <= Mathf.Epsilon) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab2);
        Vector2 proj = a + t * ab;
        return Vector2.Distance(p, proj);
    }
}
