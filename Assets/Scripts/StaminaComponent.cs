using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaminaComponent : MonoBehaviour
{
    private const float MaxStamina = 100f;
    public float staminaRemaining = MaxStamina;

    private bool isExhausted = false; //to give certain actions (running, for now) a cooldown if stamina ever reaches 0
    public float exhaustionThreshold = 15.0f; // points of stamina required to not be "exhausted'
    
    private float lastStaminaUseTime = 0;
    public float staminaRegenTimeThreshold = 1f; //in seconds
    public float staminaRegenRate = 10f; //stamina points per second

    void Start()
    {
        
    }

    void Update()
    {

        //regen stamina if enough time has passed since stamina was used
        if (Time.time - lastStaminaUseTime>=staminaRegenTimeThreshold && staminaRemaining<MaxStamina) {
            staminaRemaining = Mathf.Min(staminaRemaining+staminaRegenRate*Time.deltaTime,MaxStamina);
        }
        if (isExhausted) {
            if (staminaRemaining >= exhaustionThreshold)
            {
                isExhausted = false;
            }
        } else {
            if (staminaRemaining <= 0f)
            {
                isExhausted=true;
            }
        }
    }
    //returns true if there is any stamina left to use
    public bool UseStamina(float amount,bool useExhaustion){
        if (useExhaustion && isExhausted) {
            return false;
        }
        if (staminaRemaining > 0f){
            staminaRemaining = Mathf.Max(0f, staminaRemaining-amount);
            lastStaminaUseTime = Time.time;
            return true;
        }
        else {  return false; }
    }
    //returns true ONLY if there is enough stamina to perform the action
    public bool StrictUseStamina(float amount, bool useExhaustion) {
        if (useExhaustion && isExhausted)
        {
            return false;
        }
        float staminaAfterAction = staminaRemaining - amount;
        if (staminaAfterAction >= 0f)
        {
            lastStaminaUseTime = Time.time;
            staminaRemaining = staminaAfterAction;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void GiveStamina(float amount) {
        staminaRemaining = Mathf.Min(MaxStamina,staminaRemaining+amount);
    }
}
