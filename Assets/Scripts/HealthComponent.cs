using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class HealthComponent : MonoBehaviour
{
    public float healthRemaining = 100.0f;
    public float MaxHealth = 100.0f;
    public bool isAlive = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if (healthRemaining <= 0)
        {
            isAlive = false;
        }    
    }

    public float getHealth()
    {
        return healthRemaining;
    }

    public void giveHealth(float health) {
        healthRemaining = Mathf.Max(healthRemaining+health,MaxHealth);
    }

    public void setHealth(float health)
    {
        healthRemaining = health;
    }

    public void removeHealth(float damage)
    {
        healthRemaining -= damage;
    }
}
