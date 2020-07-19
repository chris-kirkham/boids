using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;

//[RequireComponent(typeof(BoidVision))]
//[RequireComponent(typeof(BoidMovement))]
public class BoidBehaviour : MonoBehaviour {

    private BoidVision boidVision;
    private BoidMovement boidMovement;
    public BoidCollectiveController boidCollectiveController;

    public MouseTargetPosition mouseTargetObj; //ScriptableObject which holds mouse target position, if using mouse following
    private Vector3 mouseTarget; //convenience var to hold mouse target position

    public float boidAvoidDistance;
    public bool usePreemptiveObstacleAvoidance = false;
    public bool useObstacleRepulsion = true;
    public float obstacleAvoidDistance;
    public int numClosestToCheck = 5;

    private const float BASE_UPDATE_TIME_INTERVAL = 0.1f;
    private const float UPDATE_TIME_VARIANCE = 0.1f; //vary time between boid updates so not all boids will update on the same frame
    private float updateTime = 0.0f; //used to time next vision update

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
    private Vector3 positiveBounds, negativeBounds; //bounding coords (from boundsBox object)
    private const float OUT_OF_BOUNDS_VELOCITY = 10f; //velocity with which to return to bounding area if out of bounds (added to other velocities so will be capped after)

    //obstacle avoidance
    private const float OBSTACLE_CRITICAL_DISTANCE = 10f; //distance at which boid is considered critically close to an obstacle, and will prioritise its avoidance
    private const float OBSTACLE_CHECK_DISTANCE = 50f; //distance from boid to cast ray to check if boid is heading towards an obstacle
    private const int MAX_OBSTACLE_RAYCAST_TRIES = 10; //max number of raycasts the boid will try to find a path around an obstacle
    private const float OBSTACLE_AVOID_RAY_INCREMENT = 25; //number to increase/decrease the x/y/z (depending on direction) of each raycast by when trying to find path around obstacle

    //idle behaviour
    private Vector3 idleVec;
    private const float BASE_IDLE_TIMER = 5.0f;
    private const float IDLE_TIMER_VARIANCE = BASE_IDLE_TIMER / 2;
    private float idleTimer = 0.0f; //tracks when to change idle movement vector

    //LAYER MASKS
    private const int LAYER_BOID = 1 << 9;
    private const int LAYER_OBSTACLE = 1 << 10;

    //boid move direction - not updated every tick, but stored so it can be used
    private Vector3 moveDirection = Vector3.zero;

    // Use this for initialization
    void Start () {

        boidVision = GetComponent<BoidVision>();
        boidMovement = GetComponent<BoidMovement>();
        
        //TODO: set initial boid velocity

        float halfBoundsSize = boundsSize / 2;
        positiveBounds = new Vector3(halfBoundsSize, halfBoundsSize, halfBoundsSize);
        negativeBounds = new Vector3(-halfBoundsSize, -halfBoundsSize, -halfBoundsSize);
    }   

    void Update()
    {
        if (ControlInputs.Instance.useMouseFollow) mouseTarget = mouseTargetObj.mouseTargetPosition;
    }
    
    void FixedUpdate()
    {
        updateTime -= Time.deltaTime;
        idleTimer -= Time.deltaTime;

        UpdateIdle(); //update idle behaviour

        //calculate boid behaviour
        if(updateTime <= 0.0f)
        {
            updateTime = BASE_UPDATE_TIME_INTERVAL + Random.Range(0.0f, UPDATE_TIME_VARIANCE);

            boidVision.UpdateSeenBoids();

            moveDirection = CalcRules();
        }

        boidMovement.MoveBoid(moveDirection);
        transform.right = Vector3.Lerp(transform.right, -moveDirection, 100f * Time.deltaTime); //rotate boid to face movement direction
    }

    Vector3 ReturnToBounds()
    {
        Vector3 boundsAvoidVector = Vector3.zero;

        if (ControlInputs.Instance.useBoundingCoordinates)
        {
            //if close to edge of bounding box, move away from the edge
            if (transform.position.x > positiveBounds.x)
            {
                boundsAvoidVector.x -= Mathf.Abs(transform.position.x - positiveBounds.x);
            }
            else if (transform.position.x < negativeBounds.x)
            {
                boundsAvoidVector.x += Mathf.Abs(transform.position.x - negativeBounds.x);
            }

            if (transform.position.y > positiveBounds.y)
            {
                boundsAvoidVector.y -= Mathf.Abs(transform.position.y - positiveBounds.y);
            }
            else if (transform.position.y < negativeBounds.y)
            {
                boundsAvoidVector.y += Mathf.Abs(transform.position.y - negativeBounds.y);
            }

            if (transform.position.z > positiveBounds.z)
            {
                boundsAvoidVector.z -= Mathf.Abs(transform.position.z - positiveBounds.z);
            }
            else if (transform.position.z < negativeBounds.z)
            {
                boundsAvoidVector.z += Mathf.Abs(transform.position.z - negativeBounds.z);
            }

            boundsAvoidVector *= boundsAvoidMultiplier;
        }

        return boundsAvoidVector;
    }

    //determines the boid's velocity vector after reacting to other boids in the environment (avoiding other boids, moving to centre of other boids,
    //matching velocity with other boids)
    //(this is all in one function so we don't have to loop through seen boids more than once)
    Vector3 ReactToOtherBoids()
    {
        Vector3 boidAvoidVector = Vector3.zero;
        Vector3 centre = Vector3.zero;
        Vector3 velocityMatch = Vector3.zero;

        if (boidVision.SeenBoids.Count == 0)
        {
            return Vector3.zero;
        }
        else
        {
            //check if numClosestToCheck > number of visible boids
            int numToCheck = Mathf.Min(numClosestToCheck, boidVision.SeenBoids.Count);

            for (int i = 0; i < numToCheck; i++)
            {
                //avoid other boids
                if (Vector3.Distance(transform.position, boidVision.SeenBoids[i].transform.position) < boidAvoidDistance)
                {
                    boidAvoidVector += transform.position - boidVision.SeenBoids[i].transform.position;
                    Debug.DrawLine(transform.position, boidVision.SeenBoids[i].transform.position, Color.cyan);
                }

                //move towards centre of nearby boids
                centre += boidVision.SeenBoids[i].transform.position - transform.position;

                //match velocity with nearby boids
                velocityMatch += boidVision.SeenBoids[i].GetComponent<BoidMovement>().GetVelocity(); 
            }

            centre = centre / boidVision.SeenBoids.Count;
            velocityMatch = velocityMatch / boidVision.SeenBoids.Count;
        }
        
        return (boidAvoidVector * boidAvoidMultiplier) + centre + velocityMatch;
    }

    //Check if the boid is heading towards an obstacle; if so, fire rays out in increasing angles to the left, right,
    //above and below the boid in order to find a path around an obstacle.
    //If multiple rays find a path, select the one closest to the boid's current velocity.
    Vector3 AvoidObstacles()
    {
        Vector3 avoidVector = Vector3.zero; //return vector

        //fire a ray in direction of boid's velocity (sqr magnitude of velocity in length; change this?) to see if there is an obstacle in its path.
        //if there is an obstacle in its path (regardless of whether it is currently avoiding a(nother) obstacle,
        //find avoid vector which is closest to either current velocity vector or mouse target, depending on if using mouse follow.
        Vector3 target;
        float checkDistance;
        if (ControlInputs.Instance.useMouseFollow)
        {
            target = mouseTarget - transform.position;
            checkDistance = Vector3.Distance(transform.position, mouseTarget);
        }
        else
        {
            target = boidMovement.GetVelocity();
            checkDistance = OBSTACLE_CHECK_DISTANCE;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, target, out hit, checkDistance, LAYER_OBSTACLE))
        {
            int raycastTries = 0;
            float inc = OBSTACLE_AVOID_RAY_INCREMENT;

            Vector3 closestVector = Vector3.positiveInfinity;
            bool foundAvoidVector = false;
            float minDiff = Mathf.Infinity;

            while (!foundAvoidVector && raycastTries <= MAX_OBSTACLE_RAYCAST_TRIES) 
            {
                //up
                Vector3 up = new Vector3(target.x, target.y + inc, target.z - inc);
                //Debug.DrawRay(transform.position, up, Color.blue);
                if (!Physics.Raycast(transform.position, up, checkDistance, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    closestVector = up;
                    minDiff = Vector3.SqrMagnitude(target - up);
                    foundAvoidVector = true;
                }

                //right
                Vector3 right = new Vector3(target.x + inc, target.y, target.z - inc);
                float rightDiff = Vector3.SqrMagnitude(target - right);
                //Debug.DrawRay(transform.position, right, Color.blue);
                if (rightDiff < minDiff && !Physics.Raycast(transform.position, right, checkDistance, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    closestVector = right;
                    minDiff = rightDiff;
                    foundAvoidVector = true;
                }

                //down
                Vector3 down = new Vector3(target.x, target.y - inc, target.z - inc);
                float downDiff = Vector3.SqrMagnitude(target - down);
                //Debug.DrawRay(transform.position, down, Color.blue);
                if (downDiff < minDiff && !Physics.Raycast(transform.position, down, checkDistance, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    closestVector = down;
                    minDiff = downDiff;
                    foundAvoidVector = true;
                }

                //left
                Vector3 left = new Vector3(target.x - inc, target.y, target.z - inc);
                //Debug.DrawRay(transform.position, left, Color.blue);
                if (Vector3.SqrMagnitude(target - left) < minDiff && !Physics.Raycast(transform.position, left, checkDistance, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    closestVector = left;
                    foundAvoidVector = true;
                }

                inc += OBSTACLE_AVOID_RAY_INCREMENT;
                raycastTries++;
            }

            //if we found a way/ways around obstacle on this loop, return closestVector
            if (foundAvoidVector)
            {
                Debug.DrawRay(transform.position, closestVector, Color.green);
                return closestVector * obstacleAvoidMultiplier;
            }
        }

        return Vector3.zero;
    }
    
    //causes the boid to repel itself from obstacles closer than OBSTACLE_CRITICAL_DISTANCE
    Vector3 ObstacleRepulsion()
    {
        Vector3 repulsionVec = Vector3.zero;

        /*
        //check cardinal directions for close objects
        if (Physics.Raycast(transform.position, transform.forward, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec += -transform.forward; //forward
        if (Physics.Raycast(transform.position, -transform.forward, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec += transform.forward; //back
        if (Physics.Raycast(transform.position, -transform.right, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec += transform.right; //left
        if (Physics.Raycast(transform.position, transform.right, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec += -transform.right; //right
        if (Physics.Raycast(transform.position, transform.up, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec += -transform.up; //up
        if (Physics.Raycast(transform.position, -transform.up, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec += transform.up; //down
        */

        if (Physics.Raycast(transform.position, boidMovement.GetVelocity(), out RaycastHit hit, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulsionVec = hit.normal;
        //Debug.DrawRay(transform.position, repulsionVec, Color.yellow,  0.2f);

        return repulsionVec * obstacleAvoidMultiplier * 1000;
    }
    
    Vector3 FollowCursor()
    {
        return (ControlInputs.Instance.useMouseFollow) ? (mouseTarget - transform.position) * mouseFollowMultiplier : Vector3.zero;
    }

    Vector3 MoveToGoal()
    {
        return (ControlInputs.Instance.useRandomGoal) ? (boidCollectiveController.GetGoal() + transform.position) * goalVectorMultiplier : Vector3.zero;
    }

    void UpdateIdle() //updates idleVec with new idle movement vector if idle timer <= 0 (N.B. Idle timer is decremented in FixedUpdate)
    {
        if(idleTimer <= 0.0f)
        {
            idleTimer = BASE_IDLE_TIMER + Random.Range(-IDLE_TIMER_VARIANCE, IDLE_TIMER_VARIANCE);
            idleVec = new Vector3(
                Random.Range(-boidMovement.maxSpeed, boidMovement.maxSpeed),
                Random.Range(-boidMovement.maxSpeed, boidMovement.maxSpeed),
                Random.Range(-boidMovement.maxSpeed, boidMovement.maxSpeed));
        }
    }

    //calculates and returns a velocity vector based on a priority ordering of the boid's rules
    Vector3 CalcRules()
    {
        Vector3 avoidVector = usePreemptiveObstacleAvoidance ? AvoidObstacles() : Vector3.zero; //non-zero if using obstacle avoidance and boid is heading towards an obstacle
        Vector3 repulsionVector = useObstacleRepulsion ? ObstacleRepulsion() : Vector3.zero;
        if(!(avoidVector == Vector3.zero)) //prioritise obstacle avoidance
        {
            return avoidVector + repulsionVector + ReactToOtherBoids(); 
        }
        else if(boidVision.SeenBoids.Count == 0 && !ControlInputs.Instance.useMouseFollow) //if no boids nearby, not avoiding an obstacle, and not following mouse, do idle behaviour
        {
            return idleVec + repulsionVector + ReturnToBounds();
        }
        else
        {
            return ReactToOtherBoids() + repulsionVector + FollowCursor() + MoveToGoal() + ReturnToBounds();  
        }
    }
}