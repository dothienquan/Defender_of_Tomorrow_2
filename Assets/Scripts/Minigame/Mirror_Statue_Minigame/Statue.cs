using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Statue : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;                 // Bool: "isActive"
    public LineRenderer lineRenderer;
    public LineRotator lineRotator;

    [Header("Line")]
    public Transform muzzle;                  // Ray origin; falls back to transform if null
    public float maxRayDistance = 20f;
    public LayerMask hitMask;                 // Include Statue (and Walls if you want blocking)

    [Header("VFX")]
    public GameObject snapVFX;                // The particle prefab to spawn
    public Transform snapVFXPoint;            // Custom VFX spawn position (optional)

    public int Index { get; private set; } = -1;

    private MirrorPuzzleManager manager;
    private bool isActive;
    private bool isCompleted;

    // ---------- Lifecycle / Setup ----------
    public void Init(MirrorPuzzleManager mgr, int index)
    {
        manager = mgr;
        Index = index;

        if (lineRotator != null)
            lineRotator.Bind(this, mgr);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
    }

    // ---------- State Controls ----------
    public void Activate()
    {
        isActive = true;

        if (animator) animator.SetBool("isActive", true);
        if (lineRenderer) lineRenderer.enabled = true;

        if (lineRotator)
        {
            lineRotator.ResetForActivation();
            lineRotator.EnableControl(true);
        }
    }

    public void Deactivate()
    {
        isActive = false;

        if (animator) animator.SetBool("isActive", false);
        if (lineRotator) lineRotator.EnableControl(false);

        if (!isCompleted && lineRenderer)
            lineRenderer.enabled = false;
    }

    public void SetCompleted(bool value = true)
    {
        isCompleted = value;

        if (value && lineRenderer != null)
            lineRenderer.enabled = true;
    }

    public void ResetStatue()
    {
        isCompleted = false;

        if (lineRotator) lineRotator.ResetAll();
    }

    // ---------- Interaction from LineRotator ----------
    /// <summary>
    /// Called when the ray hits a statue. Handles the snap, freeze, and puzzle advancement.
    /// </summary>
    public void HandleHit(Statue hitStatue, Vector2 hitPoint)
    {
        if (manager == null || !manager.IsCurrentStatue(this)) return;

        bool correct = manager.IsCorrectNextTarget(hitStatue);

        // Always set line positions
        if (lineRenderer)
        {
            lineRenderer.SetPosition(0, GetMuzzlePosition());
            lineRenderer.SetPosition(1, hitPoint);
        }

        if (correct)
        {
            // ---------- VFX Spawn Here ----------
            if (snapVFX != null)
            {
                Vector3 vfxPos = (snapVFXPoint != null)
                    ? snapVFXPoint.position
                    : (Vector3)hitPoint;

                Instantiate(snapVFX, vfxPos, Quaternion.identity);
            }

            // Freeze line in place
            if (lineRotator) lineRotator.FreezeLineAtSnap(hitPoint);

            // Keep line visible
            SetCompleted(true);

            // Stop rotating this statue
            if (lineRotator) lineRotator.EnableControl(false);

            // Progress puzzle
            manager.AdvanceToNextStatue();
        }
        else
        {
            // Wrong statue hit: do nothing, maintain current rotation
        }
    }

    // ---------- Helpers ----------
    public Vector2 GetMuzzlePosition()
    {
        return muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;
    }
}
