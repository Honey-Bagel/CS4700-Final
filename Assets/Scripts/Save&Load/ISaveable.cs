using System;

public interface ISaveable
{
    SaveableData SaveState();

    void LoadState(SaveableData data);

    string GetUniqueID();
}

[Serializable]
public class SaveableData
{
    // Base class for all saveable data
}