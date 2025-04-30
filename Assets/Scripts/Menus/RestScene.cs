using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestScene : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI MoneyText;

    private void OnEnable()
    {
        GameManager.OnScrapCountChanged += UpdateScrapDisplay;

        if(GameManager.Instance != null)
        {
            UpdateScrapDisplay(GameManager.Instance.ScrapCount);
        }
    }

    private void OnDisable()
    {
        GameManager.OnScrapCountChanged -= UpdateScrapDisplay;
    }
    
    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Make sure data is loaded when entering the rest scene
        if(SaveSystem.DoesSaveExist(GameManager.Instance.CurrentSaveSlot))
        {
            SaveSystem.LoadGame(GameManager.Instance.CurrentSaveSlot);
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToNextLevel);
        }
    }

    private void UpdateScrapDisplay(int scrapCount)
    {
        if(MoneyText != null)
        {
            MoneyText.text = $"$ {scrapCount}";
        }
    }
    
    // Call this when player is done with upgrades/shopping
    public void ContinueToNextLevel()
    {
        // Use GameManager to transition to the next level
        GameManager.Instance.StartNextLevel();
    }
}