using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradePriceText;

    [SerializeField] private Button buyUpgradeButton;

    private Upgrade upgrade;
    public void Setup(Upgrade upgradeInfo)
    {
        this.upgrade = upgradeInfo;
        
        upgradeNameText.text = upgrade.upgradeName;
        upgradePriceText.text = $"$ {upgrade.upgradePrice}";

        buyUpgradeButton.onClick.AddListener(OnBuyUpgradeClicked);
    }

    public void OnBuyUpgradeClicked()
    {
        if (GameManager.Instance.ScrapCount >= upgrade.upgradePrice)
        {
            Debug.Log("Buying upgrade");
            GameManager.Instance.RemoveScrap(upgrade.upgradePrice);
            GameManager.Instance.AddUpgrade(upgrade);
            buyUpgradeButton.interactable = false;
        }
    }
}