using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class TurretEnemy : Enemy
{
    [Header("Firing settings")]
    public float heatIncreaseRate = 4f;
    public float maxHeatBuildUp = 100f;
    public float coolRate = 2f;
    public float overheatCooldown = 7.5f;
    
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 5f;
    public float projectileSpeed = 100f;
    public int projectileDamage = 10;
    public float rotationThreshold = 5f;
    public float bloomAngle = 5f;

    [Header("Idle/Swivel Behavior")]
    public bool randomSwivel = true;
    public float minSwivelWaitTime = 2f;
    public float maxSwivelWaitTime = 5f;
    public float maxSwivelAngle = 90f;
    public float swivelSpeed = 3f;

    private Quaternion idleTargetRotation;
    private float nextSwivelTime;
    private bool isSwiveling = false;

    [Header("Line of Sight Settings")]
    public LayerMask obstacleLayerMask;
    public bool requireLineOfSight = true;
    public float sightCheckFrequency = 0.2f;
    private float lastSightCheck;
    private bool hasLineOfSight = false;

    private float heatLevel;
    private float nextFireTime;
    private bool isOverheated = false;
    private Quaternion targetRotation;

    protected override void Awake()
    {
        base.Awake();

        if(firePoint == null) {
            firePoint = transform;
        }
    }

    #region State Overrides
    protected override void EnterIdleState()
    {
        base.EnterIdleState();

        if(navAgent != null) {
            navAgent.isStopped = true;
        }

        if(randomSwivel) {
            SelectRandomSwivelTarget();
        }
    }

    protected override void UpdateIdleState()
    {
        base.UpdateIdleState();

        if(randomSwivel) {
            if(isSwiveling) {
                bool reachedTarget = RotateTowardsTarget(idleTargetRotation);

                if(reachedTarget && Time.time >= nextSwivelTime) {
                    SelectRandomSwivelTarget();
                }
            } else if(Time.time >= nextSwivelTime) {
                SelectRandomSwivelTarget();
            }
        }
        
        Cooldown();
    }

    protected override void EnterChaseState()
    {
        base.EnterChaseState();

        if(navAgent != null) {
            navAgent.isStopped = true;
        }
    }

    protected override void UpdateChaseState()
    {
        if (target == null) {
            ChangeState(EnemyState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if(distanceToTarget > detectionRange * 1.5f) {
            target = null;
            ChangeState(EnemyState.Idle);
            return;
        }

        // Check if we can see the target
        if(requireLineOfSight && !CheckLineOfSight()) {
            target = null;
            hasLineOfSight = false;
            ChangeState(EnemyState.Idle);
            return;
        }

        //if in range and still visible, attack
        if(distanceToTarget <= attackRange) {
            if(!requireLineOfSight || CheckLineOfSight()) {
                ChangeState(EnemyState.Attack);
                return;
            }
        }

        RotateTowardsTarget();

        Cooldown();
    }

    protected override void EnterAttackState()
    {
        base.EnterAttackState();
        if(navAgent != null) {
            navAgent.isStopped = true;
        }

        if(animator != null) {
            animator.SetBool("Attacking", true);
        }
    }

    protected override void UpdateAttackState()
    {
        if(target == null) {
            ChangeState(EnemyState.Idle);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if(distanceToTarget > attackRange || (requireLineOfSight && !CheckLineOfSight())) {
            if(distanceToTarget <= detectionRange * 1.5f && (!requireLineOfSight || CheckLineOfSight())) {
                ChangeState(EnemyState.Chase);
            } else {
                target = null;
                hasLineOfSight = false;
                ChangeState(EnemyState.Idle);
            }
            return;
        }

        bool isAimed = RotateTowardsTarget();

        if(isAimed && !isOverheated) {
            PerformAttack();
        } else {
            Cooldown();
        }

    }

    protected override void ExitAttackState()
    {
        base.ExitAttackState();
        if(animator != null) {
            animator.SetBool("Attacking", false);
        }
    }

    protected override void EnterStunnedState()
    {
        base.EnterStunnedState();
        isOverheated = true;

        if(animator != null) {
            animator.SetTrigger("Overheat");
        }

        if(GetComponent<Renderer>() != null) {
            StartCoroutine(FlashOverheat());
        }
    }

    protected override void UpdateStunnedState()
    {
        base.UpdateStunnedState();

        heatLevel = Mathf.Max(0, heatLevel - (coolRate * 2 * Time.deltaTime));
    }

    protected override void ExitStunnedState()
    {
        base.ExitStunnedState();
        isOverheated = false;
        heatLevel = 0;
    }

    protected override void LookForTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= detectionRange)
            {
                // Check line of sight before detecting
                Vector3 directionToPlayer = (player.transform.position - firePoint.position).normalized;
                float distanceToCheck = Mathf.Min(distanceToPlayer, detectionRange);
                
                if (!requireLineOfSight)
                {
                    target = player.transform;
                    ChangeState(EnemyState.Chase);
                    return;
                }
                
                RaycastHit hit;
                if (Physics.Raycast(firePoint.position, directionToPlayer, out hit, distanceToCheck, obstacleLayerMask))
                {
                    // Hit something that isn't the player - no line of sight
                    if (hit.transform != player.transform)
                    {
                        // Debug visualization
                        if (Debug.isDebugBuild)
                        {
                            Debug.DrawLine(firePoint.position, hit.point, Color.red, 0.5f);
                        }
                        return;
                    }
                }
                
                // Have line of sight to the player
                target = player.transform;
                hasLineOfSight = true;
                ChangeState(EnemyState.Chase);
                
                // Debug visualization
                if (Debug.isDebugBuild)
                {
                    Debug.DrawLine(firePoint.position, player.transform.position, Color.green, 0.5f);
                }
            }
        }
    }
    #endregion

    #region Swivel Methods
    private void SelectRandomSwivelTarget()
    {
        float randomAngle = Random.Range(-maxSwivelAngle, maxSwivelAngle);
        
        // Calculate a new forward direction based on the original forward
        Quaternion randomRotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 newDirection = randomRotation * transform.forward;
        
        // Set the new target rotation
        idleTargetRotation = Quaternion.LookRotation(newDirection);
        
        // Set next swivel time
        nextSwivelTime = Time.time + Random.Range(minSwivelWaitTime, maxSwivelWaitTime);
        isSwiveling = true;
    }

    private bool RotateTowardsTarget(Quaternion targetrot)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetrot, swivelSpeed * Time.deltaTime * 60f);
        float angleDifference = Quaternion.Angle(transform.rotation, targetrot);
        return angleDifference < rotationThreshold;
    }

    #endregion

    protected virtual void PerformAttack()
    {
        if(Time.time < nextFireTime) {
            return;
        }

        nextFireTime = Time.time + (1f / fireRate);

        heatLevel += heatIncreaseRate;

        if(animator != null) {
            animator.SetTrigger("Fire");
        }

        if(heatLevel >= maxHeatBuildUp) {
            Overheat();
            return;
        }

        if(projectilePrefab != null && firePoint != null) {

            Vector3 baseDirection = firePoint.forward;
            if (bloomAngle > 0) {
                float randomSpreadX = Random.Range(-bloomAngle, bloomAngle);
                float randomSpreadY = Random.Range(-bloomAngle, bloomAngle);

                Quaternion xRotation = Quaternion.AngleAxis(randomSpreadX, Vector3.up);
                Quaternion yRotation = Quaternion.AngleAxis(randomSpreadY, Vector3.right);

                baseDirection = xRotation * yRotation * baseDirection;
            }

            Quaternion finalRotation = Quaternion.LookRotation(baseDirection);

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, finalRotation);

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if(rb != null) {
                rb.velocity = firePoint.forward * projectileSpeed;
            }

            ProjectileComponent projComp = projectile.GetComponent<ProjectileComponent>();
            if(projComp != null) {
                projComp.damage = projectileDamage;
                projComp.owner = gameObject;
            }

            Destroy(projectile, 1f);
        }
    }

    public void Overheat()
    {
        if(currentState != EnemyState.Stunned) {
            StartCoroutine(OverheatCoroutine(overheatCooldown));
        }
    }

    private IEnumerator OverheatCoroutine(float duration)
    {
        ChangeState(EnemyState.Stunned);

        yield return new WaitForSeconds(duration);

        ChangeState(EnemyState.Idle);
    }

    private void Cooldown()
    {
        heatLevel = Mathf.Max(0, heatLevel - (coolRate * Time.deltaTime));
    }

    private bool RotateTowardsTarget()
    {
        if(target == null) {
            return false;
        }

        Vector3 targetDirection = target.position - transform.position;
        targetDirection.y = 0;
        
        if(targetDirection != Vector3.zero) {
            targetRotation = Quaternion.LookRotation(targetDirection);
            return RotateTowardsTarget(targetRotation);
        }
        return false;
    }

    private IEnumerator FlashOverheat()
    {
        Renderer renderer = GetComponent<Renderer>();
        if(renderer == null) {
            yield break;
        }

        Color originalColor = renderer.material.color;
        Color overheatColor = Color.red;

        float flashSpeed = 4f;
        float elapsed = 0f;

        while (currentState == EnemyState.Stunned) {
            elapsed += Time.deltaTime * flashSpeed;
            renderer.material.color = Color.Lerp(originalColor, overheatColor, Mathf.PingPong(elapsed, 1f));
            yield return null;
        }

        renderer.material.color = originalColor;
    }

    private bool CheckLineOfSight()
    {
        if(!requireLineOfSight || target == null) {
            return !requireLineOfSight;
        }

        if(Time.time < lastSightCheck + sightCheckFrequency) {
            return hasLineOfSight;
        }

        lastSightCheck = Time.time;

        Vector3 direction = target.position - firePoint.position;
        float distance = direction.magnitude;

        RaycastHit hit;
        if(Physics.Raycast(firePoint.position, direction.normalized, out hit, distance, obstacleLayerMask)) {
            if(hit.transform != target) {
                hasLineOfSight = false;
                if(Debug.isDebugBuild) {
                    Debug.DrawLine(firePoint.position, hit.point, Color.red, sightCheckFrequency);
                }
                return false;
            }
        }

        hasLineOfSight = true;

        if (Debug.isDebugBuild)
        {
            Debug.DrawLine(firePoint.position, target.position, Color.green, sightCheckFrequency);
        }
        
        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if(firePoint != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 2f);
        }
    }

    private void OnGUI()
    {
        if(!Debug.isDebugBuild) {
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if(screenPos.z > 0) {
            float heatPrecentage = heatLevel / maxHeatBuildUp;
            string heatText = isOverheated ? "OVERHEATED!" : $"Heat: {(heatPrecentage * 100):F0}%";
            GUI.color = isOverheated ? Color.red : Color.Lerp(Color.green, Color.red, heatPrecentage);
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 40, 100, 20), heatText);
        }
    }


}