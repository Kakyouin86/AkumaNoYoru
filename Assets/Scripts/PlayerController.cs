using UnityEngine;
using Rewired;
using Unity.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D theRB;
    public SpriteRenderer theSR;
    public Animator theAnim;

    [Header("Colliders")]
    public Collider2D standCollider;
    public Collider2D crouchCollider;
    public Collider2D crawlCollider;

    [Header("Ground Check Settings")]
    public float groundCheckRadius = 2f;
    public Vector2 bottomOffset;
    public float groundCheckWidth = 0.0f;
    public LayerMask groundLayer;
    [ReadOnly] public bool isGrounded;

    [Header("Movement Settings")]
    public bool canMove = true;
    public float moveSpeed = 120f;
    public float crawlMoveSpeed = 80f;
    public bool facingRight = true;
    [ReadOnly] public float currentSpeed;
    [ReadOnly] public float verticalVelocity;

    [Header("Crouch & Crawl Settings")]
    public bool canCrouch = true;
    public bool canCrawl = false;
    [ReadOnly] public bool isCrouching;
    [ReadOnly] public bool isCrawling;

    [Header("Jump Settings")]
    public bool canJump = true;
    public float jumpForce = 200f;
    public int totalJumps = 2;
    [ReadOnly] public int availableJumps;
    [ReadOnly] public bool isJumping;

    [Header("Jump Feel")]
    public float fallMultiplier = 25;
    public float lowJumpMultiplier = 2f;

    [Header("Coyote Settings")]
    public float coyoteTime = 0.1f;
    [ReadOnly] public float coyoteTimeCounter;

    [Header("Attack Settings")]
    public bool canAttackIdle = true;
    public bool canAttackCrouch = true;
    [ReadOnly] public bool isAttacking;
    public string attackIdleAnimation = "Heroine - Attack";
    public string attackCrouchAnimation = "Heroine - Crouch Attack";

    private Player player;

    void Awake()
    {
        theRB = GetComponent<Rigidbody2D>();
        theSR = GetComponentInChildren<SpriteRenderer>();
        theAnim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        player = ReInput.players.GetPlayer(0);
        ResetJumps();
    }

    void Update()
    {
        if (!canMove) return;

        GroundCheck();
        Walk();
        Jump();
        Crouch();
        HandleAttack();
        UpdateFacing();
        UpdateAnimator();
        UpdateDebugVars();
        UpdateColliders();
    }

    // -------------------- MOVEMENT --------------------
    void Walk()
    {
        float moveInput = player.GetAxis("Move Horizontal");

        // Bloquear movimiento si está atacando
        if (isAttacking || (isCrouching && !canCrawl))
            moveInput = 0f;

        float speed = moveSpeed;
        if (isCrouching && canCrawl)
            speed = isCrawling ? crawlMoveSpeed : moveSpeed;

        theRB.linearVelocity = new Vector2(moveInput * speed, theRB.linearVelocity.y);
    }

    void Jump()
    {
        if (!canJump) return;

        // Bloquear salto si está atacando
        if (isAttacking)
            return;

        if (player.GetButtonDown("Jump"))
        {
            if (isGrounded || coyoteTimeCounter > 0f)
            {
                theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce);
                availableJumps = totalJumps - 1;
                coyoteTimeCounter = 0f;
            }
            else if (availableJumps > 0)
            {
                theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce);
                availableJumps--;
            }
        }

        if (player.GetButtonUp("Jump") && theRB.linearVelocity.y > 0)
            theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, theRB.linearVelocity.y * 0.5f);

        if (theRB.linearVelocity.y < 0)
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (theRB.linearVelocity.y > 0 && !player.GetButton("Jump"))
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }

    void Crouch()
    {
        if (!canCrouch) return;

        bool downPressed = player.GetButton("Crouch");
        float horizontalInput = player.GetAxis("Move Horizontal");

        if (downPressed && isGrounded)
        {
            isCrouching = true;
            isCrawling = (canCrawl && Mathf.Abs(horizontalInput) > 0.01f);
        }
        else
        {
            isCrouching = false;
            isCrawling = false;
        }
    }

    // -------------------- ATTACK --------------------
    void HandleAttack()
    {
        // Ataque Idle
        if (!isAttacking && !isCrouching && isGrounded && canAttackIdle && player.GetButtonDown("Attack"))
            StartAttack(attackIdleAnimation, "IsAttackingIdle");

        // Ataque Crouch
        if (!isAttacking && isCrouching && isGrounded && canAttackCrouch && player.GetButtonDown("Attack"))
            StartAttack(attackCrouchAnimation, "IsAttackingCrouch");

        // Revisar si termina la animación
        if (isAttacking)
        {
            AnimatorStateInfo state = theAnim.GetCurrentAnimatorStateInfo(0);
            if ((state.IsName(attackIdleAnimation) || state.IsName(attackCrouchAnimation)) && state.normalizedTime >= 1f)
                FinishAttack();
        }
    }

    void StartAttack(string animName, string animBool)
    {
        isAttacking = true;
        theAnim.SetBool(animBool, true);
        theRB.linearVelocity = new Vector2(0f, theRB.linearVelocity.y); // detener movimiento
    }

    void FinishAttack()
    {
        isAttacking = false;
        theAnim.SetBool("IsAttackingIdle", false);
        theAnim.SetBool("IsAttackingCrouch", false);
    }

    // -------------------- GROUND CHECK --------------------
    void GroundCheck()
    {
        Vector2 basePos = (Vector2)transform.position + bottomOffset;
        Vector2 left = basePos + Vector2.left * groundCheckWidth;
        Vector2 right = basePos + Vector2.right * groundCheckWidth;

        bool centerHit = Physics2D.OverlapCircle(basePos, groundCheckRadius, groundLayer);
        bool leftHit = Physics2D.OverlapCircle(left, groundCheckRadius, groundLayer);
        bool rightHit = Physics2D.OverlapCircle(right, groundCheckRadius, groundLayer);

        isGrounded = centerHit || leftHit || rightHit;

        if (isGrounded)
        {
            ResetJumps();
            coyoteTimeCounter = coyoteTime;
        }
        else
            coyoteTimeCounter -= Time.deltaTime;
    }

    void ResetJumps()
    {
        availableJumps = totalJumps;
    }

    // -------------------- FACING --------------------
    void UpdateFacing()
    {
        if (theRB.linearVelocity.x > 0) facingRight = true;
        else if (theRB.linearVelocity.x < 0) facingRight = false;

        if (theSR != null)
            theSR.flipX = !facingRight;
    }

    // -------------------- ANIMATOR --------------------
    void UpdateAnimator()
    {
        float horizontalSpeed = Mathf.Abs(theRB.linearVelocity.x);
        theAnim.SetFloat("Speed", horizontalSpeed);
        theAnim.SetFloat("VerticalVelocity", theRB.linearVelocity.y);
        theAnim.SetBool("IsGrounded", isGrounded);
        theAnim.SetBool("IsCrouching", isCrouching);
        theAnim.SetBool("IsCrawling", isCrawling);
        theAnim.SetBool("IsJumping", isJumping);
        theAnim.SetBool("IsAttackingIdle", theAnim.GetBool("IsAttackingIdle"));
        theAnim.SetBool("IsAttackingCrouch", theAnim.GetBool("IsAttackingCrouch"));
    }

    void UpdateDebugVars()
    {
        currentSpeed = Mathf.Abs(theRB.linearVelocity.x);
        verticalVelocity = theRB.linearVelocity.y;
        isJumping = !isGrounded && theRB.linearVelocity.y > 0;
        isCrawling = isCrouching && canCrawl && Mathf.Abs(player.GetAxis("Move Horizontal")) > 0.01f;
    }

    // -------------------- COLLIDER MANAGEMENT --------------------
    void UpdateColliders()
    {
        if (isAttacking)
        {
            if (theAnim.GetBool("IsAttackingIdle"))
            {
                standCollider.enabled = true;
                crouchCollider.enabled = false;
                crawlCollider.enabled = false;
                return;
            }
            if (theAnim.GetBool("IsAttackingCrouch"))
            {
                standCollider.enabled = false;
                crouchCollider.enabled = true;
                crawlCollider.enabled = false;
                return;
            }
        }

        standCollider.enabled = !isCrouching && !isCrawling;
        crouchCollider.enabled = isCrouching && !isCrawling;
        crawlCollider.enabled = isCrawling;
    }

    // -------------------- DEBUG --------------------
    void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;

        Vector2 basePos = (Vector2)transform.position + bottomOffset;
        Vector2 left = basePos + Vector2.left * groundCheckWidth;
        Vector2 right = basePos + Vector2.right * groundCheckWidth;

        Gizmos.DrawWireSphere(basePos, groundCheckRadius);
        Gizmos.DrawWireSphere(left, groundCheckRadius);
        Gizmos.DrawWireSphere(right, groundCheckRadius);
    }
}
