using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// All-in-one enemy: Patrol -> Detect -> Chase -> Attack, plus hurt and death.
/// Attach to an object that has: Rigidbody2D, a Collider2D, and the layer "enemy".
/// The player must have the tag "Player" (PlayerHealth + PlayerStealth are used if present).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    private enum State { Patrol, Chase, Attack, Hurt, Dead }

    [Header("Stats")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3.2f;

    [Header("Detection")]
    [Tooltip("How far the enemy can see. Multiplied by the player's stealth Visibility.")]
    [SerializeField] private float viewRange = 6f;
    [Tooltip("Seconds the enemy keeps chasing after losing the player.")]
    [SerializeField] private float loseSightTime = 3f;
    [Tooltip("Walls that block line of sight (usually Default).")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private bool canHearPlayer = true;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private int attackDamage = 10;
    [Tooltip("Delay between starting an attack and the hit landing (player can dodge).")]
    [SerializeField] private float attackWindup = 0.35f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Patrol")]
    [SerializeField] private float patrolRadius = 3f;
    [SerializeField] private float patrolWaitTime = 1.5f;

    [Header("Hurt / Death")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float hurtStunTime = 0.2f;
    [SerializeField] private float destroyDelay = 1f;

    [Header("Optional animation")]
    [Tooltip("Tick only when the Animator has: Speed, MoveX, MoveY (floats), Attack, Hurt, Die (triggers).")]
    [SerializeField] private bool useAnimator = false;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int CurrentHealth { get; private set; }
    public event Action<Enemy> OnDied;

    private State state = State.Patrol;
    private Rigidbody2D rb;

    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerStealth playerStealth;
    private float nextFindTime;

    private Vector2 startPos;
    private Vector2 patrolTarget;
    private float patrolWaitTimer;
    private float patrolMoveTimer;

    private Vector2 moveDir;
    private float currentSpeed;
    private float lastSeenTime;
    private float nextAttackTime;
    private Color baseColor = Color.white;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        startPos = transform.position;
        PickPatrolPoint();
        FindPlayer();
    }

    // ------------------------------------------------------------ main loop
    private void Update()
    {
        if (state == State.Dead) return;

        if ((player == null || playerHealth == null) && Time.time >= nextFindTime) FindPlayer();
        bool playerAlive = player != null && playerHealth != null && !playerHealth.IsDead;

        switch (state)
        {
            case State.Patrol: UpdatePatrol(playerAlive); break;
            case State.Chase:  UpdateChase(playerAlive);  break;
            // Attack and Hurt run as coroutines
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (state == State.Dead || state == State.Hurt) return;   // hurt = knockback owns the velocity
        rb.linearVelocity = moveDir * currentSpeed;               // Unity 2022 or older: rb.velocity
    }

    // ------------------------------------------------------------ patrol
    private void UpdatePatrol(bool playerAlive)
    {
        if (playerAlive && CanDetectPlayer())
        {
            EnterChase();
            return;
        }

        currentSpeed = patrolSpeed;
        patrolMoveTimer += Time.deltaTime;

        float dist = Vector2.Distance(transform.position, patrolTarget);
        if (dist < 0.2f)
        {
            moveDir = Vector2.zero;
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f) PickPatrolPoint();
        }
        else if (patrolMoveTimer > 4f)
        {
            PickPatrolPoint();   // probably stuck on a wall, choose another point
        }
        else
        {
            moveDir = (patrolTarget - (Vector2)transform.position).normalized;
        }
    }

    private void PickPatrolPoint()
    {
        patrolTarget = startPos + UnityEngine.Random.insideUnitCircle * patrolRadius;
        patrolWaitTimer = patrolWaitTime;
        patrolMoveTimer = 0f;
    }

    private void EnterPatrol()
    {
        state = State.Patrol;
        moveDir = Vector2.zero;
        PickPatrolPoint();
    }

    // ------------------------------------------------------------ chase
    private void EnterChase()
    {
        state = State.Chase;
        lastSeenTime = Time.time;
    }

    private void UpdateChase(bool playerAlive)
    {
        if (!playerAlive) { EnterPatrol(); return; }

        if (CanDetectPlayer()) lastSeenTime = Time.time;
        else if (Time.time - lastSeenTime > loseSightTime) { EnterPatrol(); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            moveDir = Vector2.zero;
            if (Time.time >= nextAttackTime) StartCoroutine(AttackRoutine());
        }
        else
        {
            currentSpeed = chaseSpeed;
            moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        }
    }

    // ------------------------------------------------------------ detection
    private bool CanDetectPlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        float visibility = playerStealth != null ? playerStealth.Visibility : 1f;

        bool seen = dist <= viewRange * visibility && HasLineOfSight();
        bool heard = canHearPlayer && playerStealth != null && playerStealth.CanBeHeardFrom(transform.position);
        return seen || heard;
    }

    private bool HasLineOfSight()
    {
        return !Physics2D.Linecast(transform.position, player.position, obstacleLayers);
    }

    private void FindPlayer()
    {
        nextFindTime = Time.time + 1f;
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        player = go.transform;
        playerHealth = go.GetComponent<PlayerHealth>();
        playerStealth = go.GetComponent<PlayerStealth>();
    }

    // ------------------------------------------------------------ attack
    private IEnumerator AttackRoutine()
    {
        state = State.Attack;
        moveDir = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        if (useAnimator) animator.SetTrigger("Attack");

        SetColor(Color.yellow);                       // telegraph: player sees the hit coming
        yield return new WaitForSeconds(attackWindup);
        SetColor(baseColor);

        bool stillInRange = player != null && playerHealth != null && !playerHealth.IsDead
                            && Vector2.Distance(transform.position, player.position) <= attackRange * 1.2f;
        if (stillInRange) playerHealth.TakeDamage(attackDamage, transform.position);

        nextAttackTime = Time.time + attackCooldown;
        yield return new WaitForSeconds(0.2f);

        if (state == State.Attack) EnterChase();
    }

    // ------------------------------------------------------------ damage and death
    /// <summary>Called by the player's attacks (IDamageable).</summary>
    public void TakeDamage(int amount, Vector2 hitSource)
    {
        if (state == State.Dead) return;

        CurrentHealth -= amount;
        if (CurrentHealth <= 0) { Die(); return; }

        StopAllCoroutines();                          // cancels an attack in progress
        StartCoroutine(HurtRoutine(hitSource));
    }

    private IEnumerator HurtRoutine(Vector2 hitSource)
    {
        state = State.Hurt;
        moveDir = Vector2.zero;
        if (useAnimator) animator.SetTrigger("Hurt");

        Vector2 dir = ((Vector2)transform.position - hitSource).normalized;
        rb.linearVelocity = dir * knockbackForce;
        SetColor(Color.red);

        yield return new WaitForSeconds(hurtStunTime);

        SetColor(baseColor);
        rb.linearVelocity = Vector2.zero;
        EnterChase();                                 // getting hit makes the enemy aggressive
    }

    private void Die()
    {
        state = State.Dead;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = false;
        if (useAnimator) animator.SetTrigger("Die");
        SetColor(new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f));

        OnDied?.Invoke(this);                         // drops/loot systems can listen to this
        Destroy(gameObject, destroyDelay);
    }

    // ------------------------------------------------------------ helpers
    private void SetColor(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
    }

    private void UpdateAnimator()
    {
        if (!useAnimator) return;
        animator.SetFloat("Speed", moveDir.sqrMagnitude);
        if (moveDir != Vector2.zero)
        {
            animator.SetFloat("MoveX", moveDir.x);
            animator.SetFloat("MoveY", moveDir.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)startPos : transform.position, patrolRadius);
    }
}
