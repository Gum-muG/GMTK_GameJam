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

    private Vector3 wallHitNormal;
    private float wallStickForce = 30f;

    private int wallNum;

    [SerializeField] private float wallDetachDelay = 0.15f;
    private float wallDetachTimer;

    [SerializeField] private float wallJumpCoyoteTime = 0.15f;
    private float wallJumpCoyoteTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (playerMovement.wallRunning)
        {
            WallRun();
        }
    }

    private void OnDisable()
    {
        wallNum = 0;
        wallDetachTimer = 0f;
        wallJumpCoyoteTimer = 0f;

        exitingWall = false;
        wallRunTimer = 0f;
        exitWallTimer = 0f;

        if (playerMovement != null)
        {
            playerMovement.wallRunning = false;
        }

        if (rb != null)
        {
            rb.useGravity = true;
        }

        if (playerCamera != null)
        {
            playerCamera.EndWallRunClamp();
            playerCamera.ClearWallCameraOffset();
            playerCamera.FOV(70f);
            playerCamera.Tilt(0f);
        }
    }

    private void StateMachine()
    {
        xIn = Input.GetAxisRaw("Horizontal");
        yIn = Input.GetAxisRaw("Vertical");

        if (!playerMovement.wallRunning &&
            wallJumpCoyoteTimer > 0f)
        {
            wallJumpCoyoteTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) &&
            !playerMovement.isGrounded &&
            (playerMovement.wallRunning ||
             wallJumpCoyoteTimer > 0f))
        {
            if (playerMovement.wallRunning)
            {
                StopWallRun();
            }

            WallRunJump();

            wallJumpCoyoteTimer = 0f;

            return;
        }

        if (playerMovement.wallRunning)
        {
            wallRunTimer -= Time.deltaTime;

            if (wallNum <= 0)
            {
                wallDetachTimer += Time.deltaTime;

                if (wallDetachTimer >= wallDetachDelay)
                {
                    StopWallRun();

                    exitingWall = true;
                    exitWallTimer = exitWallTime;

                    return;
                }
            }
            else
            {
                wallDetachTimer = 0f;
            }

            if (wallRunTimer <= 0f)
            {
                StopWallRun();

                exitingWall = true;
                exitWallTimer = exitWallTime;

                return;
            }
        }
        else if (wallNum > 0 &&
                 yIn > 0f &&
                 !playerMovement.isGrounded &&
                 !exitingWall)
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
        wallDetachTimer = 0f;
        wallJumpCoyoteTimer = 0f;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z);

        playerCamera.BeginWallRunClamp(activeWallNormal);

        playerCamera.FOV(90f);

        bool wallIsOnRight =
            Vector3.Dot(activeWallNormal, forward.right) < 0f;

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

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z);

        Vector3 wallForward =
            Vector3.Cross(activeWallNormal, transform.up);

        if ((forward.forward - wallForward).magnitude >
            (forward.forward + wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        rb.AddForce(
            wallForward * wallRunForce,
            ForceMode.Force);

        rb.AddForce(
            -activeWallNormal * wallStickForce,
            ForceMode.Force);

        rb.AddForce(
            transform.up * gravityCounterforce,
            ForceMode.Force);
    }

    private void StopWallRun()
    {
        playerMovement.wallRunning = false;

        rb.useGravity = true;

        wallDetachTimer = 0f;
        wallJumpCoyoteTimer = wallJumpCoyoteTime;

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

        lookDirection.y =
            Mathf.Max(lookDirection.y, 0f);

        lookDirection.Normalize();

        Vector3 baseForce =
            transform.up * wallJumpUpForce +
            activeWallNormal * wallJumpSideForce;

        Vector3 lookForce =
            lookDirection * lookDirectionForce;

        Vector3 forceToApply =
            baseForce + lookForce;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z);

        rb.AddForce(
            forceToApply,
            ForceMode.Impulse);

        dashing.ResetDashCooldown();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != 7)
        {
            return;
        }

        wallNum++;
        wallDetachTimer = 0f;

        UpdateWallNormal(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer != 7)
        {
            return;
        }

        wallDetachTimer = 0f;

        UpdateWallNormal(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != 7)
        {
            return;
        }

        wallNum = Mathf.Max(0, wallNum - 1);

    }

    private void UpdateWallNormal(Collision collision)
    {
        if (collision.contactCount <= 0)
        {
            return;
        }

        wallHitNormal =
            collision.GetContact(0).normal.normalized;

        if (playerMovement != null &&
            playerMovement.wallRunning)
        {
            activeWallNormal = wallHitNormal;
        }
    }
}