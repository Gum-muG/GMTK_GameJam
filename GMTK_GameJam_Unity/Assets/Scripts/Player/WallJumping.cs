using UnityEngine;
using UnityEngine.InputSystem.XInput;

public class WallJumping : MonoBehaviour
{
    public float sphereCastRadius;
    public Transform forward;
    private RaycastHit frontWallHit;
    private bool wallFront;
    public float detectionLength;
    public float maxWallLookAngle;
    private float wallLookAngle;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    private float xIn, yIn;
    private Rigidbody rb;

    public LayerMask Wall;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        WallCheck();
        StateMachine();
    }

    private void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, forward.forward, out frontWallHit, detectionLength, Wall);
        wallLookAngle  = Vector3.Angle(forward.forward, -frontWallHit.normal);
    }

    private void StateMachine()
    {
        xIn = Input.GetAxisRaw("Horizontal");
        yIn = Input.GetAxisRaw("Vertical");
        if(wallFront && (xIn!=0||yIn!=0) && wallLookAngle < maxWallLookAngle)
        {
            
            if(Input.GetKey(KeyCode.Space))
            {
                
            }
        }
    }
}

