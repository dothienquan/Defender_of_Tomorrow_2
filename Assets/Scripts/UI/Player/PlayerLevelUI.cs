using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerLevelUI : MonoBehaviour
{
    [Header("References")]
    public PlayerLevelSystemLinear levelSystem;
    public Slider expBar;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI expText;

    private void Reset()
    {
        // Try auto-find on same GameObject if you drop this on Player, etc.
        if (levelSystem == null)
            levelSystem = FindFirstObjectByType<PlayerLevelSystemLinear>();
    }

    private void Update()
    {
        if (levelSystem == null) return;

        // Update slider
        if (expBar != null)
        {
            expBar.maxValue = levelSystem.XpToNextLevel;
            expBar.value = levelSystem.CurrentXP;
        }

        // Update texts
        if (levelText != null)
        {
            levelText.text = $"Level {levelSystem.CurrentLevel}";
        }

        if (expText != null)
        {
            expText.text = $"{levelSystem.CurrentXP} / {levelSystem.XpToNextLevel}";
        }
    }
}
