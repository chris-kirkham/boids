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
    //Boid ID
    public int BoidID { protected get; set; }

    [Header("Components")]
    public BoidBehaviourParams behaviourParams;
    public BoidCollectiveController boidCollectiveController;
    public MouseTargetPosition mouseTargetObj; //ScriptableObject which holds mouse target position, if using mouse following
    protected Vector3 mouseTarget; //convenience var to hold mouse target position
    protected BoidVision boidVision;
    protected BoidMovement boidMovement;

    [Header("Reaction to other boids")]
    protected float sqrBoidAvoidDistance;

    [Header("Bounding coordinates")]
    protected Vector3 positiveBounds, negativeBounds; //bounding coords (from boundsBox object)
    protected const float OUT_OF_BOUNDS_VELOCITY = 10f; //velocity with which to return to bounding area if out of bounds (added to other velocities so will be capped after)

    [Header("Obstacle avoidance")]
    protected const float OBSTACLE_CRITICAL_DISTANCE = 10f; //distance at which boid is considered critically close to an obstacle, and will prioritise its avoidance
    protected const float OBSTACLE_CHECK_DISTANCE = 50f; //distance from boid to cast ray to check if boid is heading towards an obstacle
    protected const int MAX_OBSTACLE_RAYCAST_TRIES = 4; //max number of raycast iterations the boid can perform to find a path around an obstacle
    protected const float OBSTACLE_AVOID_RAY_INCREMENT = 25; //number to increase/decrease the x/y/z (depending on direction) of each raycast by when trying to find path around obstacle

    /*----OTHER MEMBER VARIABLES----*/
    //boid move direction - not updated every tick, but stored so it can be used
    protected Vector3 moveDirection = Vector3.zero;

    //layer masks
    protected const int LAYER_BOID = 1 << 9;
    protected const int LAYER_OBSTACLE = 1 << 10;

    //update time params
    protected const float BASE_UPDATE_TIME_INTERVAL = 0.2f;
    protected const float MAX_UPDATE_STAGGER_TIME = 0.4f;
    protected const float UPDATE_TIME_VARIANCE = 0.1f; //vary time between boid updates so not all boids will update on the same frame
    protected float updateTime = 0.0f; //used to time next vision update

    protected virtual void Start ()
    {
        boidVision = GetComponent<BoidVision>();
        boidMovement = GetComponent<BoidMovement>();

        sqrBoidAvoidDistance = behaviourParams.boidAvoidDistance * behaviourParams.boidAvoidDistance;
        
        float halfBoundsSize = behaviourParams.boundsSize / 2;
        positiveBounds = new Vector3(halfBoundsSize, halfBoundsSize, halfBoundsSize);
        negativeBounds = new Vector3(-halfBoundsSize, -halfBoundsSize, -halfBoundsSize);

        //stagger boid update time by its spawn ID; mod so update time is never more than MAX_UPDATE_TIME
        float mod = (MAX_UPDATE_STAGGER_TIME - BASE_UPDATE_TIME_INTERVAL);
        if (mod <= 0)
        {
            updateTime = 0;
        }
        else
        {
            //updateTime = BASE_UPDATE_TIME_INTERVAL + ((UpdateTimeID / 100f) % mod);
            updateTime = (BoidID / 100f) % mod;
        }

        //Debug.Log(updateTime);
        //TODO: set initial boid velocity
        StartCoroutine(UpdateBoidCoroutine());
    }

    protected virtual void OnValidate()
    {
        sqrBoidAvoidDistance = behaviourParams.boidAvoidDistance * behaviourParams.boidAvoidDistance;
    }

    //Called in UpdateBoidCoroutine; put boid update code here
    protected abstract void UpdateBoid();

    //Calls UpdateBoid every updateTime seconds
    protected IEnumerator UpdateBoidCoroutine()
    {
        yield return new WaitForSeconds(updateTime);

        while(true)
        {
            UpdateBoid();
            yield return new WaitForSeconds(BASE_UPDATE_TIME_INTERVAL);
        }
    }
}