using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Tooltip("List of doorways")]
    public List<Doorway> doors = new List<Doorway>();

    private Bounds roomBounds;

}