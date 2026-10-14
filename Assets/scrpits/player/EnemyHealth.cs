
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 50;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount, Vector2 hitSource)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        Debug.Log($"{name} took {amount} damage! HP: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
        {
            IsDead = true;
            Debug.Log($"{name} died!");
            Destroy(gameObject);
        }
    }
}
