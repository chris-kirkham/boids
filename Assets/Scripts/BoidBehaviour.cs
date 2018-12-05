using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidBehaviour : MonoBehaviour {

    public float avoidDistance; //distance from other boids/objects at which this boid tries to stay
    public float obstacleAvoidDistance;
    public float overlapSphereRadius;

    public float velocityLimit = 10.0f;
    public int numClosestToCheck = 5;

    public const float VISION_UPDATE_TIME_INTERVAL = 0.05f;
    private float updateTime = 0.0f;

    public bool useRandomGoal;

    public bool useAdaptiveOverlapSphere;

    //coordinates to constrain boid movement
    public bool useBoundingCoordinates = true;
    public Vector3 positiveBounds, negativeBounds;
    public const float OUT_OF_BOUNDS_VELOCITY = 10.0f; //velocity with which to return to bounding area if out of bounds (added to other velocities so will be capped after)

    Rigidbody rb;
    //CapsuleCollider visionTrigger; //trigger which tells us what the boid can see
    List<GameObject> seenBoids; //list in which to store other boids seen by this boid at each timestep
    List<GameObject> seenObstacles; //list in which to store environment obstacles seen by this boid at each timestep

    public BoidCollectiveController boidCollectiveController;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
        //visionTrigger = GetComponent<CapsuleCollider>();
        seenBoids = new List<GameObject>();
        seenObstacles = new List<GameObject>();
    }   

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(this.transform.position, this.transform.position + rb.velocity, Color.red);
        Debug.Log(rb.velocity.magnitude);
    }

    void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(this.transform.position, overlapSphereRadius);
    }

    void FixedUpdate()
    {
        updateTime -= Time.deltaTime;

        if(updateTime <= 0.0f)
        {
            updateTime = VISION_UPDATE_TIME_INTERVAL;

            //clear seen objects lists before checking again
            seenBoids.Clear();
            seenObstacles.Clear();

            //find nearby objects
            Collider[] overlappingColliders = Physics.OverlapSphere(rb.transform.position, overlapSphereRadius);

            //add overlapping objects to boids/obstacles lists
            foreach (Collider collider in overlappingColliders)
            {
                if (collider.tag == "Boid" && collider.gameObject != this.gameObject)
                {
                    seenBoids.Add(collider.gameObject);
                }
                else
                {
                    seenObstacles.Add(collider.gameObject);
                }
            }

            MoveBoid();
            this.transform.right = -rb.velocity.normalized;
        }

    }

    void MoveBoid() {
        Vector3 vel = limitVelocity(CalcRules(), velocityLimit);
        //Debug.Log("SeenBoids size = " + seenBoids.Count);
        rb.AddForce(vel);
        //rb.velocity = limitVelocity(rb.velocity, velocityLimit); //cap velocity at specified magnitude
    }

    Vector3 CalcRules()
    {
        Vector3 boundsAvoidVector = new Vector3();
        Vector3 boidAvoidVector = new Vector3();
        Vector3 obstacleAvoidVector = new Vector3();
        Vector3 centre = new Vector3();
        Vector3 velocityMatch = new Vector3();
        Vector3 goalVector = new Vector3();

        /** calculate return-to-bounds vector */
        if (useBoundingCoordinates)
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
        }

        /** update goal vector */
        if (useRandomGoal) { goalVector = boidCollectiveController.GetGoal(); }

        /** calculate obstacle avoidance vector */
        if(seenObstacles.Count > 0)
        {
            foreach (GameObject obstacle in seenObstacles)
            {
                //avoid obstacles
                if (Vector3.Distance(this.transform.position, obstacle.transform.position) < obstacleAvoidDistance)
                {
                    obstacleAvoidVector += this.transform.position - obstacle.transform.position;
                    Debug.DrawLine(this.transform.position, obstacle.transform.position, Color.blue);
                }
            }
        }

        /** update and return vectors requiring knowledge of other boids (boid avoidance, local centre, velocity matching),
         *  otherwise return sum of other vectors */
        if (seenBoids.Count > 0)
        {
            //check if NUM_CLOSEST_TO_CHECK > number of visible boids
            int numToCheck = Mathf.Min(numClosestToCheck, seenBoids.Count);

            for(int i = 0; i < numToCheck; i++)
            {
                //avoid other boids
                if (Vector3.Distance(this.transform.position, seenBoids[i].transform.position) < avoidDistance)
                {
                    boidAvoidVector += this.transform.position - seenBoids[i].transform.position;
                    Debug.DrawLine(this.transform.position, seenBoids[i].transform.position, Color.cyan);
                }

                centre += seenBoids[i].transform.position - this.transform.position; //move towards centre of nearby boids
                velocityMatch += seenBoids[i].GetComponent<Rigidbody>().velocity; //match velocity with nearby boids
            }

            centre = centre / seenBoids.Count;
            velocityMatch = velocityMatch / seenBoids.Count;

            //Debug.Log("bounds avoid = " + boundsAvoidVector + ", avoid = " + avoidVector + ", centre mass = " + centre + ", match velocities = " + velocityMatch);
            return boundsAvoidVector + boidAvoidVector + centre + velocityMatch + goalVector;
        }
        else
        {
            //if no boids nearby, do some roaming/exploring behaviour?
            return boundsAvoidVector + obstacleAvoidVector + goalVector;
        }

    }

    //Limit a vector's magnitude to a certain limit if it is over that limit
    Vector3 limitVelocity(Vector3 velocity, float velocityLimit)
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

    //Set a vector's magnitude to a specified number
    Vector3 setMagnitude(Vector3 vector, float magnitude)
    {
        return (vector / vector.magnitude) * magnitude;
    }
}
