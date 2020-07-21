using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;

//[RequireComponent(typeof(BoidVision))]
//[RequireComponent(typeof(BoidMovement))]
public abstract class BoidBehaviour : MonoBehaviour 
{
    protected BoidVision boidVision;
    protected BoidMovement boidMovement;
    public BoidCollectiveController boidCollectiveController;

    public MouseTargetPosition mouseTargetObj; //ScriptableObject which holds mouse target position, if using mouse following
    protected Vector3 mouseTarget; //convenience var to hold mouse target position

    public float boidAvoidDistance;
    public bool usePreemptiveObstacleAvoidance = false;
    public bool useObstacleRepulsion = true;
    public float obstacleAvoidDistance;
    public int numClosestToCheck = 5;

    //update time params
    public float UpdateTimeID { protected get; set; }
    protected const float BASE_UPDATE_TIME_INTERVAL = 0.2f;
    protected const float MAX_UPDATE_TIME = BASE_UPDATE_TIME_INTERVAL * 2;
    protected const float UPDATE_TIME_VARIANCE = 0.2f; //vary time between boid updates so not all boids will update on the same frame
    protected float updateTime = 0.0f; //used to time next vision update

    public bool useMouseFollow;
    public bool useRandomGoal;

    //multipliers for boid/obstacle/out-of-bounds avoidance
    public float boundsAvoidMultiplier = 1f;
    public float boidAvoidMultiplier = 1f;
    public float obstacleAvoidMultiplier = 5f;
    public float mouseFollowMultiplier = 1f;
    public float goalVectorMultiplier = 1f; //multiplier of random goal direction vector

    //coordinates to constrain boid movement
    public bool useBoundingCoordinates = true;
    public float boundsSize; //size of cube representing boid bounding area (centre is at (0, 0, 0))
    protected Vector3 positiveBounds, negativeBounds; //bounding coords (from boundsBox object)
    protected const float OUT_OF_BOUNDS_VELOCITY = 10f; //velocity with which to return to bounding area if out of bounds (added to other velocities so will be capped after)

    //obstacle avoidance
    protected const float OBSTACLE_CRITICAL_DISTANCE = 10f; //distance at which boid is considered critically close to an obstacle, and will prioritise its avoidance
    protected const float OBSTACLE_CHECK_DISTANCE = 50f; //distance from boid to cast ray to check if boid is heading towards an obstacle
    protected const int MAX_OBSTACLE_RAYCAST_TRIES = 4; //max number of raycast iterations the boid can perform to find a path around an obstacle
    protected const float OBSTACLE_AVOID_RAY_INCREMENT = 25; //number to increase/decrease the x/y/z (depending on direction) of each raycast by when trying to find path around obstacle

    //idle behaviour
    protected Vector3 idleVec;
    protected const float BASE_IDLE_TIMER = 5.0f;
    protected const float IDLE_TIMER_VARIANCE = BASE_IDLE_TIMER / 2;
    protected float idleTimer = 0.0f; //tracks when to change idle movement vector

    //LAYER MASKS
    protected const int LAYER_BOID = 1 << 9;
    protected const int LAYER_OBSTACLE = 1 << 10;

    //boid move direction - not updated every tick, but stored so it can be used
    protected Vector3 moveDirection = Vector3.zero;

    protected virtual void Start ()
    {
        boidVision = GetComponent<BoidVision>();
        boidMovement = GetComponent<BoidMovement>();
        
        float halfBoundsSize = boundsSize / 2;
        positiveBounds = new Vector3(halfBoundsSize, halfBoundsSize, halfBoundsSize);
        negativeBounds = new Vector3(-halfBoundsSize, -halfBoundsSize, -halfBoundsSize);

        //stagger boid update time by its spawn ID; mod so update time is never more than MAX_UPDATE_TIME
        updateTime = BASE_UPDATE_TIME_INTERVAL + ((UpdateTimeID / 100f) % (MAX_UPDATE_TIME - BASE_UPDATE_TIME_INTERVAL));
        
        Debug.Log(updateTime);
        //TODO: set initial boid velocity

        StartCoroutine(UpdateBoidCoroutine());
    }

    //Put boid update code here; called in UpdateBoidCoroutine
    protected abstract void UpdateBoid();

    //Calls UpdateBoid every updateTime seconds
    protected IEnumerator UpdateBoidCoroutine()
    {
        while(true)
        {
            UpdateBoid();
            yield return new WaitForSeconds(updateTime);
        }
    }
}