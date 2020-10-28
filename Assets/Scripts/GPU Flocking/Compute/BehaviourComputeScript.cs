using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

[RequireComponent(typeof(GPUFlockManager))]
[RequireComponent(typeof(GPUFlockRenderer))]
public class BehaviourComputeScript : MonoBehaviour
{
    private GPUFlockManager flockManager;
    private GPUFlockRenderer flockRenderer;

    public BoidBehaviourParams behaviourParams;
    public MouseTargetPosition mouseTargetPos;
    public RenderTexture idleNoiseTex; //3D noise texture for idle movement

    public ComputeShader behaviourCompute;
    private int behaviourComputerKernelHandle;
    private uint groupSizeX;
    private const int GROUP_SIZE = 64;
    private ComputeBuffer flockBuffer; //stores flock boid structs 
    private ComputeBuffer boidPositionsBuffer; //stores only the positions of the boids in the flock; for passing to GPUFlockRenderer
    //private ComputeBuffer boidForwardDirectionsBuffer; //stores the forward vectors of the boids in the flock; for passing to GPUFlockRenderer
    //private ComputeBuffer boidUpDirectionsBuffer; //stores the up vectors of the boids in the flock; for passing to GPUFlockRenderer

    private void Start()
    {
        flockManager = GetComponent<GPUFlockManager>();
        flockRenderer = GetComponent<GPUFlockRenderer>();

        behaviourComputerKernelHandle = behaviourCompute.FindKernel("CSMain");
        behaviourCompute.GetKernelThreadGroupSizes(behaviourComputerKernelHandle, out groupSizeX, out uint dummyY, out uint dummyZ);
    }

    private void Update()
    {
        DoCompute();
    }

    private void OnDisable()
    {
        if (flockBuffer != null) flockBuffer.Release();
        if (boidPositionsBuffer != null) boidPositionsBuffer.Release();
        //if (boidForwardDirectionsBuffer != null) boidForwardDirectionsBuffer.Release();
        //if (boidUpDirectionsBuffer != null) boidUpDirectionsBuffer.Release();
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
        behaviourCompute.SetBool("usingBounds", behaviourParams.useBoundingCoordinates);
        behaviourCompute.SetFloat("boundsSize", behaviourParams.boundsSize);
        behaviourCompute.SetFloats("boundsCentre", new float[3] { behaviourParams.boundsCentre.x, behaviourParams.boundsCentre.y, behaviourParams.boundsCentre.z });
        behaviourCompute.SetFloat("boundsReturnSpeed", behaviourParams.boundsReturnSpeed);

        //idle move
        behaviourCompute.SetBool("usingIdleMvmt", behaviourParams.useIdleMvmt);
        //behaviourCompute.SetTexture(behaviourComputerKernelHandle, "idleNoiseTex", )
        behaviourCompute.SetFloat("idleNoiseFrequency", behaviourParams.idleNoiseFrequency);
        behaviourCompute.SetFloat("idleOffset", behaviourParams.useTimeOffset ? Time.timeSinceLevelLoad : 0f);
        behaviourCompute.SetFloat("idleMoveSpeed", behaviourParams.idleSpeed);

        //delta time for calculating new positions
        behaviourCompute.SetFloat("deltaTime", Time.deltaTime);

        //boid positions buffer for Graphics.DrawMeshInstancedIndirect
        if (boidPositionsBuffer != null) boidPositionsBuffer.Release();
        boidPositionsBuffer = new ComputeBuffer(flockSize, sizeof(float) * 4);
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boidPositions", flockRenderer.GetBoidPositionsBuffer());

        //boid forward and up directions buffer for Graphics.DrawMeshInstancedIndirect
        /*
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boidPositions", flockRenderer.GetBoidForwardDirectionsBuffer());
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boidPositions", boidUpDirectionsBuffer);
        */

        /* Get number of threads */
        int numGroupsX = (flockSize / (int)groupSizeX);
        if ((flockSize & (flockSize - 1)) != 0) numGroupsX++; //if flock size isn't a power of 2, add another group to catch extras (this assumes compute's group size is a power of 2)

        /* Dispatch compute shader */
        behaviourCompute.Dispatch(behaviourComputerKernelHandle, numGroupsX, 1, 1);
    }


}
