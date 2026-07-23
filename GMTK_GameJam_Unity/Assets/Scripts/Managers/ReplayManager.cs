using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager instance;
    public ReplayContainer replayContainer;
    private void Awake()
    {
        instance = this;
    }
}
