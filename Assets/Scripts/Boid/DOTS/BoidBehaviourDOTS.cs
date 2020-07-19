using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(BoidVision))]
[RequireComponent(typeof(BoidMovement_Rigidbody))]
[RequireComponent(typeof(Rigidbody))]
public class BoidBehaviourDOTS : MonoBehaviour
{
    private BoidVision boidVision;
    private BoidMovement_Rigidbody boidMovement;
    public BoidCollectiveController boidCollectiveController;

    Rigidbody rb;

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

    //layer masks
    private const int LAYER_BOID = 1 << 9;
    private const int LAYER_OBSTACLE = 1 << 10;

    /*----------------*/
    //Job variables (initialised in InitBoidBehaviourJob())
    [ReadOnly] public NativeList<Vector3> seenBoidPositions;
    [ReadOnly] public NativeList<Vector3> seenBoidVelocities;
    private int JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH = 100;
    public NativeArray<Vector3> resultDir;

    // Use this for initialization
    void Start()
    {
        boidVision = GetComponent<BoidVision>();
        boidMovement = GetComponent<BoidMovement_Rigidbody>();
        rb = GetComponent<Rigidbody>();

        //set initial boid velocity

        float halfBoundsSize = boundsSize / 2;
        positiveBounds = new Vector3(halfBoundsSize, halfBoundsSize, halfBoundsSize);
        negativeBounds = new Vector3(-halfBoundsSize, -halfBoundsSize, -halfBoundsSize);

        InitBoidBehaviourJob();
    }

    void FixedUpdate()
    {
        updateTime -= Time.deltaTime;
        idleTimer -= Time.deltaTime;


        //calculate boid behaviour
        if (updateTime <= 0.0f)
        {
            updateTime = BASE_UPDATE_TIME_INTERVAL + Random.Range(0.0f, UPDATE_TIME_VARIANCE);

            //Update seen boids lists
            seenBoidPositions.Clear();
            seenBoidVelocities.Clear();
            boidVision.UpdateSeenBoids();
            foreach(GameObject boid in boidVision.SeenBoids)
            {
                seenBoidPositions.Add(boid.transform.position);
                seenBoidVelocities.Add(boid.GetComponent<Rigidbody>().velocity);
            }
            
            //Initialise and run job
            BoidBehaviourJob job = new BoidBehaviourJob()
            {
                pos = transform.position,
                forward = transform.forward,
                right = transform.right,
                up = transform.up,
                seenBoidPositions = this.seenBoidPositions,
                seenBoidVelocities = this.seenBoidVelocities,
                boidAvoidSpeed = this.boidAvoidMultiplier,
                boidAvoidDistance = this.boidAvoidDistance,
                mouseTarget = this.mouseTarget,
                mouseFollowSpeed = this.mouseFollowMultiplier,
                positiveBounds = this.positiveBounds,
                negativeBounds = this.negativeBounds,
                boundsAvoidSpeed = this.boundsAvoidMultiplier,
                resultDir = this.resultDir
            };
            JobHandle jobHandle = job.Schedule();
            jobHandle.Complete();

            //Update boid movement with job result (move direction)
            //Debug.Log(resultDir[0]);
            boidMovement.MoveBoid(resultDir[0]);
            transform.right = -rb.velocity.normalized; //rotate boid to face movement direction
        }
    }
    private void Update()
    {
        if (ControlInputs.Instance.useMouseFollow) mouseTarget = mouseTargetObj.mouseTargetPosition;
    }

    private void OnDestroy()
    {
        seenBoidPositions.Dispose();
        seenBoidVelocities.Dispose();
    }

    public struct BoidBehaviourJob : IJob
    {
        //boid info
        public Vector3 pos;
        public Vector3 forward, right, up;

        //other boid info
        [ReadOnly] public NativeArray<Vector3> seenBoidPositions;
        [ReadOnly] public NativeArray<Vector3> seenBoidVelocities;
        public float boidAvoidSpeed;
        public float boidAvoidDistance;

        //mouse following
        public Vector3 mouseTarget;
        public float mouseFollowSpeed;

        //bounds checking
        public Vector3 positiveBounds, negativeBounds;
        public float boundsAvoidSpeed;

        //result direction vector (only one vector)
        public NativeArray<Vector3> resultDir;

        public void Execute()
        {
            //resultDir[0] = ReactToOtherBoids() + FollowCursor() + ReturnToBounds() + ObstacleRepulsion();
            //resultDir[0] = ReactToOtherBoids() + FollowCursor() + ReturnToBounds();
            resultDir[0] = FollowCursor() + ReturnToBounds();
        }

        //returns the boid's velocity vector after reacting to other boids in the environment (avoiding other boids, moving to centre of other boids,
        //matching velocity with other boids
        //(this is all in one function so we don't have to loop through seen boids more than once)
        Vector3 ReactToOtherBoids()
        {
            float numSeenBoids = seenBoidPositions.Length;
            
            Vector3 boidAvoidDir = Vector3.zero;
            Vector3 centre = Vector3.zero;
            Vector3 velocityMatch = Vector3.zero;

            for (int i = 0; i < numSeenBoids; i++)
            {
                //avoid other boids
                if (Vector3.Distance(pos, seenBoidPositions[i]) < boidAvoidDistance)
                {
                    boidAvoidDir += pos - seenBoidPositions[i];
                    Debug.DrawLine(pos, seenBoidPositions[i], Color.cyan);
                }

                centre += seenBoidPositions[i] - pos; //move towards centre of nearby boids
                velocityMatch += seenBoidVelocities[i]; //match velocity with nearby boids
            }

            centre = centre / numSeenBoids;
            velocityMatch = velocityMatch / numSeenBoids;

            return (boidAvoidDir * boidAvoidSpeed) + centre + velocityMatch;
        }

        Vector3 FollowCursor()
        {
            return (ControlInputs.Instance.useMouseFollow) ? (mouseTarget - pos) * mouseFollowSpeed : Vector3.zero;
        }

        Vector3 ReturnToBounds()
        {
            Vector3 boundsAvoidVector = Vector3.zero;

            //if close to edge of bounding box, move away from the edge
            if (ControlInputs.Instance.useBoundingCoordinates)
            {
                if (pos.x > positiveBounds.x)
                {
                    boundsAvoidVector.x -= Mathf.Abs(pos.x - positiveBounds.x);
                }
                else if (pos.x < negativeBounds.x)
                {
                    boundsAvoidVector.x += Mathf.Abs(pos.x - negativeBounds.x);
                }

                if (pos.y > positiveBounds.y)
                {
                    boundsAvoidVector.y -= Mathf.Abs(pos.y - positiveBounds.y);
                }
                else if (pos.y < negativeBounds.y)
                {
                    boundsAvoidVector.y += Mathf.Abs(pos.y - negativeBounds.y);
                }

                if (pos.z > positiveBounds.z)
                {
                    boundsAvoidVector.z -= Mathf.Abs(pos.z - positiveBounds.z);
                }
                else if (pos.z < negativeBounds.z)
                {
                    boundsAvoidVector.z += Mathf.Abs(pos.z - negativeBounds.z);
                }

                boundsAvoidVector *= boundsAvoidSpeed;
            }

            return boundsAvoidVector;
        }

        private Vector3 ObstacleRepulsion()
        {
            Vector3 repulseDirection = Vector3.zero;
            
            NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(6, Allocator.Temp);
            NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(6, Allocator.Temp);

            //check cardinal directions for close objects
            commands[0] = new RaycastCommand(pos, forward, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE);
            commands[1] = new RaycastCommand(pos, -forward, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE);
            commands[2] = new RaycastCommand(pos, -right, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE);
            commands[3] = new RaycastCommand(pos, right, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE);
            commands[4] = new RaycastCommand(pos, up, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE);
            commands[5] = new RaycastCommand(pos, -up, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE);

            //TODO: CAN ONLY CALL JOBS FROM THE MAIN THREAD
            JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1);
            handle.Complete();

            for(int i = 0; i < results.Length; i++)
            {
                //if collider is null, there was no hit
                if (results[i].collider != null) repulseDirection -= commands[i].direction; 
            }

            results.Dispose();
            commands.Dispose();

            return repulseDirection;
        }
    }

    private void InitBoidBehaviourJob()
    {
        seenBoidPositions = new NativeList<Vector3>(JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH, Allocator.Persistent);
        seenBoidVelocities = new NativeList<Vector3>(JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH, Allocator.Persistent);
        resultDir = new NativeArray<Vector3>(1, Allocator.Persistent);
    }
}