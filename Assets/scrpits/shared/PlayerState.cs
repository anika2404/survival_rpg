using UnityEngine;

/// <summary>
/// Survives scene changes so damage and resource use carry between floors.
/// Level designer: just load the next scene. PlayerRoleManager saves/restores automatically.
/// </summary>
public static class PlayerState
{
    public static RoleData Role;
    public static int Health = -1;       // -1 = no saved value (use full)
    public static float Stamina = -1f;
    public static WeaponData Weapon;
    public static int WeaponDurability = -1;
    public static int Ammo = -1;
    public static int CurrentFloor = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Role = null;
        CurrentFloor = 1;
        ResetProgress();
    }

    /// <summary>Clears carried-over health/items but keeps the chosen role.</summary>
    public static void ResetProgress()
    {
        Health = -1;
        Stamina = -1f;
        Weapon = null;
        WeaponDurability = -1;
        Ammo = -1;
    }
}
