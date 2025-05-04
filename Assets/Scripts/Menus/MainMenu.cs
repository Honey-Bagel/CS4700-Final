using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject saveSlotPanel;
    [SerializeField] private SaveSlotUIManager saveSlotUIManager;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OnPlayButtonClicked()
    {
        if(saveSlotPanel == null) {
            return;
        }
        saveSlotPanel.SetActive(true);
        mainMenuPanel.SetActive(false);

        saveSlotUIManager?.RefreshSaveSlots();
    }

    public void OnBackButtonClicked()
    {
        if(mainMenuPanel == null) {
            return;
        }
        saveSlotPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}