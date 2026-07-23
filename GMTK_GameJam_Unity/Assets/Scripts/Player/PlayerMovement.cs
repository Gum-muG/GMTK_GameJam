using Unity.VisualScripting;
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
    private CharacterControlState controlState;

    public MovementState movementState;

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
        rb.freezeRotation = true;

        //getting components
        controlState = GetComponent<CharacterControlState>();

        ReplayManager.instance.Register(this);
    }

    private void Update()
    {
        if (!controlState.IsPlayerControlled){
        return;
        }

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);

        xIn = Input.GetAxisRaw("Horizontal");
        yIn = Input.GetAxisRaw("Vertical");

        if(wallRunning)
        {
            movementState = MovementState.WALL_RUNNING;
            speed = wallRunSpeed; 
        }

        else if(isGrounded && Input.GetKey(sprintKey))
        {
            movementState = MovementState.SPRINTING;
            speed = sprintSpeed;
        } else if (isGrounded)
        {
            movementState = MovementState.WALKING;
            speed = walkSpeed;
        } else
        {
            movementState = MovementState.AIR;
        }

        if(Input.GetKey(KeyCode.Space) && canJump && isGrounded)
        {
            Jump();
            Invoke(nameof(ResetJump), 0.1f);
        }

        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > speed)
                rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }

        else
        {
            Vector3 baseVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if(baseVel.magnitude > speed)
            {
                Vector3 normalizedVel = baseVel.normalized * speed;
                rb.linearVelocity = new Vector3(normalizedVel.x, rb.linearVelocity.y, normalizedVel.z);
            }
        }

        if (isGrounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

    }

    private void FixedUpdate()
    {
        if (!controlState.IsPlayerControlled)
        return;

        moveDir = forward.forward * yIn + forward.right * xIn;

        if (wallRunning){
            moveDir = Vector3.zero;
        }
        //use camera's rotation to set a look dir, use the look dir to apply force in that dir when jumping off wall
        //change moveDir when wallrunning to not include player rotation so looking away from wall doesn't move you off of wall(wallMoveDir)
        //get dash working on my end
        //have seperate wall layers, one for runnable walls, one for non-runnable walls
        //swapping over and systems in place for timeline
        //way to spawn in platforms and walls
        //way to shoot projectiles of some sort(hitscan or otherwise, we can decide later, maybe both)
        //dodge roll
        //push
        //shield
        //reset item
        
        
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeTargetMove() * speed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 30f, ForceMode.Force);
            }
        }

        else if(isGrounded)
            rb.AddForce(moveDir.normalized * speed * 10, ForceMode.Force);
        else 
            rb.AddForce(moveDir.normalized * speed * 10 * airControl, ForceMode.Force);

        if(!wallRunning) rb.useGravity = !OnSlope();
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

    public SnapshotInfo SaveSnapshot()
    {
        PlayerSnapshotInfo playerSnapshotInfo = new PlayerSnapshotInfo()
        {
            position = transform.position,
            rotation = forward.rotation,
            state = movementState
        };
        return playerSnapshotInfo;
    }

    public void LoadSnapshot(SnapshotInfo info)
    {
        throw new System.NotImplementedException();
    }
}
