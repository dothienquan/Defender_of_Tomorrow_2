using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Statue : MonoBehaviour
{
    [Header("Setup")]
    public Direction direction = Direction.Right;
    public Transform muzzle; // where the beam starts (tip of statue)
    public LaserBeam beam;
    public Animator animator; // has bool "isActive"

    [Header("Input")]
    public bool clickable = true;

    // cache for last cast result
    private Statue _lastHit;

    private void OnEnable()
    {
        LaserManager.Instance?.Register(this);
        SetActiveAnim(false);
        if (beam != null) beam.TurnOff();
    }

    private void OnDisable()
    {
        LaserManager.Instance?.Unregister(this);
    }

    private void Reset()
    {
        // Try auto-wire common setup if added fresh
        if (muzzle == null) muzzle = transform;
        if (beam == null) beam = GetComponentInChildren<LaserBeam>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnMouseDown()
    {
        if (!clickable) return;
        Rotate90();
    }

    public void Rotate90()
    {
        direction = direction.Rotate90();
        LaserManager.Instance?.OnStatueRotated();
    }

    public void DeactivateInstant()
    {
        _lastHit = null;
        if (beam != null) beam.TurnOff();
        SetActiveAnim(false);
    }

    private void SetActiveAnim(bool on)
    {
        if (animator != null)
        {
            animator.SetBool("isActive", on);
        }
    }

    /// <summary>
    /// Activate the statue visually, cast the beam, and return the next statue hit if any.
    /// </summary>
    public Statue ActivateAndCast(out Vector3 hitPoint)
    {
        SetActiveAnim(true);
        if (beam != null) beam.TurnOn();

        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        Vector2 dir = direction.ToVector();

        _lastHit = beam != null
            ? beam.Cast(origin, dir, out hitPoint)
            : null;
        hitPoint = default;
        return _lastHit;
    }
}
