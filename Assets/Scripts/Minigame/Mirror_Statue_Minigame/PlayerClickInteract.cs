using UnityEngine;

public class PlayerClickInteract : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask statueLayer;  // Chọn layer của tượng

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Click chuột trái
        {
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, statueLayer);

            if (hit.collider != null)
            {
                var statue = hit.collider.GetComponent<StatueLaser>();
                if (statue != null)
                {
                    statue.RotateStatue();
                }
            }
        }
    }
}
