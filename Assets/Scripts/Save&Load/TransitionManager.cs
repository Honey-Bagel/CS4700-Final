using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;
    
    private void Start()
    {
        // Get parameters from GameManager
        string nextSceneName = GameManager.Instance.GetNextSceneName();
        bool requiresGeneration = GameManager.Instance.NeedsLevelGeneration();
        bool requiresDataLoading = GameManager.Instance.NeedsSaveDataLoading();
        
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("No next scene specified");
            return;
        }
        
        StartCoroutine(LoadScene(nextSceneName, requiresGeneration, requiresDataLoading));
    }
    
    private IEnumerator LoadScene(string sceneName, bool needsGeneration, bool needsDataLoading)
    {
        statusText.text = $"Loading {sceneName}...";
        
        // Begin loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // Show loading progress
        while (asyncLoad.progress < 0.9f)
        {
            progressBar.value = asyncLoad.progress;
            yield return null;
        }
        
        progressBar.value = 0.9f;
        
        // Scene is loaded but not activated yet
        if (needsGeneration)
        {
            statusText.text = "Level will be generated when scene activates...";
        }
        
        if (needsDataLoading)
        {
            statusText.text = "Save data will be loaded after generation...";
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Activate the scene
        progressBar.value = 1.0f;
        statusText.text = "Starting game...";
        yield return new WaitForSeconds(0.5f);
        
        asyncLoad.allowSceneActivation = true;
    }
}