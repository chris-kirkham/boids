using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

public class BoidBehaviourDOTS : BoidBehaviour
{
    //Job variables (initialised in InitBoidBehaviourJob())
    private JobHandle jobHandle;
    [ReadOnly] public NativeList<Vector3> seenBoidPositions;
    [ReadOnly] public NativeList<Vector3> seenBoidVelocities;
    private int JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH = 100;
    public NativeArray<Vector3> resultDir;

    // Use this for initialization
    protected override void Start()
    {
        InitBoidBehaviourJob();
        base.Start();
    }

    protected override void UpdateBoid()
    {
        if (jobHandle.IsCompleted)
        {
            jobHandle.Complete();
            //boidMovement.MoveBoid(resultDir[0]);
            //transform.right = -resultDir[0]; //rotate boid to face movement direction

            //Update seen boids lists
            seenBoidPositions.Clear();
            seenBoidVelocities.Clear();
            boidVision.UpdateSeenBoids();
            foreach (GameObject boid in boidVision.SeenBoids)
            {
                seenBoidPositions.Add(boid.transform.position);
                seenBoidVelocities.Add(boidMovement.GetVelocity());
            }

            //Initialise and run job
            BoidBehaviourJob job = new BoidBehaviourJob()
            {
                pos = transform.position,
                seenBoidPositions = this.seenBoidPositions,
                seenBoidVelocities = this.seenBoidVelocities,
                boidAvoidSpeed = this.boidAvoidMultiplier,
                boidAvoidDistance = this.boidAvoidDistance,
                useMouseFollow = ControlInputs.Instance.useMouseFollow,
                mouseTarget = this.mouseTarget,
                mouseFollowSpeed = this.mouseFollowMultiplier,
                useBoundingCoordinates = ControlInputs.Instance.useBoundingCoordinates,
                positiveBounds = this.positiveBounds,
                negativeBounds = this.negativeBounds,
                boundsAvoidSpeed = this.boundsAvoidMultiplier,
                resultDir = this.resultDir
            };
            jobHandle = job.Schedule();
        }
    }

    private void Update()
    {
        if (ControlInputs.Instance.useMouseFollow) mouseTarget = mouseTargetObj.mouseTargetPosition;

        //updateTime -= Time.deltaTime;
        idleTimer -= Time.deltaTime;

        //calculate boid behaviour
        if (updateTime <= 0.0f)
        {
            if (jobHandle.IsCompleted)
            {
                jobHandle.Complete();
                //boidMovement.MoveBoid(resultDir[0]);
                //transform.right = -resultDir[0]; //rotate boid to face movement direction

                //Update seen boids lists
                seenBoidPositions.Clear();
                seenBoidVelocities.Clear();
                boidVision.UpdateSeenBoids();
                foreach (GameObject boid in boidVision.SeenBoids)
                {
                    seenBoidPositions.Add(boid.transform.position);
                    seenBoidVelocities.Add(boidMovement.GetVelocity());
                }

                //Initialise and run job
                BoidBehaviourJob job = new BoidBehaviourJob()
                {
                    pos = transform.position,
                    seenBoidPositions = this.seenBoidPositions,
                    seenBoidVelocities = this.seenBoidVelocities,
                    boidAvoidSpeed = this.boidAvoidMultiplier,
                    boidAvoidDistance = this.boidAvoidDistance,
                    useMouseFollow = ControlInputs.Instance.useMouseFollow,
                    mouseTarget = this.mouseTarget,
                    mouseFollowSpeed = this.mouseFollowMultiplier,
                    useBoundingCoordinates = ControlInputs.Instance.useBoundingCoordinates,
                    positiveBounds = this.positiveBounds,
                    negativeBounds = this.negativeBounds,
                    boundsAvoidSpeed = this.boundsAvoidMultiplier,
                    resultDir = this.resultDir
                };
                jobHandle = job.Schedule();
            }
        }

        boidMovement.MoveBoid(resultDir[0]);
        transform.right = -resultDir[0]; //rotate boid to face movement direction
    }

    private void OnDestroy()
    {
        jobHandle.Complete(); //need to complete the job in order to deallocate the native containers safely
        seenBoidPositions.Dispose();
        seenBoidVelocities.Dispose();
        resultDir.Dispose();
    }

    [BurstCompile]
    private Vector3 ObstacleRepulsionJob(Vector3 pos)
    {
        Vector3 repulseDirection = Vector3.zero;
            
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(1, Allocator.Temp);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(1, Allocator.Temp);

        if (Physics.Raycast(transform.position, boidMovement.GetVelocity(), out RaycastHit hit, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulseDirection = hit.normal;

        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 1);
        handle.Complete();

        results.Dispose();
        commands.Dispose();

        return repulseDirection;
    }

    [BurstCompile]
    public struct BoidBehaviourJob : IJob
    {
        //boid info
        public Vector3 pos;

        //other boid info
        [ReadOnly] public NativeArray<Vector3> seenBoidPositions;
        [ReadOnly] public NativeArray<Vector3> seenBoidVelocities;
        public float boidAvoidSpeed;
        public float boidAvoidDistance;

        //mouse following
        public bool useMouseFollow;
        public Vector3 mouseTarget;
        public float mouseFollowSpeed;

        //bounds checking
        public bool useBoundingCoordinates;
        public Vector3 positiveBounds, negativeBounds;
        public float boundsAvoidSpeed;

        //result direction vector (only one vector)
        public NativeArray<Vector3> resultDir;

        public void Execute()
        {
            //resultDir[0] = ReactToOtherBoids() + FollowCursor() + ReturnToBounds() + ObstacleRepulsion();
            resultDir[0] = ReactToOtherBoids() + FollowCursor() + ReturnToBounds();
        }

        //returns the boid's velocity vector after reacting to other boids in the environment (avoiding other boids, moving to centre of other boids,
        //matching velocity with other boids
        Vector3 ReactToOtherBoids()
        {
            float numSeenBoids = seenBoidPositions.Length;
            if (numSeenBoids == 0) return Vector3.zero;

            Vector3 boidAvoidDir = Vector3.zero;
            Vector3 centre = Vector3.zero;
            Vector3 velocityMatch = Vector3.zero;

            for (int i = 0; i < numSeenBoids; i++)
            {
                //avoid other boids
                if (Vector3.Distance(pos, seenBoidPositions[i]) < boidAvoidDistance)
                {
                    boidAvoidDir += pos - seenBoidPositions[i];
                }

                centre += seenBoidPositions[i] - pos; //move towards centre of nearby boids
                velocityMatch += seenBoidVelocities[i]; //match velocity with nearby boids
            }

            centre /= numSeenBoids;
            velocityMatch /= numSeenBoids;
        
            return (boidAvoidDir * boidAvoidSpeed) + centre + velocityMatch;
        }

        Vector3 FollowCursor()
        {
            return useMouseFollow ? (mouseTarget - pos) * mouseFollowSpeed : Vector3.zero;
        }

        Vector3 ReturnToBounds()
        {
            Vector3 boundsAvoidVector = Vector3.zero;

            //if close to edge of bounding box, move away from the edge
            if (useBoundingCoordinates)
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

        
    }

    private void InitBoidBehaviourJob()
    {
        seenBoidPositions = new NativeList<Vector3>(JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH, Allocator.Persistent);
        seenBoidVelocities = new NativeList<Vector3>(JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH, Allocator.Persistent);
        resultDir = new NativeArray<Vector3>(1, Allocator.Persistent);
    }
}