using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityTime = 0.6f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackTime = 0.15f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;   // (current, max) for the health bar
    public event Action OnDied;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private bool invincible;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        CurrentHealth = maxHealth;
    }

    private void Start() => Notify();

    /// <summary>Called by PlayerRoleManager when a role is applied.</summary>
    public void Configure(int newMax)
    {
        maxHealth = newMax;
        CurrentHealth = newMax;
        Notify();
    }

    /// <summary>Used when restoring health carried over from the previous floor.</summary>
    public void SetCurrent(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 1, maxHealth);
        Notify();
    }

    public void TakeDamage(int amount, Vector2 hitSource)
    {
        if (IsDead || invincible) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Notify();

        if (CurrentHealth <= 0) { Die(); return; }

        if (animator != null) animator.SetTrigger("Hurt");
        StartCoroutine(HitRoutine(hitSource));
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        Notify();
    }

    private IEnumerator HitRoutine(Vector2 hitSource)
    {
        invincible = true;
        movement.CanMove = false;
        Vector2 dir = ((Vector2)transform.position - hitSource).normalized;
        rb.linearVelocity = dir * knockbackForce;
        yield return new WaitForSeconds(knockbackTime);
        rb.linearVelocity = Vector2.zero;
        movement.CanMove = true;

        float t = 0f;
        while (t < invincibilityTime)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.08f);
            t += 0.08f;
        }
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        invincible = false;
    }

    private void Die()
    {
        IsDead = true;
        movement.CanMove = false;
        movement.StopMoving();
        if (attack != null) attack.enabled = false;
        if (animator != null) animator.SetTrigger("Die");
        PlayerState.ResetProgress();   // dying wipes carried-over damage/items, keeps the role
        OnDied?.Invoke();
    }

    private void Notify() => OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
}
