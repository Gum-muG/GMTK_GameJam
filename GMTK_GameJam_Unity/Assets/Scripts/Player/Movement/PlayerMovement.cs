using UnityEngine;

public class PlayerMovement : MonoBehaviour, IReplayObject
{
    private float speed;
    public float walkSpeed;
    public float sprintSpeed;
    public float wallRunSpeed;

    [Tooltip("Test")]
    public float dashSpeed;

    public float groundDrag;
    public float airDrag;

    public float jumpHeight;
    public float airControl;
    bool canJump = true;

    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    public float playerHeight;
    public LayerMask groundLayer;
    public bool isGrounded;

    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public Transform forward;

    float xIn, yIn;

    Vector3 moveDir;

    Rigidbody rb;

    public MovementState movementState;
    public float coyoteTime = .03f;

    private float coyoteTimeCounter;
    public float fallMultiplier = 2.5f;

    private Vector3 lastStoodPosition;

    public enum MovementState
    {
        WALKING,
        SPRINTING,
        WALL_RUNNING,
        AIR
    }

    public bool wallRunning;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"{name}: PlayerMovement requires a Rigidbody.");
            enabled = false;
            return;
        }

        rb.freezeRotation = true;
    }

    private void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            lastStoodPosition = transform.position;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        xIn = Input.GetAxisRaw("Horizontal");
        yIn = Input.GetAxisRaw("Vertical");

        if (wallRunning)
        {
            movementState = MovementState.WALL_RUNNING;
            speed = wallRunSpeed;
        }
        else if (isGrounded && Input.GetKey(sprintKey))
        {
            movementState = MovementState.SPRINTING;
            speed = sprintSpeed;
        }
        else if (isGrounded)
        {
            movementState = MovementState.WALKING;
            speed = walkSpeed;
        }
        else
        {
            movementState = MovementState.AIR;
        }

        if (Input.GetKeyDown(jumpKey) && canJump && (coyoteTimeCounter > 0f || isGrounded))
        {
            Jump();
            coyoteTimeCounter = 0f;
            Invoke(nameof(ResetJump), 0.1f);
        }

        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > speed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * speed;
            }
        }
        else
        {
            Vector3 baseVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (baseVel.magnitude > speed)
            {
                Vector3 normalizedVel = baseVel.normalized * speed;
                rb.linearVelocity = new Vector3(normalizedVel.x, rb.linearVelocity.y, normalizedVel.z);
            }
        }

        if (isGrounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    private void FixedUpdate()
    {
        moveDir = forward.forward * yIn + forward.right * xIn;

        if (wallRunning)
        {
            moveDir = Vector3.zero;
        }

        if (rb.linearVelocity.y < 0f && !isGrounded && !wallRunning)
        {
            rb.AddForce(Vector3.down * fallMultiplier, ForceMode.Acceleration);
        }

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeTargetMove() * speed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 30f, ForceMode.Force);
            }
        }
        else if (isGrounded)
        {
            rb.AddForce(moveDir.normalized * speed * 10, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDir.normalized * speed * 10 * airControl, ForceMode.Force);
        }

        if (!wallRunning)
        {
            rb.useGravity = !OnSlope();
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        canJump = false;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        canJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeTargetMove()
    {
        return Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
    }

    public void TeleportToLastStood()
    {
        Debug.Log("A");
        rb.position = lastStoodPosition;
    }

    public SnapshotInfo SaveSnapshot()
    {
        return new PlayerSnapshotInfo
        {
            id = GetId(),
            position = transform.position,
            rotation = transform.rotation,
            state = movementState
        };
    }

    public void LoadSnapshot(SnapshotInfo info)
    {
        PlayerSnapshotInfo playerSnapshot = info as PlayerSnapshotInfo;

        if (playerSnapshot == null)
        {
            return;
        }

        transform.SetPositionAndRotation(playerSnapshot.position, playerSnapshot.rotation);
        movementState = playerSnapshot.state;
    }

    public string GetId()
    {
        return name;
    }
}