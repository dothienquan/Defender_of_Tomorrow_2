using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public int ID;
    public string Name;

    [Header("Raft")]
    public bool isRaftMaterial;   // tick = item này dùng làm nguyên liệu bè

    public virtual void PickUp()
    {
        Sprite itemIcon = GetComponent<Image>().sprite;
        if(ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPopup(Name, itemIcon);  
        }
    }

    public virtual void UseItem()
    {
        Debug.Log("Use item" + Name);
    }
}
