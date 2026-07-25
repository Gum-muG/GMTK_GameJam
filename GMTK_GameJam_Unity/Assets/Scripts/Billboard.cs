using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(Camera.main.transform.position, Vector3.up);
    }
}
