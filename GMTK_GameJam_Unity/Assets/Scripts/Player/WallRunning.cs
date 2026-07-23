using UnityEngine;

public class WallRunning : MonoBehaviour
{
    public LayerMask Ground, Wall;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float lookDirectionForce;
    public float maxWallRunTime;
    private float wallRunTimer;
    public float gravityCounterforce;

    private float xIn, yIn;

    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit, rightWallHit;
    private bool wallLeft, wallRight;

    private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    public Transform forward;
    public Transform cameraTransform;
    public PlayerCamera playerCamera;
    private PlayerMovement playerMovement;
    private Rigidbody rb;
    private bool touchingWall = false;
    private Vector3 wallHitNormal;
    private CharacterControlState controlState;

    void Start()
    {
        //getting components
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        controlState = GetComponent<CharacterControlState>();
    }

    void Update()
    {
        if (!controlState.IsPlayerControlled)
        {
            return;
        }

        CheckForWall();

        if (touchingWall && Input.GetKey(KeyCode.Space) && playerMovement.wallRunning == false && !exitingWall)
        {
            Vector3 forceToApply = transform.up * wallJumpUpForce + wallHitNormal.normalized * wallJumpSideForce;
            rb.linearVelocity = Vector3.zero;
            touchingWall = false;
            rb.AddForce(forceToApply, ForceMode.Impulse);
        }

        StateMachine();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            touchingWall = true;
            wallHitNormal = collision.contacts[0].normal;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            touchingWall = false;
        }
    }

    void FixedUpdate()
    {
        if (!controlState.IsPlayerControlled)
            return;

        if (playerMovement.wallRunning)
            WallRun();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, Wall);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, Wall);
    }

    /*use similar to above method to raycast and check if there
    is an layer next to you that isn't a wall, get rid of movement towards it so you don't stick */

    private bool AboveMinHeight()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, Ground);
    }

    private void StateMachine()
    {
        xIn = Input.GetAxisRaw("Horizontal");
        yIn = Input.GetAxisRaw("Vertical");

        if ((wallLeft || wallRight) && yIn > 0 && AboveMinHeight() && !exitingWall)
        {
            if (!playerMovement.wallRunning)
                StartWallRun();

            if (wallRunTimer > 0)
                wallRunTimer -= Time.deltaTime;

            if (wallRunTimer <= 0 && playerMovement.wallRunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKeyDown(KeyCode.Space))
                WallRunJump();
        }
        else if (exitingWall)
        {
            if (playerMovement.wallRunning)
                StopWallRun();

            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0)
            {
                exitingWall = false;
            }
        }
        else
        {
            if (playerMovement.wallRunning)
                StopWallRun();
        }
    }

    private void StartWallRun()
    {
        playerCamera.BeginWallRunClamp();
        playerMovement.wallRunning = true;

        wallRunTimer = maxWallRunTime;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        playerCamera.FOV(90f);

        if (wallLeft)
            playerCamera.Tilt(-5f);

        if (wallRight)
            playerCamera.Tilt(5f);
    }

    private void WallRun()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((forward.forward - wallForward).magnitude > (forward.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        rb.AddForce(-wallNormal * 100f, ForceMode.Force);

        rb.AddForce(transform.up * gravityCounterforce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        playerCamera.EndWallRunClamp();
        playerMovement.wallRunning = false;
        rb.useGravity = true;

        playerCamera.FOV(70f);
        playerCamera.Tilt(0f);
    }

    private void WallRunJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 lookDirection = cameraTransform.forward;

        lookDirection.y = Mathf.Max(lookDirection.y, 0f);
        lookDirection.Normalize();

        Vector3 baseForce =
            transform.up * wallJumpUpForce +
            wallNormal.normalized * wallJumpSideForce;

        Vector3 lookForce =
            lookDirection * lookDirectionForce;

        Vector3 forceToApply =
            baseForce + lookForce;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}