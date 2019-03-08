using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoidVision))]
[RequireComponent(typeof(BoidMovement))]
[RequireComponent(typeof(Rigidbody))]
public class BoidBehaviour : MonoBehaviour {

    private BoidVision boidVision;
    private BoidMovement boidMovement;
    public BoidCollectiveController boidCollectiveController;

    Rigidbody rb;
    List<GameObject> seenBoids; //list in which to store other boids seen by this boid at each timestep
    List<GameObject> seenObstacles; //list in which to store environment obstacles seen by this boid at each timestep

    public MouseTargetPosition mouseTargetObj; //ScriptableObject which holds mouse target position, if using mouse following
    private Vector3 mouseTarget;

    public float boidAvoidDistance; 
    public float obstacleAvoidDistance;
    public int numClosestToCheck = 5;

    private const float BASE_UPDATE_TIME_INTERVAL = 0.1f;
    private const float UPDATE_TIME_VARIANCE = 0.1f; //vary time between boid updates so not all boids will update on the same frame
    private float updateTime = 0.0f; //used to time next vision update

    public bool useMouseFollow;
    public bool useRandomGoal;

    //multipliers for boid/obstacle/out-of-bounds avoidance
    public float boundsAvoidMultiplier = 1.0f;
    public float boidAvoidMultiplier = 1.0f;
    public float obstacleAvoidMultiplier = 5.0f;
    public float mouseFollowMultiplier = 1.0f;
    public float goalVectorMultiplier = 1.0f; //multiplier of random goal direction vector

    //coordinates to constrain boid movement
    public bool useBoundingCoordinates = true;
    public float boundsSize; //size of cube representing boid bounding area (centre is at (0, 0, 0))
    private Vector3 positiveBounds, negativeBounds; //bounding coords (from boundsBox object)
    private const float OUT_OF_BOUNDS_VELOCITY = 10.0f; //velocity with which to return to bounding area if out of bounds (added to other velocities so will be capped after)

    //obstacle avoidance
    private const float OBSTACLE_CRITICAL_DISTANCE = 10.0f; //distance at which boid is considered critically close to an obstacle, and will prioritise its avoidance
    private const float OBSTACLE_CHECK_DISTANCE = 50.0f; //distance from boid to cast ray to check if boid is heading towards an obstacle
    private const int MAX_OBSTACLE_RAYCAST_TRIES = 10; //max number of raycasts the boid will try to find a path around an obstacle
    private float rayIncrement = 10f; //number to increase/decrease the x/y/z (depending on direction) of each raycast by when trying to find path around obstacle

    //idle behaviour
    private Vector3 idleVec;
    private const float BASE_IDLE_TIMER = 5.0f;
    private const float IDLE_TIMER_VARIANCE = BASE_IDLE_TIMER / 2;
    private float idleTimer = 0.0f; //tracks when to change idle movement vector

    //LAYER MASKS
    private const int LAYER_BOID = 1 << 9;
    private const int LAYER_OBSTACLE = 1 << 10;

    // Use this for initialization
    void Start () {

        boidVision = GetComponent<BoidVision>();
        boidMovement = GetComponent<BoidMovement>();
        rb = GetComponent<Rigidbody>();

        //set initial boid velocity

        float halfBoundsSize = boundsSize / 2;
        positiveBounds = new Vector3(halfBoundsSize, halfBoundsSize, halfBoundsSize);
        negativeBounds = new Vector3(-halfBoundsSize, -halfBoundsSize, -halfBoundsSize);

        seenBoids = new List<GameObject>();
        seenObstacles = new List<GameObject>();
    }   

    void Update()
    {
        if(ControlInputs.Instance.useMouseFollow) mouseTarget = mouseTargetObj.mouseTargetPosition;
        //Debug.DrawLine(transform.position, transform.position + rb.velocity, Color.red);
    }
    
    /*
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
        Gizmos.DrawSphere(transform.position, obstacleAvoidDistance);
    }
    */

    void FixedUpdate()
    {
        updateTime -= Time.deltaTime;
        idleTimer -= Time.deltaTime;

        UpdateIdle(); //update idle behaviour

        //calculate boid behaviour
        if(updateTime <= 0.0f)
        {
            updateTime = BASE_UPDATE_TIME_INTERVAL + Random.Range(0.0f, UPDATE_TIME_VARIANCE);

            //clear seen objects lists before checking again
            seenBoids.Clear();
            seenObstacles.Clear();

            seenBoids = boidVision.GetSeenBoids();
            seenObstacles = boidVision.GetSeenObstacles();

            boidMovement.MoveBoid(CalcRules());
            transform.right = -rb.velocity.normalized; //rotate boid to face movement direction
        }
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
    //matching velocity with other boids
    //(this is all in one function so we don't have to loop through seen boids more than once)
    Vector3 ReactToOtherBoids()
    {
        Vector3 boidAvoidVector = Vector3.zero;
        Vector3 centre = Vector3.zero;
        Vector3 velocityMatch = Vector3.zero;

        if (seenBoids.Count > 0) //this function shouldn't be called if there are no seen boids, but check again here to avoid divide-by-zero errors
        {
            //check if numClosestToCheck > number of visible boids
            int numToCheck = Mathf.Min(numClosestToCheck, seenBoids.Count);

            for (int i = 0; i < numToCheck; i++)
            {
                //avoid other boids
                if (Vector3.Distance(transform.position, seenBoids[i].transform.position) < boidAvoidDistance)
                {
                    boidAvoidVector += transform.position - seenBoids[i].transform.position;
                    Debug.DrawLine(transform.position, seenBoids[i].transform.position, Color.cyan);
                }

                centre += seenBoids[i].transform.position - transform.position; //move towards centre of nearby boids
                velocityMatch += seenBoids[i].GetComponent<Rigidbody>().velocity; //match velocity with nearby boids
                
            }

            centre = centre / seenBoids.Count;
            velocityMatch = velocityMatch / seenBoids.Count;
        }
        
        //Debug.Log("bounds avoid = " + boundsAvoidVector + ", avoid = " + avoidVector + ", centre mass = " + centre + ", match velocities = " + velocityMatch);
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
            target = rb.velocity;
            checkDistance = OBSTACLE_CHECK_DISTANCE;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, target, out hit, checkDistance, LAYER_OBSTACLE))
        {
            int raycastTries = 0;
            float inc = rayIncrement;

            Vector3 closestVector = Vector3.positiveInfinity;
            bool foundAvoidVector = false;
            float minDiff = Mathf.Infinity;
            while (raycastTries <= MAX_OBSTACLE_RAYCAST_TRIES) 
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

                //if we found a way/ways around obstacle on this loop, return closestVector
                if(foundAvoidVector)
                {
                    Debug.DrawRay(transform.position, closestVector, Color.green);
                    return closestVector * obstacleAvoidMultiplier;
                }
                else
                { 
                    inc += rayIncrement;
                    raycastTries++;
                }
            }
        }

        return Vector3.zero;
    }
    
    //causes the boid to repel itself from obstacles closer than OBSTACLE_CRITICAL_DISTANCE
    Vector3 ObstacleRepulsion()
    {
        Vector3 avoidVector = Vector3.zero;

        /* check cardinal directions for close objects */
        //forward
        if(Physics.Raycast(transform.position, transform.forward, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE))
        {
            avoidVector += -transform.forward;
        }

        //back
        if (Physics.Raycast(transform.position, -transform.forward, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE))
        {
            avoidVector += transform.forward;
        }

        //left
        if (Physics.Raycast(transform.position, -transform.right, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE))
        {
            avoidVector += transform.right;
        }

        //right
        if (Physics.Raycast(transform.position, transform.right, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE))
        {
            avoidVector += -transform.right;
        }

        //up
        if (Physics.Raycast(transform.position, transform.up, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE))
        {
            avoidVector += -transform.up;
        }

        //down
        if (Physics.Raycast(transform.position, -transform.up, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE))
        {
            avoidVector += transform.up;
        }

        Debug.DrawRay(transform.position, avoidVector, Color.yellow);
        return avoidVector * obstacleAvoidMultiplier;
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
                Random.Range(-boidMovement.velocityLimit, boidMovement.velocityLimit),
                Random.Range(-boidMovement.velocityLimit, boidMovement.velocityLimit),
                Random.Range(-boidMovement.velocityLimit, boidMovement.velocityLimit));
        }
    }

    //calculates and returns a velocity vector based on a priority ordering of the boid's rules
    Vector3 CalcRules()
    {
        Vector3 avoidVector = AvoidObstacles();

        if(!(avoidVector == Vector3.zero)) //prioritise obstacle avoidance
        {
            return avoidVector + ObstacleRepulsion() + ReactToOtherBoids(); 
        }
        else if(seenBoids.Count == 0 && !ControlInputs.Instance.useMouseFollow) //if no boids nearby, not avoiding an obstacle, and not following mouse, do idle behaviour
        {
            return idleVec + ObstacleRepulsion() + ReturnToBounds();
        }
        else
        {
            return ReactToOtherBoids() + ObstacleRepulsion() + FollowCursor() + MoveToGoal() + ReturnToBounds();  
        }

    }

    /*
    //debug function to check if a vector3 contains a NaN value
    bool CheckNaN(Vector3 vec)
    {
        return float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z);
    }

    //debug function to check if a vector3 contains a +/-infinity value
    bool CheckInfinity(Vector3 vec)
    {
        return float.IsPositiveInfinity(vec.x) || float.IsPositiveInfinity(vec.y) || float.IsPositiveInfinity(vec.z)
            || float.IsNegativeInfinity(vec.x) || float.IsNegativeInfinity(vec.y) || float.IsNegativeInfinity(vec.z);
    }
    */
}