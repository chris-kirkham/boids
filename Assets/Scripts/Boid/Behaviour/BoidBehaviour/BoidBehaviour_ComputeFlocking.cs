using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Version of boid behaviour script which uses a compute shader for flocking and other behaviours that don't require raycasting
/// </summary>
public class BoidBehaviour_ComputeFlocking : BoidBehaviour
{
    public static ComputeShader behaviourCompute;
    private static int behaviourComputerKernelHandle;

    private static List<GameObject> boids;
    private static Boid_Compute[] boidComputeData;

    //struct containing info about a boid. Identical to the Boid struct in the compute shader
    public struct Boid_Compute
    {
        public float3 position, velocity;

        public Boid_Compute(float3 position, float3 velocity)
        {
            this.position = position;
            this.velocity = velocity;
        }
    }
    private const int sizeOfBoid_Compute = sizeof(float) * 6;

    protected override void Start()
    {
        base.Start();
        behaviourComputerKernelHandle = behaviourCompute.FindKernel("CSMain");
    }

    protected override void UpdateBoid()
    {
        throw new System.NotImplementedException();
    }

    protected static void DoCompute(BoidBehaviourParams behaviourParams)
    {
        /* Update boids compute data */
        for (int i = 0; i < boids.Count; i++)
        {
            boidComputeData[i] = new Boid_Compute(boids[i].transform.position, boids[i].GetComponent<BoidMovement>().GetVelocity());
        }

        /* Create a ComputeBuffer with data for existing boids */
        ComputeBuffer buffer = new ComputeBuffer(boids.Count, sizeOfBoid_Compute);
        buffer.SetData(boidComputeData);

        /* Set compute shader data */
        //boid info
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boids", buffer);
        behaviourCompute.SetInt("numBoids", boids.Count);

        //flocking params
        behaviourCompute.SetFloat("avoidDist", behaviourParams.boidAvoidDistance);
        behaviourCompute.SetFloat("avoidSpeed", behaviourParams.boidAvoidSpeed);

        //cursor following
        behaviourCompute.SetBool("usingCursorFollow", behaviourParams.useCursorFollow);
        behaviourCompute.SetFloat("cursorFollowSpeed", behaviourParams.cursorFollowSpeed);

        //movement bounds
        behaviourCompute.SetBool("usingBounds", behaviourParams.useBoundingCoordinates);
        behaviourCompute.SetFloat("boundsSize", behaviourParams.boundsSize);
        behaviourCompute.SetFloats("boundsCentre", new float[3] { 0, 0, 0 }); //TODO: GET BOUNDS CENTRE VALUE
        behaviourCompute.SetFloat("boundsReturnSpeed", behaviourParams.boundsReturnSpeed);

        //idle move
        behaviourCompute.SetBool("usingIdleMvmt", behaviourParams.useIdleMvmt);
        behaviourCompute.SetFloat("idleNoiseFrequency", behaviourParams.idleNoiseFrequency);
        behaviourCompute.SetFloat("idleOffset", behaviourParams.useTimeOffset ? Time.timeSinceLevelLoad : 0f);
        behaviourCompute.SetFloat("idleSpeed", behaviourParams.idleSpeed);

        /* Dispatch compute shader */
        behaviourCompute.Dispatch(behaviourComputerKernelHandle, boids.Count, 1, 1);

        /* Get data from buffer */
        buffer.GetData(boidComputeData);

        buffer.Release();
    }
}
