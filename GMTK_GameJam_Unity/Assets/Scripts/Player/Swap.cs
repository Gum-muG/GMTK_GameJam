using UnityEngine;

public class Swap : MonoBehaviour
{
    public Transform counterpart;
    public Transform forward;
    public Rigidbody rb;
    private Vector3 positionTemp; 
    private Quaternion rotationTemp; 

    public void SwapPlaces()
    {
        
        positionTemp = rb.position;
        rotationTemp = rb.rotation;
        Debug.Log(forward.rotation);
        rb.rotation = counterpart.rotation;
        rb.position = counterpart.position;
        Debug.Log("Set Me");
        counterpart.rotation = rotationTemp;
        counterpart.position = positionTemp;
        Debug.Log(forward.rotation);
        Debug.Log("Set Counter");
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SwapPlaces();
        }
    }
}
