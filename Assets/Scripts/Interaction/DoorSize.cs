using UnityEngine;

public enum DoorSize
{
    Small,
    Default,
    Large,
}

public static class DoorSizeExtensions
{
    // Dictionary mapping door sizes to dimensions
    private static readonly System.Collections.Generic.Dictionary<DoorSize, Vector2> SizeDimensions = 
        new System.Collections.Generic.Dictionary<DoorSize, Vector2>
        {
            { DoorSize.Small, new Vector2(0.8f, 2.0f) },
            { DoorSize.Default, new Vector2(1.0f, 2.0f) },
            { DoorSize.Large, new Vector2(3f, 4f) }
        };

    // Get full dimensions as Vector2
    public static Vector2 GetDimensions(this DoorSize size)
    {
        return SizeDimensions[size];
    }

    // Get just the width
    public static float GetWidth(this DoorSize size)
    {
        return SizeDimensions[size].x;
    }

    // Get just the height
    public static float GetHeight(this DoorSize size)
    {
        return SizeDimensions[size].y;
    }
    
    // Convert string to enum (for backward compatibility)
    public static DoorSize FromString(string sizeType)
    {
        return sizeType.ToLower() switch
        {
            "small" => DoorSize.Small,
            "large" => DoorSize.Large,
            _ => DoorSize.Default
        };
    }
}