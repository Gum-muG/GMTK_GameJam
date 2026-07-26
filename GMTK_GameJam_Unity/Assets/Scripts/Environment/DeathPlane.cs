using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    [SerializeField] private GameObject firePlayer;
    [SerializeField] private PlayerMovement fireScript;
    [SerializeField] private GameObject icePlayer;
    [SerializeField] private PlayerMovement iceScript;
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("D");
        if (other.gameObject.name == "FireMesh" )
        {
            Debug.Log("S");
            fireScript.TeleportToLastStood();
        } else if (other.gameObject.name == "IceMesh")
        {
            Debug.Log("W");
            iceScript.TeleportToLastStood();
        }
    }
}
