using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

[RequireComponent(typeof(GPUFlockManager))]
[RequireComponent(typeof(GPUFlockRenderer))]
public class BehaviourComputeScript_Staggered : MonoBehaviour
{
    private GPUFlockManager flockManager;
    private GPUFlockRenderer flockRenderer;

    public BoidBehaviourParams behaviourParams;
    public MouseTargetPosition mouseTargetPos;
    public RenderTexture idleNoiseTex; //3D noise texture for idle movement

    public ComputeShader behaviourCompute;
    private int behaviourComputerKernelHandle;
    private uint groupSizeX;

    //stagger params
    [Min(1)] public int framesToComputeEntireFlock = 2;
    private int boidsToComputePerFrame;
    private int offset = 0;


    private void Start()
    {
        flockManager = GetComponent<GPUFlockManager>();
        flockRenderer = GetComponent<GPUFlockRenderer>();

        behaviourComputerKernelHandle = behaviourCompute.FindKernel("CSMain");
        behaviourCompute.GetKernelThreadGroupSizes(behaviourComputerKernelHandle, out groupSizeX, out uint dummyY, out uint dummyZ);

        boidsToComputePerFrame = flockManager.GetFlockSize() / framesToComputeEntireFlock;
    }

    private void Update()
    {
        DoCompute();
        offset += boidsToComputePerFrame;
        if (offset >= flockManager.GetFlockSize()) offset = 0;
    }

    private void DoCompute()
    {
        int flockSize = flockManager.GetFlockSize();

        /* Set compute shader data */
        //offset
        behaviourCompute.SetInt("boidsToCompute", boidsToComputePerFrame);
        behaviourCompute.SetInt("boidOffset", offset);
        behaviourCompute.SetInt("framesToComputeEntireFlock", framesToComputeEntireFlock);

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
