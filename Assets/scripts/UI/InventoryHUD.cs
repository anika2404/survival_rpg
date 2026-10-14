using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Creates a usable pixel-game HUD at runtime: health bar, inventory, item details,
/// pickup prompt, and pause overlay. Attach to the same GameObject as InventorySystem.</summary>
public class InventoryHUD : MonoBehaviour
{
    [SerializeField] private InventorySystem inventory;
    [SerializeField, Min(1f)] private float initialMaxHealth = 100f;
    [SerializeField] private bool showInventoryOnStart;

    private GameObject inventoryPanel;
    private GameObject descriptionPanel;
    private GameObject promptPanel;
    private GameObject pausePanel;
    private Image healthFill;
    private Text healthText;
    private Text promptText;
    private Image detailIcon;
    private Text detailName;
    private Text detailDescription;
    private readonly List<SlotWidget> slotWidgets = new List<SlotWidget>();
    private bool inventoryOpen;
    private bool paused;
    private float maxHealth = 100f;
    private float currentHealth = 100f;

    private class SlotWidget
    {
        public Button button;
        public Image icon;
        public Text nameText;
        public Text countText;
    }

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<InventorySystem>();
        if (inventory == null) inventory = FindFirstObjectByType<InventorySystem>();
        maxHealth = Mathf.Max(1f, initialMaxHealth);
        currentHealth = maxHealth;
        BuildHUD();
    }

    private void Start()
    {
        if (inventory == null) inventory = FindFirstObjectByType<InventorySystem>();
        if (inventory != null) inventory.InventoryChanged += RefreshInventory;
        RefreshInventory();
        SetHealth(currentHealth, maxHealth);
        SetInventoryVisible(showInventoryOnStart);
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.InventoryChanged -= RefreshInventory;
        if (paused) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.iKey.wasPressedThisFrame && !paused)
            SetInventoryVisible(!inventoryOpen);
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (inventoryOpen) SetInventoryVisible(false);
            else SetPaused(!paused);
        }
    }

    public void SetHealth(float current, float maximum)
    {
        maxHealth = Mathf.Max(1f, maximum);
        currentHealth = Mathf.Clamp(current, 0f, maxHealth);
        if (healthFill != null) healthFill.fillAmount = currentHealth / maxHealth;
        if (healthText != null) healthText.text = $"HEALTH  {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
    }

    public void ShowPickupPrompt(string message)
    {
        if (promptText != null) promptText.text = message;
        if (promptPanel != null) promptPanel.SetActive(true);
    }

    public void HidePickupPrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    public void SetInventoryVisible(bool visible)
    {
        inventoryOpen = visible;
        if (inventoryPanel != null) inventoryPanel.SetActive(visible);
        if (!visible && descriptionPanel != null) descriptionPanel.SetActive(false);
        if (visible) RefreshInventory();
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (pausePanel != null) pausePanel.SetActive(value);
        Time.timeScale = value ? 0f : 1f;
    }

    private void BuildHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("InventoryHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (EventSystem.current == null)
            new GameObject("InventoryUIEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Health display (top-left)
        Image healthPanel = MakePanel(canvas.transform, "HealthPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(300, 78), new Color(0.04f, 0.06f, 0.08f, 0.92f));
        healthText = MakeText(healthPanel.transform, "HealthText", "HEALTH  100 / 100", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -7), new Vector2(-20, 25), 18, TextAnchor.MiddleLeft, Color.white);
        Image healthBack = MakePanel(healthPanel.transform, "HealthBarBackground", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(10, 12), new Vector2(-20, 22), new Color(0.18f, 0.2f, 0.22f, 1f));
        healthFill = MakePanel(healthBack.transform, "HealthFill", Vector2.zero, new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, new Color(0.75f, 0.12f, 0.12f, 1f));
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.fillAmount = 1f;

        // Inventory window and 20 (or configured capacity) clickable slots
        inventoryPanel = MakePanel(canvas.transform, "InventoryPanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -100), new Vector2(400, 425), new Color(0.035f, 0.045f, 0.065f, 0.97f)).gameObject;
        MakeText(inventoryPanel.transform, "InventoryTitle", "INVENTORY  [I]", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(-20, 36), 22, TextAnchor.MiddleCenter, Color.white);
        Text instruction = MakeText(inventoryPanel.transform, "InventoryHint", "Select an item to view its description", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(-20, 24), 13, TextAnchor.MiddleCenter, new Color(0.75f, 0.78f, 0.82f));
        instruction.raycastTarget = false;

        RectTransform gridRect = MakeRect(inventoryPanel.transform, "InventorySlots", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -5), new Vector2(-24, -92));
        gridRect.offsetMin = new Vector2(12, 42);
        gridRect.offsetMax = new Vector2(-12, -54);
        GridLayoutGroup grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(66, 66);
        grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        int slotsToCreate = inventory != null ? Mathf.Max(1, inventory.Capacity) : 20;
        for (int i = 0; i < slotsToCreate; i++)
            slotWidgets.Add(CreateSlot(gridRect, i));

        // Item description card (bottom-right)
        Image detail = MakePanel(canvas.transform, "ItemDescriptionPanel", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(400, 150), new Color(0.035f, 0.045f, 0.065f, 0.96f));
        descriptionPanel = detail.gameObject;
        detailIcon = MakePanel(detail.transform, "ItemIcon", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(72, 72), new Color(0.16f, 0.19f, 0.23f, 1f));
        detailName = MakeText(detail.transform, "ItemName", "Item", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(96, -12), new Vector2(-110, 32), 19, TextAnchor.MiddleLeft, Color.white);
        detailDescription = MakeText(detail.transform, "ItemDescription", "Item description", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(96, -48), new Vector2(-110, -58), 14, TextAnchor.UpperLeft, new Color(0.82f, 0.84f, 0.88f));
        detailDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailDescription.verticalOverflow = VerticalWrapMode.Overflow;
        descriptionPanel.SetActive(false);

        // Context prompt (bottom-centre)
        Image prompt = MakePanel(canvas.transform, "InteractionPrompt", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 88), new Vector2(600, 54), new Color(0.035f, 0.045f, 0.065f, 0.94f));
        promptPanel = prompt.gameObject;
        promptText = MakeText(prompt.transform, "PromptText", "Press E to interact", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter, Color.white);
        promptPanel.SetActive(false);

        // Pause overlay (Escape)
        Image pause = MakePanel(canvas.transform, "PauseOverlay", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f));
        pausePanel = pause.gameObject;
        MakeText(pause.transform, "PausedTitle", "PAUSED", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 105), new Vector2(360, 60), 34, TextAnchor.MiddleCenter, Color.white);
        Button resume = MakeButton(pause.transform, "ResumeButton", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(220, 52));
        resume.onClick.AddListener(() => SetPaused(false));
        MakeText(pause.transform, "PauseHint", "Press ESC to resume", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(300, 30), 15, TextAnchor.MiddleCenter, new Color(0.8f, 0.82f, 0.85f));
        pausePanel.SetActive(false);
    }

    private SlotWidget CreateSlot(Transform parent, int index)
    {
        Image background;
        Button button = MakeButton(parent, $"Slot_{index + 1:00}", "", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(66, 66), out background);
        background.color = new Color(0.12f, 0.15f, 0.19f, 1f);
        Image icon = MakePanel(button.transform, "Icon", new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        icon.preserveAspect = true;
        icon.enabled = false;
        Text name = MakeText(button.transform, "ItemLabel", "", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(-4, 20), 11, TextAnchor.MiddleCenter, Color.white);
        Text count = MakeText(button.transform, "CountLabel", "", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-3, -2), new Vector2(48, 17), 11, TextAnchor.MiddleRight, Color.white);
        int capturedIndex = index;
        button.onClick.AddListener(() => SelectSlot(capturedIndex));
        return new SlotWidget { button = button, icon = icon, nameText = name, countText = count };
    }

    private void SelectSlot(int index)
    {
        if (inventory == null || index < 0 || index >= inventory.Slots.Count) return;
        InventorySlot slot = inventory.Slots[index];
        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            descriptionPanel.SetActive(false);
            return;
        }
        detailName.text = slot.item.itemName;
        detailDescription.text = string.IsNullOrWhiteSpace(slot.item.description) ? "No description available." : slot.item.description;
        detailIcon.sprite = slot.item.icon;
        detailIcon.enabled = slot.item.icon != null;
        descriptionPanel.SetActive(true);
    }

    private void RefreshInventory()
    {
        if (inventory == null) inventory = FindFirstObjectByType<InventorySystem>();
        for (int i = 0; i < slotWidgets.Count; i++)
        {
            SlotWidget widget = slotWidgets[i];
            InventorySlot slot = inventory != null && i < inventory.Slots.Count ? inventory.Slots[i] : null;
            bool hasItem = slot != null && !slot.IsEmpty && slot.item != null;
            widget.icon.enabled = hasItem && slot.item.icon != null;
            widget.icon.sprite = hasItem ? slot.item.icon : null;
            widget.nameText.text = hasItem ? (slot.item.icon == null ? slot.item.itemName : ShortName(slot.item.itemName)) : "";
            widget.countText.text = hasItem ? $"x{slot.quantity}" : "";
            widget.button.interactable = hasItem;
        }
    }

    private static string ShortName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= 8 ? value : value.Substring(0, 7) + "…";
    }

    private static RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image MakePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = MakeRect(parent, name, anchorMin, anchorMax, pivot, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text MakeText(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = MakeRect(parent, name, anchorMin, anchorMax, pivot, position, size);
        Text text = rect.gameObject.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        return MakeButton(parent, name, label, anchorMin, anchorMax, position, size, out _);
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, out Image image)
    {
        RectTransform rect = MakeRect(parent, name, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, size);
        image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.28f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (!string.IsNullOrEmpty(label))
            MakeText(rect, "Label", label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter, Color.white);
        return button;
    }
}
