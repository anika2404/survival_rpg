using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("Walls etc. that block ranged shots")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private Animator animator;
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private bool debugLogs = true;

    private PlayerMovement movement;
    private PlayerStamina stamina;
    private float nextAttackTime;
    private float damageMultiplier = 1f;

    public WeaponData CurrentWeapon => currentWeapon;
    public int CurrentDurability { get; private set; }
    public int CurrentAmmo { get; private set; }
    public bool IsBroken => currentWeapon != null && currentWeapon.maxDurability > 0 && CurrentDurability <= 0;

    /// <summary>Darsh's UI listens to refresh weapon icon / ammo.</summary>
    public event System.Action OnWeaponChanged;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        stamina = GetComponent<PlayerStamina>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (currentWeapon != null) EquipWeapon(currentWeapon);
    }

    public void SetDamageMultiplier(float value) => damageMultiplier = value;

    /// <summary>Inventory calls this. Pass -1 to use the weapon's default durability/ammo.</summary>
    public void EquipWeapon(WeaponData weapon, int durability = -1, int ammo = -1)
    {
        currentWeapon = weapon;
        CurrentDurability = durability >= 0 ? durability : (weapon != null ? weapon.maxDurability : 0);
        CurrentAmmo = ammo >= 0 ? ammo : (weapon != null ? weapon.startingAmmo : 0);
        OnWeaponChanged?.Invoke();
    }

    public void AddAmmo(int amount)
    {
        CurrentAmmo += amount;
        OnWeaponChanged?.Invoke();
    }

    /// <summary>Repair system calls this. efficiency comes from RoleData.repairEfficiency.</summary>
    public void Repair(int amount, float efficiency = 1f)
    {
        if (currentWeapon == null || currentWeapon.maxDurability <= 0) return;
        CurrentDurability = Mathf.Min(currentWeapon.maxDurability,
                                      CurrentDurability + Mathf.RoundToInt(amount * efficiency));
        OnWeaponChanged?.Invoke();
    }

    private void Update()
    {
        if (currentWeapon == null) return;
        bool pressed = Input.GetKeyDown(attackKey) || Input.GetMouseButtonDown(0);
        if (pressed && Time.time >= nextAttackTime) TryAttack();
    }

    private void TryAttack()
    {
        if (IsBroken) { if (debugLogs) Debug.Log("Weapon is broken - repair it."); return; }
        if (currentWeapon.type == WeaponType.Ranged && CurrentAmmo <= 0)
        {
            if (debugLogs) Debug.Log("Out of ammo.");
            return;
        }
        if (stamina != null && !stamina.TryUse(currentWeapon.staminaCost))
        {
            if (debugLogs) Debug.Log("Not enough stamina.");
            return;
        }

        nextAttackTime = Time.time + currentWeapon.cooldown;
        if (animator != null) animator.SetTrigger("Attack");

        if (currentWeapon.type == WeaponType.Melee) DealMelee();
        else { DealRanged(); CurrentAmmo--; }

        if (currentWeapon.maxDurability > 0) CurrentDurability--;
        OnWeaponChanged?.Invoke();
    }

    private int Damage => Mathf.RoundToInt(currentWeapon.damage * damageMultiplier);

    private void DealMelee()
    {
        Vector2 center = (Vector2)transform.position + movement.FacingDirection * currentWeapon.reach;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, currentWeapon.attackRange, enemyLayers);
        if (debugLogs) Debug.Log("Melee at " + center + " | colliders: " + hits.Length);

        var alreadyHit = new HashSet<IDamageable>();
        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                if (debugLogs) Debug.LogWarning("Hit '" + hit.name + "' but it has no IDamageable script.");
                continue;
            }
            if (!alreadyHit.Add(target)) continue;
            target.TakeDamage(Damage, transform.position);
        }
    }

    private void DealRanged()
    {
        Vector2 dir = movement.FacingDirection;
        Vector2 origin = (Vector2)transform.position + dir * 0.5f;
        LayerMask mask = enemyLayers | obstacleLayers;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, currentWeapon.range, mask);
        Debug.DrawRay(origin, dir * currentWeapon.range, Color.yellow, 0.3f);

        if (hit.collider == null) return;
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null) target.TakeDamage(Damage, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentWeapon == null || currentWeapon.type != WeaponType.Melee) return;
        Vector2 facing = movement != null ? movement.FacingDirection : Vector2.down;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + facing * currentWeapon.reach, currentWeapon.attackRange);
    }
}
