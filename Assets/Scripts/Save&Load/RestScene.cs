using UnityEngine;
using UnityEngine.UI;

public class RestScene : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    
    private void Start()
    {
        // Make sure data is loaded when entering the rest scene
        if (!SaveSystem.DoesSaveExist())
        {
            Debug.LogWarning("No save data found when entering rest scene");
        }
        else
        {
            GameManager.Instance.LoadGame();
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToNextLevel);
        }
    }
    
    // Call this when player is done with upgrades/shopping
    public void ContinueToNextLevel()
    {
        // Use GameManager to transition to the next level
        GameManager.Instance.StartNextLevel();
    }
}