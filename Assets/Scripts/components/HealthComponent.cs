using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public float healthRemaining = 100.0f;
    public float MaxHealth = 100.0f;
    public bool isAlive = true;
    public static event Action OnDeath; 

    void Update()
    {
        if (isAlive && healthRemaining <= 0)
        {
            isAlive = false;
            OnDeath?.Invoke();
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

    public void TakeDamage(float damage)
    {
        healthRemaining -= damage;
        Mathf.Max(healthRemaining, 0.0f);
    }
}