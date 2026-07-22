using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float sensitivity;

    public Transform playerForward;

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
        transform.rotation = Quaternion.Euler(xRot, yRot, 0);
        playerForward.rotation = Quaternion.Euler(0, yRot, 0);
    }
}
