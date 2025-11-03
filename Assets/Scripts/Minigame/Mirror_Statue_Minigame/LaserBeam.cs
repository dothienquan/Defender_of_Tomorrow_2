using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask hitMask;
    public float maxDistance = 50f;
    public float edgeShrink = 0.05f; // pull back a bit from the exact hit for nicer visuals

    [Header("Visuals")]
    public float baseWidth = 0.06f;
    public float pulseAmplitude = 0.02f;
    public float pulseSpeed = 6f;

    private LineRenderer _lr;
    private Coroutine _pulseCo;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = 2;
        _lr.enabled = false;

        // simple defaults for crisp beam
        _lr.useWorldSpace = true;
        _lr.numCapVertices = 6;
        _lr.numCornerVertices = 0;
        _lr.startWidth = baseWidth;
        _lr.endWidth = baseWidth;
    }

    public void TurnOff()
    {
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        _lr.enabled = false;
    }

    public void TurnOn()
    {
        _lr.enabled = true;
        if (_pulseCo == null) _pulseCo = StartCoroutine(PulseWidth());
    }

    private IEnumerator PulseWidth()
    {
        while (true)
        {
            float w = baseWidth + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            _lr.startWidth = w;
            _lr.endWidth = w;
            yield return null;
        }
    }

    /// <summary>
    /// Cast a laser from origin towards dir. Draws the beam and returns the statue hit (if any).
    /// </summary>
    public Statue Cast(Vector3 origin, Vector2 dir, out Vector3 hitPoint)
    {
        hitPoint = origin + (Vector3)(dir.normalized * maxDistance);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, hitMask);

        Vector3 endPoint = hitPoint;
        Statue targetStatue = null;

        if (hit.collider != null)
        {
            endPoint = hit.point - dir.normalized * edgeShrink;

            // Prefer exact component on the hit
            if (hit.collider.TryGetComponent<Statue>(out var s))
            {
                targetStatue = s;
            }
            else
            {
                // Or search up the hierarchy
                targetStatue = hit.collider.GetComponentInParent<Statue>();
            }
        }

        _lr.SetPosition(0, origin);
        _lr.SetPosition(1, endPoint);
        return targetStatue;
    }
}
