using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public ProjectileShooter projectileShooter;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            projectileShooter.FireProjectile();
        }
    }
}