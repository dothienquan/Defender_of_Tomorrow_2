using Random = UnityEngine.Random;
using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class BossMover : MonoBehaviour
{
    private Func<Vector2> areaCenterGetter;
    private Vector2 areaSize = Vector2.one;
    private float speed = 1.5f;
    private float idle = 0.5f;

    private Vector2 target;
    private float idleTimer = 0f;
    private bool moving = false;

    public void SetArea(Func<Vector2> centerGetter, Vector2 size)
    {
        areaCenterGetter = centerGetter;
        areaSize = size;
    }

    public void SetSpeed(float s)
    {
        speed = s;
    }

    public void SetIdleTime(float t)
    {
        idle = t;
    }

    private void Start()
    {
        PickNewTarget();
        idleTimer = Random.Range(0f, idle);
    }

    private void Update()
    {
        if (!moving)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                PickNewTarget();
                moving = true;
            }
            return;
        }

        Vector3 pos = transform.position;
        Vector3 dir = (target - (Vector2)pos);
        float step = speed * Time.deltaTime;

        if (dir.sqrMagnitude <= step * step)
        {
            transform.position = target;
            moving = false;
            idleTimer = idle + Random.Range(-idle * 0.4f, idle * 0.4f);
        }
        else
        {
            transform.position = pos + (Vector3)(dir.normalized * step);
        }
    }

    private void PickNewTarget()
    {
        Vector2 center = (areaCenterGetter != null) ? areaCenterGetter() : (Vector2)transform.position;
        Vector2 half = areaSize * 0.5f;
        target = center + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
    }
}
