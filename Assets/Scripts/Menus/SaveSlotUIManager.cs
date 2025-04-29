using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SaveSlotUIManager : MonoBehaviour
{
    [Header("Save Slot UI")]
    [SerializeField] private GameObject saveSlotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject confirmDeletePanel;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelDeleteButton;
    
    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "MainGame"; 
    
    private List<SaveSlotUI> slotUIs = new List<SaveSlotUI>();
    private int selectedSlotForDeletion = -1;
    
    private void Start()
    {
        LoadSaveSlots();
        confirmDeletePanel.SetActive(false);

        confirmDeleteButton.onClick.AddListener(OnDeleteConfirmed);
        cancelDeleteButton.onClick.AddListener(OnDeleteCancelled);
    }
    
    private void LoadSaveSlots()
    {
        // Clear existing slots first
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();
        
        // Get all save slots
        List<SaveSlotInfo> slots = SaveSystem.GetAllSaveSlots();
        
        // Create UI for each slot
        foreach (var slotInfo in slots)
        {
            GameObject slotObj = Instantiate(saveSlotPrefab, slotsContainer);
            SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();
            slotUI.Setup(slotInfo, this);
            slotUIs.Add(slotUI);
        }
    }
    
    // Called when New Game button is clicked on a slot
    public void OnNewGameClicked(int slotId)
    {
        if (SaveSystem.DoesSaveExist(slotId))
        {
            // Ask for confirmation before overwriting
            selectedSlotForDeletion = slotId;
            confirmDeletePanel.SetActive(true);
        }
        else
        {
            CreateNewGame(slotId);
        }
    }
    
    // Called when Load Game button is clicked on a slot
    public void OnLoadGameClicked(int slotId)
    {
        if (SaveSystem.DoesSaveExist(slotId))
        {
            GameManager.Instance.SetCurrentSaveSlot(slotId);
            GameManager.Instance.LoadSceneWithTransition(gameSceneName, false, true);
        }
    }
    
    // Called when Delete Save button is clicked on a slot
    public void OnDeleteSaveClicked(int slotId)
    {
        selectedSlotForDeletion = slotId;
        confirmDeletePanel.SetActive(true);
    }
    
    // Delete confirmation panel responses
    public void OnDeleteConfirmed()
    {
        if (selectedSlotForDeletion >= 0)
        {
            SaveSystem.DeleteSave(selectedSlotForDeletion);
            confirmDeletePanel.SetActive(false);
            LoadSaveSlots(); // Refresh UI
        }
    }
    
    public void OnDeleteCancelled()
    {
        confirmDeletePanel.SetActive(false);
        selectedSlotForDeletion = -1;
    }
    
    // Create new game in selected slot
    public void OnCreateNewGameConfirmed()
    {
        if (selectedSlotForDeletion >= 0)
        {
            CreateNewGame(selectedSlotForDeletion);
            confirmDeletePanel.SetActive(false);
        }
    }
    
    private void CreateNewGame(int slotId)
    {
        SaveSystem.CreateNewGame(slotId);
        GameManager.Instance.LoadSceneWithTransition(gameSceneName, true, false);
    }
    
    // Return to main menu
    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}