using System;
using UnityEngine;

[Serializable]
public class SerializableUpgrade
{
    public string upgradeName;
    public int count;
    public UpgradeType upgradeType;

    public SerializableUpgrade(Upgrade upgrade, int count)
    {
        this.upgradeName = upgrade.upgradeName;
        this.count = count;
        this.upgradeType = upgrade.upgradeType;
    }
}