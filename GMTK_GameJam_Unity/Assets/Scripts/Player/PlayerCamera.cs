using UnityEngine;
using DG.Tweening;

public class PlayerCamera : MonoBehaviour
{
    public float sensitivity;

    public Transform playerForward;
    public Transform camHolder;

    private bool clampWallRunHorizontal;
    private float wallRunHorizontalCenter;

    public float wallRunHorizontalLimit = 60f;

    private float verticalRotation;
    private float horizontalRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity;

        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity;

        float proposedHorizontalRotation = horizontalRotation + mouseX;

        if (clampWallRunHorizontal)
        {
            float currentHorizontalOffset = Mathf.DeltaAngle(wallRunHorizontalCenter, horizontalRotation);

            float proposedHorizontalOffset = Mathf.DeltaAngle(wallRunHorizontalCenter, proposedHorizontalRotation);

            if (currentHorizontalOffset > wallRunHorizontalLimit)
            {
                proposedHorizontalOffset = Mathf.Min(proposedHorizontalOffset, currentHorizontalOffset);
            }
            else if (currentHorizontalOffset < -wallRunHorizontalLimit)
            {
                proposedHorizontalOffset = Mathf.Max(proposedHorizontalOffset, currentHorizontalOffset);
            }
            else
            {
                proposedHorizontalOffset = Mathf.Clamp(proposedHorizontalOffset, -wallRunHorizontalLimit, wallRunHorizontalLimit);
            }

            horizontalRotation = wallRunHorizontalCenter + proposedHorizontalOffset;
        }
        else
        {
            horizontalRotation = proposedHorizontalRotation;
        }

        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        camHolder.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

        playerForward.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    public void BeginWallRunClamp(Vector3 wallNormal)
    {
        Vector3 wallDirection = Vector3.Cross(wallNormal, Vector3.up).normalized;

        Vector3 currentHorizontalLookDirection = Vector3.ProjectOnPlane(camHolder.forward, Vector3.up).normalized;

        if (Vector3.Dot(wallDirection, currentHorizontalLookDirection) < 0f)
        {
            wallDirection = -wallDirection;
        }

        wallRunHorizontalCenter = Mathf.Atan2(wallDirection.x, wallDirection.z) * Mathf.Rad2Deg;

        clampWallRunHorizontal = true;
    }

    public void EndWallRunClamp()
    {
        clampWallRunHorizontal = false;
    }

    public void FOV(float targetFOV)
    {
        GetComponent<Camera>().DOFieldOfView(targetFOV, 0.25f);
    }

    public void Tilt(float targetTilt)
    {
        transform.DOLocalRotate(new Vector3(0f, 0f, targetTilt), 0.25f);
    }
}