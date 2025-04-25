using UnityEngine;
using System.Collections.Generic;


public class ItemSpawner : MonoBehaviour
{
    public bool spawnItems = true;
    public float spawnChance = 0.5f;
    public List<Item> items = new List<Item>();

    public void SpawnItems(List<GameObject> rooms)
    {
        foreach(var room in rooms)
        {
            RoomInfo roomInfo = room.GetComponent<RoomInfo>();
            if(roomInfo == null)
            {
                Debug.LogError("Room script not found on room object: " + room.name);
                continue;
            }

            foreach(var spawnPoint in roomInfo.itemSpawnPoints)
            {
                if (Random.value < spawnChance)
                {
                    Item itemToSpawn = items[Random.Range(0, items.Count)];
                    GameObject item = Instantiate(itemToSpawn.itemPrefab, spawnPoint.transform.position, Quaternion.identity);
                    item.transform.SetParent(spawnPoint.transform);
                    item.SetActive(true);

                    PickableItem pickableItem = item.GetComponent<PickableItem>();
                    if(pickableItem != null)
                    {
                        pickableItem.SetRandomizedPrice(GameManager.Instance.Difficulty, GameManager.Instance.CurrentLevel);
                    }
                    spawnPoint.GetComponent<ItemSpawnPoint>().isOccupied = true; // Mark the spawn point as occupied
                }
            }

        }
    }
}