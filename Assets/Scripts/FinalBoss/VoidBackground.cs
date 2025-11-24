using UnityEngine;

public class VoidBackground : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 15f;

    void Update()
    {
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}
