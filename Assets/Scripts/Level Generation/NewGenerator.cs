using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NewGenerator : MonoBehaviour
{
    // Generation Settings
    [Tooltip("Number of rooms to generate")]
    public int numberOfRooms = 10;
    [Tooltip("Random seed for generation (0 = random)")]
    public int seed = 0;
    [Tooltip("Parent transform for rooms in the level")]
    public Transform roomParent;
    [Tooltip("Layer Mask that contains the masks that are applied to all room geometry. (for positioning)")]
    public LayerMask roomLayerMask;

    // Room Prefabs
    public List<Room> roomList = new List<Room>();
    [Tooltip("Prefab of starting room for the level.")]
    public GameObject startRoomPrefab;

    // Debug Settings
    [Tooltip("Should show debug information.")]
    public bool showDebug = true;
    [Tooltip("Show debug messages")]
    public bool showLogs = true;
    [Tooltip("How long debug information will stay in scene")]
    public float debugLineDuration = 3.0f;

    // Events
    public static event System.Action OnLevelGenerationComplete;

    public List<Collider> collisions;

    private List<GameObject> rooms = new List<GameObject>();
    private List<Doorway> openDoors = new List<Doorway>();
    private Stack<GameObject> roomHistory = new Stack<GameObject>();

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        ClearDungeon();

        if(roomList.Count == 0) {
            Debug.LogError("No rooms in the room list. Add one to continue.");
            return;
        } 

        if(roomParent == null) {
            GameObject rootObj = new GameObject("Dungeon");
            roomParent = rootObj.transform;
        }

        if(seed == 0) {
            int newSeed = Random.Range(0, int.MaxValue);
            Random.InitState(newSeed);
            Debug.Log("Current Seed: " + newSeed);
        } else {
            Random.InitState(seed);
        }

        // Spawn first room
        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity, roomParent);
        rooms.Add(startRoom);
        roomHistory.Push(startRoom);

        RoomInfo startRoomInfo = startRoom.AddComponent<RoomInfo>();
        startRoomInfo.distanceFromStart = 0;

        // Add first room's door
        Doorway[] doors = startRoom.GetComponentsInChildren<Doorway>();
        foreach(Doorway door in doors) {
            openDoors.Add(door);
        }

        // Generate the rest of the rooms
        GenerateRooms();

        Debug.Log("Number of rooms: " + rooms.Count);
        Debug.Log("Remaining open doors: " + openDoors.Count);

        if(OnLevelGenerationComplete != null) {
            OnLevelGenerationComplete.Invoke();
        }
    }

    void ClearDungeon()
    {
        foreach(GameObject room in rooms) {
            DestroyImmediate(room);
        }
        rooms.Clear();
        openDoors.Clear();
        roomHistory.Clear();
    }

    bool GenerateRooms() 
    {
        if(rooms.Count >= numberOfRooms) {
            return true;
        }

        while(openDoors.Count > 0 && rooms.Count < numberOfRooms) {
            List<Room> potentialPrefabs = new List<Room>(roomList);
            Doorway selectedDoor = openDoors[0];

            while(potentialPrefabs.Count > 0) {
                GameObject newRoomPrefab = GetRandomWeightedRoomPrefab(roomList);
                GameObject newRoom = Instantiate(newRoomPrefab, roomParent);
                Physics.SyncTransforms();

                // Get all the possible doors for this new room
                List<Doorway> newRoomDoors = newRoom.GetComponentsInChildren<Doorway>().ToList();
                if(newRoomDoors.Count == 0) {
                    Debug.LogWarning("This prefab(" + newRoomPrefab.name + ")has no valid doors");
                    DestroyImmediate(newRoom);
                    continue;
                }
                
                // Go through all the doors to find a viable door if possible
                while(newRoomDoors.Count > 0) {
                    Doorway candidateDoor = newRoomDoors[0];

                    AlignRooms(selectedDoor, candidateDoor, newRoom);

                    // Check if the room is trying to be placed where another room already is
                    if(!ValidRoomLocation(newRoom)) {
                        newRoomDoors.RemoveAt(0);
                        continue;
                    }

                    // Check to see if the doors on the new room are in valid positions
                    if(!ValidDoorLocations(newRoom)) {
                        newRoomDoors.RemoveAt(0);
                        continue;
                    }

                    List<Doorway> copyOpenDoors = new List<Doorway>(openDoors);

                    // Setup door connections
                    selectedDoor.connected = true;
                    candidateDoor.connected = true;
                    openDoors.Remove(selectedDoor);

                    // Keep rooms list up to date
                    rooms.Add(newRoom);
                    roomHistory.Push(newRoom);

                    int parentDistance = selectedDoor.transform.parent.GetComponent<RoomInfo>().distanceFromStart;
                    RoomInfo newRoomInfo = newRoom.AddComponent<RoomInfo>();
                    newRoomInfo.distanceFromStart = parentDistance + 1;
                    // Add the remaining doorways of the new room to the openDoors list
                    AddDoorsFromRoom(newRoom);

                    if(GenerateRooms()) { // Successfully hit an end condition
                        return true;
                    } else { // Something went wrong, backtrack
                        while(roomHistory.Peek() != newRoom) {
                            GameObject tempRoom = roomHistory.Pop();
                            Doorway[] tempDoors = tempRoom.GetComponentsInChildren<Doorway>();
                            foreach(Doorway door in tempDoors) {
                                door.connected = false;
                                if(openDoors.Contains(door)) {
                                    openDoors.Remove(door);
                                }
                            }
                            rooms.Remove(tempRoom);
                            Collider tempCollider = tempRoom.GetComponent<Collider>();
                            if(tempCollider != null) {
                                tempCollider.enabled = false;
                            }
                            DestroyImmediate(tempRoom);
                        }
                        roomHistory.Pop();
                        Doorway[] tempNewDoors = newRoom.GetComponentsInChildren<Doorway>();
                        foreach(Doorway door in tempNewDoors) {
                            door.connected = false;
                            if(openDoors.Contains(door)) {
                                openDoors.Remove(door);
                            }
                        }
                        selectedDoor.connected = false;
                        
                        Collider tempRCollider = newRoom.GetComponent<Collider>();
                        if(tempRCollider != null) {
                            tempRCollider.enabled = false;
                        }

                        rooms.Remove(newRoom);
                        DestroyImmediate(newRoom);
                        openDoors = copyOpenDoors;

                        break;
                    }
                } // end newRoomDoors.Count loop

                // If there is not a valid door for the selected room prefab
                potentialPrefabs.RemoveAll(rm => rm.roomPrefab == newRoomPrefab);
                DestroyImmediate(newRoom);

            } // End Potential Prefabs Count loop

            openDoors.Remove(selectedDoor);

        } // End main loop

        // Could not successfully attach room to door
        return false;
    }

    bool ValidRoomLocation(GameObject room) 
    {
        Collider roomCollider = room.GetComponent<BoxCollider>();
        Vector3 center = roomCollider.transform.position;

        collisions = Physics.OverlapBox(center, roomCollider.bounds.extents / 2f, Quaternion.identity, roomLayerMask, QueryTriggerInteraction.Collide).ToList();
        if(collisions.Count > 0) {
            foreach(Collider collision in collisions) {
                if(collision == roomCollider) {
                    continue;
                }
                return false;
            }
        }

        Vector3 startRoomPos = rooms[0].transform.position;
        Vector3 newRoomPos = room.transform.position;

        bool alignedX = Mathf.Abs(newRoomPos.x - startRoomPos.x) < 0.1f;
        if(alignedX) { // If a room is in lign with the start room, reject the new room
            return false;
        }

        return true;
    }

    bool ValidDoorLocations(GameObject room)
    {
        Doorway[] doors = room.GetComponentsInChildren<Doorway>();
        List<List<Doorway>> validPairs = new List<List<Doorway>>();

        foreach(Doorway door in doors) {
            Vector3 origin = door.transform.position;
            Vector3 direction = door.transform.forward;

            RaycastHit hit;
            if(!Physics.Raycast(origin, direction, out hit, 0.1f)) {
                continue;
            }
            if(hit.collider.gameObject.CompareTag("door")) {
                Doorway otherDoor = hit.collider.GetComponent<Doorway>();
                validPairs.Add(new List<Doorway> {door, otherDoor});
            } else {
                return false;
            }
        }
        foreach(List<Doorway> pair in validPairs) {
            Doorway d1 = pair.ElementAt(0);
            Doorway d2 = pair.ElementAt(1);
            d1.connected = true;
            d2.connected = true;
            d1.SetConnecetedDoor(d2);
            d2.SetConnecetedDoor(d1);
        }
        return true;
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

    void AddDoorsFromRoom(GameObject room)
    {
        Doorway[] doorways = room.GetComponentsInChildren<Doorway>();
        foreach(Doorway door in doorways) {
            if(!door.connected && !openDoors.Contains(door)) {
                openDoors.Add(door);
            }
        }
    }

    private GameObject GetRandomWeightedRoomPrefab(List<Room> availablePrefabs)
    {
        // Calculate the total weight
        bool allWeightsZero = false;
        float totalWeight = 0f;
        foreach (var weightedRoom in availablePrefabs)
        {
            totalWeight += weightedRoom.weight;
        }

        if(totalWeight == 0) {
            allWeightsZero = true;
            totalWeight = availablePrefabs.Count;
        }
        
        // Generate a random value between 0 and the total weight
        float randomValue = Random.Range(0f, totalWeight);
        float weightSum = 0f;
        
        // Find which room the random value falls into
        foreach (var weightedRoom in availablePrefabs)
        {
            weightSum += allWeightsZero ? 1 : weightedRoom.weight;
            if (randomValue <= weightSum)
            {
                return weightedRoom.roomPrefab;
            }
        }
        
        // Fallback (should never reach here if weights > 0)
        return availablePrefabs[0].roomPrefab;
    }
}