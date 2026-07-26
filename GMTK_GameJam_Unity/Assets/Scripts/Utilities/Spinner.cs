using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float spinSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, spinSpeed));
    }
}