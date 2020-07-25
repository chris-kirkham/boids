using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

public class BoidBehaviourDOTS : BoidBehaviour
{
    //Job variables (initialised in InitBoidBehaviourJob())
    private JobHandle jobHandle;
    public NativeList<float3> seenBoidPositions;
    public NativeList<float3> seenBoidVelocities;
    private int JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH = 100;
    public NativeArray<float3> resultDir;

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
                boidAvoidSpeed = this.boidAvoidSpeed,
                sqrBoidAvoidDistance = this.sqrBoidAvoidDistance,
                useMouseFollow = ControlInputs.Instance.useMouseFollow,
                mouseTarget = this.mouseTarget,
                mouseFollowSpeed = this.cursorFollowSpeed,
                useBoundingCoordinates = ControlInputs.Instance.useBoundingCoordinates,
                positiveBounds = this.positiveBounds,
                negativeBounds = this.negativeBounds,
                boundsAvoidSpeed = this.boundsReturnSpeed,
                idleNoiseFrequency = this.idleNoiseFrequency,
                offset = useTimeOffset ? Time.timeSinceLevelLoad : 0,
                resultDir = this.resultDir
            };
            jobHandle = job.Schedule();
        }
    }

    private void FixedUpdate()
    {
        boidMovement.MoveBoid(resultDir[0]);
    }

    private void Update()
    {
        if (ControlInputs.Instance.useMouseFollow) mouseTarget = mouseTargetObj.mouseTargetPosition;
    }

    private void OnDestroy()
    {
        jobHandle.Complete(); //need to complete the job in order to deallocate the native containers safely
        seenBoidPositions.Dispose();
        seenBoidVelocities.Dispose();
        resultDir.Dispose();
    }

    [BurstCompile]
    public struct BoidBehaviourJob : IJob
    {
        //boid info
        public float3 pos;

        //other boid info
        [ReadOnly] public NativeList<float3> seenBoidPositions;
        [ReadOnly] public NativeList<float3> seenBoidVelocities;
        public float boidAvoidSpeed;
        public float sqrBoidAvoidDistance;

        //mouse following
        public bool useMouseFollow;
        public float3 mouseTarget;
        public float mouseFollowSpeed;

        //bounds checking
        public bool useBoundingCoordinates;
        public float3 positiveBounds, negativeBounds;
        public float boundsAvoidSpeed;

        //idle movement
        public float idleNoiseFrequency;
        public float offset;

        //result direction vector (only one vector)
        public NativeArray<float3> resultDir;

        public void Execute()
        {
            //resultDir[0] = ReactToOtherBoids() + FollowCursor() + ReturnToBounds() + ObstacleRepulsion();
            resultDir[0] = ReactToOtherBoids() + FollowCursor() + ReturnToBounds() + MoveIdle();
        }

        //returns the boid's velocity vector after reacting to other boids in the environment (avoiding other boids, moving to centre of other boids,
        //matching velocity with other boids
        float3 ReactToOtherBoids()
        {
            float numSeenBoids = seenBoidPositions.Length;
            if (numSeenBoids == 0) return float3.zero;

            float3 boidAvoidDir = float3.zero;
            float3 centre = float3.zero;
            float3 velocityMatch = float3.zero;

            for (int i = 0; i < numSeenBoids; i++)
            {
                //avoid other boids
                if (Vector3.SqrMagnitude(pos - seenBoidPositions[i]) < sqrBoidAvoidDistance)
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

        float3 FollowCursor()
        {
            return useMouseFollow ? (float3)Vector3.Normalize(mouseTarget - pos) * mouseFollowSpeed : float3.zero;
        }

        float3 ReturnToBounds()
        {
            float3 boundsAvoidVector = float3.zero;

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

        float3 MoveIdle()
        {
            return DirectionalPerlin.Directional3D(pos, idleNoiseFrequency, offset);
        }
    }

    [BurstCompile]
    private struct ObstacleRepulsionJob : IJob
    {
        NativeArray<float3> repulseDirection;
        
        NativeArray<RaycastCommand> rayCommands;
        NativeArray<RaycastHit> rayResults;

        public void Execute()
        {

        }

        //if (Physics.Raycast(transform.position, boidMovement.GetVelocity(), out RaycastHit hit, OBSTACLE_CRITICAL_DISTANCE, LAYER_OBSTACLE)) repulseDirection = hit.normal;
    }

    private void InitBoidBehaviourJob()
    {
        seenBoidPositions = new NativeList<float3>(JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH, Allocator.Persistent);
        seenBoidVelocities = new NativeList<float3>(JOB_MAX_SEEN_BOIDS_ARRAYS_LENGTH, Allocator.Persistent);
        resultDir = new NativeArray<float3>(1, Allocator.Persistent);
    }
}