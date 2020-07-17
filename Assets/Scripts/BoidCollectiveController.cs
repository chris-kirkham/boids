using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class which deals with the collectively-changing aspects of boid behaviour (for example, a changing goal direction which they all follow)
public class BoidCollectiveController : MonoBehaviour {

    public const float GOAL_UPDATE_TIME_INTERVAL = 10.0f;
    public const float VELOCITY_LIMIT = 10.0f;
    private float goalUpdateTime = 0.0f;
    private Vector3 goalDir = new Vector3();

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        goalUpdateTime -= Time.deltaTime;

        if(goalUpdateTime <= 0.0f)
        {
            goalUpdateTime = GOAL_UPDATE_TIME_INTERVAL;
            SetNewRandomGoal();
        }

        //Debug.DrawLine(this.transform.position, goalVector, Color.green);

    }

    public void SetNewRandomGoal()
    {
        goalDir = new Vector3(Random.Range(-VELOCITY_LIMIT, VELOCITY_LIMIT), Random.Range(-VELOCITY_LIMIT, VELOCITY_LIMIT), Random.Range(-VELOCITY_LIMIT, VELOCITY_LIMIT));
        goalDir = (goalDir / goalDir.magnitude) * VELOCITY_LIMIT; //scale goal vector to maximum velocity
    }

    public Vector3 GetGoal()
    {
        return goalDir;
    }
}
