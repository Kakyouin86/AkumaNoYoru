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
    public int availableJumps;
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
    public bool canAttackJump = true;
    [ReadOnly] public bool isAttackingIdle;
    [ReadOnly] public bool isAttackingCrouch;
    [ReadOnly] public bool isAttackingJump;
    public string attackIdleAnimation = "Heroine - Attack";
    public string attackCrouchAnimation = "Heroine - Crouch Attack";
    public string attackJumpAnimation = "Heroine - Jump Attack";


    private Player player;

    // -------------------- UNITY EVENTS --------------------
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

        if (isAttackingIdle || isAttackingCrouch || !canMove)
            moveInput = 0f;

        if (isCrouching && !canCrawl)
            moveInput = 0f;

        float speed = moveSpeed;

        if (isCrouching && canCrawl)
            speed = isCrawling ? crawlMoveSpeed : moveSpeed;

        theRB.linearVelocity = new Vector2(moveInput * speed, theRB.linearVelocity.y);
    }

    void Jump()
    {
        if (!canJump) return;

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
        if (!isAttackingIdle && !isCrouching && isGrounded && canAttackIdle && player.GetButtonDown("Attack"))
            StartAttack(ref isAttackingIdle, "IsAttackingIdle", attackIdleAnimation, true);

        // Ataque Crouch
        if (!isAttackingCrouch && isCrouching && isGrounded && canAttackCrouch && player.GetButtonDown("Attack"))
            StartAttack(ref isAttackingCrouch, "IsAttackingCrouch", attackCrouchAnimation, true);

        // Check finish attacks
        CheckAttackFinish(ref isAttackingIdle, attackIdleAnimation, "IsAttackingIdle");
        CheckAttackFinish(ref isAttackingCrouch, attackCrouchAnimation, "IsAttackingCrouch");
    }

    void StartAttack(ref bool attackState, string animBool, string animName, bool stopMovement)
    {
        attackState = true;
        theAnim.SetBool(animBool, true);
        if (stopMovement) theRB.linearVelocity = new Vector2(0f, theRB.linearVelocity.y);
    }

    void CheckAttackFinish(ref bool attackState, string animName, string animBool)
    {
        AnimatorStateInfo state = theAnim.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(animName) && state.normalizedTime >= 1f)
            FinishAttack(ref attackState, animBool);
    }

    void FinishAttack(ref bool attackState, string animBool)
    {
        attackState = false;
        theAnim.SetBool(animBool, false);
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
        theAnim.SetBool("IsAttackingIdle", isAttackingIdle);
        theAnim.SetBool("IsAttackingCrouch", isAttackingCrouch);
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
        if (isAttackingIdle)
        {
            if (standCollider != null) standCollider.enabled = true;
            if (crouchCollider != null) crouchCollider.enabled = false;
            if (crawlCollider != null) crawlCollider.enabled = false;
            return;
        }

        if (isAttackingCrouch)
        {
            if (standCollider != null) standCollider.enabled = false;
            if (crouchCollider != null) crouchCollider.enabled = true;
            if (crawlCollider != null) crawlCollider.enabled = false;
            return;
        }

        if (standCollider != null) standCollider.enabled = !isCrouching && !isCrawling;
        if (crouchCollider != null) crouchCollider.enabled = isCrouching && !isCrawling;
        if (crawlCollider != null) crawlCollider.enabled = isCrawling;
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
