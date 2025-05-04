

using System.Collections;
using UnityEngine.AI;
using UnityEngine;

public class NavMeshBuilder : MonoBehaviour
{
    [SerializeField]
    private bool buildAsynchronously = true;
    
    [SerializeField]
    private Transform roomParent; // Optional: Reference to the parent transform containing all rooms

    public static event System.Action OnNavMeshBuilt;
    
    private void OnEnable()
    {
        Generator.OnLevelGenerationComplete += OnLevelGenerated;
    }

    private void OnDisable()
    {
        Generator.OnLevelGenerationComplete -= OnLevelGenerated;
    }

    private void OnLevelGenerated()
    {
        if (buildAsynchronously)
        {
            StartCoroutine(BuildNavMeshAsync());
        }
        else
        {
            BuildAllNavMeshes();
        }
        OnNavMeshBuilt?.Invoke();
    }

    private void BuildAllNavMeshes()
    {
        Debug.Log("Building NavMeshes for all rooms...");
        
        NavMeshSurface[] surfaces;
        
        // If roomParent is assigned, get surfaces only from rooms
        if (roomParent != null)
        {
            surfaces = roomParent.GetComponentsInChildren<NavMeshSurface>();
        }
        else
        {
            // Otherwise find all surfaces in the scene
            surfaces = FindObjectsOfType<NavMeshSurface>();
        }
        
        if (surfaces.Length == 0)
        {
            Debug.LogWarning("No NavMeshSurfaces found to build!");
            return;
        }
        
        // Build each surface
        foreach (NavMeshSurface surface in surfaces)
        {
            surface.BuildNavMesh();
        }
        
        Debug.Log($"Built NavMesh for {surfaces.Length} room surfaces successfully!");
    }
    
    private IEnumerator BuildNavMeshAsync()
    {
        Debug.Log("Starting asynchronous NavMesh build...");
        
        // Wait a frame to ensure all rooms are properly placed
        yield return null;
        
        NavMeshSurface[] surfaces;
        
        // If roomParent is assigned, get surfaces only from rooms
        if (roomParent != null)
        {
            surfaces = roomParent.GetComponentsInChildren<NavMeshSurface>();
        }
        else
        {
            // Otherwise find all surfaces in the scene
            surfaces = FindObjectsOfType<NavMeshSurface>();
        }
        
        if (surfaces.Length == 0)
        {
            Debug.LogWarning("No NavMeshSurfaces found to build!");
            yield break;
        }
        
        // Build each surface with a small delay between builds to prevent freezing
        for (int i = 0; i < surfaces.Length; i++)
        {
            surfaces[i].BuildNavMesh();
            
            if (i % 5 == 4) // Add a yield every few surfaces to prevent long freezes
            {
                yield return null;
            }
        }
        
        Debug.Log($"Built NavMesh for {surfaces.Length} room surfaces successfully!");
    }
}