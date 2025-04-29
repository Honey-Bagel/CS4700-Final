using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestScene : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI MoneyText;
    
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

    void Awake()
    {
        if(GameManager.Instance != null)
        {
            MoneyText.text = $"$ {GameManager.Instance.ScrapCount}";
        }
    }
    
    // Call this when player is done with upgrades/shopping
    public void ContinueToNextLevel()
    {
        // Use GameManager to transition to the next level
        GameManager.Instance.StartNextLevel();
    }
}