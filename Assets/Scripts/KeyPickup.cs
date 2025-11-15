using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] private string keyId = "MainKey"; // nếu sau này có nhiều loại key

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // TODO: xử lý logic giữ key ở đâu đó
        // ví dụ: GameManager.Instance.AddKey(keyId);
        Debug.Log("[KeyPickup] Player picked key: " + keyId);

        Destroy(gameObject);
    }
}
