using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

public class AIDirector : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public EnemyData enemyData;
        public float initialSpawnDelay = 0f;
        public float spawnInterval = 30f;
        public int maxConcurrent = 2;
        public float difficultyThreshold = 0f; // Minimum difficulty level to spawn this enemy
        public float spawnWeight = 1f; // Weight for random selection (higher = more likely)
        public bool ignoreGlobalLimit = false; // If true, this enemy type doesn't count toward the global limit
    }
    
    [Header("Spawn Settings")]
    public List<EnemySpawnInfo> enemyTypes = new List<EnemySpawnInfo>();
    public int minimumRoomsFromStart = 2; // Minimum number of rooms away from start to spawn enemies
    public float spawnHeightOffset = 0.5f; // Height above floor to spawn enemies
    public float minDistanceFromPlayer = 10f; // Minimum distance from player for spawning
    public float minDistanceBetweenEnemies = 3f; // Minimum distance between enemies
    public LayerMask floorLayerMask; // Layer mask for detecting floors
    
    [Header("Global Enemy Limit")]
    public int maxGlobalEnemies = 10; // Maximum number of enemies that can be spawned at once
    public float globalSpawnInterval = 15f; // Time between global spawn attempts
    private float lastGlobalSpawnTime = 0f;
    
    [Header("Per-Level Enemy Limit")]
    public int maxEnemiesPerLevel = 30; // Maximum total enemies to spawn in this level
    public bool enablePerLevelLimit = true; // Toggle for the per-level limit
    private int totalEnemiesSpawned = 0; // Counter for total enemies spawned in this level
    private Dictionary<GameObject, int> enemiesSpawnedByType = new Dictionary<GameObject, int>(); // Track spawns per type
    
    [Header("Difficulty Settings")]
    public float baseDifficulty = 1f;
    public float difficultyIncreaseRate = 0.1f; // Per minute
    public float currentDifficulty = 1f;
    public float maxDifficulty = 10f;
    
    // Runtime tracking
    private Dictionary<GameObject, List<Enemy>> spawnedEnemies = new Dictionary<GameObject, List<Enemy>>();
    private float gameTimer = 0f;
    private Dictionary<GameObject, int> roomDistanceFromStart = new Dictionary<GameObject, int>();
    private GameObject startRoom;
    private List<GameObject> validSpawnRooms = new List<GameObject>();
    private List<GameObject> allRooms = new List<GameObject>();
    private int totalActiveEnemies = 0; // Track enemies that count towards the global limit

    private void Awake()
    {
        enabled = false;
        NavMeshBuilder.OnNavMeshBuilt += OnLevelGenerated; // Subscribe to NavMesh built event
    }

    private void OnLevelGenerated()
    {
        enabled = true;
        FindAndMapRooms();
        
        // Reset spawn counters for the new level
        totalEnemiesSpawned = 0;
        enemiesSpawnedByType.Clear();

        foreach(var enemyInfo in enemyTypes)
        {
            spawnedEnemies[enemyInfo.enemyPrefab] = new List<Enemy>();
            enemiesSpawnedByType[enemyInfo.enemyPrefab] = 0; // Initialize counter for this enemy type

            if(enemyInfo.initialSpawnDelay >= 0) {
                StartCoroutine(SpawnWithDelay(enemyInfo, enemyInfo.initialSpawnDelay));
            }
        }

        NavMeshBuilder.OnNavMeshBuilt -= OnLevelGenerated;
    }

    private void FindAndMapRooms()
    {
        // Find all rooms in the level
        GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("room");
        allRooms.AddRange(roomObjects);
        
        // Find the start room
        startRoom = GameObject.FindGameObjectWithTag("startroom");
        
        foreach(var room in allRooms)
        {
            RoomInfo roomInfo = room.GetComponent<RoomInfo>();
            if(roomInfo != null && roomInfo.distanceFromStart >= minimumRoomsFromStart && roomInfo.isEnemySpawnAllowed)
            {
                validSpawnRooms.Add(room);
            }
        }
    }
    
    private void Update()
    {
        // Update game timer
        gameTimer += Time.deltaTime;
        
        // Update difficulty based on time
        currentDifficulty = Mathf.Min(maxDifficulty, baseDifficulty + (difficultyIncreaseRate * (gameTimer / 60f)));
        
        // Clean up tracking of destroyed enemies and update count
        totalActiveEnemies = 0;
        foreach (var enemyType in enemyTypes)
        {
            List<Enemy> enemiesOfType = spawnedEnemies[enemyType.enemyPrefab];
            enemiesOfType.RemoveAll(e => e == null);
            
            // Only count enemies that aren't ignored for global limit
            if (!enemyType.ignoreGlobalLimit)
            {
                totalActiveEnemies += enemiesOfType.Count;
            }
        }
        
        // Check if we can spawn a new enemy based on concurrent limit and per-level limit
        bool canSpawnMoreEnemies = totalActiveEnemies < maxGlobalEnemies && 
                                  (!enablePerLevelLimit || totalEnemiesSpawned < maxEnemiesPerLevel);
                                  
        if (canSpawnMoreEnemies && Time.time - lastGlobalSpawnTime >= globalSpawnInterval)
        {
            // Time to attempt a spawn
            AttemptGlobalSpawn();
            lastGlobalSpawnTime = Time.time;
        }
    }
    
    private IEnumerator SpawnWithDelay(EnemySpawnInfo enemyInfo, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemy(enemyInfo);
    }
    
    private void SpawnEnemy(EnemySpawnInfo enemyInfo)
    {
        // Check if we've reached the per-level limit
        if (enablePerLevelLimit && totalEnemiesSpawned >= maxEnemiesPerLevel)
        {
            return;
        }
        
        if (validSpawnRooms.Count == 0)
        {
            Debug.LogWarning("No valid rooms for enemy spawning");
            return;
        }
        
        // Try to find a valid spawn position
        Vector3 spawnPosition;
        GameObject selectedRoom;
        int maxAttempts = 10;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Select a random room from valid rooms
            selectedRoom = validSpawnRooms[Random.Range(0, validSpawnRooms.Count)];
            
            // Find a random position within the room
            if (TryGetSpawnPositionInRoom(selectedRoom, out spawnPosition))
            {
                // Spawn the enemy
                GameObject enemyObject = Instantiate(enemyInfo.enemyPrefab, spawnPosition, Quaternion.identity);
                Enemy enemy = enemyObject.GetComponent<Enemy>();
                
                if (enemy != null)
                {
                    // Apply enemy data if provided
                    if (enemyInfo.enemyData != null)
                    {
                        enemy.enemyData = enemyInfo.enemyData;
                    }
                    
                    // Scale stats based on current difficulty
                    ScaleEnemyToDifficulty(enemy);
                    
                    // Add spawn tracker component
                    EnemySpawnTracker tracker = enemyObject.AddComponent<EnemySpawnTracker>();
                    tracker.spawnTime = Time.time;
                    tracker.spawnRoomDistance = selectedRoom.GetComponent<RoomInfo>().distanceFromStart;
                    
                    // Add to tracking list
                    spawnedEnemies[enemyInfo.enemyPrefab].Add(enemy);
                    
                    // Update spawn counters
                    if (!enemyInfo.ignoreGlobalLimit)
                    {
                        totalEnemiesSpawned++;
                    }
                    enemiesSpawnedByType[enemyInfo.enemyPrefab]++;
                    
                    // Make enemy look in a random direction
                    enemy.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    return; // Successfully spawned
                }
            }
        }
        
        Debug.LogWarning("Failed to find valid spawn position after " + maxAttempts + " attempts");
    }

    private void AttemptGlobalSpawn()
    {
        // Check if we've reached the per-level limit
        if (enablePerLevelLimit && totalEnemiesSpawned >= maxEnemiesPerLevel)
        {
            return;
        }
    
        // Get all eligible enemy types based on difficulty
        List<EnemySpawnInfo> eligibleTypes = new List<EnemySpawnInfo>();
        float totalWeight = 0f;
        
        foreach (var enemyInfo in enemyTypes)
        {
            List<Enemy> enemiesOfType = spawnedEnemies[enemyInfo.enemyPrefab];
            
            // Check if this type is eligible (below max concurrent and meets difficulty threshold)
            if (enemiesOfType.Count < enemyInfo.maxConcurrent && 
                currentDifficulty >= enemyInfo.difficultyThreshold)
            {
                eligibleTypes.Add(enemyInfo);
                totalWeight += enemyInfo.spawnWeight;
            }
        }
        
        // If no eligible types, exit
        if (eligibleTypes.Count == 0 || totalWeight <= 0f)
            return;
        
        // Select a random enemy type based on weights
        float randomValue = Random.Range(0f, totalWeight);
        float weightSum = 0f;
        EnemySpawnInfo selectedType = null;
        
        foreach (var enemyInfo in eligibleTypes)
        {
            weightSum += enemyInfo.spawnWeight;
            if (randomValue <= weightSum)
            {
                selectedType = enemyInfo;
                break;
            }
        }
        
        // If we have a selected type, spawn it
        if (selectedType != null)
        {
            SpawnEnemy(selectedType);
        }
    }

    private bool TryGetSpawnPositionInRoom(GameObject room, out Vector3 position)
    {
        position = Vector3.zero;
        if (room == null) return false;
        
        // Get the room's collider (assuming Box Collider for simplicity)
        BoxCollider roomCollider = room.GetComponent<BoxCollider>();
        if (roomCollider == null) 
        {
            // Try to find a collider in children if none on the room itself
            roomCollider = room.GetComponentInChildren<BoxCollider>();
            if (roomCollider == null) return false;
        }
        
        // Get room bounds in world space
        Bounds roomBounds = roomCollider.bounds;
        
        // Try multiple positions
        for (int i = 0; i < 10; i++)
        {
            // Get a random point within the room bounds
            Vector3 randomPoint = new Vector3(
                Random.Range(roomBounds.min.x + 1f, roomBounds.max.x - 1f),
                roomBounds.center.y,
                Random.Range(roomBounds.min.z + 1f, roomBounds.max.z - 1f)
            );

            Vector3 rayOrigin = randomPoint;

            // Cast ray down to find the floor
            RaycastHit hit;
            if (Physics.Raycast(randomPoint, Vector3.down, out hit, 4f, floorLayerMask))
            {
                Vector3 potentialPosition = hit.point + Vector3.up * spawnHeightOffset;
                
                // Check distance from other enemies
                bool tooCloseToOtherEnemies = false;
                foreach (var enemyList in spawnedEnemies.Values)
                {
                    foreach (var enemy in enemyList)
                    {
                        if (enemy != null && Vector3.Distance(potentialPosition, enemy.transform.position) < minDistanceBetweenEnemies)
                        {
                            tooCloseToOtherEnemies = true;
                            break;
                        }
                    }
                    if (tooCloseToOtherEnemies) break;
                }
                
                if (tooCloseToOtherEnemies) continue;
                
                // Check if the position is on the NavMesh
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(potentialPosition, out navHit, 1.0f, NavMesh.AllAreas))
                {
                    position = navHit.position;
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void ScaleEnemyToDifficulty(Enemy enemy)
    {
        // Apply difficulty scaling
        float difficultyModifier = currentDifficulty / baseDifficulty;
        
        // Scale health (increasing with difficulty)
        enemy.health = Mathf.RoundToInt(enemy.health * Mathf.Lerp(1f, 1.5f, (difficultyModifier - 1) / (maxDifficulty - 1)));
        
        // Scale movement speed (slight increase)
        enemy.movementSpeed *= Mathf.Lerp(1f, 1.2f, (difficultyModifier - 1) / (maxDifficulty - 1));
        
        // Apply changes to NavMeshAgent if present
        if (enemy.GetComponent<NavMeshAgent>() != null)
        {
            enemy.GetComponent<NavMeshAgent>().speed = enemy.movementSpeed;
        }
    }
    
    // Helper component to track spawn time and room distance
    private class EnemySpawnTracker : MonoBehaviour
    {
        public float spawnTime;
        public int spawnRoomDistance; // How far from start room this enemy was spawned
    }
    
    public int GetCurrentEnemyCount(bool includeIgnored = false)
    {
        if (includeIgnored)
        {
            int total = 0;
            foreach (var list in spawnedEnemies.Values)
            {
                total += list.Count;
            }
            return total;
        }
        return totalActiveEnemies;
    }
    
    public float GetGlobalSpawnProgress()
    {
        float timeSinceLastSpawn = Time.time - lastGlobalSpawnTime;
        return Mathf.Clamp01(timeSinceLastSpawn / globalSpawnInterval);
    }

    public void AdjustDifficulty(float amount)
    {
        currentDifficulty = Mathf.Clamp(currentDifficulty + amount, baseDifficulty * 0.5f, maxDifficulty);
    }
    
    public void TriggerSpecialEncounter()
    {
        // Example: Spawn a mini-boss or special encounter
        // Implementation would depend on your game's needs
    }
    
    public void RefreshRoomData()
    {
        FindAndMapRooms();
    }

    // Get total enemies spawned this level
    public int GetTotalEnemiesSpawned(bool includeIgnored = false)
    {
        if (!includeIgnored)
        {
            return totalEnemiesSpawned;
        }
        else
        {
            int total = 0;
            foreach (var count in enemiesSpawnedByType.Values)
            {
                total += count;
            }
            return total;
        }
    }
    
    // Get remaining enemies that can still be spawned
    public int GetRemainingSpawns()
    {
        if (!enablePerLevelLimit)
            return -1; // Unlimited
            
        return Mathf.Max(0, maxEnemiesPerLevel - totalEnemiesSpawned);
    }
    
    // Get spawned enemies by type
    public int GetEnemiesSpawnedByType(GameObject enemyPrefab)
    {
        if (enemiesSpawnedByType.ContainsKey(enemyPrefab))
        {
            return enemiesSpawnedByType[enemyPrefab];
        }
        return 0;
    }
}

// Example Enemy Behavior Component System - Alternative approach using components
// This shows an alternative component-based approach that can be used alongside the inheritance system

// Base enemy behavior component
public abstract class EnemyBehaviorComponent : MonoBehaviour
{
    protected Enemy enemyBase;
    
    protected virtual void Awake()
    {
        enemyBase = GetComponent<Enemy>();
    }
    
    public abstract void OnEnterState(EnemyState state);
    public abstract void OnUpdateState(EnemyState state);
    public abstract void OnExitState(EnemyState state);
}

// Example behavior: Circle target
public class EnemyCircleMovement : EnemyBehaviorComponent
{
    public float circleRadius = 5f;
    public float circleSpeed = 50f;
    public bool clockwise = true;
    
    private float currentAngle = 0f;
    private Vector3 targetPosition;
    
    public override void OnEnterState(EnemyState state)
    {
        if (state == EnemyState.Chase)
        {
            // Initialize angle based on current position
            if (enemyBase.target != null)
            {
                Vector3 toTarget = transform.position - enemyBase.target.position;
                currentAngle = Mathf.Atan2(toTarget.z, toTarget.x) * Mathf.Rad2Deg;
            }
        }
    }
    
    public override void OnUpdateState(EnemyState state)
    {
        if (state == EnemyState.Chase && enemyBase.target != null)
        {
            // Update angle
            float angleChange = circleSpeed * Time.deltaTime;
            currentAngle += clockwise ? -angleChange : angleChange;
            
            // Calculate position on circle
            float x = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * circleRadius;
            float z = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * circleRadius;
            
            // Set target position
            targetPosition = enemyBase.target.position + new Vector3(x, 0, z);
            
            // Move to position
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.SetDestination(targetPosition);
            }
            
            // Look at player
            transform.LookAt(enemyBase.target);
        }
    }
    
    public override void OnExitState(EnemyState state)
    {
        // Nothing needed here for this component
    }
}