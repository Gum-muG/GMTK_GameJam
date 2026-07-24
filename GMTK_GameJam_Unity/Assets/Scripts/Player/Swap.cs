using UnityEngine;

public class Swap : MonoBehaviour
{
    public Transform counterpart;
    private Transform temp;

    public void SwapPlaces()
    {
        temp = transform;
        transform.rotation = counterpart.rotation;
        transform.position = counterpart.position;
        counterpart.rotation = temp.rotation;
        counterpart.position = temp.position;
    }

    public void Update()
    {
        if (Input.GetKey(KeyCode.T))
        {
            SwapPlaces();
        }
    }
}
