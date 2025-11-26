using UnityEngine;

public class RaftMaterialCollector : MonoBehaviour
{
    [Header("Material Settings")]
    public int requiredWood = 10;     // số gỗ cần để được chơi mini game
    public int currentWood = 0;       // đang có bao nhiêu gỗ

    public bool HasEnoughMaterials => currentWood >= requiredWood;

    // Gọi hàm này khi player nhặt được 1 nguyên liệu (vd: gỗ)
    public void AddMaterial(int amount)
    {
        currentWood += amount;
        Debug.Log($"Wood: {currentWood}/{requiredWood}");
    }
}
