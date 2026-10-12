using UnityEngine;

/// <summary>
/// One asset per role: Fighter, Engineer, Scout.
/// Create via Right-click > Create > Survival RPG > Role.
/// </summary>
[CreateAssetMenu(fileName = "NewRole", menuName = "Survival RPG/Role")]
public class RoleData : ScriptableObject
{
    public string roleName = "Fighter";
    [TextArea] public string description;
    public Sprite icon;
    public Color placeholderColor = Color.white;

    [Header("Survival")]
    public int maxHealth = 100;
    public float maxStamina = 100f;
    public float staminaRegenPerSec = 15f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float sneakMultiplier = 0.5f;

    [Header("Combat")]
    public float damageMultiplier = 1f;
    public WeaponData startingWeapon;

    [Header("Stealth (lower visibility = harder to spot)")]
    [Range(0.1f, 2f)] public float visibilityMultiplier = 1f;
    public float detectionRadius = 0f;   // Scout can sense enemies nearby

    [Header("Crafting (used by crafting/repair systems)")]
    public float craftingSkill = 1f;
    public float repairEfficiency = 1f;
}
