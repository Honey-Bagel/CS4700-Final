using UnityEngine;
using System;

public enum UpgradeType
{
    Health,
    Speed,
    StaminaIncrease,
    StaminaRecharge,
    Damage,
}
    
[CreateAssetMenu(fileName = "Upgrades", menuName = "Upgrades/Upgrade")]
[Serializable]
public class Upgrade : ScriptableObject
{
    public string upgradeName;
    public int upgradePrice;
    public UpgradeType upgradeType;
    public float modifier;

    public void AddUpgrade() {
        
    }
}