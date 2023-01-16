using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;


public class AIRagdollCameraScene : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject AI;

    private AIStateControl _aiStateControl;
    private RPGCharacterMovementController _rpgCharacterMovementController;
    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
    private CinemachineFreeLook flCam;
    private float originalFOV;
    private bool resetCamFollow;

    [SerializeField]private float timer;

    [SerializeField] private float timeOfCutscene;

    private bool lookAt;
    // Start is called before the first frame update
    void Start()
    {
        originalFOV = _cinemachineVirtualCamera.m_Lens.FieldOfView;
        resetCamFollow = false;
        timer = 0;
        lookAt = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (resetCamFollow)
        {
            timer += Time.deltaTime;
        }
        if (timer >= timeOfCutscene)
        {
            //player.GetComponent<RPGCharacterController>().Unlock(true, true);
            _cinemachineVirtualCamera.Follow = player.transform;
            _cinemachineVirtualCamera.m_Lens.FieldOfView = originalFOV;
            if (!lookAt)
            {
                //AI.transform.LookAt(player.transform);
                lookAt = true;
                AI.GetComponent<Rigidbody>().useGravity = true;
                AI.GetComponent<CapsuleCollider>().enabled = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !resetCamFollow)
        {
            Debug.Log("fuck you tai");
            _cinemachineVirtualCamera.Follow = AI.transform;
            //_cinemachineVirtualCamera.m_Lens.FieldOfView = 30f;
            resetCamFollow = true;
            //player.GetComponent<RPGCharacterController>().Lock(true, true, true, 0.0f, timeOfCutscene);
        }
    }
}
