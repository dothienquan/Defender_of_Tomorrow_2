using UnityEngine;

public class AutoMove : MonoBehaviour
{
    public enum MoveDirection { Horizontal, Vertical }
    public MoveDirection moveDirection = MoveDirection.Horizontal;

    public float moveDistance = 3f;   
    public float moveSpeed = 2f;      

    private Vector3 _startPos;
    private int _direction = 1;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        Vector3 targetPos = _startPos;

        if (moveDirection == MoveDirection.Horizontal)
            targetPos.x = _startPos.x + Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        else
            targetPos.y = _startPos.y + Mathf.Sin(Time.time * moveSpeed) * moveDistance;

        transform.position = targetPos;
    }
}
