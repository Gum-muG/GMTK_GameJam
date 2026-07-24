using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform aimTransform;

    public float projectileSpeed = 30f;
    public float fireCooldown = 0.25f;

    private float fireCooldownTimer;

    private void Update()
    {
        if (fireCooldownTimer > 0f)
        {
            fireCooldownTimer -= Time.deltaTime;
        }
    }

    public void FireProjectile()
    {
        if (fireCooldownTimer > 0f)
        {
            return;
        }

        Vector3 direction = aimTransform.forward;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity = direction * projectileSpeed;
        }

        fireCooldownTimer = fireCooldown;
    }

    public void FireProjectileAt(Transform target)
    {
        if (fireCooldownTimer > 0f || target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPosition - firePoint.position).normalized;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity = direction * projectileSpeed;
        }

        fireCooldownTimer = fireCooldown;
    }
}