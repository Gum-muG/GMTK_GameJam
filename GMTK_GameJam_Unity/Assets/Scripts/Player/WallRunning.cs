using UnityEngine;

public class WallRunning : MonoBehaviour
{
    public LayerMask Ground, Wall;
    public float wallRunForce;
    public float maxWallRunTime;
    private float wallRunTimer;

    private float xIn, yIn;

    public float wallCheckDistance;
}
