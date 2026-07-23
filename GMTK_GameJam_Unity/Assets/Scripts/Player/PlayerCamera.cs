using UnityEngine;
using DG.Tweening;

public class PlayerCamera : MonoBehaviour
{
    public float sensitivity;

    public Transform playerForward;
    public Transform camHolder;

    float xRot, yRot;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity;

        yRot += mouseX;
        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);
        camHolder.rotation = Quaternion.Euler(xRot, yRot, 0);
        playerForward.rotation = Quaternion.Euler(0, yRot, 0);
    }

    public void FOV(float targetFOV)
    {
        GetComponent<Camera>().DOFieldOfView(targetFOV, 0.25f);
    }
    public void Tilt(float targetTilt)
    {
        transform.DOLocalRotate(new Vector3(0,0,targetTilt), 0.25f);
    }
}
