using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidBehaviour : MonoBehaviour {

    Rigidbody rb;
    List<GameObject> seenBoids; //list in which to store other boids seen by this boid at each timestep
    List<GameObject> seenObstacles; //list in which to store environment obstacles seen by this boid at each timestep
    public BoidCollectiveController boidCollectiveController;

    public MouseTargetPosition mouseTargetObj; //ScriptableObject which holds mouse target position, if using mouse following
    private Vector3 mouseTarget;

    public float boidAvoidDistance; 
    public float obstacleAvoidDistance;
    public float overlapSphereRadius;
    private float minAdaptiveOverlapRadius, maxAdaptiveOverlapRadius;
    private float adaptiveOverlapSphereInc = 0.5f; //number to increment/decrement adaptive overlap sphere size by if using it
    public float velocityLimit;
    public int numClosestToCheck = 5;

    private const float VISION_UPDATE_TIME_INTERVAL = 0.1f;
    private float updateTime = 0.0f; //used to time next vision update

    public bool useMouseFollow;
    public bool useRandomGoal;
    public bool useAdaptiveOverlapSphere;

    //multipliers for boid/obstacle/out-of-bounds avoidance
    public float boundsAvoidMultiplier = 1.0f;
    public float boidAvoidMultiplier = 1.0f;
    public float obstacleAvoidMultiplier = 5.0f;
    public float mouseFollowMultiplier = 1.0f;
    public float goalVectorMultiplier = 1.0f; //multiplier of random goal direction vector

    //coordinates to constrain boid movement
    public bool useBoundingCoordinates = true;
    //public GameObject boundsBox; //object whose bounds will be used as boid bounding area - must have a Collider
    public float boundsSize; //size of cube representing boid bounding area (centre is at (0, 0, 0))
    private Vector3 positiveBounds, negativeBounds; //bounding coords (from boundsBox object)
    private const float OUT_OF_BOUNDS_VELOCITY = 10.0f; //velocity with which to return to bounding area if out of bounds (added to other velocities so will be capped after)

    //obstacle avoidance
    private const float OBSTACLE_CRITICAL_DISTANCE = 10.0f; //distance at which boid is considered critically close to an obstacle, and will prioritise its avoidance
    private const float OBSTACLE_CHECK_DISTANCE = 50.0f; //distance from boid to cast ray to check if boid is heading towards an obstacle
    private const int MAX_OBSTACLE_RAYCAST_TRIES = 10; //max number of raycasts the boid will try to find a path around an obstacle
    private float rayIncrement = 10f; //number to increase/decrease the x/y/z (depending on direction) of each raycast by when trying to find path around obstacle

    private Vector3 currAvoidGoal = Vector3.zero;

    //LAYER MASKS
    private const int LAYER_OBSTACLE = 1 << 10;

    // Use this for initialization
    void Start () {
        Time.timeScale = 1f;
        rb = GetComponent<Rigidbody>();

        minAdaptiveOverlapRadius = overlapSphereRadius;
        maxAdaptiveOverlapRadius = overlapSphereRadius * 10;

        float halfBoundsSize = boundsSize / 2;
        positiveBounds = new Vector3(halfBoundsSize, halfBoundsSize, halfBoundsSize);
        negativeBounds = new Vector3(-halfBoundsSize, -halfBoundsSize, -halfBoundsSize);

        seenBoids = new List<GameObject>();
        seenObstacles = new List<GameObject>();
    }   

    // Update is called once per frame
    void Update()
    {
        if(ControlInputs.Instance.useMouseFollow) mouseTarget = mouseTargetObj.mouseTargetPosition;
        if (currAvoidGoal != Vector3.zero)
        {
            //Debug.DrawLine(transform.position, transform.position + currAvoidGoal, Color.green);
            //Debug.Log(GetInstanceID() + " avoid timer = " + avoidTimer);
        }

        //Debug.DrawLine(this.transform.position, this.transform.position + rb.velocity, Color.red);
    }
    /*
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
        Gizmos.DrawSphere(this.transform.position, overlapSphereRadius);
    }
    */
    void FixedUpdate()
    {
        updateTime -= Time.deltaTime;

        if(updateTime <= 0.0f)
        {
            updateTime = VISION_UPDATE_TIME_INTERVAL;

            //clear seen objects lists before checking again
            seenBoids.Clear();
            //seenObstacles.Clear();

            //find nearby objects
            Collider[] overlappingColliders = Physics.OverlapSphere(rb.transform.position, overlapSphereRadius);

            //add overlapping objects to boids/obstacles lists
            foreach (Collider collider in overlappingColliders)
            {
                if (collider.tag == "Boid" && collider.gameObject != this.gameObject)
                {
                    seenBoids.Add(collider.gameObject);
                }
            }

            //ADAPTIVE OVERLAP SPHERE: if current pass didn't find enough boids, increase overlap sphere size; if it did, reduce it
            if (useAdaptiveOverlapSphere)
            {
                if (seenBoids.Count < numClosestToCheck && overlapSphereRadius < maxAdaptiveOverlapRadius)
                {
                    overlapSphereRadius += adaptiveOverlapSphereInc;
                }
                else if(overlapSphereRadius > minAdaptiveOverlapRadius) 
                {
                    overlapSphereRadius -= adaptiveOverlapSphereInc;
                }
            }

            MoveBoid();
            transform.right = -rb.velocity.normalized; //rotate boid to face movement direction
        }


    }

    void MoveBoid()
    {
        Vector3 vel = LimitVelocity(CalcRules(), velocityLimit);
        rb.AddForce(vel);
        rb.velocity = LimitVelocity(rb.velocity, velocityLimit);
    }

    Vector3 ReturnToBounds()
    {
        Vector3 boundsAvoidVector = Vector3.zero;

        if (ControlInputs.Instance.useBoundingCoordinates)
        {
            //if close to edge of bounding box, move away from the edge
            if (this.transform.position.x > positiveBounds.x)
            {
                boundsAvoidVector.x -= Mathf.Abs(this.transform.position.x - positiveBounds.x);
            }
            else if (this.transform.position.x < negativeBounds.x)
            {
                boundsAvoidVector.x += Mathf.Abs(this.transform.position.x - negativeBounds.x);
            }

            if (this.transform.position.y > positiveBounds.y)
            {
                boundsAvoidVector.y -= Mathf.Abs(this.transform.position.y - positiveBounds.y);
            }
            else if (this.transform.position.y < negativeBounds.y)
            {
                boundsAvoidVector.y += Mathf.Abs(this.transform.position.y - negativeBounds.y);
            }

            if (this.transform.position.z > positiveBounds.z)
            {
                boundsAvoidVector.z -= Mathf.Abs(this.transform.position.z - positiveBounds.z);
            }
            else if (this.transform.position.z < negativeBounds.z)
            {
                boundsAvoidVector.z += Mathf.Abs(this.transform.position.z - negativeBounds.z);
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
                if (Vector3.Distance(this.transform.position, seenBoids[i].transform.position) < boidAvoidDistance)
                {
                    boidAvoidVector += this.transform.position - seenBoids[i].transform.position;
                    Debug.DrawLine(this.transform.position, seenBoids[i].transform.position, Color.cyan);
                }

                centre += seenBoids[i].transform.position - this.transform.position; //move towards centre of nearby boids
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
    //If multiple rays find a path, select one at random - this means the boid will always try all four directions, 
    //but should provide more realistic behaviour as each boid will take a random path around the object, 
    //rather than them all going in the same direction.
    //When a path is found, member variable isAvoidingObstacle is set to true
    Vector3 AvoidObstacles()
    {
        Vector3 avoidVector = Vector3.zero; //return vector
        
        //fire a ray in direction of boid's velocity (sqr magnitude of velocity in length; change this?) to see if there is an obstacle in its path.
        //if there is an obstacle in its path (regardless of whether it is currently avoiding a(nother) obstacle, calculate new avoid vector
        RaycastHit hit;
        if (Physics.Raycast(transform.position, rb.velocity, out hit, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
        {
            //Debug.Log(GetInstanceID() + "obstacle raycast hit");
            currAvoidGoal = Vector3.zero;

            int raycastTries = 0;
            float inc = rayIncrement * rb.velocity.sqrMagnitude; //angle in radians to cast rays at (made negative for left/down casts)

            //find avoid vector which is closest to current velocity vector
            Vector3 closestVector = Vector3.positiveInfinity;
            bool foundAvoidVector = false;
            float minDiff = Mathf.Infinity;
            while (raycastTries <= MAX_OBSTACLE_RAYCAST_TRIES) 
            {
                //up
                Vector3 up = new Vector3(rb.velocity.x, rb.velocity.y + inc, rb.velocity.z - inc);
                
                if (!Physics.Raycast(transform.position, up, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    float upDiff = Vector3.SqrMagnitude(rb.velocity - up);
                    if (upDiff < minDiff)
                    {
                        closestVector = up;
                        minDiff = upDiff;
                        foundAvoidVector = true;
                    }
                }

                //right
                Vector3 right = new Vector3(rb.velocity.x + inc, rb.velocity.y, rb.velocity.z - inc);
                if (!Physics.Raycast(transform.position, right, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    if (Vector3.SqrMagnitude(rb.velocity - right) < minDiff) closestVector = right;
                    foundAvoidVector = true;
                }

                //down
                Vector3 down = new Vector3(rb.velocity.x, rb.velocity.y - inc, rb.velocity.z - inc);
                if (!Physics.Raycast(transform.position, down, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    if (Vector3.SqrMagnitude(rb.velocity - down) < minDiff) closestVector = down;
                    foundAvoidVector = true;
                }

                //left
                Vector3 left = new Vector3(rb.velocity.x - inc, rb.velocity.y, rb.velocity.z - inc);
                if (!Physics.Raycast(transform.position, left, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE)) //if this raycast doesn't hit 
                {
                    if (Vector3.SqrMagnitude(rb.velocity - left) < minDiff) closestVector = left;
                    foundAvoidVector = true;
                }

                //if we found a way/ways around obstacle on this loop, return closestVector
                //if(closestVector != Vector3.positiveInfinity)
                if(foundAvoidVector)
                {
                    //Debug.Log(closestVector);
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
        /*
        else //if raycast didn't hit an obstacle
        {
            
            //Debug.Log(GetInstanceID() + "avoiding obstacle");
            if(avoidTimer > 0) //continue on current avoid vector
            {
                avoidTimer--;
                //Debug.Log("avoid timer = " + avoidTimer);
                avoidVector = currAvoidGoal;
            }
            else //stop avoiding obstacle and reset avoidTimer
            {
                avoidTimer = AVOID_TIMER;
            }
            
            return Vector3.zero;
        }
        */

        return Vector3.zero;
    }

    //Computationally cheaper version of AvoidObstacles which just uses .left/right/up/down/back directions for obstacle avoidance,
    //rather than casting rays to find an avoid vector
    Vector3 AvoidObstaclesCheap()
    {
        Vector3 closestVector = Vector3.zero;

        //checks whether each cardinal direction avoids the obstacle, and returns the one closest to the boid's velocity if so; returns zero if no avoid vector found 
        RaycastHit hit;
        if (Physics.Raycast(transform.position, rb.velocity, out hit, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
        {
            float minDiff = float.PositiveInfinity;

            float upDiff = Vector3.SqrMagnitude(rb.velocity - Vector3.up);
            if (!Physics.Raycast(transform.position, Vector3.up, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
            {
                closestVector = Vector3.up;
                minDiff = upDiff;
            }

            float downDiff = Vector3.SqrMagnitude(rb.velocity - Vector3.down);
            if (downDiff < minDiff && !Physics.Raycast(transform.position, Vector3.down, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
            {
                closestVector = Vector3.down;
                minDiff = downDiff;
            }

            float leftDiff = Vector3.SqrMagnitude(rb.velocity - Vector3.left);
            if (leftDiff < minDiff && !Physics.Raycast(transform.position, Vector3.left, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
            {
                closestVector = Vector3.left;
                minDiff = leftDiff;
            }

            float rightDiff = Vector3.SqrMagnitude(rb.velocity - Vector3.right);
            if (rightDiff < minDiff && !Physics.Raycast(transform.position, Vector3.right, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
            {
                closestVector = Vector3.right;
                minDiff = rightDiff;
            }

            float backDiff = Vector3.SqrMagnitude(rb.velocity - Vector3.back);
            if(backDiff < minDiff && !Physics.Raycast(transform.position, Vector3.right, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
            {
                closestVector = Vector3.back;
            }
        }

        Debug.DrawRay(transform.position, closestVector, Color.green);
        return closestVector * obstacleAvoidMultiplier;
    }
    //causes obstacles to emit a repulsive force on the boid
    /*
    Vector3 ObstacleRepulsion()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, rb.velocity, out hit, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE))
        {
            Debug.DrawRay(transform.position, (transform.position - hit.point) * (100.0f - Vector3.Distance(transform.position, hit.point)));
            return (transform.position - hit.point) * (100.0f - Vector3.Distance(transform.position, hit.point));
        }

        return Vector3.zero;
    }
    */

    Vector3 FollowCursor()
    {
        return (ControlInputs.Instance.useMouseFollow) ? (mouseTarget - transform.position) * mouseFollowMultiplier : Vector3.zero;
    }

    Vector3 MoveToGoal()
    {
        return (ControlInputs.Instance.useRandomGoal) ? (boidCollectiveController.GetGoal() - transform.position) * goalVectorMultiplier : Vector3.zero;
    }

    //calculates and returns a velocity vector based on a priority ordering of the boid's rules
    Vector3 CalcRules()
    {
        Vector3 velVector = Vector3.zero;
        Vector3 avoidVector = AvoidObstacles();

        /**DEBUG**/
        if (CheckInfinity(AvoidObstacles())) Debug.LogWarning("AvoidObstacles() returns NaN");
        if (CheckInfinity(ReactToOtherBoids())) Debug.LogWarning("ReactToOtherBoids() returns NaN");
        if (CheckInfinity(FollowCursor())) Debug.LogWarning("FollowCursor() returns NaN");
        if (CheckInfinity(MoveToGoal())) Debug.LogWarning("MoveToGoal() returns NaN");
        if (CheckInfinity(ReturnToBounds())) Debug.LogWarning("ReturnToBounds() returns NaN");
        /**DEBUG**/

        if(!(avoidVector == Vector3.zero))
        {
            return AvoidObstaclesCheap();
        }
        else
        {
            return ReactToOtherBoids() + FollowCursor() + MoveToGoal() + ReturnToBounds();  
        }
        /** PRIORITIES (MAY CHANGE - DYNAMIC?):
         * Avoid obstacles if close to obstacle > avoid other boids = centre boids = match velocity = follow path = follow random goal = return to bounds */
        //if boid is heading towards an obstacle which is closer than OBSTACLE_CRITICAL_DISTANCE, prioritise avoiding it
        /*
        RaycastHit hit;
        if(isAvoidingObstacle || Physics.Raycast(transform.position, rb.velocity, out hit, OBSTACLE_CHECK_DISTANCE, LAYER_OBSTACLE) 
            && Vector3.Distance(transform.position, hit.point) < OBSTACLE_CRITICAL_DISTANCE)
        {
            velVector = AvoidObstacles() * obstacleAvoidMultiplier;
        }
        else
        {
            velVector = AvoidObstacles() + ReactToOtherBoids() + FollowCursor() + MoveToGoal() + ReturnToBounds();
            //velVector = ReactToOtherBoids() + FollowCursor() + MoveToGoal() + ReturnToBounds();
        }

        return velVector; */

        //return AvoidObstacles() + ReactToOtherBoids() + FollowCursor() + MoveToGoal() + ReturnToBounds();
    }

    //Limit a vector's magnitude to a certain limit if it is over that limit
    Vector3 LimitVelocity(Vector3 velocity, float velocityLimit)
    {
        float velMagnitude = velocity.magnitude;

        if(velMagnitude > velocityLimit)
        {
            return (velocity / velMagnitude) * velocityLimit;
        }
        else
        {
            return velocity;
        }
    }

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
}
