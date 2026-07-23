using UnityEngine;

public class CharacterControlState : MonoBehaviour
{

    private void Start()
    {
        //temporary
        SetPlayerControl(true);
    }
    public bool IsPlayerControlled { get; private set; }

    public void SetPlayerControl(bool enabled)
    {
        IsPlayerControlled = enabled;
    }
}