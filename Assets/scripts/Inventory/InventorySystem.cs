
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

public class InventorySystem : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int capacity = 20;

    [SerializeField]
    private List<InventorySlot> slots = new List<InventorySlot>();

    public event Action InventoryChanged;

    public int Capacity => capacity;
    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        ResizeSlots();
    }

    private void OnValidate()
    {
        capacity = Mathf.Max(1, capacity);
    }

    private void ResizeSlots()
    {
        while (slots.Count < capacity)
        {
            slots.Add(new InventorySlot());
        }

        while (slots.Count > capacity)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        ResizeSlots();

        int maxStack = item.isStackable
            ? Mathf.Max(1, item.maxStackSize)
            : 1;

        int availableSpace = 0;

        foreach (InventorySlot slot in slots)
        {
            if (slot.IsEmpty)
                availableSpace += maxStack;
            else if (item.isStackable && slot.item == item)
                availableSpace += Mathf.Max(0, maxStack - slot.quantity);
        }

        if (amount > availableSpace)
            return false;

        int remaining = amount;

        // Fill existing stacks first.
        if (item.isStackable)
        {
            foreach (InventorySlot slot in slots)
            {
                if (remaining == 0)
                    break;

                if (slot.IsEmpty || slot.item != item)
                    continue;

                int space = maxStack - slot.quantity;
                int added = Mathf.Min(space, remaining);

                slot.quantity += added;
                remaining -= added;
            }
        }

        // Fill empty slots.
        foreach (InventorySlot slot in slots)
        {
            if (remaining == 0)
                break;

            if (!slot.IsEmpty)
                continue;

            int added = Mathf.Min(maxStack, remaining);

            slot.item = item;
            slot.quantity = added;
            remaining -= added;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (GetQuantity(item) < amount)
            return false;

        int remaining = amount;

        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot slot = slots[i];

            if (slot.item != item || slot.IsEmpty)
                continue;

            int removed = Mathf.Min(slot.quantity, remaining);

            slot.quantity -= removed;
            remaining -= removed;

            if (slot.quantity <= 0)
                slot.Clear();
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public int GetQuantity(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item && !slot.IsEmpty)
                total += slot.quantity;
        }

        return total;
    }

    public bool UseItem(int slotIndex, PlayerHealth playerHealth)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        if (playerHealth == null || playerHealth.IsDead)
            return false;

        InventorySlot slot = slots[slotIndex];

        if (slot.IsEmpty || slot.item == null)
            return false;

        if (slot.item.itemType != ItemType.Consumable)
            return false;

        if (slot.item.healAmount <= 0)
            return false;

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            return false;

        ItemData itemToUse = slot.item;

        // Heal first, then consume one potion.
        playerHealth.Heal(itemToUse.healAmount);
        return RemoveItem(itemToUse, 1);
    }
}
