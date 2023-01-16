using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISpawner : MonoBehaviour
{
    public float spawnTime;

    public float maxTime = 5f;

    [SerializeField] private GameObject aiPrefab;

    private GameObject ai;

    [SerializeField] private Transform[] patrolPoints;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        spawnTime += Time.deltaTime;
        if (spawnTime >= maxTime)
        {
            ai = Instantiate(aiPrefab, transform.position - new Vector3(0, 3f, 0), Quaternion.identity);
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                ai.GetComponent<AIStateControl>().waypoints.Add(patrolPoints[i]);
            }
            spawnTime = 0;
        }
    }

    public void AssignPatrolPoints()
    {
        if (ai)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                ai.GetComponent<AIStateControl>().waypoints.Add(patrolPoints[i]);
            }
        }
    }
}
