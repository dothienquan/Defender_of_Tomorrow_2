using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public float speed = 3f;
    private Transform target;
    private bool chasing = false;

    public void BeginChase(Transform player)
    {
        target = player;
        chasing = true;
    }

    public void StopChase()
    {
        chasing = false;
        target = null;
    }

    private void Update()
    {
        if (!chasing || target == null) return;

        // Move straight to the player
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );
    }
}
