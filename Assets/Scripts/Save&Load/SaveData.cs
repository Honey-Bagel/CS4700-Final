using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public float TotalPlaytimeMinutes = 0;
    public int CurrentLevel = 1;
    public int DeathCount = 0;
    public int ScrapCount = 0;
    public DateTime LastSaveDate;
    public Dictionary<string, SaveableData> GameObjectData = new Dictionary<string, SaveableData>();
}