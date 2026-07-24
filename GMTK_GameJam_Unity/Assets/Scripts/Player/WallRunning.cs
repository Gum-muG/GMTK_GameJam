using UnityEngine;

public class WallRunning : MonoBehaviour
{
    public LayerMask Ground;

    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float lookDirectionForce;
    public float maxWallRunTime;
    private float wallRunTimer;
    public float gravityCounterforce;

    private float xIn, yIn;

    public float minJumpHeight;

    private Vector3 activeWallNormal;

    private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    public Transform forward;
    public Transform cameraTransform;
    public PlayerCamera playerCamera;
    public Dashing dashing;

    private PlayerMovement playerMovement;
    private Rigidbody rb;
    private CharacterControlState controlState;

    private bool touchingWall;
    private Vector3 wallHitNormal;
    float wallStickForce = 30f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        controlState = GetComponent<CharacterControlState>();
    }

    private void Update()
    {
        if (!controlState.IsPlayerControlled)
            return;

        StateMachine();
    }

    private void FixedUpdate()
    {
        if (!controlState.IsPlayerControlled)
            return;

        if (playerMovement.wallRunning)
        {
            WallRun();
        }
    }

    private void StateMachine()
    {
        xIn = Input.GetAxisRaw("Horizontal");
        yIn = Input.GetAxisRaw("Vertical");

        if (playerMovement.wallRunning)
        {
            wallRunTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopWallRun();
                WallRunJump();
                return;
            }

            if (!touchingWall)
            {
                StopWallRun();

                exitingWall = true;
                exitWallTimer = exitWallTime;

                return;
            }

            if (wallRunTimer <= 0f)
            {
                StopWallRun();

                exitingWall = true;
                exitWallTimer = exitWallTime;

                return;
            }
        }
        else if (touchingWall && yIn > 0f && !playerMovement.isGrounded && !exitingWall)
        {
            StartWallRun();
        }

        if (exitingWall)
        {
            exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0f)
            {
                exitingWall = false;
            }
        }
    }

    private void StartWallRun()
    {
        activeWallNormal = wallHitNormal.normalized;
        playerCamera.SetWallCameraOffset(activeWallNormal);

        playerMovement.wallRunning = true;
        wallRunTimer = maxWallRunTime;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        playerCamera.BeginWallRunClamp(activeWallNormal);

        playerCamera.FOV(90f);

        bool wallIsOnRight = Vector3.Dot(activeWallNormal, forward.right) < 0f;

        if (wallIsOnRight)
        {
            playerCamera.Tilt(5f);
        }
        else
        {
            playerCamera.Tilt(-5f);
        }
    }

    private void WallRun()
    {
        rb.useGravity = false;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 wallForward = Vector3.Cross(activeWallNormal, transform.up);

        if ((forward.forward - wallForward).magnitude > (forward.forward + wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        rb.AddForce(-activeWallNormal * wallStickForce, ForceMode.Force);

        rb.AddForce(transform.up * gravityCounterforce, ForceMode.Force);
    }

    private void StopWallRun()
    {
        playerMovement.wallRunning = false;
        rb.useGravity = true;

        playerCamera.EndWallRunClamp();
        playerCamera.ClearWallCameraOffset();

        playerCamera.FOV(70f);
        playerCamera.Tilt(0f);
    }

    private void WallRunJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 lookDirection = cameraTransform.forward;

        lookDirection.y = Mathf.Max(lookDirection.y, 0f);

        lookDirection.Normalize();

        Vector3 baseForce = transform.up * wallJumpUpForce + activeWallNormal * wallJumpSideForce;

        Vector3 lookForce = lookDirection * lookDirectionForce;

        Vector3 forceToApply = baseForce + lookForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(forceToApply, ForceMode.Impulse);
        dashing.ResetDashCooldown();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            touchingWall = true;
            wallHitNormal = collision.contacts[0].normal;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            touchingWall = true;
            wallHitNormal = collision.contacts[0].normal;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == 7)
        {
            touchingWall = false;
        }
    }
}