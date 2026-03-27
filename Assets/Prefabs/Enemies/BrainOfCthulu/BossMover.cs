using System;
using UnityEngine;

public class BossMover : MonoBehaviour
{
    private Func<Vector2> areaCenterGetter;
    private Vector2 areaSize;
    private float speed;
    private float idleTime;

    private Vector2 target;
    private float idleTimer;
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
        idleTime = t;
    }

    private void Start()
    {
        IdleThenMove();
    }

    private void Update()
    {
        if (!moving)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                PickTarget();
                moving = true;
            }
            return;
        }

        MoveTowardsTarget();
    }

    private void IdleThenMove()
    {
        idleTimer = idleTime;
        moving = false;
    }

    private void PickTarget()
    {
        Vector2 center = areaCenterGetter();
        Vector2 half = areaSize * 0.5f;

        target = center + new Vector2(
            UnityEngine.Random.Range(-half.x, half.x),
            UnityEngine.Random.Range(-half.y, half.y)
        );
    }

    private void MoveTowardsTarget()
    {
        Vector3 pos = transform.position;
        Vector3 dir = target - (Vector2)pos;
        float step = speed * Time.deltaTime;

        if (dir.sqrMagnitude <= step * step)
        {
            transform.position = target;
            IdleThenMove();
        }
        else
        {
            transform.position = pos + dir.normalized * step;
        }
    }
}
