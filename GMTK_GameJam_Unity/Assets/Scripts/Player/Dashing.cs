using System;
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
    private CharacterControlState controlState;

    public bool IsDashing => isDashing;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        dashTimer = dashTime;

        //getting components
        controlState = GetComponent<CharacterControlState>();
    }

    // Update is called once per frameaa
    void Update()
    {
        if (!controlState.IsPlayerControlled)
        return;

        if (canDash && Input.GetKeyDown(KeyCode.E))
        {
            canDash = false;
            isDashing = true;
        }

        if (isDashing && dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
        }

        if (isDashing && dashTimer <= 0f)
        {
            isDashing = false;
        }

        if (!canDash && playerMovement.isGrounded && dashTimer <= 0)
        {
            canDash = true;
            isDashing = false;
            dashTimer = dashTime;
        }
    }

    private void FixedUpdate()
    {
        if (!controlState.IsPlayerControlled)
            return;

        if (!isDashing || dashTimer <= 0f)
            return;

        rb.AddForce(forward.forward * Mathf.Pow(playerMovement.dashSpeed, dashTimer / dashTime * 2f), ForceMode.Force);

}
}
