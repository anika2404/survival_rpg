
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWander : MonoBehaviour
{
    [Header("Map Area")]
    [SerializeField] private BoxCollider2D mapArea;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float wallCheckRadius = 0.2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float arrivalDistance = 0.15f;
    [SerializeField] private float minPause = 0.3f;
    [SerializeField] private float maxPause = 1.2f;
    [SerializeField] private float destinationTimeout = 5f;
    [SerializeField] private int attempts = 30;

    private Rigidbody2D rb;
    private Vector2 destination;
    private float timer;
    private bool moving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ChooseDestination();
    }

    private void Update()
    {
        if (mapArea == null) return;

        if (moving)
        {
            timer -= Time.deltaTime;

            if (Vector2.Distance(rb.position, destination) <= arrivalDistance ||
                timer <= 0f)
            {
                StopAndPause();
            }
        }
        else
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
                ChooseDestination();
        }
    }

    private void FixedUpdate()
    {
        if (!moving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (destination - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void ChooseDestination()
    {
        if (mapArea == null)
        {
            moving = false;
            Debug.LogWarning(name + ": Assign the map area's BoxCollider2D.", this);
            return;
        }

        Bounds bounds = mapArea.bounds;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 point = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            // Don't choose a point inside a wall.
            if (Physics2D.OverlapCircle(point, wallCheckRadius, wallLayers))
                continue;

            if (Vector2.Distance(point, rb.position) < 1f)
                continue;

            destination = point;
            timer = destinationTimeout;
            moving = true;
            return;
        }

        // Try again after a short pause if no suitable point was found.
        StopAndPause();
    }

    private void StopAndPause()
    {
        moving = false;
        rb.linearVelocity = Vector2.zero;
        timer = Random.Range(minPause, maxPause);
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (mapArea == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(mapArea.bounds.center, mapArea.bounds.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(destination, 0.12f);
    }
}
