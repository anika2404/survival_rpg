using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple death handling: wait, then restart the current floor.
/// Later Darsh's Game Over screen can replace/extend this.
/// The scene must be in File > Build Profiles > Scene List.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class DeathReloader : MonoBehaviour
{
    [SerializeField] private float delay = 2.5f;
    private PlayerHealth health;

    private void Awake() => health = GetComponent<PlayerHealth>();
    private void OnEnable() => health.OnDied += HandleDeath;
    private void OnDisable() => health.OnDied -= HandleDeath;

    private void HandleDeath() => StartCoroutine(Reload());

    private IEnumerator Reload()
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
