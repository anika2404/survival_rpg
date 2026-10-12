using UnityEngine;

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
    }
}