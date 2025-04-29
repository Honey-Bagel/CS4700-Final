using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// Static utility class for handling save/load operations
public static class SaveSystem
{
    private static string SavePath => $"{Application.persistentDataPath}/saves/";
    private static string GetSaveFileName(int slotId) => $"gamesave_slot{slotId}.dat";
    private static readonly int MaxSaveSlots = 3;
    
    // Save game data to disk
    public static void SaveGame(int slotId)
    {
        try
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
            
            // Collect data from all saveable objects
            Dictionary<string, SaveableData> gameObjectData = new Dictionary<string, SaveableData>();
            
            foreach (var saveable in FindAllSaveables())
            {
                string id = saveable.GetUniqueID();
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning("Object implementing ISaveable has no uniqueID: " + 
                        (saveable as MonoBehaviour)?.gameObject.name);
                    continue;
                }
                
                SaveableData data = saveable.SaveState();
                gameObjectData[id] = data;
                Debug.Log($"Saved object with ID: {id}");
            }
            
            // Create save data container
            SaveData saveData = new SaveData
            {
                TotalPlaytimeMinutes = GameManager.Instance.TotalPlaytimeMinutes,
                CurrentLevel = GameManager.Instance.CurrentLevel,
                DeathCount = GameManager.Instance.DeathCount,
                ScrapCount = GameManager.Instance.ScrapCount,
                GameObjectData = gameObjectData,
                LastSaveDate = DateTime.Now
            };
            
            // Write to disk
            string fullPath = Path.Combine(SavePath, GetSaveFileName(slotId));
            BinaryFormatter formatter = new BinaryFormatter();
            
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                formatter.Serialize(stream, saveData);
            }
            
            Debug.Log($"Game saved successfully at: {fullPath} with {gameObjectData.Count} objects");

            UpdateSaveSlotMetadata(slotId, saveData);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving game: " + e.Message);
        }
    }
    
    // Load game data from disk
    public static bool LoadGame(int slotId)
    {
        string fullPath = Path.Combine(SavePath, GetSaveFileName(slotId));
        
        if (!File.Exists(fullPath))
        {
            Debug.Log("No save file found at " + fullPath);
            return false;
        }
        
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            
            using (FileStream stream = new FileStream(fullPath, FileMode.Open))
            {
                SaveData saveData = (SaveData)formatter.Deserialize(stream);
                
                if (saveData != null)
                {
                    // Update GameManager state first
                    GameManager.Instance.UpdateGameState(
                        saveData.CurrentLevel,
                        saveData.DeathCount,
                        saveData.TotalPlaytimeMinutes,
                        saveData.ScrapCount
                    );
                    
                    Debug.Log($"Found {saveData.GameObjectData.Count} saved objects");
                    
                    // Update all saveable objects
                    foreach (var saveable in FindAllSaveables())
                    {
                        string id = saveable.GetUniqueID();
                        if (string.IsNullOrEmpty(id)) continue;
                        
                        if (saveData.GameObjectData.TryGetValue(id, out SaveableData data))
                        {
                            saveable.LoadState(data);
                            Debug.Log($"Loaded data for object with ID: {id}");
                        }
                        else
                        {
                            Debug.LogWarning($"No saved data found for object with ID: {id}");
                        }
                    }
                    
                    Debug.Log("Game loaded successfully from: " + fullPath);

                    GameManager.Instance.SetCurrentSaveSlot(slotId);

                    return true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading game: " + e.Message);
        }
        
        return false;
    }
    
    // Helper method to find all saveable objects in the current scene
    private static ISaveable[] FindAllSaveables()
    {
        return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
            .OfType<ISaveable>()
            .ToArray();
    }
    
    // Utility methods
    public static bool DoesSaveExist(int slotId)
    {
        string fullPath = Path.Combine(SavePath, GetSaveFileName(slotId));
        return File.Exists(fullPath);
    }
    
    public static void DeleteSave(int slotId)
    {
        string fullPath = Path.Combine(SavePath, GetSaveFileName(slotId));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log("Save file deleted");

            DeleteSaveSlotMetadata(slotId);
        }
    }

    public static List<SaveSlotInfo> GetAllSaveSlots()
    {
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();

        for(int i = 0; i < MaxSaveSlots; i++) {
            SaveSlotInfo info = GetSaveSlotInfo(i);
            slots.Add(info);
        }

        return slots;
    }

    public static SaveSlotInfo GetSaveSlotInfo(int slotId)
    {
        string metadataPath = Path.Combine(SavePath, $"slot_{slotId}_metadata.json");

        if(File.Exists(metadataPath))
        {
            try {
                string json = File.ReadAllText(metadataPath);
                SaveSlotInfo info = JsonUtility.FromJson<SaveSlotInfo>(json);
                info.SlotId = slotId;
                info.Exists = true;
                return info;
            } catch (Exception e) {
                Debug.LogError($"Error reading save slot metadata: {e.Message}");
            }
        }

        return new SaveSlotInfo {
            SlotId = slotId,
            Exists = false,
            DisplayName = $"Empty Slot {slotId + 1}"
        };
    }

    private static void UpdateSaveSlotMetadata(int slotId, SaveData saveData)
    {
        try {
            if(!Directory.Exists(SavePath)) {
                Directory.CreateDirectory(SavePath);
            }

            SaveSlotInfo info = new SaveSlotInfo {
                SlotId = slotId,
                Exists = true,
                DisplayName = $"Save {slotId + 1}",
                LastSaveDate = saveData.LastSaveDate,
                TotalPlaytimeMinutes = saveData.TotalPlaytimeMinutes,
                CurrentLevel = saveData.CurrentLevel,
            };

            string json = JsonUtility.ToJson(info);
            string metadataPath = Path.Combine(SavePath, $"slot_{slotId}_metadata.json");
            File.WriteAllText(metadataPath, json);
        } catch (Exception e) {
            Debug.LogError($"Error writing save slot metadata: {e.Message}");
        }
    }

    private static void DeleteSaveSlotMetadata(int slotId)
    {
        string metadataPath = Path.Combine(SavePath, $"slot_{slotId}_metadata.json");
        if(File.Exists(metadataPath)) {
            File.Delete(metadataPath);
        }
    }

    public static void CreateNewGame(int slotId) {
        GameManager.Instance.ResetGameState();

        GameManager.Instance.SetCurrentSaveSlot(slotId);

        SaveGame(slotId);
    }
}

[Serializable]
public class SaveSlotInfo
{
    public int SlotId;
    public bool Exists;
    public string DisplayName;
    public DateTime LastSaveDate;
    public float TotalPlaytimeMinutes;
    public int CurrentLevel;

    public string FormattedPlaytime => $"{(int)TotalPlaytimeMinutes/60}h {(int)TotalPlaytimeMinutes%60}m";
    public string FormattedLastSaved => LastSaveDate.ToString("MM/dd/yyyy");
    public string LevelDisplay => $"Level {CurrentLevel}";
}