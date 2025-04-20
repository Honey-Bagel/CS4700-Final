using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemy : Enemy
{
    [Header("Patrol Settings")]
    public bool usePatrol = true;
    public float patrolRadius = 10f;
    public float idleTime = 3f;
    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private float patrolTimer;

    [Header("Navigation Settings")]
    public float pathTimeout = 4.0f;
    public float stuckDistance = 0.2f;
    private float pathTimer = 0f;
    private Vector3 lastPosition;
    private bool isStuck = false;
    
    [Header("Attack Settings")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Detection Settings")]
    public bool requireLineOfSight = true;
    public LayerMask obstacleLayerMask;
    public float sightCheckFrequency = 0.2f;
    [Tooltip("How long the enemy will remember and chase a hidden player (in seconds)")]
    public float memoryDuration = 5f;
    private float lastSightCheck;
    private bool hasLineOfSight = false;
    private float lostSightTime = 0f;
    private bool isSearching = false;
    
    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
        
        if (enemyData != null)
        {
            attackDamage = enemyData.attackDamage;
            attackCooldown = enemyData.attackCooldown;
            patrolRadius = enemyData.patrolRadius;
            idleTime = enemyData.idleTime;
            usePatrol = enemyData.canPatrol;
        }
    }
    
    protected override void Start()
    {
        base.Start();

        lastPosition = transform.position;
        
        // If patrol is enabled, start with patrol state
        if (usePatrol)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    protected override void Update()
    {
        base.Update();

        CheckIfStuck();
    }

    private void CheckIfStuck()
    {
        if (navAgent == null || !navAgent.hasPath || navAgent.isStopped)
        {
            pathTimer = 0;
            return;
        }

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if(distanceMoved < stuckDistance) {
            pathTimer += Time.deltaTime;

            if(pathTimer > pathTimeout && !isStuck) {
                isStuck = true;
                StartCoroutine(GetUnstuck());
            }
        } else {
            pathTimer = 0;
            isStuck = false;
        }

        lastPosition = transform.position;
    }

    private IEnumerator GetUnstuck()
    {
        //Debug.Log($"{gameObject.name} is stuck - attempting to find new path!");

        if(navAgent != null)
        {
            navAgent.ResetPath();
        }

        yield return new WaitForSeconds(0.5f);

        switch (currentState)
        {
            case EnemyState.Patrol:
                FindNewPatrolTarget();
                break;
            case EnemyState.Chase:
                if(target != null) {
                    NavMeshHit hit;
                    Vector3 randomDirection = Random.insideUnitSphere * 5f + transform.position;

                    if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
                    {
                        navAgent.SetDestination(hit.position);
                        Debug.DrawLine(transform.position, hit.position, Color.red, 3f);
                    }
                }
                break;
        }

        yield return new WaitForSeconds(1f);
        isStuck = false;
        pathTimer = 0f;
    }

    protected override void EnterIdleState()
    {
        base.EnterIdleState();
        isSearching = false;
    }
    
    // Patrol State Implementation
    protected override void EnterPatrolState()
    {
        base.EnterPatrolState();
        isSearching = false;
        patrolTimer = 0f;
        FindNewPatrolTarget();
    }
    
    protected override void UpdatePatrolState()
    {
        base.UpdatePatrolState();
        
        if (navAgent != null && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            // Reached patrol point - wait for a bit
            patrolTimer += Time.deltaTime;
            
            if (patrolTimer >= idleTime)
            {
                FindNewPatrolTarget();
            }
        }
    }
    
    private void FindNewPatrolTarget()
    {
        int attempts = 0;
        const int maxAttempts = 5;

        while (attempts < maxAttempts) {
            // Find a random position within patrol radius
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += startPosition;
            
            // Use NavMesh.SamplePosition to find a valid point on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
            {
                // Check if a path exists to this point
                NavMeshPath path = new NavMeshPath();
                if (navAgent.CalculatePath(hit.position, path) && 
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    patrolTarget = hit.position;
                    if (navAgent != null)
                    {
                        navAgent.SetDestination(patrolTarget);
                    }
                    
                    patrolTimer = 0f;
                    return; // Success!
                }
            }
        }
        
        patrolTimer = 0f;
    }
    
    // Attack Implementation
    protected override void UpdateAttackState()
    {
        if (target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        // Check if we still have line of sight for attacking
        if (requireLineOfSight && !CheckLineOfSight(target))
        {
            // Lost sight of target during attack, return to chase
            ChangeState(EnemyState.Chase);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Face the target
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        
        // Check if target is out of attack range
        if (distanceToTarget > attackRange)
        {
            ChangeState(EnemyState.Chase);
            return;
        }
        
        // If we can attack, perform the attack
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
    }
    
    protected virtual void PerformAttack()
    {
        if (target == null) return;
        
        // Play attack animation if available
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Check if player is in front of the enemy with a raycast
        RaycastHit hit;
        Vector3 direction = (target.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, direction, out hit, attackRange))
        {
            if (hit.transform == target)
            {
                // Deal damage to player
                //PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                // if (playerHealth != null)
                // {
                //     playerHealth.TakeDamage(attackDamage);
                // }
            }
        }
        
        lastAttackTime = Time.time;
    }

    protected override void LookForTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= detectionRange)
            {
                // If we don't require line of sight, or if we do have line of sight
                if (!requireLineOfSight || CheckLineOfSight(player.transform))
                {
                    target = player.transform;
                    hasLineOfSight = true;
                    isSearching = false;
                    ChangeState(EnemyState.Chase);
                    
                    // Debug visualization
                    if (Debug.isDebugBuild)
                    {
                        Debug.DrawLine(transform.position, player.transform.position, Color.green, sightCheckFrequency);
                    }
                }
                else if (Debug.isDebugBuild)
                {
                    // Player in range but no line of sight - debug visualization
                    RaycastHit hit;
                    Vector3 direction = (player.transform.position - transform.position).normalized;
                    if (Physics.Raycast(transform.position, direction, out hit, detectionRange, obstacleLayerMask))
                    {
                        Debug.DrawLine(transform.position, hit.point, Color.red, sightCheckFrequency);
                    }
                }
            }
        }
    }

    private bool CheckLineOfSight(Transform targetTransform)
    {
        if (!requireLineOfSight || targetTransform == null)
        {
            return !requireLineOfSight;
        }

        if (Time.time < lastSightCheck + sightCheckFrequency)
        {
            return hasLineOfSight;
        }

        lastSightCheck = Time.time;

        Vector3 direction = targetTransform.position - transform.position;
        float distance = direction.magnitude;

        // Use the head position as the starting point if available
        Vector3 startPoint = transform.position + Vector3.up * 1.5f;

        RaycastHit hit;
        if (Physics.Raycast(startPoint, direction.normalized, out hit, distance, obstacleLayerMask))
        {
            // If we hit something that isn't the player, no line of sight
            if (hit.transform != targetTransform)
            {
                hasLineOfSight = false;
                return false;
            }
        }

        hasLineOfSight = true;
        return true;
    }

    protected override void UpdateChaseState()
    {
        if (target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        // Check if we still have line of sight to target
        if (requireLineOfSight && !CheckLineOfSight(target))
        {
            // Lost sight of target, but don't forget immediately
            if (!isSearching)
            {
                // Start the search timer when first losing sight
                lostSightTime = Time.time;
                isSearching = true;
                
                // Optional: Play animation or sound for "searching" behavior
                if (animator != null)
                {
                    animator.SetBool("Searching", true);
                }
            }
            
            // Check if memory duration has elapsed
            if (Time.time - lostSightTime >= memoryDuration)
            {
                // Finally forget about the target
                target = null;
                hasLineOfSight = false;
                isSearching = false;
                
                if (animator != null)
                {
                    animator.SetBool("Searching", false);
                }
                
                // Return to previous behavior
                if (usePatrol)
                {
                    ChangeState(EnemyState.Patrol);
                }
                else
                {
                    ChangeState(EnemyState.Idle);
                }
                return;
            }
            
            // Continue searching for target during memory duration
            // You could implement specific search behavior here, like:
            // - Moving to last known position
            // - Looking around in different directions
            // - Patrolling a small area around last known position
            
            if (navAgent != null && navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                // When reached the last known position, look around
                transform.Rotate(0, Time.deltaTime * 120f, 0); // Rotate to look around
            }
        }
        else
        {
            // We can see the target again, reset search state
            isSearching = false;
            
            if (animator != null)
            {
                animator.SetBool("Searching", false);
            }
        }

        // Existing chase logic
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Move toward target or last known position
        if (navAgent != null)
        {
            navAgent.SetDestination(target.position);
        }
        
        // Only attack if we actually have line of sight
        if (distanceToTarget <= attackRange && (!requireLineOfSight || CheckLineOfSight(target)))
        {
            ChangeState(EnemyState.Attack);
        }
        
        // Check if target is out of detection range
        if (distanceToTarget > detectionRange * 1.5f)
        {
            target = null;
            isSearching = false;
            ChangeState(EnemyState.Idle);
        }
    }
    
    // Stunned State Implementation
    public void Stun(float duration)
    {
        if (currentState != EnemyState.Stunned)
        {
            StartCoroutine(StunCoroutine(duration));
        }
    }
    
    private IEnumerator StunCoroutine(float duration)
    {
        ChangeState(EnemyState.Stunned);
        
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
        yield return new WaitForSeconds(duration);
        
        if (navAgent != null)
        {
            navAgent.isStopped = false;
        }
        
        ChangeState(EnemyState.Idle);
    }
    
    // Example of a derived enemy implementation with custom behavior
}