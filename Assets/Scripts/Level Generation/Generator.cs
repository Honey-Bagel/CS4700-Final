using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Generator : MonoBehaviour
{
    // Singleton
    public static Generator Instance { get; private set; }

    public bool IsGenerationComplete { get; private set; } = false;
    public bool IsGenerationInProgress { get; private set; } = false;

    // Generation Settings
    [Tooltip("Number of rooms to generate")]
    public int numberOfRooms = 10;
    [Tooltip("Random seed for generation (0 = random)")]
    public int seed = 0;
    [Tooltip("Parent transform for rooms in the level")]
    public Transform roomParent;
    [Tooltip("Layer Mask that contains the masks that are applied to all room geometry. (for positioning)")]
    public LayerMask roomLayerMask;
    public GameObject startRoom;

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

    // Door settings
    public List<DoorPrefab> doorPrefabs = new List<DoorPrefab>();
    
    [System.Serializable]
    public class DoorPrefab
    {
        public DoorSize doorSize;
        public GameObject doorPrefab;
        public GameObject lockedDoorPrefab;
    }

    [Header("Item Spawning")]
    public ItemSpawner itemSpawner;

    // Events
    public static event System.Action OnLevelGenerationComplete;

    public List<Collider> collisions;

    private List<GameObject> rooms = new List<GameObject>();
    private List<Doorway> openDoors = new List<Doorway>();
    private Stack<GameObject> roomHistory = new Stack<GameObject>();

    private void Awake()
    {
        if(Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void FinalizeGeneration()
    {
        if(itemSpawner != null) {
            itemSpawner.SpawnItems(rooms);
        }
        
        IsGenerationComplete = true;
        IsGenerationInProgress = false;

        if(OnLevelGenerationComplete != null) {
            OnLevelGenerationComplete.Invoke();
        }

        DisableDoorwayColliders();
    }

    void Start()
    {
        float modifier = UnityEngine.Random.Range(0.8f, 1.2f);
        float difficultyFactor = 1.0f + (GameManager.Instance.Difficulty * 0.1f);
        float levelFactor = 1.0f + (GameManager.Instance.CurrentLevel * 0.05f);

        // Adjust the number of rooms based on the current level
        numberOfRooms = Mathf.RoundToInt(numberOfRooms * modifier * difficultyFactor * levelFactor);

        if(!IsSceneBeingLoadedAsync()) {
            StartGeneration();
        }
    }

    private bool IsSceneBeingLoadedAsync()
    {
        return !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded;
    }

    public void StartGeneration()
    {
        if(IsGenerationInProgress || IsGenerationComplete) {
            return;
        }

        IsGenerationInProgress = true;
        IsGenerationComplete = false;

        StartCoroutine(GenerateDungeonDelayed());
    }

    private System.Collections.IEnumerator GenerateDungeonDelayed()
    {
        yield return null;
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

        if(startRoom == null) {
            Debug.LogError("No start room prefab assigned. Please assign one to continue.");
            return;
        }

        // Spawn first room
        // GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity, roomParent);
        rooms.Add(startRoom);
        roomHistory.Push(startRoom);

        RoomInfo startRoomInfo = startRoom.GetComponent<RoomInfo>();
        if(startRoomInfo == null) {
            startRoomInfo = startRoom.AddComponent<RoomInfo>();
        }
        startRoomInfo.distanceFromStart = 0;

        // Add first room's door
        Doorway[] doors = startRoom.GetComponentsInChildren<Doorway>();
        foreach(Doorway door in doors) {
            openDoors.Add(door);
        }

        // Generate the rest of the rooms
        GenerateRooms();

        GenerateDoors();

        Debug.Log("Number of rooms: " + rooms.Count);
        Debug.Log("Remaining open doors: " + openDoors.Count);

        FinalizeGeneration();
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
                    // selectedDoor.connected = true;
                    // candidateDoor.connected = true;
                    openDoors.Remove(selectedDoor);

                    // Keep rooms list up to date
                    rooms.Add(newRoom);
                    roomHistory.Push(newRoom);

                    int parentDistance = selectedDoor.transform.parent.GetComponent<RoomInfo>().distanceFromStart;
                    RoomInfo newRoomInfo = newRoom.GetComponent<RoomInfo>();
                    if(newRoomInfo == null) {
                        newRoomInfo = newRoom.AddComponent<RoomInfo>();
                    } 
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

    void GenerateDoors()
    {
        // Get all doorways in all rooms
        List<Doorway> allDoorways = new List<Doorway>();
        foreach (GameObject room in rooms)
        {
            allDoorways.AddRange(room.GetComponentsInChildren<Doorway>());
        }
        
        // Track processed doorways to ensure we spawn only one door per pair
        HashSet<Doorway> processedDoorways = new HashSet<Doorway>();
        
        // Process connected doorways (doorway pairs)
        foreach (Doorway doorway in allDoorways)
        {
            // Skip already processed doorways
            if (processedDoorways.Contains(doorway))
                continue;
                    
            if (doorway.connected && doorway.connectedDoor != null)
            {
                // Mark both doorways as processed
                processedDoorways.Add(doorway);
                processedDoorways.Add(doorway.connectedDoor);

                if(doorway.spawnDoor == false || doorway.connectedDoor.spawnDoor == false)
                    continue;
                
                // Find the matching door prefab for this doorway size
                GameObject doorPrefab = GetDoorPrefabForSize(doorway.doorSize);
                
                if (doorPrefab != null)
                {
                    // Calculate the horizontal midpoint between the two doorways
                    // but maintain the correct height for the door
                    float midX = (doorway.transform.position.x + doorway.connectedDoor.transform.position.x) / 2f;
                    float midZ = (doorway.transform.position.z + doorway.connectedDoor.transform.position.z) / 2f;
                    
                    // Use the floor height (Y) for door placement
                    // Find the lower of the two doorway Y positions
                    float doorY = Mathf.Min(doorway.transform.position.y, doorway.connectedDoor.transform.position.y);
                    
                    Vector3 doorPosition = new Vector3(midX, doorY, midZ);
                    
                    // Determine the correct rotation
                    Quaternion doorRotation;
                    
                    // Get the direction vector between the two doorways (ignore Y)
                    Vector3 doorwayDirection = doorway.connectedDoor.transform.position - doorway.transform.position;
                    doorwayDirection.y = 0; // Keep it on the horizontal plane
                    
                    // Calculate rotation to face along this direction
                    if (doorwayDirection != Vector3.zero)
                    {
                        doorRotation = Quaternion.LookRotation(doorwayDirection.normalized);
                    }
                    else
                    {
                        // Fallback if they're at the same position somehow
                        doorRotation = doorway.transform.rotation;
                    }
                    
                    // Create a parent object to hold the door
                    GameObject doorContainer = new GameObject("DoorContainer");
                    doorContainer.transform.SetParent(doorway.transform.parent);
                    doorContainer.transform.position = doorPosition;
                    doorContainer.transform.rotation = doorRotation;
                    
                    // Create door as child with local position offset if needed
                    GameObject door = Instantiate(doorPrefab);
                    door.transform.SetParent(doorContainer.transform);
                    
                    // Reset the local position to ensure it's centered in the doorway
                    door.transform.localPosition = Vector3.zero;
                    door.transform.localRotation = Quaternion.identity;
                    
                    // Set up the door component
                    Door doorComponent = door.GetComponent<Door>();
                    if (doorComponent != null)
                    {
                        doorComponent.doorSize = doorway.doorSize;
                    }
                    
                    // Debug visualization
                    if (showDebug)
                    {
                        Debug.DrawLine(doorway.transform.position, doorway.connectedDoor.transform.position, Color.green, 5f);
                        Debug.DrawRay(doorPosition, Vector3.up, Color.yellow, 5f);
                    }
                }
                else
                {
                    Debug.LogWarning($"No door prefab found for size: {doorway.doorSize}");
                }
            }
        }
        
        // Process unconnected doorways (spawn locked doors)
        foreach (Doorway doorway in allDoorways)
        {
            // Skip already processed doorways
            if (processedDoorways.Contains(doorway))
                continue;
                    
            // This is an unconnected doorway - spawn a locked door
            GameObject lockedDoorPrefab = GetLockedDoorPrefabForSize(doorway.doorSize);
            
            if (lockedDoorPrefab != null)
            {            
                // Create container at doorway position
                GameObject doorContainer = new GameObject("LockedDoorContainer");
                doorContainer.transform.SetParent(doorway.transform.parent);
                doorContainer.transform.position = doorway.transform.position;
                doorContainer.transform.rotation = doorway.transform.rotation;
                
                // Create door as child with zero local offset
                GameObject door = Instantiate(lockedDoorPrefab);
                door.transform.SetParent(doorContainer.transform);
                door.transform.localPosition = Vector3.zero;
                door.transform.localRotation = Quaternion.identity;
                
                Door doorComponent = door.GetComponent<Door>();
                if (doorComponent != null)
                {
                    doorComponent.doorSize = doorway.doorSize;
                    doorComponent.isLocked = true;
                }
            }
            else
            {
                Debug.LogWarning($"No locked door prefab found for size: {doorway.doorSize}");
            }
            
            // Mark as processed
            processedDoorways.Add(doorway);
        }
    }

    private Vector3 GetDoorCenterOffset(GameObject doorPrefab)
    {
        // Try to find a collider to determine the center
        Collider doorCollider = doorPrefab.GetComponentInChildren<Collider>();
        if (doorCollider != null)
        {
            // Get the center point of the collider in local space
            return doorCollider.bounds.center - doorPrefab.transform.position;
        }
        
        // If no collider, check for a renderer to find visual center
        Renderer doorRenderer = doorPrefab.GetComponentInChildren<Renderer>();
        if (doorRenderer != null)
        {
            return doorRenderer.bounds.center - doorPrefab.transform.position;
        }
        
        // If neither is found, default to no offset
        return Vector3.zero;
    }

    GameObject GetDoorPrefabForSize(DoorSize size)
    {
        foreach (DoorPrefab doorPrefab in doorPrefabs)
        {
            if (doorPrefab.doorSize == size)
            {
                return doorPrefab.doorPrefab;
            }
        }
        
        // If no exact match, return the default door as fallback
        foreach (DoorPrefab doorPrefab in doorPrefabs)
        {
            if (doorPrefab.doorSize == DoorSize.Default)
            {
                return doorPrefab.doorPrefab;
            }
        }
        
        return doorPrefabs.Count > 0 ? doorPrefabs[0].doorPrefab : null;
    }

    // Helper method to get the appropriate locked door prefab for a given door size
    GameObject GetLockedDoorPrefabForSize(DoorSize size)
    {
        foreach (DoorPrefab doorPrefab in doorPrefabs)
        {
            if (doorPrefab.doorSize == size)
            {
                return doorPrefab.lockedDoorPrefab != null ? 
                    doorPrefab.lockedDoorPrefab : 
                    doorPrefab.doorPrefab; // Fall back to regular door prefab if no locked version exists
            }
        }
        
        // If no exact match, return the default locked door as fallback
        foreach (DoorPrefab doorPrefab in doorPrefabs)
        {
            if (doorPrefab.doorSize == DoorSize.Default)
            {
                return doorPrefab.lockedDoorPrefab != null ? 
                    doorPrefab.lockedDoorPrefab : 
                    doorPrefab.doorPrefab;
            }
        }
        
        return doorPrefabs.Count > 0 ? 
            (doorPrefabs[0].lockedDoorPrefab != null ? doorPrefabs[0].lockedDoorPrefab : doorPrefabs[0].doorPrefab) : 
            null;
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
            if(!Physics.Raycast(origin, direction, out hit, 0.2f)) {
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
            if(d1== d2){
                d2 = rooms[0].GetComponentInChildren<Doorway>();
            }
            d1.connected = true;
            d2.connected = true;
            d1.SetConnectedDoor(d2);
            d2.SetConnectedDoor(d1);
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

    private void DisableDoorwayColliders()
    {
        foreach(GameObject room in rooms)
        {
            Doorway[] doorways = room.GetComponentsInChildren<Doorway>();
            foreach(Doorway door in doorways)
            {
                if(door != null && door.GetComponent<Collider>() != null)
                {
                    door.GetComponent<Collider>().enabled = false;
                }
            }
        }
    }
}