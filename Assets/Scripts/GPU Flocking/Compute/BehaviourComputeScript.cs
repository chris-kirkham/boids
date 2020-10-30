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

    public ComputeShader behaviourCompute;
    private int behaviourComputerKernelHandle;
    private uint groupSizeX;

    private void Start()
    {
        flockManager = GetComponent<GPUFlockManager>();
        flockRenderer = GetComponent<GPUFlockRenderer>();
        affectorManager = GetComponent<GPUAffectorManager>();

        behaviourComputerKernelHandle = behaviourCompute.FindKernel("CSMain");
        behaviourCompute.GetKernelThreadGroupSizes(behaviourComputerKernelHandle, out groupSizeX, out uint dummyY, out uint dummyZ);
    }

    private void Update()
    {
        DoCompute();
    }

    private void DoCompute()
    {
        int flockSize = flockManager.GetFlockSize();
        
        /* Set compute shader data */
        //boid info
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boids", flockManager.GetFlockBuffer());
        behaviourCompute.SetInt("numBoids", flockSize);

        //boid movement params
        behaviourCompute.SetFloat("moveSpeed", behaviourParams.moveSpeed);
        behaviourCompute.SetFloat("mass", behaviourParams.mass);
        behaviourCompute.SetFloat("friction", behaviourParams.friction);

        //flocking params
        behaviourCompute.SetFloat("neighbourDist", behaviourParams.neighbourDistance);
        behaviourCompute.SetFloat("avoidDist", behaviourParams.avoidDistance);
        behaviourCompute.SetFloat("avoidSpeed", behaviourParams.avoidSpeed);

        //cursor following
        behaviourCompute.SetBool("usingCursorFollow", behaviourParams.useCursorFollow);
        behaviourCompute.SetFloat("cursorFollowSpeed", behaviourParams.cursorFollowSpeed);
        behaviourCompute.SetFloat("arrivalSlowStartDist", behaviourParams.arrivalSlowStartDist);
        float[] cursorFollowPos = new float[3] { mouseTargetPos.mouseTargetPosition.x, mouseTargetPos.mouseTargetPosition.y, mouseTargetPos.mouseTargetPosition.z };
        behaviourCompute.SetFloats("cursorPos", cursorFollowPos);

        //movement bounds
        /*
        behaviourCompute.SetBool("usingBounds", behaviourParams.useBoundingCoordinates);
        behaviourCompute.SetFloat("boundsSize", behaviourParams.boundsSize);
        behaviourCompute.SetFloats("boundsCentre", new float[3] { behaviourParams.boundsCentre.x, behaviourParams.boundsCentre.y, behaviourParams.boundsCentre.z });
        behaviourCompute.SetFloat("boundsReturnSpeed", behaviourParams.boundsReturnSpeed);
        */

        //idle move
        behaviourCompute.SetBool("usingIdleMvmt", behaviourParams.useIdleMvmt);
        //behaviourCompute.SetTexture(behaviourComputerKernelHandle, "idleNoiseTex", )
        behaviourCompute.SetFloat("idleNoiseFrequency", behaviourParams.idleNoiseFrequency);
        behaviourCompute.SetFloat("idleOffset", behaviourParams.useTimeOffset ? Time.timeSinceLevelLoad : 0f);
        behaviourCompute.SetFloat("idleMoveSpeed", behaviourParams.idleSpeed);

        //delta time for calculating new positions
        behaviourCompute.SetFloat("deltaTime", Time.deltaTime);

        //affectors buffers
        ComputeBuffer affectorDummy = new ComputeBuffer(1, sizeof(int)); //dummy compute buffer for if there are no affectors of that type (must be a better way to do this)

        int numAttractors = affectorManager.GetNumAttractors();
        behaviourCompute.SetInt("numAttractors", numAttractors);
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "attractors", numAttractors > 0 ? affectorManager.GetAttractorsBuffer() : affectorDummy);

        int numRepulsors = affectorManager.GetNumRepulsors();
        behaviourCompute.SetInt("numRepulsors", numRepulsors);
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "repulsors", numRepulsors > 0 ? affectorManager.GetRepulsorsBuffer() : affectorDummy);

        int numPushers = affectorManager.GetNumPushers();
        behaviourCompute.SetInt("numPushers", numPushers);
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "pushers", numPushers > 0 ? affectorManager.GetPushersBuffer() : affectorDummy);

        //boid positions buffer for Graphics.DrawMeshInstancedIndirect
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boidPositions", flockRenderer.GetBoidPositionsBuffer());

        //boid forward and up directions buffer for Graphics.DrawMeshInstancedIndirect
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boidForwardDirs", flockRenderer.GetBoidForwardDirsBuffer());

        /* Get number of threads */
        int numGroupsX = (flockSize / (int)groupSizeX);
        if (flockSize % groupSizeX != 0) numGroupsX++; //if flock size isn't divisible by groupSizeX, add an extra group for the stragglers

        /* Dispatch compute shader */
        behaviourCompute.Dispatch(behaviourComputerKernelHandle, numGroupsX, 1, 1);
    }


}
