using UnityEngine;

public class WallRunning : MonoBehaviour
{
    public LayerMask Ground, Wall;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
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
    public PlayerCamera playerCamera;
    private PlayerMovement playerMovement;
    private Rigidbody rb;
    private bool touchingWall = false;
    private Vector3 wallHitNormal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        CheckForWall();
        if(touchingWall && Input.GetKey(KeyCode.Space) && playerMovement.wallRunning == false && !exitingWall)
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
        if(playerMovement.wallRunning)
            WallRun();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, forward.right, out rightWallHit, wallCheckDistance, Wall); 
        wallLeft = Physics.Raycast(transform.position, -forward.right, out leftWallHit, wallCheckDistance, Wall); 

    }

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
            if(!playerMovement.wallRunning)
                StartWallRun();
            if(wallRunTimer > 0) 
                wallRunTimer -= Time.deltaTime;
            if(wallRunTimer <= 0 && playerMovement.wallRunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }
            if(Input.GetKeyDown(KeyCode.Space)) WallRunJump();
        }
        else if (exitingWall)
        {
            if (playerMovement.wallRunning)
                StopWallRun();
            if(exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if(exitWallTimer <= 0)
            {
                exitingWall = false;
            }
        }
        else
        {
            if(playerMovement.wallRunning)
                StopWallRun();
        }

    }

    private void StartWallRun()
    {
        playerMovement.wallRunning = true;

        wallRunTimer = maxWallRunTime;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        

        playerCamera.FOV(90f);
        if(wallLeft) playerCamera.Tilt(-5f);
        if (wallRight) playerCamera.Tilt(5f);
    }
    private void WallRun()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((forward.forward - wallForward).magnitude > (forward.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if(!(wallLeft && xIn > 0) && !(wallRight && yIn < 0)) {
            rb.AddForce(-wallNormal*100, ForceMode.Force);
        }

        rb.AddForce(transform.up * gravityCounterforce, ForceMode.Force);
    }
    private void StopWallRun()
    {
        playerMovement.wallRunning = false;

        playerCamera.FOV(70f);
        playerCamera.Tilt(0f);
    }

    private void WallRunJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}
