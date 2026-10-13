
using UnityEngine;

public enum ItemType
{
    Material,
    Consumable,
    Key,
    Ammo,
    Equipment
}

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Survival RPG/Inventory/Item Data"
)]
public class ItemData : ScriptableObject
{
    [Header("Item Information")]
    public string itemName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Item Behaviour")]
    public ItemType itemType = ItemType.Material;

    [Min(0)]
    public int healAmount = 0;

    [Header("Stack Settings")]
    public bool isStackable = true;

    [Min(1)]
    public int maxStackSize = 99;

    private void OnValidate()
    {
        maxStackSize = Mathf.Max(1, maxStackSize);

        if (!isStackable)
        {
            maxStackSize = 1;
        }

        healAmount = Mathf.Max(0, healAmount);
    }
}
