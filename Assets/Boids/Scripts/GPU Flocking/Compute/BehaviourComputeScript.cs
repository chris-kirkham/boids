using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

[RequireComponent(typeof(GPUFlockManager))]
[RequireComponent(typeof(GPUFlockRenderer))]
[RequireComponent(typeof(GPUAffectorManager))]
public class BehaviourComputeScript : MonoBehaviour
{
    private GPUFlockManager flockManager;
    private GPUFlockRenderer flockRenderer;
    private GPUAffectorManager affectorManager;

    public BoidBehaviourParams behaviourParams;
    public MouseTargetPosition mouseTargetPos;
    public RenderTexture idleNoiseTex; //3D noise texture for idle movement

    //compute shaders
    public ComputeShader behaviourCompute;
    public ComputeShader behaviourComputeBatched;

    //params for batch compute
    [Tooltip("If true, divide flock into n batches and compute one batch per frame. This will improve performance considerably" +
        "and, when n is not too high, should not make a noticeable difference to the responsiveness of the flock. NOTE: This should not be changed during gameplay")]
    public bool useBatchedCompute = false;
    
    [Min(1)] public int framesToComputeEntireFlock = 2;
    private int boidsToComputePerFrame;
    private int offset = 0;

    //compute kernel handles
    private int behaviourComputeKernelHandle;
    private int behaviourComputeBatchedKernelHandle;

    //compute group sizes
    private uint nonBatchedGroupSizeX;
    private uint batchedGroupSizeX;

    //dummy compute buffer passed to shader when there are no affectors of a certain type (must be a better way to do this)
    private ComputeBuffer affectorDummy;

    private void Start()
    {
        flockManager = GetComponent<GPUFlockManager>();
        flockRenderer = GetComponent<GPUFlockRenderer>();
        affectorManager = GetComponent<GPUAffectorManager>();

        behaviourComputeKernelHandle = behaviourCompute.FindKernel("CSMain");
        behaviourCompute.GetKernelThreadGroupSizes(behaviourComputeKernelHandle, out nonBatchedGroupSizeX, out _, out _);
        behaviourComputeBatchedKernelHandle = behaviourComputeBatched.FindKernel("CSMain");
        behaviourCompute.GetKernelThreadGroupSizes(behaviourComputeKernelHandle, out batchedGroupSizeX, out _, out _);

        affectorDummy = new ComputeBuffer(1, sizeof(int));

        boidsToComputePerFrame = flockManager.GetFlockSize() / framesToComputeEntireFlock;
    }

    private void Update()
    {
        DoCompute();
        if(useBatchedCompute)
        {
            offset += boidsToComputePerFrame;
            if (offset >= flockManager.GetFlockSize()) offset = 0;
        }
    }

    private void OnDisable()
    {
        if (affectorDummy != null) affectorDummy.Release();
    }

    private void DoCompute()
    {
        /* Select correct compute shader, handle and thread groups for mode; set params exclusive to batched shader */
        ComputeShader compute;
        int kernelHandle;
        int numThreadGroupsX;
        if(useBatchedCompute)
        {
            compute = behaviourComputeBatched;
            kernelHandle = behaviourComputeBatchedKernelHandle;
            numThreadGroupsX = GetNumGroups(flockManager.GetFlockSize(), (int)batchedGroupSizeX);

            BatchedComputeSetBatchParams(compute); //set batching params only for batching shader
        }
        else
        {
            compute = behaviourCompute;
            kernelHandle = behaviourComputeKernelHandle;
            numThreadGroupsX = GetNumGroups(flockManager.GetFlockSize(), (int)nonBatchedGroupSizeX);
        }

        /* Set compute shader data */
        ComputeSetBoidParams(compute, kernelHandle);
        ComputeSetCursorFollow(compute);
        ComputeSetMovementBounds(compute);
        ComputeSetIdleParams(compute);
        ComputeSetAffectors(compute, kernelHandle);
        ComputeSetDeltaTime(compute);
        ComputeSetRendererBuffers(compute, kernelHandle);

        /* Dispatch compute shader */
        compute.Dispatch(kernelHandle, numThreadGroupsX, 1, 1);
    }

    private void ComputeSetBoidParams(ComputeShader compute, int kernelHandle)
    {
        //boid info
        compute.SetBuffer(kernelHandle, "boids", flockManager.GetFlockBuffer());
        compute.SetInt("numBoids", flockManager.GetFlockSize());

        //boid movement params
        compute.SetFloat("moveSpeed", behaviourParams.moveSpeed);
        compute.SetFloat("maxSpeed", behaviourParams.maxSpeed);
        compute.SetFloat("mass", behaviourParams.mass);
        compute.SetFloat("friction", behaviourParams.drag);

        //flocking params
        compute.SetFloat("neighbourDist", behaviourParams.neighbourDistance);
        compute.SetFloat("avoidDist", behaviourParams.avoidDistance);
        compute.SetFloat("avoidSpeed", behaviourParams.avoidSpeed);
        //compute.SetBool("useRandomNeighbourHack", behaviourParams.useRandomNeighbourHack);
    }

    private void ComputeSetCursorFollow(ComputeShader compute)
    {
        compute.SetBool("usingCursorFollow", behaviourParams.useCursorFollow);
        compute.SetFloat("cursorFollowSpeed", behaviourParams.cursorFollowSpeed);
        compute.SetFloat("arrivalSlowStartDist", behaviourParams.arrivalSlowStartDist);
        float[] cursorFollowPos = new float[3] { mouseTargetPos.mouseTargetPosition.x, mouseTargetPos.mouseTargetPosition.y, mouseTargetPos.mouseTargetPosition.z };
        compute.SetFloats("cursorPos", cursorFollowPos);
    }

    private void ComputeSetMovementBounds(ComputeShader compute)
    {
        compute.SetBool("usingBounds", behaviourParams.useBounds);
        compute.SetFloat("boundsSize", behaviourParams.boundsSize);
        compute.SetFloats("boundsCentre", new float[3] { behaviourParams.boundsCentre.x, behaviourParams.boundsCentre.y, behaviourParams.boundsCentre.z });
        compute.SetFloat("boundsReturnSpeed", behaviourParams.boundsReturnSpeed);
    }

    private void ComputeSetIdleParams(ComputeShader compute)
    {
        compute.SetBool("usingIdleMvmt", behaviourParams.useIdleMvmt);
        //compute.SetTexture(behaviourComputerKernelHandle, "idleNoiseTex", )
        compute.SetFloat("idleNoiseFrequency", behaviourParams.idleNoiseFrequency);
        compute.SetFloat("idleOffset", behaviourParams.useTimeOffset ? Time.timeSinceLevelLoad : 0f);
        compute.SetFloat("idleMoveSpeed", behaviourParams.idleSpeed);
    }

    private void ComputeSetAffectors(ComputeShader compute, int kernelHandle)
    {
        int numAffectors = affectorManager.GetNumAffectors();
        compute.SetInt("numAffectors", numAffectors);
        compute.SetBuffer(kernelHandle, "affectors", numAffectors > 0 ? affectorManager.GetAffectorsBuffer() : affectorDummy);
    }

    private void ComputeSetDeltaTime(ComputeShader compute)
    {
        compute.SetFloat("deltaTime", Time.deltaTime);
    }

    private void ComputeSetRendererBuffers(ComputeShader compute, int kernelHandle)
    {
        //boid positions buffer for Graphics.DrawMeshInstancedIndirect
        compute.SetBuffer(kernelHandle, "boidPositions", flockRenderer.GetBoidPositionsBuffer());

        //boid forward and up directions buffer for Graphics.DrawMeshInstancedIndirect
        compute.SetBuffer(kernelHandle, "boidForwardDirs", flockRenderer.GetBoidForwardDirsBuffer());
    }

    private void BatchedComputeSetBatchParams(ComputeShader compute)
    {
        compute.SetInt("boidsToCompute", boidsToComputePerFrame);
        compute.SetInt("boidOffset", offset);
        compute.SetInt("framesToComputeEntireFlock", framesToComputeEntireFlock);
    }

    private int GetNumGroups(int flockSize, int groupSize)
    {
        if(groupSize <= 0)
        {
            throw new System.Exception("Compute shader group size must be > 0.");
        }

        int numGroups = flockSize / (int)groupSize;
        if (flockSize % groupSize != 0) numGroups++; //if flock size is not divisible by group size, add an extra group for stragglers

        return numGroups;
    }
}
