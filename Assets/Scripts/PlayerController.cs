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
    public bool canCrawl = true;
    [ReadOnly] public bool isCrouching;
    [ReadOnly] public bool isCrawling;

    [Header("Jump Settings")]
    public bool canJump = true;
    public float jumpForce = 200f;
    public int totalJumps = 2;
    [ReadOnly] public int availableJumps;
    [ReadOnly] public bool isJumping;

    [Header("Jump Feel")]
    public float fallMultiplier = 25f;
    public float lowJumpMultiplier = 2f;

    [Header("Coyote Settings")]
    public float coyoteTime = 0.1f;
    [ReadOnly] public float coyoteTimeCounter;

    [Header("Attack Settings")]
    public bool canAttackIdle = true;
    public bool canAttackCrouch = true;
    public bool canAttackJump = true;
    public bool canAttackJumpDown = true;
    public float jumpDownSpeed = -300f;

    [ReadOnly] public bool isAttackingIdle;
    [ReadOnly] public bool isAttackingCrouch;
    [ReadOnly] public bool isAttackingJump;
    [ReadOnly] public bool isAttackingJumpDown;

    public string attackIdleAnimation = "Heroine - Attack";
    public string attackCrouchAnimation = "Heroine - Crouch Attack";
    public string attackJumpAnimation = "Heroine - Jump Attack";
    //public string attackJumpDownAnimation = "Heroine - Jump Down Attack";

    // -------------------- ENUMS --------------------
    public enum JumpAttackDirection { Up, Down, Both }
    public enum JumpDownAttackDirection { Up, Down, Both }
    public enum AirAttackMovement { Freeze, Free }

    [Header("Jump Attack Settings")]
    public JumpAttackDirection jumpAttackDirection = JumpAttackDirection.Both;
    public AirAttackMovement jumpAttackMovement = AirAttackMovement.Freeze;

    [Header("Jump Down Attack Settings")]
    public JumpDownAttackDirection jumpDownAttackDirection = JumpDownAttackDirection.Both;
    public AirAttackMovement jumpDownAttackMovement = AirAttackMovement.Free;

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
        Jump();
        Walk();
        Crouch();
        HandleAttack();
        UpdateFacing();
        UpdateAnimator();
        UpdateDebugVars();
        UpdateColliders();
    }

    // -------------------- MOVEMENT --------------------
    public void Walk()
    {
        float moveInput = player.GetAxis("Move Horizontal");

        if ((isAttackingIdle || isAttackingCrouch) ||
            (isAttackingJump && jumpAttackMovement == AirAttackMovement.Freeze) ||
            (isAttackingJumpDown && jumpDownAttackMovement == AirAttackMovement.Freeze))
        {
            moveInput = 0f;
        }

        float speed = moveSpeed;
        if (isCrouching && canCrawl)
            speed = isCrawling ? crawlMoveSpeed : moveSpeed;

        theRB.linearVelocity = new Vector2(moveInput * speed, theRB.linearVelocity.y);
    }

    public void Jump()
    {
        if (!canJump || isAttackingJumpDown) return;

        if (player.GetButtonDown("Jump"))
        {
            if (isGrounded || coyoteTimeCounter > 0f)
            {
                theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, jumpForce);
                availableJumps = totalJumps - 1;
                coyoteTimeCounter = 0f;

                if (isAttackingIdle) FinishAttackIdle();
                if (isAttackingCrouch) FinishAttackCrouch();
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

    public void Crouch()
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
    public void HandleAttack()
    {
        if (player.GetButtonDown("Jump"))
            return;

        // -------------------- ATAQUES EN AIRE --------------------
        // Ataque Jump Down (↓ + Attack) PRIORIDAD ALTA
        if (canAttackJumpDown && !isAttackingJumpDown && !isGrounded && player.GetButton("Crouch") && player.GetButtonDown("Attack"))
        {
            bool canPerform = false;

            switch (jumpDownAttackDirection)
            {
                case JumpDownAttackDirection.Up: canPerform = theRB.linearVelocity.y > 0f; break;
                case JumpDownAttackDirection.Down: canPerform = theRB.linearVelocity.y <= 0f; break;
                case JumpDownAttackDirection.Both: canPerform = true; break;
            }

            if (canPerform)
            {
                StartAttackJumpDown();
                return; // Prioridad alta, no ejecutar otros ataques
            }
        }

        // Ataque Jump normal (aire)
        if (canAttackJump && !isAttackingJump && !isAttackingJumpDown && !isGrounded && player.GetButtonDown("Attack"))
        {
            bool canPerform = false;

            switch (jumpAttackDirection)
            {
                case JumpAttackDirection.Up: canPerform = theRB.linearVelocity.y > 0f; break;
                case JumpAttackDirection.Down: canPerform = theRB.linearVelocity.y <= 0f; break;
                case JumpAttackDirection.Both: canPerform = true; break;
            }

            if (canPerform)
            {
                StartAttackJump();
                return;
            }
        }

        // -------------------- ATAQUES EN SUELO --------------------
        // Ataque Crouch
        if (canAttackCrouch && !isAttackingCrouch && isCrouching && isGrounded && player.GetButtonDown("Attack"))
        {
            StartAttackCrouch();
            return;
        }

        // Ataque Idle
        if (canAttackIdle && !isAttackingIdle && !isCrouching && isGrounded && player.GetButtonDown("Attack"))
        {
            StartAttackIdle();
            return;
        }

        // -------------------- REVISAR FIN DE ANIMACIONES --------------------
        if (isAttackingIdle) CheckAttackEnd(attackIdleAnimation, FinishAttackIdle);
        if (isAttackingCrouch) CheckAttackEnd(attackCrouchAnimation, FinishAttackCrouch);
        if (isAttackingJump) CheckAttackEnd(attackJumpAnimation, FinishAttackJump);
        //if (isAttackingJumpDown) CheckAttackEnd(attackJumpDownAnimation, FinishAttackJumpDown);
    }

    public void CheckAttackEnd(string animName, System.Action finishMethod)
    {
        AnimatorStateInfo state = theAnim.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(animName) && state.normalizedTime >= 1f)
            finishMethod.Invoke();
    }

    // -------------------- ATTACK METHODS --------------------
    public void StartAttackIdle()
    {
        isAttackingIdle = true;
        theAnim.SetBool("IsAttackingIdle", true);
        theRB.linearVelocity = new Vector2(0f, theRB.linearVelocity.y);
    }

    public void FinishAttackIdle()
    {
        isAttackingIdle = false;
        theAnim.SetBool("IsAttackingIdle", false);
    }

    public void StartAttackCrouch()
    {
        isAttackingCrouch = true;
        theAnim.SetBool("IsAttackingCrouch", true);
        theRB.linearVelocity = new Vector2(0f, theRB.linearVelocity.y);
    }

    public void FinishAttackCrouch()
    {
        isAttackingCrouch = false;
        theAnim.SetBool("IsAttackingCrouch", false);
    }

    public void StartAttackJump()
    {
        isAttackingJump = true;
        theAnim.SetBool("IsAttackingJump", true);

        if (jumpAttackMovement == AirAttackMovement.Freeze)
            theRB.linearVelocity = new Vector2(0f, theRB.linearVelocity.y);
    }

    public void FinishAttackJump()
    {
        isAttackingJump = false;
        theAnim.SetBool("IsAttackingJump", false);
    }

    public void StartAttackJumpDown()
    {
        isAttackingJumpDown = true;
        theAnim.SetBool("IsAttackingJumpDown", true);
        theRB.linearVelocity = new Vector2(0f, jumpDownSpeed);
    }

    public void FinishAttackJumpDown()
    {
        isAttackingJumpDown = false;
        theAnim.SetBool("IsAttackingJumpDown", false);
    }

    // -------------------- GROUND CHECK --------------------
    public void GroundCheck()
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

            if (isAttackingJumpDown) FinishAttackJumpDown();
            if (isAttackingJump) FinishAttackJump();
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;

            if (isAttackingJumpDown)
                theRB.linearVelocity = new Vector2(0f, jumpDownSpeed);
        }
    }

    public void ResetJumps()
    {
        availableJumps = totalJumps;
    }

    // -------------------- FACING --------------------
    public void UpdateFacing()
    {
        if (theRB.linearVelocity.x > 0) facingRight = true;
        else if (theRB.linearVelocity.x < 0) facingRight = false;

        if (theSR != null)
            theSR.flipX = !facingRight;
    }

    // -------------------- ANIMATOR --------------------
    public void UpdateAnimator()
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
        theAnim.SetBool("IsAttackingJump", isAttackingJump);
        theAnim.SetBool("IsAttackingJumpDown", isAttackingJumpDown);
    }

    public void UpdateDebugVars()
    {
        currentSpeed = Mathf.Abs(theRB.linearVelocity.x);
        verticalVelocity = theRB.linearVelocity.y;
        isJumping = !isGrounded && theRB.linearVelocity.y > 0;
        isCrawling = isCrouching && canCrawl && Mathf.Abs(player.GetAxis("Move Horizontal")) > 0.01f;
    }

    // -------------------- COLLIDER MANAGEMENT --------------------
    public void UpdateColliders()
    {
        if (isAttackingIdle)
        {
            standCollider.enabled = true;
            crouchCollider.enabled = false;
            crawlCollider.enabled = false;
            return;
        }
        if (isAttackingCrouch)
        {
            standCollider.enabled = false;
            crouchCollider.enabled = true;
            crawlCollider.enabled = false;
            return;
        }

        standCollider.enabled = !isCrouching && !isCrawling;
        crouchCollider.enabled = isCrouching && !isCrawling;
        crawlCollider.enabled = isCrawling;
    }

    // -------------------- DEBUG --------------------
    public void OnDrawGizmos()
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
