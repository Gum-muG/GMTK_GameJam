using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform cameraTransform;

    public float projectileSpeed = 30f;
    public float fireCooldown = 0.25f;

    private float fireCooldownTimer;

    private void Update()
    {
        if (fireCooldownTimer > 0f)
        {
            fireCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(0) && fireCooldownTimer <= 0f)
        {
            FireProjectile();
        }
    }

    private void FireProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(cameraTransform.forward));

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity = cameraTransform.forward * projectileSpeed;
        }

        fireCooldownTimer = fireCooldown;
    }
}