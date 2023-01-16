using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TryAgainFSM.Actions;
using TryAgainFSM.Lookups;
using UnityEngine;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.AI;
using UnityEditor;
using Random = UnityEngine.Random;

public class AIStateControl : SuperStateMachine
{
	[ToggleLeft]
	public bool editAttributes;
	[ToggleLeft]
	public bool editJumpingAttributes;
	[ToggleLeft]
	public bool editSlidingAttributes;
	[ToggleLeft]
	public bool editKillAttributes;
	
	#region Component References
	[Header("References")]
	[HideInInspector] public AICharacterController _AICharacterController;
	[HideInInspector] public Rigidbody _rb;
	[HideInInspector] public Animator _animator;
	private RPGCharacterMovementController _rpgCharacterMovementController;
	[HideInInspector] public CapsuleCollider capsuleCollider;
	#endregion

	#region State References
	[HideInInspector] public VaultState vaultState;
	[HideInInspector] public ClimbState climbState;
	#endregion
	
	#region Animation IDs
	[HideInInspector]public int _animIDSpeed;
	[HideInInspector]public int _animIDTargetSpeed;
	[HideInInspector]public int _animIDGrounded;
	[HideInInspector]public int _animIDJump;
	[HideInInspector]public int _animIDFalling;
	[HideInInspector]public int _animIDSlide;
	[HideInInspector]public int _animIDEndSlide;
	[HideInInspector]public int _animIDVault;
	[HideInInspector]public int _animIDVaultType;
	[HideInInspector]public int _animIDClimb;
	[HideInInspector] public int _animIDClimbType;
	[HideInInspector] public int _animIDAttack;
	[HideInInspector] public int _animIDAttackType;
	[HideInInspector] public int _animIDTackleType;
	#endregion
	
	#region References
    [Header("Nav Mesh Agent")]
    private NavMeshAgent navMeshAgent;
    [Header("Player reference")] 
    [HideInInspector]public GameObject player;
    #endregion

    #region Jump Attributes
    
    /// <summary>
    /// Internal flag for when the character can jump again. Mainly to prevent air jump
    /// Jump FC is the frame count for jump state
    /// </summary>
    [HideInInspector] public bool canJump;
    [HideInInspector] public int jumpStateFC = 0;

    /// <summary>
    /// If jump key is released mid-jump, canVariableJump will be set to false - pressing jump again won't enable variable jump
    /// </summary>
    [HideInInspector] public bool canVariableJump;

    /// <summary>
    /// Threshold determines the frame variable jump starts, so light press on jump won't trigger variable jump
    /// Duration determine the maximum frames of upward force variable can provide
    /// Strength is the upward force provided by variable jump each frame
    /// </summary>
    [Space(20)]
    [ColorFoldoutGroup("Jump", 1f, 1f, 1f), ShowIf("editJumpingAttributes")] public int variableJumpThreshold = 5;
    [ColorFoldoutGroup("Jump"), ShowIf("editJumpingAttributes")]public int variableJumpDuration = 25;
    [ColorFoldoutGroup("Jump"), ShowIf("editJumpingAttributes")]public float variableJumpStrength = 10.0f;
    [ColorFoldoutGroup("Jump"), ShowIf("editJumpingAttributes")]public float jumpForce = 300.0f;
    [ColorFoldoutGroup("Jump"), ShowIf("editJumpingAttributes")]public float jumpGroundCheckCooldown = 0.2f;
    [ColorFoldoutGroup("Jump"), ShowIf("editJumpingAttributes")] public float distanceToEdgeThreshold = 1.5f;
    [ColorFoldoutGroup("Jump"), ShowIf("editJumpingAttributes")] public float feetToGapDistanceOffset = 0.2f;
    private float _jumpGroundCheckTimer = 0.0f;
    private Vector3 feetPos = Vector3.zero;
    #endregion
    
    #region Slide Attributes
    [ColorFoldoutGroup("Sliding", 1f, 1f, 1f), ShowIf( "editSlidingAttributes",true)]
    [SerializeField]private float maxSlideTime = 2.0f;
    [SerializeField, ColorFoldoutGroup("Sliding"), ShowIf( "editSlidingAttributes",true)]private float minSlideTime = 0.5f;
    [SerializeField, ColorFoldoutGroup("Sliding"), ShowIf( "editSlidingAttributes",true)]private float slideForce;
    [ColorFoldoutGroup("Sliding"), ShowIf( "editSlidingAttributes",true)]public float slideSpeedThreshold = 5.0f;
    [ColorFoldoutGroup("Sliding"), ShowIf( "editSlidingAttributes",true)]public float slideTime = 0.0f;
    [ColorFoldoutGroup("Sliding"), ShowIf( "editSlidingAttributes",true)]public bool slideCoolDown = false;

    /// <summary>
    /// State check variables
    /// </summary>
    private Vector3 startPointTop = Vector3.zero;
    private Vector3 startPointEnd = Vector3.zero;
    [ColorFoldoutGroup("Sliding"), ShowIf("editSlidingAttributes", true)] public float lengthSlideSphereCast = 0;
    [ColorFoldoutGroup("Sliding"), ShowIf("editSlidingAttributes", true)] public float radiusSlideSphereCast = 0;
    [ColorFoldoutGroup("Sliding"), ShowIf("editSlidingAttributes", true)]public float _slideSpeed;
    private float slideDrag = 2.0f;
    private bool maxSlideTimeReached = false;
    private Vector3 _slideDirection = Vector3.zero;
    private float _startCapsuleRadius;
    private float _startCapsuleHeight;
    private float slideCapsuleHeight = 0.5f;
    private bool isSliding;
    private float slideCoolDownTime = 0.0f;
    private float maxSlideCoolDownTime = 0.2f;
    private float slideDistance = 3f;
    private float lerp = 0;
    private float slopeDetect = 3f;
    
    #endregion

    #region Movement Attributes
    [ColorFoldoutGroup("Movement"), ShowIf( "editAttributes",true)] [SerializeField] private float walkSpeed;
    [ColorFoldoutGroup("Movement"), ShowIf( "editAttributes",true)] [SerializeField] private float runSpeed;
    [ColorFoldoutGroup("Movement"), ShowIf( "editAttributes",true)] [SerializeField] private LayerMask navMeshLayer;
    #endregion
    
    #region Fall Attributes
    [ColorFoldoutGroup("Fall", 1f, 1f, 1f), ShowIf( "editAttributes",true)]
    [ColorFoldoutGroup("Fall"), ShowIf( "editAttributes",true)] public bool setFallTrigger = false;
    #endregion
    
    #region Grounded Checks
    [ColorFoldoutGroup("GroundChecks", 1f, 1f, 1f), ShowIf( "editAttributes",true)]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check"), ColorFoldoutGroup("GroundChecks")]
    public bool grounded = false;
    [HideInInspector] public bool acquiringGround = true;
    private float _groundClearance = 0;
    [SerializeField, ColorFoldoutGroup("GroundChecks"), ShowIf( "editAttributes",true)] private float groundedDistance = 0.1f;
    [SerializeField, ColorFoldoutGroup("GroundChecks"), ShowIf( "editAttributes",true)] private float acquiringGroundedDistance = 0.5f;
    [SerializeField, ColorFoldoutGroup("GroundChecks"), ShowIf( "editAttributes",true)] private float rayCastSphereRadius = 1.0f;
    [SerializeField, ColorFoldoutGroup("GroundChecks"), ShowIf( "editAttributes",true)] private LayerMask groundLayers = -1;
    #endregion

    #region Patrol AttributesDebug.Log("2");
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] public List<Transform> waypoints = new List<Transform>();
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] private int m_CurrentWayPointIndex;
    [ColorFoldoutGroup("Patrol"), ShowIf("editAttributes", true)] [SerializeField] private bool m_IsPatrol = false;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] [SerializeField] private LayerMask playerMask;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] [SerializeField] private LayerMask obstacleMask;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] [SerializeField] private bool m_playerInRange;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] [SerializeField] private float startWaitTime;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] private Vector3 m_PlayerPosition;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] private float m_WaitTime = 0;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] private bool m_PlayerNear;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)]
    [Header("Searching values")]
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] [SerializeField] private float searchDetectionAngle;
    [ColorFoldoutGroup("Patrol"),ShowIf( "editAttributes",true)] [SerializeField] private float searchRadius;
    #endregion

    #region Kill Attributes
    [ColorFoldoutGroup("Kill"), ShowIf( "editKillAttributes",true)] [SerializeField]private float killRadius = 1.0f;
    [ColorFoldoutGroup("Kill"), ShowIf("editKillAttributes", true)] [SerializeField] private List<AttackParams> attackParams;
    [ColorFoldoutGroup("Kill"), ShowIf("editKillAttributes", true)] [HideInInspector] public BaseAttack currentAttack;
    [ColorFoldoutGroup("Kill"), ShowIf("editKillAttributes"), SerializeField] private LayerMask traceMask;
    [ColorFoldoutGroup("Kill"), ShowIf("editKillAttributes"), SerializeField] private TackleType tackleType;
    [ColorFoldoutGroup("Kill"), ShowIf("editKillAttributes"), SerializeField] private DeathType deathType;
    [ColorFoldoutGroup("Kill"), ShowIf( "editKillAttributes",true)] [SerializeField]private float strangleRadius = 1.0f;
    [ColorFoldoutGroup("Kill"), ShowIf( "editKillAttributes",true)] [SerializeField]private float groundAndPoundRadius = 1.0f;
    [ColorFoldoutGroup("Kill"), ShowIf( "editKillAttributes",true)] [SerializeField]private float headSlamRadius = 1.0f;
	private Vector3 startPosition = Vector3.zero;
	private Quaternion startRotation;
	private DeathBehavior rpgCharacterAnimBehavior;
    #endregion
    
    #region Chase Attributes
    [ColorFoldoutGroup("Chase"),ShowIf( "editAttributes",true)] public bool caughtPlayer;
    [HideInInspector]public bool seePlayer = false;
    [ColorFoldoutGroup("Chase"),ShowIf( "editAttributes",true)] private Vector3 playerLastPosition = Vector3.zero;
    [ColorFoldoutGroup("Chase"), ShowIf("editAttributes", true)] [SerializeField] private float chaseRadius;
    [ColorFoldoutGroup("Chase"), ShowIf("editAttributes", true)] [SerializeField] private float maxChaseSpeed;
    [ColorFoldoutGroup("Chase"), ShowIf("editAttributes", true)] [SerializeField] private float accelerationFactor;
    [ColorFoldoutGroup("Chase"), ShowIf("editAttributes", true)] [SerializeField] private float maxDistanceToAccel;
    private float startSpeed;
    #endregion
    
    #region AI Sensor Attributes
    [ColorFoldoutGroup("AI Sensor Attributes"),ShowIf( "editAttributes",true)] [SerializeField] private float viewDistance;
    [ColorFoldoutGroup("AI Sensor Attributes"),ShowIf( "editAttributes",true)] [SerializeField] private float viewAngle;
    [ColorFoldoutGroup("AI Sensor Attributes"), ShowIf("editAttributes", true)] [SerializeField] private float viewHeight;
    [ColorFoldoutGroup("AI Sensor Attributes"), ShowIf("editAttributes", true)] [SerializeField] private Color meshColor = Color.red;
    [ColorFoldoutGroup("AI Sensor Attributes"), ShowIf("editAttributes", true)] [SerializeField] private int scanFrequency = 30;
    [ColorFoldoutGroup("AI Sensor Attributes"), ShowIf("editAttributes", true)] [SerializeField] private LayerMask layers;
    [ColorFoldoutGroup("AI Sensor Attributes"), ShowIf("editAttributes", true)] [SerializeField] private LayerMask occlusionLayers;
    private Collider[] colliders = new Collider[50];
    private int count;
    private float scanInterval;
    private float scanTimer;
    private Mesh mesh;
    #endregion

    #region Failed Action Attributes

    private float backDistance;
    private bool failedState;
    private bool aiStop;

    #endregion
    
    
    /*
     * AI CURRENTLY HAS PATROLLING FUNCTIONALITY, A SEARCH RADIUS/ANGLE TO FIND THE PLAYER
     * AS WELL AS A CHASE AND ATTACKING STATE. WALK SPEED AND RUN SPEED TWEAK THE
     * NAVMESHAGENT TO WALK/RUN.
     * TWEAK THE NAVMESH AGENT AI VARIABLES
    */
    [ColorFoldoutGroup("Debugging"), ShowIf( "editAttributes",true)] [SerializeField] private bool ragDollTest;
    [ColorFoldoutGroup("Debugging"), ShowIf( "editAttributes",true)] [SerializeField] private bool showKillArea;
    [ColorFoldoutGroup("Debugging"), ShowIf( "editAttributes",true)] [SerializeField] private bool showViewArea;
    [ColorFoldoutGroup("Debugging"), ShowIf( "editAttributes",true)] [SerializeField] private bool showSlideSpheres;
    public bool debug = false;
    private bool killed = false;
    
	private void Awake()
	{
		_AICharacterController = GetComponent<AICharacterController>();
		_rb = GetComponent<Rigidbody>();
		_animator = GetComponent<Animator>();
		navMeshAgent = GetComponent<NavMeshAgent>();
		player = GameObject.FindGameObjectWithTag("Player");
		rpgCharacterAnimBehavior = player.GetComponent<DeathBehavior>();
		capsuleCollider = GetComponent<CapsuleCollider>();
		
		#region State Initialization
		climbState = GetComponent<ClimbState>();
		vaultState = GetComponent<VaultState>();
		#endregion
		
		_AICharacterController.SetHandler(HandlerTypes.Patrol, new AIPatrol(this));
		_AICharacterController.SetHandler(HandlerTypes.Idle, new AIIdle(this));
		_AICharacterController.SetHandler(HandlerTypes.Attack, new AIAttack(this));
		_AICharacterController.SetHandler(HandlerTypes.Chase, new AIChase(this));
		_AICharacterController.SetHandler(HandlerTypes.Vault, new AIVault(this));
		_AICharacterController.SetHandler(HandlerTypes.Fall, new AIFall(this));
		_AICharacterController.SetHandler(HandlerTypes.Slide, new AISlide(this));
		_AICharacterController.SetHandler(HandlerTypes.Jump, new AIJump(this));
		_AICharacterController.SetHandler(HandlerTypes.Climb, new AIClimb(this));
		AssignAnimationIDs();

		
		_startCapsuleRadius = capsuleCollider.radius;
		_startCapsuleHeight = capsuleCollider.height;

		backDistance = 0;
		failedState = false;
		aiStop = false;
	}

	private void Start()
	{
		if (_rpgCharacterMovementController == null)
		{
			_rpgCharacterMovementController = player.GetComponent<RPGCharacterMovementController>();
		}

		if (player == null)
		{
			player = FindObjectOfType<RPGCharacterController>().gameObject;
		}
		if (navMeshAgent.enabled)
		{
			navMeshAgent.isStopped = false;
			navMeshAgent.speed = walkSpeed;
		}
		
		if (waypoints.Count != 0)
		{
			navMeshAgent.SetDestination(waypoints[m_CurrentWayPointIndex].position);
			//navMeshAgent.destination = waypoints[m_CurrentWayPointIndex].position;
		}
		
		startPosition = transform.position;
		startRotation = transform.rotation;
		startSpeed = runSpeed;
		
		capsuleCollider = GetComponent<CapsuleCollider>();
		caughtPlayer = false;
		seePlayer = false;
		m_CurrentWayPointIndex = 0;
		m_WaitTime = startWaitTime;
		
		// Set our currentState to idle on startup.
		_rb.useGravity = true;
		_AICharacterController.StartAction("Idle");
		currentState = CharacterState.Idle;
	}

	// Put any code in here you want to run BEFORE the state's update function.
	// This is run regardless of what state you're in.
	protected override void EarlyGlobalSuperUpdate()
	{
		GroundedCheck();
		ReAssignKillRadius(deathType, tackleType);
		if (_rpgCharacterMovementController.dead)
		{
			runSpeed = 0;
			walkSpeed = 0;
			navMeshAgent.speed = 0;
			seePlayer = false;
		}
		else
		{
			if (playerInKillArea() && !killed && !_rpgCharacterMovementController.dead)
			{
				killed = true;
				_AICharacterController.StartAction("Attack");
				Debug.Log(gameObject.name);
			}
			else if (PlayerIsInSight(player.transform.gameObject))
			{
				runSpeed = startSpeed;
				walkSpeed = startSpeed;
				seePlayer = true;
				CatchUpToPlayer();
			}
		}
	}

	// Put any code in here you want to run AFTER the state's update function.
	// This is run regardless of what state you're in.
	protected override void LateGlobalSuperUpdate()
	{
		//print(currentState);
	}

	// Put any code in here you want to run BEFORE the state's fixed update function.
	// This is run regardless of what state you're in.
	protected override void EarlyGlobalFixedUpdate()
	{
		if (Input.GetKeyDown(KeyCode.H))
		{
			Debug.Log("restart");
			navMeshAgent.enabled = false;
			transform.position = startPosition;
			navMeshAgent.enabled = true;
		}
		if (currentState == null && _AICharacterController.CanStartAction(HandlerTypes.Idle))
		{
			_AICharacterController.StartAction(HandlerTypes.Idle);
		}
		if (_rb.velocity.y <= 0.0f && !grounded && !acquiringGround &&
		    (CharacterState)currentState != CharacterState.WallRun &&
		    (CharacterState)currentState != CharacterState.Climb &&
		    (CharacterState)currentState != CharacterState.Vault)
		{
			if (_AICharacterController.CanStartAction("Fall"))
			{
				if ((CharacterState)currentState == CharacterState.Idle || (CharacterState)currentState == CharacterState.Move ||
				    grounded || acquiringGround)
				{
					setFallTrigger = true;
				}
				_AICharacterController.StartAction("Fall");
			}
		}
	}

	// Put any code in here you want to run AFTER the state's fixed update function.
	// This is run regardless of what state you're in.
	// For example, gravity can be applied here
	protected override void LateGlobalFixedUpdate()
	{
		if (seePlayer && ((CharacterState)currentState == CharacterState.Patrol || (CharacterState)currentState == CharacterState.Idle))
		{
			_AICharacterController.StartAction("Chase");
		}
		else if (((CharacterState)currentState == CharacterState.Patrol || (CharacterState)currentState == CharacterState.Idle) && waypoints.Count > 0)
		{
			_AICharacterController.StartAction("Patrol");
		}
		
	}

	#region Movement Functions
	protected void Move()
	{
		return;
	}

	protected void Rotate()
	{
		return;
	}


	#endregion


	#region Grounded Check
	/// <summary>
	/// Set the rpgcharactercontroller maintaining ground to the same
	/// check as we did in the TPC script, didnt do anything special
	/// </summary>
	private void GroundedCheck() //to see when ai lands
	{
		grounded = false;
		acquiringGround = false;
		
		RaycastHit hit;
		if(Physics.SphereCast(transform.position + GetComponent<CapsuleCollider>().center, rayCastSphereRadius, Vector3.down, out hit, 1.0f, groundLayers, QueryTriggerInteraction.Ignore))
		{
			if ((CharacterState)currentState != CharacterState.Vault)
			{
				navMeshAgent.enabled = true;
			}
			grounded = true;
		}
		Vector3 currPos = transform.position;
		currPos.y -= 1 + groundedDistance;
		//Debug.DrawRay(currPos, Vector3.up * 100, Color.cyan);

		if (Physics.Raycast(transform.position, -Vector3.up, out hit, GetComponent<CapsuleCollider>().bounds.extents.y + acquiringGroundedDistance))
		{
			acquiringGround = true;
		}

		// If it is grounded, acquiringGround determines if it should start falling
		if (_animator.GetBool(_animIDGrounded))
		{
			_animator.SetBool(_animIDGrounded, acquiringGround);
		}
		// If it is in air, grounded determines if it should be grounded
		else
		{
			_animator.SetBool(_animIDGrounded, grounded);
		}
	}
	#endregion

	void Update()
	{
		gameObject.SendMessage("SuperUpdate", SendMessageOptions.DontRequireReceiver);
		//print(currentState);
	}

	private void FixedUpdate()
	{
		
		gameObject.SendMessage("SuperFixedUpdate", SendMessageOptions.DontRequireReceiver);
		if (navMeshAgent.enabled)
		{
			_animator.SetFloat(_animIDSpeed, navMeshAgent.speed);
			_animator.SetFloat(_animIDTargetSpeed, navMeshAgent.speed);
		}
	}

	#region Idle
	private void Idle_EnterState()
	{
		//Debug.Log("Entering Idle");
	}

	private void Idle_SuperUpdate()
	{
		
	}

	private void Idle_FixedUpdate() 
	{ 
	}

	private void Idle_ExitState()
	{
		//Debug.Log("Exiting Idle");
	}

	#endregion

	#region Fall
	private void Fall_EnterState()
	{
		Debug.Log("Enter fall state");
		navMeshAgent.enabled = false;
		
		if (setFallTrigger)
		{
			_animator.SetTrigger(_animIDFalling);
		}
		setFallTrigger = false;

	}
	private void Fall_FixedUpdate()
	{
		navMeshAgent.enabled = false;

		if (grounded && _AICharacterController.CanEndAction("Fall"))
		{
			_AICharacterController.EndAction("Fall");
			if (_AICharacterController.CanStartAction("Idle"))
			{
				_AICharacterController.StartAction("Idle");
			}
		}

		// Grab is grab point exist, distance is close enough, and the ledge hasn't been grabbed during this fall
		/*if (climbState.CheckClimb())
		{
			bool success = _AICharacterController.TryEndAction("Fall");

			if (success)
			{
				_AICharacterController.TryStartAction("Climb");
			}
		}*/
	}

	private void Fall_ExitState()
	{
		//_animator.SetBool(_animIDFreeFall, false);
		_animator.ResetTrigger(_animIDFalling);
		navMeshAgent.enabled = true;
	}
	
	#endregion
	
	#region Jump
	private void Jump_EnterState()
	{
		print("entered jump state");
		navMeshAgent.enabled = false;
		_animator.SetTrigger(_animIDJump);
		//_animator.ResetTrigger(_animIDEndSlide);
		_animator.SetBool(_animIDGrounded, false);

		/*Vector3 curVel = _rb.velocity;
		curVel.y = 0;
		_rb.velocity = curVel;*/
		_rb.AddForce(transform.forward * jumpForce + new Vector3(0, jumpForce, 0), ForceMode.Impulse);
		canJump = false;
		_jumpGroundCheckTimer = 0;
	}

	// Run every fixed update frame we are in the Jump state.
	private void Jump_FixedUpdate()
	{
		navMeshAgent.enabled = false;
		//TODO: Variable jump implementation
		/*if (!mc._rpgCharacterController.HasJumpInput()) canVariableJump = false;

		//Counting frames spent in jump
		jumpStateFC += 1;
		if (canVariableJump && jumpStateFC > variableJumpThreshold && jumpStateFC < variableJumpThreshold + variableJumpDuration)
		{
			mc._rb.AddForce(new Vector3(0, variableJumpStrength, 0));
		}*/
		// Grab is grab point exist, distance is close enough, and the ledge hasn't been grabbed during this fall
		//TODO: implement climb into the mix
		if (climbState.CheckClimb())
		{
			bool success = _AICharacterController.TryEndAction("Jump");

			if (success)
			{
				_AICharacterController.TryStartAction("Climb");
			}
		}
		navMeshAgent.enabled = false;
		_jumpGroundCheckTimer += Time.fixedDeltaTime;
		if (_jumpGroundCheckTimer >= jumpGroundCheckCooldown && grounded)
		{
			if (_AICharacterController.TryEndAction("Jump"))
			{
				bool success = _AICharacterController.TryStartAction("Idle");
				if (!success)
				{
					_AICharacterController.TryStartAction("Start");
				}
			}
		}
	}

	private void Jump_ExitState()
	{
		_animator.ResetTrigger(_animIDJump);
		_animator.SetBool(_animIDGrounded, true);
		canJump = true;
		Debug.Log("Exiting Jump");
	}
	#endregion

	#region Climb

	private void Climb_EnterState()
	
	{
		Debug.Log("Hit");

		navMeshAgent.enabled = false;
		climbState.Climb_EnterState();
	}

	private void Climb_FixedUpdate()
	{
		climbState.Climb_FixedUpdate();
	}

	private void Climb_ExitState()
	{
		navMeshAgent.enabled = true;
		climbState.Climb_ExitState();
	}
	#endregion
	
	#region Slide

	private void Slide_EnterState()
	{
		//Navmesh agent height is set to 1 to allow for fluid speed instead of rb force
		navMeshAgent.speed = _slideSpeed;
		_slideDirection = transform.forward;
		
		capsuleCollider.height = slideCapsuleHeight;
		Vector3 fixedCenterPos = new Vector3(capsuleCollider.center.x, -0.75f, capsuleCollider.center.z);
		capsuleCollider.center = fixedCenterPos;
		 
		_rb.AddForce(2.0f * Vector3.down, ForceMode.Force);
		_rb.AddForce(slideForce * _slideDirection, ForceMode.Impulse);
		slideTime = 0;
		_animator.SetTrigger(_animIDSlide);
	}

	private void Slide_FixedUpdate()
	{
		slideTime += Time.fixedDeltaTime * 0.5f;
		if (slideTime < maxSlideTime && (slideTime > minSlideTime))
		{
			_AICharacterController.StartAction("Idle");
		}
	}

	private void Slide_ExitState()
	{
		_animator.SetTrigger(_animIDEndSlide);

		capsuleCollider.radius = _startCapsuleRadius;

		capsuleCollider.height = _startCapsuleHeight;

		capsuleCollider.center = Vector3.zero;

		_animator.ResetTrigger("Slide");
	}

	#endregion
	
	#region Vault

	private void Vault_EnterState()
	{
		print("entered vault state ");
		navMeshAgent.enabled = false;
		vaultState.Vault_EnterState();
	}

	private void Vault_FixedUpdate()
	{
		vaultState.Vault_FixedUpdate();
	}

	private void Vault_ExitState()
	{
		print("exit vault state");
		navMeshAgent.enabled = true;
		vaultState.Vault_ExitState();
	}
	#endregion
	
	#region Chase
	private void Chase_EnterState()
	{
		Debug.Log("Entering Chase");
		m_PlayerNear = false;
		playerLastPosition = Vector3.zero;
	}

	private void Chase_SuperUpdate()
	{
		if (!caughtPlayer)
		{
			Move(runSpeed);
			if (navMeshAgent.enabled)
			{
				navMeshAgent.SetDestination(player.transform.position);
				seePlayer = true; //TODO: set angle of sight to a circular level, m_LastKnownPosition?
			}
		}
	}

	private void Chase_FixedUpdate()
	{

	}

	private void Chase_ExitState()
	{
		Debug.Log("Exiting Chase");
	}

	#endregion

	#region Attack
	private void Attack_EnterState()
	{
		Debug.Log("Entering Attack");
		//Disable navmeshagent to stop following
		navMeshAgent.enabled = false;
		//Initial make ai look at player and stop it from moving
		_rpgCharacterMovementController.transform.LookAt(transform.position, Vector3.up);
		transform.LookAt(player.transform.position, Vector3.up);
		_rb.velocity = new Vector3(0, _rb.velocity.y, 0);
		_rb.constraints = RigidbodyConstraints.FreezeAll;

		if (!_rpgCharacterMovementController.dead)
		{
			//Animation for attacking
			_animator.SetTrigger(_animIDAttack);
			rpgCharacterAnimBehavior.tackleType = tackleType;
			rpgCharacterAnimBehavior.deathType = deathType;
			switch (deathType)
			{
				case DeathType.Punched:
					_animator.SetInteger(_animIDAttackType, 1);
					deathType = DeathType.Tackled;
					break;
				case DeathType.Kicked:
					_animator.SetInteger(_animIDAttackType, 1);
					deathType = DeathType.Tackled;
					break;
				case DeathType.Tackled:
					_animator.SetInteger(_animIDAttackType, 3);
					switch (tackleType)
					{
						case TackleType.Headslam:
							tackleType = TackleType.Strangled;
							_animator.SetInteger(_animIDTackleType, 1);
							deathType = DeathType.Punched;
							break;
						case TackleType.GroundandPound:
							tackleType = TackleType.Headslam;
							_animator.SetInteger(_animIDTackleType, 2);
							deathType = DeathType.Kicked;
							break;
						case TackleType.Strangled:
							tackleType = TackleType.GroundandPound;
							_animator.SetInteger(_animIDTackleType, 3);
							deathType = DeathType.Punched;
							break;
					}
					break;
			}
			_rpgCharacterMovementController._rpgCharacterController.StartAction("Death");

			//StartCoroutine(WaitUntilAnimationDone());
		}
		else
		{
			_AICharacterController.StartAction("Idle");
		}
	}

	private void Attack_SuperUpdate()
	{
		
	}

	private void Attack_FixedUpdate()
	{

	}

	private void Attack_ExitState()
	{
		Debug.Log("Exiting Attack");
		//Revert back to the original death type and renable navmesh agent
		navMeshAgent.enabled = true;
		_rb.constraints = RigidbodyConstraints.None;
		_rpgCharacterMovementController.deathAnimBehavior.deathType = DeathType.Original;
		
		caughtPlayer = false;
		killed = false;
		seePlayer = false;
		
		_AICharacterController.Unlock(true, true);
		_animator.ResetTrigger(_animIDAttack);
		//_rpgCharacterMovementController._rpgCharacterController.StartAction("Death");
	}

	public void StopAttacking()
	{
		_AICharacterController.StartAction("Idle");
		navMeshAgent.enabled = false;
		transform.position = startPosition;
		transform.rotation = startRotation;
		navMeshAgent.enabled = true;
	}
	#endregion

	#region Patrol
	private void Patrol_EnterState()
	{
		//Debug.Log("Entering Patrol");
	}

	private void Patrol_SuperUpdate()
	{
		
	}

	private void Patrol_FixedUpdate()
	{
		//TODO: if player is near AI, have a smaller detection circular radius

		if (m_playerInRange)
		{
			_AICharacterController.StartAction("Chase");
		}
        
		else
		{
			if (waypoints.Count > 0)
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
			else
			{
				Move(0);
			}
		}
	}

	private void Patrol_ExitState()
	{
		Debug.Log("Exiting Patrol");

	}

	#endregion

	#region AI Movement Functionality
	public void NextPoint()
	{
		if (waypoints.Count > 0)
		{
			m_CurrentWayPointIndex = (m_CurrentWayPointIndex + 1) % waypoints.Count;
			navMeshAgent.SetDestination(waypoints[m_CurrentWayPointIndex].position);
		}
		else
		{
			Debug.Log("There are no waypoints!");
		}
	}

	void Move(float speed)
	{
		if (navMeshAgent.enabled)
		{
			navMeshAgent.isStopped = false;
			navMeshAgent.speed = speed;
		}
	}

	void Stop()
	{
		if (navMeshAgent.enabled)
		{
			navMeshAgent.isStopped = true;
			navMeshAgent.speed = 0;
		}
	}

	void LookAround()
	{
		//TODO: look around 45 degree angle from the transform.forward
        
	}
	

	#endregion
	
    #region Utility Functions

    private void FailedActionFix()
    {
	    //go back a few blocks 
	    //distance allocate to go back check for obstacles
	    //then return to original position
	    if ((CharacterState)lastState == CharacterState.Jump)
	    {
		    
	    }
    }
    #region Player Detection
    
    private bool PlayerIsInSight(GameObject obj)
    {
	    Vector3 origin = transform.position;
	    Vector3 dest = obj.transform.position;
	    Vector3 direction = dest - origin;
	    
	    //Height
	    if (direction.y > viewHeight || direction.y < -1)
	    {
		    return false;
	    }
	    
	    //Angle
	    float angle = Vector3.Angle(transform.forward, direction);
	    if (Mathf.Abs(angle) >= viewAngle)
	    {
		    return false;
	    }
	    
	    //Distance
	    if (Vector3.Distance(transform.position, obj.transform.position) > viewDistance)
	    {
		    return false;
	    }
	    return true;
    }

    private void CatchUpToPlayer()
    {
	    if (Vector3.Distance(transform.position, player.transform.position) >= maxDistanceToAccel && runSpeed <= maxChaseSpeed && !aiStop)
	    {
		    runSpeed += accelerationFactor * Time.deltaTime;
	    }
	    else if(Vector3.Distance(transform.position, player.transform.position) < maxDistanceToAccel)
	    {
		    runSpeed = startSpeed;
	    }
    }
    Mesh CreateWedgeMesh()
    {
	    Mesh mesh = new Mesh();
	    int segments = 10;
	    int numTriangles = (segments * 4) + 2 + 2;
	    int numVertices = numTriangles * 3;
		
	    Vector3[] vertices = new Vector3[numVertices];
	    int[] triangles = new int[numVertices];

	    Vector3 bottomCenter = Vector3.zero;
	    Vector3 bottomLeft = Quaternion.Euler(0, -viewAngle, 0) * Vector3.forward * viewDistance;
	    Vector3 bottomRight = Quaternion.Euler(0, viewAngle, 0) * Vector3.forward * viewDistance;

	    Vector3 topCenter = bottomCenter + Vector3.up * viewHeight;
	    Vector3 topLeft = bottomLeft + Vector3.up * viewHeight;
	    Vector3 topRight = bottomRight + Vector3.up * viewHeight;

	    int vert = 0;
	    
	    //left side
	    vertices[vert++] = bottomCenter;
	    vertices[vert++] = bottomLeft;
	    vertices[vert++] = topLeft;
	    
	    vertices[vert++] = topLeft;
	    vertices[vert++] = topCenter;
	    vertices[vert++] = bottomCenter;
	    //right side
	    vertices[vert++] = bottomCenter;
	    vertices[vert++] = topCenter;
	    vertices[vert++] = topRight;
	    
	    vertices[vert++] = topRight;
	    vertices[vert++] = bottomRight;
	    vertices[vert++] = bottomCenter;

	    float currentAngle = -viewAngle;
	    float deltaAngle = (viewAngle * 2) / segments;
	    for (int i = 0; i < segments; ++i)
	    {
		    bottomLeft = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * viewDistance;
		    bottomRight = Quaternion.Euler(0, currentAngle + deltaAngle, 0) * Vector3.forward * viewDistance;

		    topLeft = bottomLeft + Vector3.up * viewHeight;
		    topRight = bottomRight + Vector3.up * viewHeight;
		    
		    //far side
		    vertices[vert++] = bottomLeft;
		    vertices[vert++] = bottomRight;
		    vertices[vert++] = topRight;
	    
		    vertices[vert++] = topRight;
		    vertices[vert++] = topLeft;
		    vertices[vert++] = bottomLeft;
		    //top
		    vertices[vert++] = topCenter;
		    vertices[vert++] = topLeft;
		    vertices[vert++] = topRight;
		    //bottom
		    vertices[vert++] = bottomCenter;
		    vertices[vert++] = bottomRight;
		    vertices[vert++] = bottomLeft;

		    currentAngle += deltaAngle;
	    }
	    
	    for (int i = 0; i < numVertices; ++i)
	    {
		    triangles[i] = i;
	    }

	    mesh.vertices = vertices;
	    mesh.triangles = triangles;
	    mesh.RecalculateNormals();
	    return mesh;
    }

    private void OnValidate()
    {
	    mesh = CreateWedgeMesh();
    }
	private bool playerInKillArea()
	{
		RaycastHit hit;
		if (player)
		{
			Vector3 directionOfPlayer = player.transform.position - transform.position;
			if (directionOfPlayer.y > viewHeight || directionOfPlayer.y < -1)
			{
				return false;
			}
			if(Physics.Raycast(transform.position, directionOfPlayer, out hit, killRadius, playerMask))
			{
				//Debug.Log("caught player");
				Debug.DrawLine(transform.position, player.transform.position, Color.yellow);
				caughtPlayer = true;
				return true;
			}
		}
		
		caughtPlayer = false;
		return false;
	}
	#endregion
	

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("AISlide"))
		{
			_AICharacterController.StartAction("Slide");
		}

		if (other.gameObject.CompareTag("AIJump") && grounded)
		{
			_AICharacterController.StartAction("Jump");
		}
		if (other.gameObject.CompareTag("AIVault") && grounded)
		{
			if (vaultState.VaultCheck())
			{
				_AICharacterController.StartAction("Vault");
			}
		}
		if (other.gameObject.CompareTag("DestroyAI"))
		{
			Destroy(gameObject);
		}

		if (other.gameObject.CompareTag("AI"))
		{
			AIStop();
		}
	}

	private void OnTriggerExit(Collider other)
	{

	}
    
	private void AssignAnimationIDs()
	{
		_animIDSpeed = Animator.StringToHash("Speed");
		_animIDTargetSpeed = Animator.StringToHash("TargetSpeed");
		_animIDGrounded = Animator.StringToHash("Grounded");
		_animIDJump = Animator.StringToHash("Jump");
		_animIDFalling = Animator.StringToHash("Falling");
		_animIDSlide = Animator.StringToHash("Slide");
		_animIDEndSlide = Animator.StringToHash("EndSlide");
		_animIDVault = Animator.StringToHash("Vault");
		_animIDVaultType = Animator.StringToHash("VaultType");
		_animIDClimb = Animator.StringToHash("Climb");
		_animIDClimbType = Animator.StringToHash("ClimbType");
		_animIDAttack = Animator.StringToHash("Attack");
		_animIDAttackType = Animator.StringToHash("KillType");
		_animIDTackleType = Animator.StringToHash("TackleType");
	}

	
	public void AIStop()
	{
		runSpeed = 0;
		walkSpeed = 0;
		killRadius = 3;
		aiStop = true;
		navMeshAgent.speed = 0;
		Debug.Log("hit");
	}

	private void AIResume()
	{
		runSpeed = startSpeed;
	}

	private void ReAssignKillRadius(DeathType attackType, TackleType tackleType)
	{
		switch (attackType)
		{
			case DeathType.Punched:
				killRadius = 1.0f;
				break;
			case DeathType.Kicked:
				killRadius = 1.0f;
				break;
			case DeathType.Tackled:
				switch (tackleType)
				{
					case TackleType.Headslam:
						killRadius = headSlamRadius;
						break;
					case TackleType.GroundandPound:
						killRadius = groundAndPoundRadius;
						break;
					case TackleType.Strangled:
						killRadius = strangleRadius;
						break;
				}
				break;
		}
	}
	#endregion

    
#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
	    if (mesh)
	    {
		    Gizmos.color = meshColor;
		    Gizmos.DrawMesh(mesh,transform.position, transform.rotation);
	    }
	    if (showKillArea)
	    {
		    // Draw the attack zone
		    Gizmos.color = Color.red;
		    Handles.color = Color.red;
		    Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, 360.0f, killRadius);
	    }
	    //Grounded Check
	    Gizmos.DrawSphere(transform.position + GetComponent<CapsuleCollider>().center + Vector3.down * 1.0f, rayCastSphereRadius);
	    //Debug.DrawLine(transform.position, transform.position + transform.forward, Color.red);

	    if (showSlideSpheres)
	    {
		    //Gizmos.DrawSphere(transform.position + GetComponent<CapsuleCollider>().center + Vector3.down * 1.0f, radiusSlideSphereCast);

		    Gizmos.DrawSphere(transform.position + new Vector3(GetComponent<CapsuleCollider>().radius, GetComponent<CapsuleCollider>().height / 2.5f, 0), radiusSlideSphereCast);
		    Gizmos.DrawSphere(transform.position + new Vector3(GetComponent<CapsuleCollider>().radius, -GetComponent<CapsuleCollider>().height / 2.5f, 0), radiusSlideSphereCast);
	    }
	    var nav = GetComponent<NavMeshAgent>();
	    if( nav == null || nav.path == null )
		    return;
 
	    var line = GetComponent<LineRenderer>();
	    if( line == null )
	    {
		    line = gameObject.AddComponent<LineRenderer>();
		    line.material = new Material( Shader.Find( "Sprites/Default" ) ) { color = Color.yellow };
		    line.SetWidth( 0.5f, 0.5f );
		    line.SetColors( Color.yellow, Color.yellow );
	    }
 
	    var path = nav.path;
 
	    line.SetVertexCount( path.corners.Length );
 
	    for( int i = 0; i < path.corners.Length; i++ )
	    {
		    line.SetPosition( i, path.corners[ i ] );
	    }
    }
#endif
}
