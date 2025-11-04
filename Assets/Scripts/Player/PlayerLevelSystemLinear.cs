using UnityEngine;
using System;

/// <summary>
/// Level hệ tuyến tính (linear) theo yêu cầu:
/// - Max level = 30
/// - Mốc tổng XP: L1=100, L2=300, L3=500, ... => thêm 200 mỗi cấp sau cấp 1
///   -> XP cần để "lên tiếp" khi đang ở level n là:
///      n==1 ? 100 : 200
/// - Gọi AddXP(x) để cộng XP; tự gọi OnLevelUp(newLevel) khi vượt ngưỡng.
/// </summary>
public class PlayerLevelSystemLinear : MonoBehaviour
{
    [Header("Level & XP")]
    public int currentLevel = 1;
    public int currentXP = 0;          // XP đang tích lũy trong cấp hiện tại
    public int maxLevel = 30;

    public event Action<int> OnLevelUp;

    private void Awake()
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, maxLevel);
    }

    /// <summary> XP cần để lên cấp TIẾP THEO từ cấp hiện tại. </summary>
    public int GetXPToNext()
    {
        if (currentLevel >= maxLevel) return 0;
        return (currentLevel == 1) ? 100 : 200;
    }

    /// <summary> Cộng XP và xử lý lên cấp (có thể lên nhiều cấp nếu XP lớn). </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0 || currentLevel >= maxLevel) return;

        currentXP += amount;

        while (currentLevel < maxLevel)
        {
            int need = GetXPToNext();
            if (currentXP >= need)
            {
                currentXP -= need;
                currentLevel++;
                OnLevelUp?.Invoke(currentLevel);
            }
            else break;
        }
    }

    [ContextMenu("Force Level Up")]
    public void ForceLevelUp()
    {
        if (currentLevel >= maxLevel) return;
        currentLevel++;
        currentXP = 0;
        OnLevelUp?.Invoke(currentLevel);
    }
}