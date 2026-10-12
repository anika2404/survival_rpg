using UnityEngine;

/// <summary>
/// Applies the chosen role to every player system and carries state between floors.
/// DARSH's role-select screen: call PlayerRoleManager.ChooseRole(roleData) once at game start.
/// </summary>
[RequireComponent(typeof(PlayerMovement), typeof(PlayerHealth), typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerStamina))]
public class PlayerRoleManager : MonoBehaviour
{
    [Tooltip("Used when no role was chosen (e.g. testing a floor directly)")]
    [SerializeField] private RoleData defaultRole;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public RoleData CurrentRole { get; private set; }

    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerAttack attack;
    private PlayerStamina stamina;
    private PlayerStealth stealth;
    private bool ready;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
        attack = GetComponent<PlayerAttack>();
        stamina = GetComponent<PlayerStamina>();
        stealth = GetComponent<PlayerStealth>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        RoleData role = PlayerState.Role != null ? PlayerState.Role : defaultRole;
        if (role == null)
        {
            Debug.LogError("PlayerRoleManager: no role chosen and no Default Role assigned.");
            return;
        }
        ApplyRole(role);
        RestoreState();
        ready = true;
    }

    /// <summary>Static helper for the role-select UI (before the player exists).</summary>
    public static void ChooseRole(RoleData role)
    {
        PlayerState.ResetProgress();
        PlayerState.Role = role;
    }

    private void ApplyRole(RoleData role)
    {
        CurrentRole = role;
        PlayerState.Role = role;

        health.Configure(role.maxHealth);
        stamina.Configure(role);
        movement.ApplyRole(role);
        attack.SetDamageMultiplier(role.damageMultiplier);
        if (stealth != null) stealth.Configure(role);

        if (attack.CurrentWeapon == null && role.startingWeapon != null)
            attack.EquipWeapon(role.startingWeapon);

        if (spriteRenderer != null) spriteRenderer.color = role.placeholderColor;
    }

    private void RestoreState()
    {
        if (PlayerState.Health >= 0) health.SetCurrent(PlayerState.Health);
        if (PlayerState.Stamina >= 0f) stamina.SetCurrent(PlayerState.Stamina);
        if (PlayerState.Weapon != null)
            attack.EquipWeapon(PlayerState.Weapon, PlayerState.WeaponDurability, PlayerState.Ammo);
    }

    public void SaveState()
    {
        if (!ready || health.IsDead) return;
        PlayerState.Health = health.CurrentHealth;
        PlayerState.Stamina = stamina.Current;
        PlayerState.Weapon = attack.CurrentWeapon;
        PlayerState.WeaponDurability = attack.CurrentDurability;
        PlayerState.Ammo = attack.CurrentAmmo;
    }

    // Runs automatically when a new floor scene loads.
    private void OnDestroy() => SaveState();
}
