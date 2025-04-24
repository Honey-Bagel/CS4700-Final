using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        // Check if we have a save to continue
        bool hasSave = SaveSystem.DoesSaveExist();
        
        if (hasSave)
        {
            // If we have a save, load it and continue from where we left off
            // First go to the transition scene, which will then load the proper scene
            GameManager.Instance.LoadSceneWithTransition(
                GameManager.Instance.GetMainGameSceneName(),  // Use the scene name from GameManager
                true,   // Needs level generation
                true    // Load save data
            );
        }
        else
        {
            // If no save, start a new game
            // Reset any game state if needed
            GameManager.Instance.ResetGameState();
            
            GameManager.Instance.LoadSceneWithTransition(
                GameManager.Instance.GetMainGameSceneName(),  // Use the scene name from GameManager
                true,   // Needs level generation
                false   // No save data to load for a new game
            );
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}