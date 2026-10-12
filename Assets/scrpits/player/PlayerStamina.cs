using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float regenDelay = 1f;

    public float Max { get; private set; } = 100f;
    public float Current { get; private set; } = 100f;

    /// <summary>Darsh's stamina bar subscribes: (current, max).</summary>
    public event Action<float, float> OnStaminaChanged;

    private float regenPerSec = 15f;
    private float lastUseTime;

    public void Configure(RoleData role)
    {
        Max = role.maxStamina;
        regenPerSec = role.staminaRegenPerSec;
        Current = Max;
        Notify();
    }

    public void SetCurrent(float value)
    {
        Current = Mathf.Clamp(value, 0f, Max);
        Notify();
    }

    /// <summary>One-off cost (attacks). Returns false if not enough stamina.</summary>
    public bool TryUse(float amount)
    {
        if (amount <= 0f) return true;
        if (Current < amount) return false;
        Current -= amount;
        lastUseTime = Time.time;
        Notify();
        return true;
    }

    /// <summary>Continuous cost (sprinting). Call every frame.</summary>
    public void Drain(float perSecond)
    {
        Current = Mathf.Max(0f, Current - perSecond * Time.deltaTime);
        lastUseTime = Time.time;
        Notify();
    }

    private void Update()
    {
        if (Current >= Max || Time.time - lastUseTime < regenDelay) return;
        Current = Mathf.Min(Max, Current + regenPerSec * Time.deltaTime);
        Notify();
    }

    private void Notify() => OnStaminaChanged?.Invoke(Current, Max);
}
