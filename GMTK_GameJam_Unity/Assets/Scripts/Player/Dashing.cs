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
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        dashTimer = dashTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (canDash && Input.GetKey(KeyCode.LeftControl))
        {
            canDash = false;
        }

        if (!canDash && dashTimer > 0) 
        {
            rb.AddForce(forward.forward * Mathf.Pow(playerMovement.dashSpeed, dashTimer / dashTime * 2f), ForceMode.Force);
            dashTimer -= Time.deltaTime;
        }

        if(!canDash && playerMovement.isGrounded && dashTimer <= 0)
        {
            canDash = true;
            dashTimer = dashTime;
        }
    }
}
