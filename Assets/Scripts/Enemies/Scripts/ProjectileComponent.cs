using UnityEngine;

public class ProjectileComponent : MonoBehaviour
{
    public int damage = 10;
    public GameObject owner;
    public GameObject hitEffect;
    
    private void OnCollisionEnter(Collision collision)
    {
        // Avoid hitting the owner
        if (owner != null && collision.gameObject == owner)
            return;
            
        // Check if we hit something that can take damage
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        Debug.Log("Hit: " + collision.gameObject.name);
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        // Spawn hit effect if available
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Destroy the projectile
        Destroy(gameObject);
    }
}

// Interface for anything that can take damage
public interface IDamageable
{
    void TakeDamage(int damage);
}