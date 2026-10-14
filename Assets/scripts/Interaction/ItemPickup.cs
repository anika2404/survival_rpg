using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Collectible item. Add a Collider2D set to Is Trigger and a Rigidbody2D to the pickup
/// (Kinematic, Gravity Scale 0), unless the player already has a Rigidbody2D.</summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private InventorySystem inventoryOverride;
    [SerializeField] private InventoryHUD hudOverride;

    private bool playerInRange;
    private InventorySystem targetInventory;
    private InventoryHUD hud;
    private Collider2D pickupCollider;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
        quantity = Mathf.Max(1, quantity);
        hud = hudOverride != null ? hudOverride : FindFirstObjectByType<InventoryHUD>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayer = other.CompareTag(playerTag) || other.GetComponentInParent<InventorySystem>() != null;
        if (!isPlayer) return;

        targetInventory = other.GetComponentInParent<InventorySystem>();
        if (targetInventory == null)
            targetInventory = inventoryOverride != null ? inventoryOverride : FindFirstObjectByType<InventorySystem>();
        if (targetInventory == null) return;

        playerInRange = true;
        if (hud == null) hud = hudOverride != null ? hudOverride : FindFirstObjectByType<InventoryHUD>();
        hud?.ShowPickupPrompt(itemData != null ? $"Press E to pick up {itemData.itemName} x{quantity}" : "Item data missing");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        bool isPlayer = other.CompareTag(playerTag) || other.GetComponentInParent<InventorySystem>() != null;
        if (!isPlayer) return;
        playerInRange = false;
        hud?.HidePickupPrompt();
        targetInventory = null;
    }

    private void Update()
    {
        if (!playerInRange || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame)
            TryPickup();
    }

    private void TryPickup()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: assign an ItemData asset to this pickup.", this);
            return;
        }

        if (targetInventory == null)
            targetInventory = inventoryOverride != null ? inventoryOverride : FindFirstObjectByType<InventorySystem>();

        if (targetInventory == null)
        {
            Debug.LogWarning("No InventorySystem found in the scene.", this);
            return;
        }

        if (!targetInventory.AddItem(itemData, quantity))
        {
            hud?.ShowPickupPrompt("Inventory full!");
            return;
        }

        hud?.HidePickupPrompt();
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        quantity = Mathf.Max(1, quantity);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}
