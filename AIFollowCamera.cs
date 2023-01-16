using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using TryAgainFSM.Actions;
using TryAgainFSM.Lookups;
using VLB;

public class AIFollowCamera : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject AI;

    private AIStateControl _aiStateControl;
    private RPGCharacterMovementController _rpgCharacterMovementController;
    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;

    private float distanceAIToPlayer;

    private float originalFOV;
    [SerializeField] private float fovThreshold;
    [SerializeField] private float maxDistanceBetweenAIToPlayer;
    private bool resetCamFollow;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // Start is called before the first frame update
    private void Start()
    {
        originalFOV = _cinemachineVirtualCamera.m_Lens.FieldOfView;
        distanceAIToPlayer = 0;
        _aiStateControl = AI.GetComponent<AIStateControl>();
        _rpgCharacterMovementController = player.GetComponent<RPGCharacterMovementController>();
        resetCamFollow = false;
    }

    void Update()
    {
        //print(distanceAIToPlayer);
        distanceAIToPlayer = Vector3.Distance(player.transform.position, AI.transform.position);
        if (_aiStateControl.seePlayer && distanceAIToPlayer <= maxDistanceBetweenAIToPlayer)
        {
            _cinemachineVirtualCamera.Follow = transform;
            
            //standard distance is 20
            _cinemachineVirtualCamera.m_Lens.FieldOfView = originalFOV + (distanceAIToPlayer - fovThreshold);
        }
        else if (_rpgCharacterMovementController.dead && !resetCamFollow)
        {
            resetCamFollow = true;
            _cinemachineVirtualCamera.m_Lens.FieldOfView = originalFOV;
        }
        else
        {
            _cinemachineVirtualCamera.m_Lens.FieldOfView = originalFOV;
            _cinemachineVirtualCamera.Follow = player.transform;
        }

        resetCamFollow = false;
        if ((player.transform.position - AI.transform.position).normalized.z > 0)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, (player.transform.position.z + AI.transform.position.z) / 2);
        }
        else
        {
            transform.position = new Vector3((player.transform.position.x + AI.transform.position.x) / 2, transform.position.y, transform.position.z);
        }
        
    }
}
