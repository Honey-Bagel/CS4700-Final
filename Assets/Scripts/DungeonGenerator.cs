using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public List<GameObject> roomPrefabs;
    public GameObject startRoomPrefab;
    public int numberOfRooms = 10;
    public int seed = 1;
    public LayerMask roomLayerMask;

    private List<Doorway> openDoorways = new List<Doorway>();
    private List<GameObject> rooms = new List<GameObject>();

    [ContextMenu("Clear All Rooms and generate new level.")]
    public void ClearAndGenerateNewLevel() {
        DestroyAllRooms();

        Random.InitState(Random.Range(0, int.MaxValue));

        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        AddRoomDoorways(startRoom);
        rooms.Add(startRoom);

        bool success = TryGenerateRooms(numberOfRooms, rooms);
        if(!success)
        {
            Debug.LogWarning("Dungeon generation failed to reach the target room count.");
        }
    }

    void DestroyAllRooms() {
        foreach(GameObject room in rooms)
        {
            if(room != null) {
                foreach(Collider col in room.GetComponentsInChildren<Collider>()) {
                    col.enabled = false;
                }
                Destroy(room);
            }
        }
        rooms.Clear();
        openDoorways.Clear();
    }

    void Start()
    {
        if (seed != 0)
        {
            Random.InitState(seed);
        }

        // Uncomment one of these generation methods as needed.
        //GenerateLevel();
        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        AddRoomDoorways(startRoom);
        rooms.Add(startRoom);

        bool success = TryGenerateRooms(numberOfRooms, rooms);
        if(!success)
        {
            Debug.LogWarning("Dungeon generation failed to reach the target room count.");
        }
    }

    void GenerateLevel()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0)
        {
            Debug.LogError("No room prefabs assigned");
            return;
        }

        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        AddRoomDoorways(startRoom);
        rooms.Add(startRoom);

        int count = 1;
        int tries = 0;
        int maxTries = 100;
        while (count < numberOfRooms && openDoorways.Count > 0 && tries < maxTries)
        {
            tries++;
            int doorwayIndex = Random.Range(0, openDoorways.Count);
            Doorway chosenDoorway = openDoorways[doorwayIndex];

            GameObject roomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];

            GameObject newRoom = Instantiate(roomPrefab);
            Doorway[] newRoomDoorways = newRoom.GetComponentsInChildren<Doorway>();

            if (newRoomDoorways.Length == 0)
            {
                Debug.LogWarning("The room prefab \"" + newRoom.name + "\" has no doorways");
                Destroy(newRoom);
                continue;
            }

            Doorway attachDoorway = newRoomDoorways[Random.Range(0, newRoomDoorways.Length)];
            AlignRooms(chosenDoorway, attachDoorway, newRoom);

            if (!CheckValidPosition(newRoom))
            {
                Destroy(newRoom);
                chosenDoorway.connected = true;
                openDoorways.Remove(chosenDoorway);
                continue;
            }

            // Mark the doorways as connected and update the open list.
            chosenDoorway.connected = true;
            openDoorways.Remove(chosenDoorway);
            attachDoorway.connected = true;

            foreach (Doorway door in newRoomDoorways)
            {
                if (!door.connected)
                {
                    openDoorways.Add(door);
                }
            }
            rooms.Add(newRoom);
            count++;
        }
    }

    bool TryGenerateRooms(int targetRoomCount, List<GameObject> placedRooms)
    {
        // If we meet the required count, the branch succeeded.
        if (placedRooms.Count >= targetRoomCount)
            return true;

        // Work with a copy of the current open doorways
        List<Doorway> currentOpenDoors = new List<Doorway>(openDoorways);

        // If there are no open doorways to attach a new room, backtrack.
        if (currentOpenDoors.Count == 0)
            return false;

        // Try each open doorway (we use a copy so that modifications don't affect iteration)
        foreach (Doorway selectedDoor in currentOpenDoors)
        {
            // Create a copy of the room prefabs list to try from.
            List<GameObject> roomPrefabsCopy = new List<GameObject>(roomPrefabs);

            while (roomPrefabsCopy.Count > 0)
            {
                // Pick a random room prefab and remove it from the copy list after trying.
                int index = Random.Range(0, roomPrefabsCopy.Count);
                GameObject roomPrefab = roomPrefabsCopy[index];
                roomPrefabsCopy.RemoveAt(index);

                GameObject newRoom = Instantiate(roomPrefab);
                rooms.Add(newRoom);
                List<Doorway> candidateDoors = newRoom.GetComponentsInChildren<Doorway>().ToList();

                if (candidateDoors.Count == 0)
                {
                    Debug.Log("The room prefab \"" + newRoom.name + "\" has no doorways");
                    rooms.Remove(newRoom);
                    Destroy(newRoom);
                    continue;
                }

                // Try each candidate door until one works.
                while (candidateDoors.Count > 0)
                {
                    int candidateIndex = Random.Range(0, candidateDoors.Count);
                    Doorway candidateDoor = candidateDoors[candidateIndex];

                    // Save state for backtracking.
                    List<Doorway> savedOpenDoors = new List<Doorway>(openDoorways);
                    List<GameObject> savedPlacedRooms = new List<GameObject>(placedRooms);

                    AlignRooms(selectedDoor, candidateDoor, newRoom);

                    if (!CheckValidPosition(newRoom))
                    {
                        // Restore state and remove this candidate door, then try another.
                        openDoorways = new List<Doorway>(savedOpenDoors);
                        placedRooms = new List<GameObject>(savedPlacedRooms);
                        candidateDoors.RemoveAt(candidateIndex);
                        continue;
                    }

                    // Successful alignment: mark doorways and update lists.
                    selectedDoor.connected = true;
                    candidateDoor.connected = true;
                    openDoorways.Remove(selectedDoor);

                    // Add any new, unconnected doors from this room.
                    foreach (Doorway door in newRoom.GetComponentsInChildren<Doorway>())
                    {
                        CheckAndConnectDoorway(door);
                        if (!door.connected && !openDoorways.Contains(door))
                        {
                            openDoorways.Add(door);
                        } else if(door.connected && openDoorways.Contains(door)) {
                            openDoorways.Remove(door);
                        }
                    }
                    placedRooms.Add(newRoom);

                    // Recurse to try and generate the remaining rooms.
                    if (TryGenerateRooms(targetRoomCount, placedRooms))
                    {
                        return true;
                    }
                    else
                    {
                        // Backtrack: restore state and try another candidate door.
                        openDoorways = new List<Doorway>(savedOpenDoors);
                        placedRooms = new List<GameObject>(savedPlacedRooms);
                        rooms.Remove(newRoom);
                        Destroy(newRoom);
                        candidateDoors.RemoveAt(candidateIndex);
                    }
                } // End candidateDoors loop

                if(candidateDoors.Count == 0) {
                    rooms.Remove(newRoom);
                    Destroy(newRoom);
                }

                // If we reached here, this roomPrefab did not work, so try the next one.
            } // End roomPrefabsCopy loop

            // If we've exhausted room prefabs for this selected door, remove it from openDoorways.
            if (openDoorways.Contains(selectedDoor))
                openDoorways.Remove(selectedDoor);
        }

        // If none of the options worked, return false.
        return false;
    }

    void AddRoomDoorways(GameObject room)
    {
        Doorway[] doors = room.GetComponentsInChildren<Doorway>();
        foreach (Doorway door in doors)
        {
            if (!door.connected && !openDoorways.Contains(door))
            {
                openDoorways.Add(door);
            }
        }
    }

    void AlignRooms(Doorway chosenDoorway, Doorway attachDoorway, GameObject newRoom)
    {
        // Calculate the rotation offset: we want attachDoorway to face opposite the chosen door.
        Quaternion targetRotation = Quaternion.LookRotation(-chosenDoorway.transform.forward);
        Quaternion doorRotation = attachDoorway.transform.rotation;
        Quaternion rotationOffset = targetRotation * Quaternion.Inverse(doorRotation);

        newRoom.transform.rotation = rotationOffset * newRoom.transform.rotation;

        // Adjust the position so the doors line up.
        Vector3 positionOffset = chosenDoorway.transform.position - attachDoorway.transform.position;
        newRoom.transform.position += positionOffset;
    }

    bool CheckValidPosition(GameObject newRoom)
    {
        // Use an OverlapBox to check if the room collides with something (using roomLayerMask)
        Collider[] hitColliders = Physics.OverlapBox(newRoom.transform.position, newRoom.transform.localScale / 2, Quaternion.identity, roomLayerMask);
        if (hitColliders.Length > 0)
        {
            Debug.Log("Tried to spawn a room in an invalid spot. (overlapping)");
            return false;
        }
        else
        {
            return true;
        }
    }

    void CheckAndConnectDoorway(Doorway door) {
        float checkRadius = 0.1f;
        Collider[] hits = Physics.OverlapSphere(door.transform.position, checkRadius);
        foreach(Collider hit in hits) {
            Doorway otherDoor = hit.GetComponent<Doorway>();
            if(otherDoor != null && otherDoor != door) {
                if(Vector3.Dot(door.transform.forward, otherDoor.transform.forward) < -0.9f) {
                    door.connected = true;
                    otherDoor.connected = true;
                    break;
                }
            }
        }
    }
}