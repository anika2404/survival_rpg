using UnityEngine;

public class TestDummy : MonoBehaviour, IDamageable
{
    public int health = 50;

    public void TakeDamage(int amount, Vector2 hitSource)
    {
        health -= amount;
        Debug.Log("Dummy hit! HP: " + health);
        if (health <= 0) Destroy(gameObject);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            var p = FindFirstObjectByType<PlayerHealth>();
            if (p != null) p.TakeDamage(20, transform.position);
        }
    }
}