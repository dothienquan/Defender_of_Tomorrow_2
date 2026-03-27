using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label; // nếu dùng TMP: TextMeshProUGUI
    [SerializeField] private string hintKey = "F";
    [SerializeField] private bool useGameObjectActive = false; // Nếu true, dùng gameObject.SetActive thay vì label.enabled

    private void Awake()
    {
        if (label == null) label = GetComponent<TextMeshProUGUI>();
        Hide();
    }

    public void Show(string displayName)
    {
        if (label == null)
        {
            Debug.LogWarning("[InteractionUI] Label is null! Vui lòng gán TextMeshProUGUI component.");
            return;
        }
        
        label.text = $"{displayName}";
        
        if (useGameObjectActive)
        {
            gameObject.SetActive(true);
        }
        else
        {
            label.enabled = true;
        }
    }

    public void Hide()
    {
        if (label == null) return;
        
        if (useGameObjectActive)
        {
            gameObject.SetActive(false);
        }
        else
        {
            label.enabled = false;
        }
    }
}
