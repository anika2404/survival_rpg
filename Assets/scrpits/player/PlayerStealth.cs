using UnityEngine;

/// <summary>
/// DHRUV: enemies read these values to decide if they notice the player.
///   Visibility  : multiply your enemy's sight range by this (Scout ~0.5, sneaking lower).
///   NoiseRadius : enemy hears the player if within this distance.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerStealth : MonoBehaviour
{
    private PlayerMovement movement;
    private float roleVisibility = 1f;
    private float detectionRadius;

    public float Visibility { get; private set; } = 1f;
    public float NoiseRadius { get; private set; }

    /// <summary>Scout ability: radius in which the player can sense enemies.</summary>
    public float DetectionRadius => detectionRadius;

    private void Awake() => movement = GetComponent<PlayerMovement>();

    public void Configure(RoleData role)
    {
        roleVisibility = role.visibilityMultiplier;
        detectionRadius = role.detectionRadius;
    }

    private void Update()
    {
        float state = 1f;
        float noise = 3f;

        if (!movement.IsMoving)       { state = 0.6f; noise = 0f; }
        else if (movement.IsSprinting){ state = 1.3f; noise = 8f; }
        else if (movement.IsSneaking) { state = 0.4f; noise = 1f; }

        Visibility = state * roleVisibility;
        NoiseRadius = noise * roleVisibility;
    }

    public bool CanBeSeenFrom(Vector2 enemyPos, float enemyViewRange)
        => Vector2.Distance(enemyPos, transform.position) <= enemyViewRange * Visibility;

    public bool CanBeHeardFrom(Vector2 enemyPos)
        => Vector2.Distance(enemyPos, transform.position) <= NoiseRadius;
}
