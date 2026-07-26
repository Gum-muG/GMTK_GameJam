using System;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    private static readonly Dictionary<string, ProjectileShooter> shootersById = new Dictionary<string, ProjectileShooter>();

    [Header("Ownership")]
    [SerializeField] private ProjectileFaction projectileFaction = ProjectileFaction.Enemy;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform aimTransform;

    public float projectileSpeed = 30f;
    public float fireCooldown = 0.25f;

    private float fireCooldownTimer;
    private TimelineObject timelineObject;

    public string ShooterId
    {
        get
        {
            if (timelineObject != null)
            {
                return timelineObject.TimelineId;
            }

            return gameObject.name;
        }
    }

    private void Awake()
    {
        timelineObject = GetComponentInParent<TimelineObject>();
        RegisterShooter();
    }

    private void OnDestroy()
    {
        if (shootersById.TryGetValue(ShooterId, out ProjectileShooter registeredShooter) && registeredShooter == this)
        {
            shootersById.Remove(ShooterId);
        }
    }

    private void Update()
    {
        if (fireCooldownTimer > 0f)
        {
            fireCooldownTimer -= Time.deltaTime;
        }
    }

    public void FireProjectile()
    {
        if (fireCooldownTimer > 0f || aimTransform == null)
        {
            return;
        }

        SpawnLiveProjectile(aimTransform.forward.normalized);
        fireCooldownTimer = fireCooldown;
    }

    public void FireProjectileAt(Transform target)
    {
        if (fireCooldownTimer > 0f || target == null || firePoint == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * 1.5f;
        Vector3 direction = (targetPosition - firePoint.position).normalized;

        SpawnLiveProjectile(direction);
        fireCooldownTimer = fireCooldown;
    }

    public GameObject SpawnReplayProjectile(string projectileId, ProjectileFaction faction, Vector3 recordedPosition, Vector3 recordedDirection)
    {
        Vector3 direction = recordedDirection.sqrMagnitude > 0.001f ? recordedDirection.normalized : transform.forward;

        return SpawnProjectileObject(projectileId, faction, recordedPosition, direction, 0f, true);
    }

    public static bool TryGetShooter(string shooterId, out ProjectileShooter shooter)
    {
        return shootersById.TryGetValue(shooterId, out shooter);
    }

    private void SpawnLiveProjectile(Vector3 direction)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError($"{name}: ProjectileShooter is missing its prefab or fire point.");
            return;
        }

        string projectileId = $"Projectile_{Guid.NewGuid():N}";

        GameObject projectile = SpawnProjectileObject(projectileId, projectileFaction, firePoint.position, direction, projectileSpeed, false);

        CharacterSwapManager manager = CharacterSwapManager.instance;

        if (projectile != null && manager != null)
        {
            manager.RecordProjectileFired(projectileId, ShooterId, projectileFaction, firePoint.position, Quaternion.LookRotation(direction), direction, projectileSpeed, GetProjectileLifetime());
        }
    }

    private GameObject SpawnProjectileObject(string projectileId, ProjectileFaction faction, Vector3 position, Vector3 direction, float speed, bool replayVisualOnly)
    {
        if (projectilePrefab == null)
        {
            return null;
        }

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));

        TimelineObject projectileTimelineObject = projectile.GetComponent<TimelineObject>();

        if (projectileTimelineObject == null)
        {
            projectileTimelineObject = projectile.AddComponent<TimelineObject>();
        }

        projectileTimelineObject.SetTimelineId(projectileId);

        Projectile projectileScript = projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.Initialize(projectileId, ShooterId, faction, replayVisualOnly, GetProjectileLifetime());
        }

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            if (replayVisualOnly)
            {
                projectileRb.linearVelocity = Vector3.zero;
                projectileRb.angularVelocity = Vector3.zero;
                projectileRb.useGravity = false;
                projectileRb.isKinematic = true;
            }
            else
            {
                projectileRb.linearVelocity = direction.normalized * speed;
            }
        }

        if (replayVisualOnly)
        {
            foreach (Collider projectileCollider in projectile.GetComponentsInChildren<Collider>())
            {
                projectileCollider.enabled = false;
            }
        }

        return projectile;
    }

    private float GetProjectileLifetime()
    {
        if (projectilePrefab == null)
        {
            return 5f;
        }

        Projectile projectile = projectilePrefab.GetComponent<Projectile>();

        return projectile != null ? projectile.lifetime : 5f;
    }

    private void RegisterShooter()
    {
        if (shootersById.ContainsKey(ShooterId) && shootersById[ShooterId] != this)
        {
            Debug.LogWarning($"Duplicate ProjectileShooter timeline ID '{ShooterId}'.");
        }

        shootersById[ShooterId] = this;
    }
}