using UnityEngine;

public class Dashing : MonoBehaviour
{
    public PlayerCamera playerCamera;
    public Transform forward;

    private PlayerMovement playerMovement;
    private Rigidbody rb;

    public float dashTime;

    private float dashTimer;
    private bool canDash = true;
    private bool isDashing;

    public bool IsDashing => isDashing;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        dashTimer = dashTime;
    }

    private void Update()
    {
        if (canDash && Input.GetKeyDown(KeyCode.LeftControl))
        {
            canDash = false;
            isDashing = true;
        }

        if (isDashing && dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
        }

        if (isDashing && dashTimer <= 0f)
        {
            isDashing = false;
        }

        if (!canDash && playerMovement.isGrounded && dashTimer <= 0f)
        {
            canDash = true;
            isDashing = false;
            dashTimer = dashTime;
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing || dashTimer <= 0f)
        {
            return;
        }

        rb.AddForce(forward.forward * Mathf.Pow(playerMovement.dashSpeed, dashTimer / dashTime * 2f), ForceMode.Force);
    }

    private void OnDisable()
    {
        ResetDashCooldown();
    }

    public void ResetDashCooldown()
    {
        canDash = true;
        isDashing = false;
        dashTimer = dashTime;
    }
}