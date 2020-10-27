using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;


public class BehaviourComputeScript : MonoBehaviour
{
    public ComputeShader behaviourCompute;
    private int behaviourComputerKernelHandle;
    private const int GROUP_SIZE = 64;
    
    public BoidBehaviourParams behaviourParams;
    public MouseTargetPosition mouseTargetPos;
    public GPUFlockManager flockManager;
    public GPUFlockRenderer flockRenderer;
    public RenderTexture idleNoiseTex; //3D noise texture for idle movement

    private ComputeBuffer flockBuffer; //stores flock boid structs 
    private ComputeBuffer boidPositionsBuffer; //stores only the positions of the boids in the flock; for passing to GPUFlockRenderer

    private void Start()
    {
        behaviourComputerKernelHandle = behaviourCompute.FindKernel("CSMain");
    }

    private void Update()
    {
        DoCompute();
    }

    private void DoCompute()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        /* Get flock from flock manager */
        GPUBoid[] flock = flockManager.GetFlock();

        /* Create a ComputeBuffer with data for existing boids */
        if (flockBuffer != null) flockBuffer.Release();
        flockBuffer = new ComputeBuffer(flock.Length, GPUBoid.sizeOfGPUBoid);
        flockBuffer.SetData(flock);

        /* Set compute shader data */
        //boid info
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boids", flockBuffer);
        behaviourCompute.SetInt("numBoids", flock.Length);

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
        boidPositionsBuffer = new ComputeBuffer(flock.Length, sizeof(float) * 4);
        behaviourCompute.SetBuffer(behaviourComputerKernelHandle, "boidPositions", boidPositionsBuffer);

        /* Get number of threads */
        int numGroupsX = (flock.Length / GROUP_SIZE) + 1;

        /* Dispatch compute shader */
        behaviourCompute.Dispatch(behaviourComputerKernelHandle, numGroupsX, 1, 1);

        /* Send updated flock to flock manager */
        //flockManager.SetFlockBuffer(flockBuffer);
        flockBuffer.GetData(flock);
        flockManager.SetFlock(flock);

        /* Send updated boid positions to flock renderer */
        flockRenderer.SetBoidPositionsBuffer(boidPositionsBuffer);

        stopwatch.Stop();
        Debug.Log("Boid compute time: " + stopwatch.ElapsedMilliseconds + " ms");
    }


}
