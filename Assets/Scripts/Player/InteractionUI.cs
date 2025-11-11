using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label; // nếu dùng TMP: TextMeshProUGUI
    [SerializeField] private string hintKey = "F";

    private void Awake()
    {
        if (label == null) label = GetComponent<TextMeshProUGUI>();
        Hide();
    }

    public void Show(string displayName)
    {
        if (label == null) return;
        label.text = $"[{hintKey}] {displayName}";
        label.enabled = true;
    }

    public void Hide()
    {
        if (label == null) return;
        label.enabled = false;
    }
}
