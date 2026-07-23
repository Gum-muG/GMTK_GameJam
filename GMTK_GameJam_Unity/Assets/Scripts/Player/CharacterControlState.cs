using UnityEngine;

public class CharacterControlState : MonoBehaviour
{

    private void Start()
    {
        SetPlayerControl(true);
    }
    public bool IsPlayerControlled { get; private set; }

    public void SetPlayerControl(bool enabled)
    {
        IsPlayerControlled = enabled;
    }
}