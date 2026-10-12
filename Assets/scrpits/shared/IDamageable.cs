using UnityEngine;

/// <summary>
/// Shared contract: anything that can be hurt implements this.
/// Dhruv's enemies implement it so the player's attacks can hit them,
/// and Dhruv's enemy attacks call it on the player.
/// DO NOT change this signature without telling the team.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount, Vector2 hitSource);
}
