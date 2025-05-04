using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    public EnemyData enemyData;
    public int health;
    public float detectionRange;
    public float attackRange;
    public float movementSpeed;
    
    [Header("State")]
    protected EnemyState currentState;
    public Transform target;
    
    // Components
    protected UnityEngine.AI.NavMeshAgent navAgent;
    protected Animator animator;
    
    protected virtual void Awake()
    {
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Initialize from data if provided
        if (enemyData != null)
        {
            health = enemyData.maxHealth;
            detectionRange = enemyData.detectionRange;
            attackRange = enemyData.attackRange;
            movementSpeed = enemyData.movementSpeed;
        }
    }
    
    protected virtual void Start()
    {
        // Set initial state
        ChangeState(EnemyState.Idle);
        
        // Set NavMeshAgent speed
        if (navAgent != null)
        {
            navAgent.speed = movementSpeed;
        }
    }
    
    protected virtual void Update()
    {
        UpdateCurrentState();
    }
    
    // State machine management
    public virtual void ChangeState(EnemyState newState)
    {
        // Exit current state
        switch (currentState)
        {
            case EnemyState.Idle:
                ExitIdleState();
                break;
            case EnemyState.Patrol:
                ExitPatrolState();
                break;
            case EnemyState.Chase:
                ExitChaseState();
                break;
            case EnemyState.Attack:
                ExitAttackState();
                break;
            case EnemyState.Stunned:
                ExitStunnedState();
                break;
        }
        
        // Set new state
        currentState = newState;
        
        // Enter new state
        switch (currentState)
        {
            case EnemyState.Idle:
                EnterIdleState();
                break;
            case EnemyState.Patrol:
                EnterPatrolState();
                break;
            case EnemyState.Chase:
                EnterChaseState();
                break;
            case EnemyState.Attack:
                EnterAttackState();
                break;
            case EnemyState.Stunned:
                EnterStunnedState();
                break;
        }
    }
    
    protected virtual void UpdateCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState();
                break;
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;
            case EnemyState.Chase:
                UpdateChaseState();
                break;
            case EnemyState.Attack:
                UpdateAttackState();
                break;
            case EnemyState.Stunned:
                UpdateStunnedState();
                break;
        }
    }
    
    // State Methods - to be overridden by derived classes
    #region State Methods
    protected virtual void EnterIdleState() { }
    protected virtual void UpdateIdleState() 
    {
        // Default behavior: Look for player
        LookForTarget();
    }
    protected virtual void ExitIdleState() { }
    
    protected virtual void EnterPatrolState() { }
    protected virtual void UpdatePatrolState() 
    {
        // Default behavior: Look for player while patrolling
        LookForTarget();
    }
    protected virtual void ExitPatrolState() { }
    
    protected virtual void EnterChaseState() { }
    protected virtual void UpdateChaseState() 
    {
        if (target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        // Move toward target
        if (navAgent != null)
        {
            navAgent.SetDestination(target.position);
        }
        
        // Check if close enough to attack
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            ChangeState(EnemyState.Attack);
        }
        
        // Check if target is out of detection range
        if (distanceToTarget > detectionRange * 1.5f)
        {
            target = null;
            ChangeState(EnemyState.Idle);
        }
    }
    protected virtual void ExitChaseState() 
    {
        if (navAgent != null)
        {
            navAgent.ResetPath();
        }
    }
    
    protected virtual void EnterAttackState() { }
    protected virtual void UpdateAttackState() 
    {
        if (target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        // Face the target
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        
        // Check if target is out of attack range
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > attackRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    protected virtual void ExitAttackState() { }
    
    protected virtual void EnterStunnedState() { }
    protected virtual void UpdateStunnedState() { }
    protected virtual void ExitStunnedState() { }
    #endregion
    
    // Common functionality
    protected virtual void LookForTarget()
    {
        // Default implementation: Look for player tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= detectionRange)
            {
                target = player.transform;
                ChangeState(EnemyState.Chase);
            }
        }
    }
    
    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Optional: Play hit animation or sound
        }
    }
    
    protected virtual void Die()
    {
        // Base implementation
        Destroy(gameObject);
    }
    
    // Optional: Draw gizmos for easier debugging
    protected virtual void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
