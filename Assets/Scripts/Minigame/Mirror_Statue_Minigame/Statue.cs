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

        // We are now the controllable statue
        if (lineRotator)
        {
            lineRotator.ResetForActivation();   // clears freeze; keeps previous dir if you want to
            lineRotator.EnableControl(true);
        }
    }

    // Immediately leave Active state (Option 1), but keep line if completed
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

        // Ensure line remains visible after completion
        if (value && lineRenderer != null)
            lineRenderer.enabled = true;
    }

    public void ResetStatue()
    {
        isCompleted = false;

        // Reset aiming logic only, not visuals
        if (lineRotator) lineRotator.ResetAll();

        // Keep lineRenderer & animation states as they are (do not disable)
    }

    // ---------- Interaction from LineRotator ----------
    /// <summary>
    /// Called by LineRotator when the ray hits a statue. Handles snapping & progression.
    /// </summary>
    public void HandleHit(Statue hitStatue, Vector2 hitPoint)
    {
        if (manager == null || !manager.IsCurrentStatue(this)) return;

        bool correct = manager.IsCorrectNextTarget(hitStatue);

        // Snap the line to the exact hit point immediately
        if (lineRenderer)
        {
            lineRenderer.SetPosition(0, GetMuzzlePosition());
            lineRenderer.SetPosition(1, hitPoint);
        }

        if (correct)
        {
            // Freeze the line so it stops updating and keeps the exact length
            if (lineRotator) lineRotator.FreezeLineAtSnap(hitPoint);

            // Mark completed, keep line visible
            SetCompleted(true);

            // ✅ Keep animation active, but disable control so player can't rotate it anymore
            if (lineRotator) lineRotator.EnableControl(false);

            // Advance puzzle
            manager.AdvanceToNextStatue();
        }
        else
        {
            // Wrong target: keep angle; no snap-back
        }
    }


    // ---------- Helpers ----------
    public Vector2 GetMuzzlePosition()
    {
        return muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;
    }
}
