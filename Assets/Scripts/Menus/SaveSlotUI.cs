using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI playtimeText;
    [SerializeField] private TextMeshProUGUI lastSavedText;
    [SerializeField] private TextMeshProUGUI scrapText;
    
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button deleteButton;
    
    [SerializeField] private GameObject saveInfoPanel;
    [SerializeField] private GameObject emptySlotPanel;
    
    private SaveSlotInfo slotInfo;
    private SaveSlotUIManager manager;
    
    public void Setup(SaveSlotInfo info, SaveSlotUIManager slotManager)
    {
        slotInfo = info;
        manager = slotManager;
        
        if (info.Exists)
        {
            // Show save info
            saveInfoPanel.SetActive(true);
            emptySlotPanel.SetActive(false);
            
            slotNameText.text = info.DisplayName;
            levelText.text = info.LevelDisplay;
            playtimeText.text = info.FormattedPlaytime;
            lastSavedText.text = "Last Saved: " + info.FormattedLastSaved;
            
            loadGameButton.gameObject.SetActive(true);
            deleteButton.gameObject.SetActive(true);
        }
        else
        {
            // Show empty slot
            saveInfoPanel.SetActive(false);
            emptySlotPanel.SetActive(true);
            
            loadGameButton.gameObject.SetActive(false);
            deleteButton.gameObject.SetActive(false);
        }
        
        // Set up button listeners
        newGameButton.onClick.AddListener(() => manager.OnNewGameClicked(info.SlotId));
        loadGameButton.onClick.AddListener(() => manager.OnLoadGameClicked(info.SlotId));
        deleteButton.onClick.AddListener(() => manager.OnDeleteSaveClicked(info.SlotId));
    }
}