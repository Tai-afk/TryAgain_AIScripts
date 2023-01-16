using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class AIMovementController : SuperStateMachine
{
    #region References
    [Header("Nav Mesh Agent")]
    public NavMeshAgent navMeshAgent;
    [Header("Player reference")] 
    public GameObject player;
    #endregion
    [ToggleLeft]
    public bool editAttributes;
    
    
    
    [ColorFoldoutGroup("Movement"), ShowIf( "editAttributes",true)] 
    [SerializeField] private float walkSpeed;
    [ColorFoldoutGroup("Movement"), ShowIf( "editAttributes",true)] 
    [SerializeField] private float runSpeed;
    
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    public Transform[] waypoints;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    private int m_CurrentWayPointIndex;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private bool m_IsPatrol;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private float viewRadius;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private float viewAngle;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private LayerMask playerMask;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private LayerMask obstacleMask;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private bool m_playerInRange;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private float startWaitTime;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    private Vector3 m_PlayerPosition;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    private float m_WaitTime = 0;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    private bool m_PlayerNear;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [Header("Searching values")]
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private float searchDetectionAngle;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [SerializeField] private float searchRadius;
    
    [ColorFoldoutGroup("Chase"),ShowIf( "editAttributes",true)] 
    [SerializeField] private bool m_CaughtPlayer;
    private bool m_SeePlayer = false;
    [ColorFoldoutGroup("Chase"),ShowIf( "editAttributes",true)] 
    private Vector3 playerLastPosition = Vector3.zero;
    [ColorFoldoutGroup("Chase"), ShowIf("editAttributes", true)] [SerializeField]
    private float chaseRadius;
    
    [ColorFoldoutGroup("Kill"), ShowIf( "editAttributes",true)] 
    private float kill;

    [ColorFoldoutGroup("Debugging"), ShowIf( "editAttributes",true)] 
    [SerializeField] private bool showKillArea;
    [ColorFoldoutGroup("Debugging"), ShowIf( "editAttributes",true)] 
    [SerializeField] private bool showViewArea;
    
    //TODO: Smoother rotation towards player
    //TODO: Have an IDLE STATE
    //TODO: Better patrolling mechanics (when ai stops at patrol point, have them "look around"
    //TODO: Different enemy types?
    //TODO: When discover player, widen the search radius to the sphere cast or something
    /*
     * AI CURRENTLY HAS PATROLLING FUNCTIONALITY, A SEARCH RADIUS/ANGLE TO FIND THE PLAYER
     * AS WELL AS A CHASE AND ATTACKING STATE. WALK SPEED AND RUN SPEED TWEAK THE
     * NAVMESHAGENT TO WALK/RUN.
     * TWEAK THE NAVMESH AGENT AI VARIABLES
    */
    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        m_IsPatrol = false;
        m_CaughtPlayer = false;

        navMeshAgent.isStopped = false;
        navMeshAgent.speed = walkSpeed;
        m_CurrentWayPointIndex = 0;
        m_WaitTime = startWaitTime;
        //Start patrol
        if (waypoints.Length != 0)
        {
            navMeshAgent.SetDestination(waypoints[m_CurrentWayPointIndex].position);
            //navMeshAgent.destination = waypoints[m_CurrentWayPointIndex].position;
        }
    }
    void Update()
    {
        //EnviromentView();
        
        if (playerInFront() && playerInLineOfSight())
        {
            Chase();
        }
        else
        {
            Patroling();
        }
    }

    private void Chase()
    {
        m_PlayerNear = false;
        playerLastPosition = Vector3.zero;

        if (!m_CaughtPlayer)
        {
            Move(runSpeed);
            navMeshAgent.SetDestination(player.transform.position);
            m_SeePlayer = true; //TODO: set angle of sight to a circular level, m_LastKnownPosition?
        } 
        
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)//if enemy arrives at player
        {
            
            //Do our attacking here
            Attack();
            //DONT: Don't use this until i figure out how to fix

            /*if (m_WaitTime <= 0 && !m_CaughtPlayer &&
                Vector3.Distance(transform.position, player.transform.position) >= 6f) //if player is not near ai, return to patrol
            {
                m_IsPatrol = true;
                m_PlayerNear = false;
                Move(walkSpeed);
                m_WaitTime = startWaitTime;
                navMeshAgent.SetDestination(waypoints[m_CurrentWayPointIndex].position);
            }
            else
            {
                if (Vector3.Distance(transform.position, player.transform.position) >= 2.5f)
                {
                    Stop();
                }
                m_WaitTime -= Time.deltaTime;
            }*/
        }
    }
    private void Patroling()
    {
        //TODO: if player is near AI, have a smaller detection circular radius

        if (m_playerInRange)
        {
            Chase();
        }
        
        else
        {
            Debug.DrawLine(transform.position, waypoints[m_CurrentWayPointIndex].position, Color.magenta);
            Move(walkSpeed);
            playerLastPosition = Vector3.zero;
            navMeshAgent.SetDestination(waypoints[m_CurrentWayPointIndex].position);
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (m_WaitTime <= 0)
                {
                    NextPoint();
                    Move(walkSpeed);
                    m_WaitTime = startWaitTime; 
                }
                else
                {
                    Stop();
                    LookAround();
                    m_WaitTime -= Time.deltaTime;
                }
            }
        }
    }

    private void Attack()
    {
        m_CaughtPlayer = true;
        //Lock player movement, move it towards ai, play death animation
        //AI play kill animation
        //Set benny death state
        
    }
    public void NextPoint()
    {
        m_CurrentWayPointIndex = (m_CurrentWayPointIndex + 1) % waypoints.Length;
        navMeshAgent.SetDestination(waypoints[m_CurrentWayPointIndex].position);
    }

    void Move(float speed)
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = speed;
    }

    void Stop()
    {
        navMeshAgent.isStopped = true;
        navMeshAgent.speed = 0;
    }

    void LookAround()
    {
        //TODO: look around 45 degree angle from the transform.forward
        
    }
    #region Utility Functions
void EnviromentView() //NOT CURRENTLY WORKING
    {
        Collider[] playerInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask); //  Make an overlap sphere around the enemy to detect the playermask in the view radius

        for (int i = 0; i < playerInRange.Length; i++)
        {
            Transform player = playerInRange[i].transform;
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
            {
                float dstToPlayer =
                    Vector3.Distance(transform.position, player.position); //  Distance of the enmy and the player
                if (!Physics.Raycast(transform.position, dirToPlayer, dstToPlayer, obstacleMask))
                {
                    m_playerInRange = true; //  The player has been seeing by the enemy and then the nemy starts to chasing the player
                    m_IsPatrol = false; //  Change the state to chasing the player
                }
                else
                {
                    /*
                     *  If the player is behind a obstacle the player position will not be registered
                     * */
                    m_playerInRange = false;
                }
            }

            if (Vector3.Distance(transform.position, player.position) > viewRadius)
            {
                /*
                 *  If the player is further than the view radius, then the enemy will no longer keep the player's current position.
                 *  Or the enemy is a safe zone, the enemy will no chase
                 * */
                m_playerInRange = false; //  Change the sate of chasing
            }

            if (m_playerInRange)
            {
                /*
                 *  If the enemy no longer sees the player, then the enemy will go to the last position that has been registered
                 * */
                m_PlayerPosition = player.transform.position; //  Save the player's current position if the player is in range of vision
            }
        }
    }

protected bool FindTarget(Vector3 target)
{
    Vector3 dirTarget = target - transform.position;
    float distToTarget = dirTarget.magnitude;

    Vector3 playerPosition = player.transform.position + new Vector3(0, 2, 0);
    Vector3 enemyPosition = transform.position + new Vector3(0, 1, 0); //height of the ai
    //if(Physics.SphereCast(enemyPosition, ))
    return false;
}

private bool playerInFront()
{
    Vector3 directionOfPlayer = player.transform.position - transform.position ;
    float angle = Vector3.Angle(transform.forward, directionOfPlayer);
    if (Mathf.Abs(angle) < searchDetectionAngle)
    {
        Debug.DrawLine(transform.position, player.transform.position, Color.red);
        return true;
    }

    return false;
}

private bool playerInLineOfSight()
{
    RaycastHit hit;
    Vector3 directionOfPlayer = player.transform.position - transform.position;

    if (Physics.Raycast(transform.position, directionOfPlayer, out hit, searchRadius))
    {
        if (hit.transform.CompareTag("Player"))
        {
            Debug.DrawLine(transform.position, player.transform.position, Color.green);
            return true;
        }
    }

    return false;
}
#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
        if (showViewArea)
        {
            //Detection Angle
            Gizmos.color = Color.blue;
            Handles.color = Color.blue;
            Vector3 sideDir = Quaternion.AngleAxis(-searchDetectionAngle, Vector3.up) * transform.forward;
            sideDir.Normalize();
            Vector3 otherSideDir = Quaternion.AngleAxis(searchDetectionAngle, Vector3.up) * transform.forward;
            otherSideDir.Normalize();
            
            Gizmos.DrawLine(transform.position, transform.position + sideDir * searchRadius);
            Gizmos.DrawLine(transform.position, transform.position + otherSideDir * searchRadius);
            Handles.DrawWireArc(transform.position, Vector3.up, sideDir, searchDetectionAngle * 2.0f, searchRadius);
        }
    }
#endif
    

    #endregion
    
}
