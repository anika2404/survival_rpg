using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Defaults (overridden by the Role)")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float sneakMultiplier = 0.5f;
    [SerializeField] private float sprintStaminaPerSecond = 20f;

    [Header("Keys")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode sneakKey = KeyCode.LeftControl;

    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private PlayerStamina stamina;
    private Vector2 input;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;
    public bool CanMove { get; set; } = true;
    public bool IsMoving => input != Vector2.zero;
    public bool IsSprinting { get; private set; }
    public bool IsSneaking { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        stamina = GetComponent<PlayerStamina>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void ApplyRole(RoleData role)
    {
        moveSpeed = role.moveSpeed;
        sprintMultiplier = role.sprintMultiplier;
        sneakMultiplier = role.sneakMultiplier;
    }

    private void Update()
    {
        if (!CanMove)
        {
            input = Vector2.zero;
            IsSprinting = false;
            IsSneaking = false;
        }
        else
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (input != Vector2.zero) FacingDirection = input;

            bool wantsSprint = Input.GetKey(sprintKey) && IsMoving;
            bool hasStamina = stamina == null || stamina.Current > 5f;
            IsSprinting = wantsSprint && hasStamina;
            IsSneaking = !IsSprinting && Input.GetKey(sneakKey);

            if (IsSprinting && stamina != null) stamina.Drain(sprintStaminaPerSecond);
        }

        if (animator != null)
        {
            animator.SetFloat("MoveX", FacingDirection.x);
            animator.SetFloat("MoveY", FacingDirection.y);
            animator.SetFloat("Speed", input.sqrMagnitude);
        }
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;
        float speed = moveSpeed;
        if (IsSprinting) speed *= sprintMultiplier;
        else if (IsSneaking) speed *= sneakMultiplier;
        rb.linearVelocity = input * speed;   // Unity 2022 or older: rb.velocity
    }

    public void StopMoving()
    {
        input = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }
}
