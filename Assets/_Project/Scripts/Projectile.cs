using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage;
    public float lifetime = 5f;
    public MonoBehaviour owner; 

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
