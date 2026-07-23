using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 25f;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Projectile hit: " + collision.gameObject.name);

        Destroy(gameObject);
    }
}