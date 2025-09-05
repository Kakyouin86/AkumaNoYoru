using UnityEngine;
using Rewired;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float moveSpeedModifier = 1.5f;
    public bool canMove = true;

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public int totalJumps = 2;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Components")]
    public Rigidbody2D theRB;
    public SpriteRenderer theSR;

    [Header("Jump Feel")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("State")]
    public bool facingRight = true;
    public bool isGrounded;
    public int availableJumps;

    private Player player;

    void Awake()
    {
        // Catch de componentes
        if (theRB == null)
            theRB = GetComponent<Rigidbody2D>();

        if (theSR == null)
            theSR = GetComponentInChildren<SpriteRenderer>();

        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.parent = transform;
            if (theSR != null)
                groundCheck.localPosition = new Vector3(0, -theSR.bounds.extents.y, 0);
            else
                groundCheck.localPosition = Vector3.down * 0.5f;
        }
    }

    void Start()
    {
        player = Rewired.ReInput.players.GetPlayer(0);
        ResetJumps();
    }

    void Update()
    {
        if (!canMove) return;

        CheckGround();
        Walk();
        Jump();
        UpdateFacing();
    }

    void Walk()
    {
        float moveInput = player.GetAxis("Move Horizontal");
        float currentSpeed = player.GetButton("Run") ? moveSpeed * moveSpeedModifier : moveSpeed;

        theRB.linearVelocity = new Vector2(moveInput * currentSpeed, theRB.linearVelocity.y);
    }

    void Jump()
    {
        if (player.GetButtonDown("Jump"))
        {
            if (isGrounded || availableJumps > 0)
            {
                if (!isGrounded) availableJumps--;
                theRB.linearVelocity = Vector2.up * jumpForce;
            }
        }

        if (player.GetButtonUp("Jump") && theRB.linearVelocity.y > 0)
        {
            theRB.linearVelocity = new Vector2(theRB.linearVelocity.x, theRB.linearVelocity.y * 0.5f);
        }

        // Mejora caída
        if (theRB.linearVelocity.y < 0)
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (theRB.linearVelocity.y > 0 && !player.GetButton("Jump"))
            theRB.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded) ResetJumps();
        Debug.DrawRay(groundCheck.position, Vector3.down * groundCheckRadius, isGrounded ? Color.green : Color.red);
    }

    void ResetJumps()
    {
        availableJumps = totalJumps;
    }

    void UpdateFacing()
    {
        if (theRB.linearVelocity.x > 0) facingRight = true;
        else if (theRB.linearVelocity.x < 0) facingRight = false;

        if (theSR != null) theSR.flipX = !facingRight;
    }
}
