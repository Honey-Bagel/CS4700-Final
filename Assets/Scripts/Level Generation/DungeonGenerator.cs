using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public List<GameObject> roomPrefabs;

    public List<RoomData> roomDatas;
    public GameObject startRoomPrefab;
    public int numberOfRooms = 10;
    public int seed = 1;
    public LayerMask floorLayerMask;
    public GameObject taggedWall;

    private List<Doorway> openDoorways = new List<Doorway>();
    private List<GameObject> rooms = new List<GameObject>();

    [ContextMenu("Clear All Rooms and generate new level.")]
    public void ClearAndGenerateNewLevel() {
        DestroyAllRooms();

        int newSeed = Random.Range(0, int.MaxValue);
        Debug.Log("SEED: " + newSeed);

        Random.InitState(newSeed);

        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        AddRoomDoorways(startRoom);
        rooms.Add(startRoom);

        bool success = TryGenerateRooms(numberOfRooms, rooms);
        if(!success)
        {
            Debug.LogWarning("Dungeon generation failed to reach the target room count.");
        }
        Debug.Log("Door Count: " + openDoorways.Count);
        Debug.Log("Room count: " + rooms.Count);

        CheckDoorConnections();
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
        } else {
            int newSeed = Random.Range(0, int.MaxValue);
            Random.InitState(newSeed);
            Debug.Log("SEED: " + newSeed);
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
        Debug.Log("Door Count: " + openDoorways.Count);
        Debug.Log("Room count: " + rooms.Count);

        CheckDoorConnections();
    }

    bool TryGenerateRooms(int targetRoomCount, List<GameObject> placedRooms)
    {
        // Base case: if we've reached the target room count, we're done.
        if (placedRooms.Count >= targetRoomCount)
            return true;

        // Work with a copy of the current open doorways.
        List<Doorway> currentOpenDoors = new List<Doorway>(openDoorways);
        // If no open doorway exists, we cannot attach any more rooms.
        if (currentOpenDoors.Count == 0)
            return false;

        // Iterate over each open door in the snapshot.
        foreach (Doorway selectedDoor in currentOpenDoors)
        {
            // Save the current state for backtracking.
            int roomsCountBeforeDoor = placedRooms.Count;
            int doorsCountBeforeDoor = openDoorways.Count;

            // Work with a copy of the room prefabs.
            List<GameObject> roomPrefabsCopy = new List<GameObject>(roomPrefabs);

            while (roomPrefabsCopy.Count > 0)
            {
                // Pick a random room prefab and remove it after trying.
                int prefabIndex = Random.Range(0, roomPrefabsCopy.Count);
                GameObject roomPrefab = roomPrefabsCopy[prefabIndex];
                roomPrefabsCopy.RemoveAt(prefabIndex);

                GameObject newRoom = Instantiate(roomPrefab);
                Physics.SyncTransforms();
                List<Doorway> candidateDoors = newRoom.GetComponentsInChildren<Doorway>().ToList();
                if (candidateDoors.Count == 0)
                {
                    Debug.Log("The room prefab \"" + newRoom.name + "\" has no doorways.");
                    Destroy(newRoom);
                    continue;
                }

                // Save counts before trying to attach this new room.
                int roomsCountBeforeRoom = placedRooms.Count;
                int doorsCountBeforeRoom = openDoorways.Count;

                // Try to use each candidate doorway from the new room.
                while (candidateDoors.Count > 0)
                {
                    Doorway candidateDoor = candidateDoors[0];

                    // Attempt to align newRoom using the selected door.
                    AlignRooms(selectedDoor, candidateDoor, newRoom);

                    // If the placement is invalid, revert the changes made (if any)
                    // and remove this candidate from further attempts.
                    if (!CheckValidPosition(newRoom))
                    {
                        candidateDoors.RemoveAt(0);
                        continue;
                    }

                    // Valid alignment! Mark door connections.
                    selectedDoor.connected = true;
                    selectedDoor.SetConnecetedDoor(candidateDoor);
                    candidateDoor.connected = true;
                    candidateDoor.SetConnecetedDoor(selectedDoor);
                    openDoorways.Remove(selectedDoor);

                    // Add any new, unconnected doorways from the new room.
                    AddRoomDoorways(newRoom);

                    // Now that the new room is properly aligned,
                    // add it to the list of placed rooms.
                    placedRooms.Add(newRoom);

                    // Recurse: try to generate the remainder of the level.
                    if (TryGenerateRooms(targetRoomCount, placedRooms))
                    {
                        return true; // Found a complete solution.
                    }
                    else
                    {
                        // Backtracking: the recursive branch failed.
                        // Revert the changes by removing the new room from placedRooms.
                        while (placedRooms.Count > roomsCountBeforeRoom)
                        {
                            // (Assumes newRoom is the only room added in this branch)
                            placedRooms.Remove(newRoom);
                            Destroy(newRoom);
                        }

                        // Also, revert any changes made to the openDoorways list.
                        while (openDoorways.Count > doorsCountBeforeRoom)
                            openDoorways.RemoveAt(openDoorways.Count - 1);

                        // Remove this candidate door option so we can try the next one.
                        candidateDoors.RemoveAt(0);
                    }
                } // End of candidate door loop.

                // No candidate door worked for this roomPrefab.
                Destroy(newRoom);
                // Make sure to restore the lists to the state before trying this room.
                while (placedRooms.Count > roomsCountBeforeDoor)
                    placedRooms.RemoveAt(placedRooms.Count - 1);
                while (openDoorways.Count > doorsCountBeforeRoom)
                    openDoorways.RemoveAt(openDoorways.Count - 1);
            } // End of roomPrefabsCopy loop.

            // All room prefabs have been exhausted for the current selected door.
            if (openDoorways.Contains(selectedDoor))
                openDoorways.Remove(selectedDoor);
        } // End of open-door loop.

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
        float doorGap = 0.1f;
        // Calculate the rotation offset: we want attachDoorway to face opposite the chosen door.
        Quaternion targetRotation = Quaternion.LookRotation(-chosenDoorway.transform.forward);
        Quaternion doorRotation = attachDoorway.transform.rotation;
        Quaternion rotationOffset = targetRotation * Quaternion.Inverse(doorRotation);

        newRoom.transform.rotation = rotationOffset * newRoom.transform.rotation;

        // Adjust the position so the doors line up.
        Vector3 positionOffset = chosenDoorway.transform.position - attachDoorway.transform.position;
        newRoom.transform.position += positionOffset;

        newRoom.transform.position += chosenDoorway.transform.forward * doorGap;
    }

    bool CheckValidPosition(GameObject newRoom)
    {
        BoxCollider roomCollider = newRoom.GetComponent<BoxCollider>();
        if(roomCollider != null) {
            // Use an OverlapBox to check if the room collides with something (using roomLayerMask)
            Collider[] hitColliders = Physics.OverlapBox(roomCollider.transform.TransformPoint(roomCollider.center), roomCollider.size * 0.99f / 2, roomCollider.transform.rotation, floorLayerMask);

            foreach(Collider collider in hitColliders) {
                if(collider.gameObject == newRoom) continue;

                //Debug.Log("Tried to spawn a room in an invalid spot. (overlapping) - " + collider.gameObject.name);
                return false;
            }

            return CheckValidDoors(newRoom);
        } else {
            //Debug.Log("No room collider");
            return false;
        }
    }

    bool CheckValidDoors(GameObject newRoom) {
        Doorway[] doors = newRoom.GetComponentsInChildren<Doorway>();
        List<List<Doorway>> validPairs = new List<List<Doorway>>();

        foreach(Doorway d in doors) {
            Vector3 origin = d.transform.position;
            Vector3 direction = d.transform.forward;

            Debug.DrawRay(origin, direction * 0.1f, Color.magenta, 10f);


            RaycastHit hit;
            if(!Physics.Raycast(origin, direction, out hit, 0.1f)) {
                continue;
            }
            if(!hit.collider.gameObject.CompareTag("door")) {
                //Debug.Log(hit.collider.name);
                //Debug.Log("Error: Object hit by door " + d.name + " is not a valid doorway.");
                return false;
            } else if(hit.collider.gameObject.CompareTag("door")){
                Doorway otherDoor = hit.collider.GetComponent<Doorway>();
                validPairs.Add(new List<Doorway> {d, otherDoor});
            }
        }
        foreach(List<Doorway> pair in validPairs) {
            Doorway d1 = pair.ElementAt(0);
            Doorway d2 = pair.ElementAt(1);
            d1.connected = true;
            d1.SetConnecetedDoor(d2);
            d2.connected = true;
            d2.SetConnecetedDoor(d1);
        }
        return true;
    }

    void CheckDoorConnections() {
        foreach(GameObject room in rooms) {
            foreach(Doorway door in room.GetComponentsInChildren<Doorway>()) {
                if(door.GetConnectedDoor()) {
                    
                    if(door.GetConnectedDoor().GetConnectedDoor() != door) {
                        door.connected = false;
                        door.GetConnectedDoor().connected = false;
                    }
                } else {
                    door.connected = false;
                }
            }
        }
    }

    bool newCheckValidDoors(GameObject newRoom) {
        Doorway[] doors = newRoom.GetComponentsInChildren<Doorway>();
        List<List<Doorway>> validPairs = new List<List<Doorway>>();

        foreach(Doorway door in doors) {
            Vector3 origin = door.transform.position;
            Vector3 direction = door.transform.forward;

            Collider[] colliders = Physics.OverlapBox(origin, new Vector3(0.05f, 0.05f, 0.05f));

            foreach(Collider collider in colliders) {
                if(collider.gameObject.tag == "wall") {
                    Debug.Log("wall");
                    taggedWall = collider.gameObject;
                    return false;
                } else if(collider.gameObject.tag == "door") {
                    Doorway otherDoor = collider.GetComponentInParent<Doorway>();
                    validPairs.Add(new List<Doorway> {door, otherDoor});
                }
            }
        }

        foreach(List<Doorway> pair in validPairs) {
            Doorway d1 = pair.ElementAt(0);
            Doorway d2 = pair.ElementAt(1);
            d1.connected = true;
            d2.connected = true;
        }

        return true;
    }

    void OnDrawGizmos() {
        GameObject[] allObjs = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach(GameObject go in allObjs) {
            if(go.tag != "room") continue;
            BoxCollider roomCollider = go.GetComponent<BoxCollider>();
            if(roomCollider != null) {
                Gizmos.color = Color.blue;
                // Calculate the world-space center and extents
                Vector3 worldCenter = roomCollider.transform.TransformPoint(roomCollider.center);
                Vector3 extents = roomCollider.size / 2;
                Gizmos.matrix = Matrix4x4.TRS(worldCenter, roomCollider.transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, roomCollider.size);
            }
        }
        
    }
}