using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxComponent : MonoBehaviour
{
    public float damage;
    public bool isContinuousDamage; // if this is true, damage is now expressed in damage RATE (points of damage per second)

    private HashSet<HealthComponent> alreadyHitTargets;
    // Start is called before the first frame update
    void Start()
    {
        alreadyHitTargets = new HashSet<HealthComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        HealthComponent target = other.GetComponent<HealthComponent>();
        if (target != null)
        {
            if (isContinuousDamage)
            {
                doDamageOverTime(target);
            }
            else
            {
                doDamageFlat(target);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isContinuousDamage) { return; }

        HealthComponent target = other.GetComponent<HealthComponent>();
        if (target != null)
        {
            if (isContinuousDamage)
            {
                doDamageOverTime(target);
            }
            else
            {
                doDamageFlat(target);
            }
        }
    }

    void doDamageFlat(HealthComponent target) { 
        if (target !=null && target.isAlive && !alreadyHitTargets.Contains(target)) {
            alreadyHitTargets.Add(target);
            target.removeHealth(damage);
        }
    }

    void doDamageOverTime(HealthComponent target) {
        if (target != null && target.isAlive)
        {
            target.removeHealth(damage*Time.deltaTime);
        }
    }
}
