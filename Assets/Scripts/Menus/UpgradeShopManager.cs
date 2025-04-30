using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class UpgradeShopManager : MonoBehaviour
{
    [Header("Upgrade Slot UI")]
    [SerializeField] private GameObject upgradeSlotPrefab;
    [SerializeField] private int numUpgradesToShow = 6;
    [SerializeField] private List<Transform> rowContainers = new List<Transform>();
    [SerializeField] private List<Upgrade> availableUpgrades = new List<Upgrade>();

    private List<UpgradeSlotUI> upgradeSlots = new List<UpgradeSlotUI>();
    
    private void Start()
    {
        LoadUpgrades();
    }

    private void LoadUpgrades()
    {
        if(availableUpgrades.Count == 0) return;
        foreach(Transform rowContainer in rowContainers) {
            foreach(Transform child in rowContainer) {
                Destroy(child.gameObject);
            }
        }
        upgradeSlots.Clear();

        for(int i = 0, row = 0; i < numUpgradesToShow; i++) {
            GameObject upgradeObj = Instantiate(upgradeSlotPrefab, rowContainers[row]);
            UpgradeSlotUI upgradeUI = upgradeObj.GetComponent<UpgradeSlotUI>();
            Upgrade selectedUpgrade = availableUpgrades[UnityEngine.Random.Range(0, availableUpgrades.Count)];
            Debug.Log(selectedUpgrade.upgradeName);
            upgradeUI.Setup(selectedUpgrade);
            upgradeSlots.Add(upgradeUI);
            if(rowContainers[row].childCount >= 2) {
                row++;
            }
        }

    }
}