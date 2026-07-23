using UnityEngine;
using DG.Tweening;

public class PlayerCamera : MonoBehaviour
{
    public float sensitivity;

    public Transform playerForward;
    public Transform camHolder;

    private bool clampWallRunYaw;
    private float wallRunYawCenter;
    public float wallRunYawLimit = 60f;

    float xRot, yRot;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX =
            Input.GetAxisRaw("Mouse X") *
            Time.deltaTime *
            sensitivity;

        float mouseY =
            Input.GetAxisRaw("Mouse Y") *
            Time.deltaTime *
            sensitivity;

        yRot += mouseX;

        if (clampWallRunYaw)
        {
            yRot = Mathf.Clamp(
                yRot,
                wallRunYawCenter - wallRunYawLimit,
                wallRunYawCenter + wallRunYawLimit
            );
        }

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        camHolder.rotation = Quaternion.Euler(xRot, yRot, 0f);
        playerForward.rotation = Quaternion.Euler(0f, yRot, 0f);
    }

    public void BeginWallRunClamp()
    {
        wallRunYawCenter = yRot;
        clampWallRunYaw = true;
    }

    public void EndWallRunClamp()
    {
        clampWallRunYaw = false;
    }

    public void FOV(float targetFOV)
    {
        GetComponent<Camera>().DOFieldOfView(targetFOV, 0.25f);
    }

    public void Tilt(float targetTilt)
    {
        transform.DOLocalRotate(
            new Vector3(0f, 0f, targetTilt),
            0.25f
        );
    }
}

