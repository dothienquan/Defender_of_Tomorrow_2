using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TMPro;

public class BossHPUI : MonoBehaviour
{
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    private EnemyHealth bossHealth;
    private FieldInfo fi_current;
    private FieldInfo fi_start;

    private bool active = false;

    void Awake()
    {
        gameObject.SetActive(false);

        var t = typeof(EnemyHealth);
        fi_current = t.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        fi_start = t.GetField("startingHealth", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    void Update()
    {
        if (!active || bossHealth == null) return;

        int current = (int)fi_current.GetValue(bossHealth);
        int start = (int)fi_start.GetValue(bossHealth);

        hpSlider.value = Mathf.Clamp01((float)current / start);

        // UPDATE TEXT
        hpText.text = current + " / " + start;

        if (current <= 0)
        {
            HideBossHP();
        }
    }

    public void ShowBossHP(EnemyHealth boss)
    {
        if (boss == null)
        {
            Debug.LogWarning("[BossHPUI] ShowBossHP called with null boss reference", this);
            return;
        }

        bossHealth = boss;
        active = true;
        gameObject.SetActive(true);
    }

    public void HideBossHP()
    {
        active = false;
        bossHealth = null;
        gameObject.SetActive(false);
    }
}
