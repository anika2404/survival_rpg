using UnityEngine;

public enum WeaponType { Melee, Ranged }

/// <summary>Create via Right-click > Create > Survival RPG > Weapon.</summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Survival RPG/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Fists";
    [TextArea] public string description;
    public Sprite icon;
    public WeaponType type = WeaponType.Melee;

    [Header("Common")]
    public int damage = 10;
    public float cooldown = 0.4f;
    public float staminaCost = 5f;
    public float knockback = 5f;
    [Tooltip("0 = never breaks")] public int maxDurability = 0;

    [Header("Melee")]
    public float attackRange = 0.9f;  // radius of hit circle
    public float reach = 0.7f;        // distance of circle from player

    [Header("Ranged")]
    public float range = 8f;
    public int startingAmmo = 12;
}