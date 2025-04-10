using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NewGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public List<GameObject> roomPrefabs;
    public GameObject startRoomPrefab;
    public int numberOfRooms = 10;
    public int seed = 0;
    public LayerMask roomLayerMask;

    public List<Collider> collisions;

    private List<GameObject> rooms = new List<GameObject>();
    private List<Doorway> openDoors = new List<Doorway>();
    private Stack<GameObject> roomHistory = new Stack<GameObject>();

    void Start()
    {
        if(seed == 0) {
            int newSeed = Random.Range(0, int.MaxValue);
            Random.InitState(newSeed);
            Debug.Log("Current Seed: " + newSeed);
        } else {
            Random.InitState(seed);
        }

        // Spawn first room
        GameObject startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        rooms.Add(startRoom);
        roomHistory.Push(startRoom);
        // Add first room's door
        Doorway[] doors = startRoom.GetComponentsInChildren<Doorway>();
        foreach(Doorway door in doors) {
            openDoors.Add(door);
        }

        // Generate the rest of the rooms
        GenerateRooms();

        Debug.Log("Number of rooms: " + rooms.Count);
        Debug.Log("Remaining open doors: " + openDoors.Count);
    }

    bool GenerateRooms() 
    {
        if(rooms.Count >= numberOfRooms) {
            return true;
        }

        while(openDoors.Count > 0 && rooms.Count < numberOfRooms) {
            List<GameObject> potentialPrefabs = new List<GameObject>(roomPrefabs);
            Doorway selectedDoor = openDoors[0];

            while(potentialPrefabs.Count > 0) {
                GameObject newRoomPrefab = potentialPrefabs[Random.Range(0, potentialPrefabs.Count)];
                GameObject newRoom = Instantiate(newRoomPrefab);
                Physics.SyncTransforms();

                // Get all the possible doors for this new room
                List<Doorway> newRoomDoors = newRoom.GetComponentsInChildren<Doorway>().ToList();
                if(newRoomDoors.Count == 0) {
                    Debug.Log("This prefab(" + newRoomPrefab.name + ")has no valid doors");
                    Destroy(newRoom);
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
                            Destroy(tempRoom);
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
                        Destroy(newRoom);
                        openDoors = copyOpenDoors;

                        break;
                    }
                } // end newRoomDoors.Count loop

                // If there is not a valid door for the selected room prefab
                potentialPrefabs.Remove(newRoomPrefab);
                Destroy(newRoom);

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
                Debug.Log("Collided with something and couldn't spawn");
                return false;
            }
        }
        return true;
    }

    bool ValidDoorLocations(GameObject room)
    {
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
}