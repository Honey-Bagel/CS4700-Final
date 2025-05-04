using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Stats")]
    public string enemyName;
    public int maxHealth = 100;
    public float movementSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    
    [Header("Attack Properties")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    
    [Header("Advanced Behavior")]
    public bool canPatrol = true;
    public float patrolRadius = 10f;
    public float idleTime = 3f;
    public float stunDuration = 2f;
    
    [Header("Drops")]
    public GameObject[] possibleDrops;
    [Range(0, 1)] public float dropChance = 0.5f;
}