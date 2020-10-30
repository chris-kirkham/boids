using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles converting attractors/repulsors/other affector GameObjects in the scene to buffers, and stores them for use by the flock compute shader
/// </summary>
public class GPUAffectorManager : MonoBehaviour
{
    private ComputeBuffer attractorsBuffer, repulsorsBuffer, pushersBuffer;
    private int numAttractors, numRepulsors, numPushers;

    void Start()
    {
        /* Get affectors from scene and convert to ComputeBuffers */ 
        BoidAffector[] affectors = FindObjectsOfType<BoidAffector>();
        List<GPUAttractorRepulsor> attractors = new List<GPUAttractorRepulsor>();
        List<GPUAttractorRepulsor> repulsors = new List<GPUAttractorRepulsor>();
        List<GPUPusher> pushers = new List<GPUPusher>();
        foreach (BoidAffector ar in affectors)
        {
            switch(ar.type)
            {
                case BoidAffector.Type.Attractor:
                    attractors.Add(new GPUAttractorRepulsor(ar.transform.position, ar.radius, ar.strength));
                    break;
                case BoidAffector.Type.Repulsor:
                    repulsors.Add(new GPUAttractorRepulsor(ar.transform.position, ar.radius, ar.strength));
                    break;
                case BoidAffector.Type.Pusher:
                    pushers.Add(new GPUPusher(ar.transform.position, ar.transform.forward, ar.radius, ar.strength));
                    break;
                default:
                    Debug.LogError("Tried to add unknown affector type to affector buffers; did you add a new one and not update this????");
                    break;
            }
        }

        numAttractors = attractors.Count;
        numRepulsors = repulsors.Count;
        numPushers = pushers.Count;

        if(numAttractors > 0)
        {
            attractorsBuffer = new ComputeBuffer(attractors.Count, GPUAttractorRepulsor.sizeofGPUAttractorRepulsor);
            attractorsBuffer.SetData(attractors);
        }

        if(numRepulsors > 0)
        {
            repulsorsBuffer = new ComputeBuffer(repulsors.Count, GPUAttractorRepulsor.sizeofGPUAttractorRepulsor);
            repulsorsBuffer.SetData(repulsors);
        }

        if(numPushers > 0)
        {
            pushersBuffer = new ComputeBuffer(pushers.Count, GPUPusher.sizeofGPUPusher);
            pushersBuffer.SetData(pushers);
        }
    }

    private void OnDisable()
    {
        if (attractorsBuffer != null) attractorsBuffer.Release();
        if (repulsorsBuffer != null) repulsorsBuffer.Release();
        if (pushersBuffer != null) pushersBuffer.Release();
    }

    public ComputeBuffer GetAttractorsBuffer()
    {
        return attractorsBuffer;
    }

    public ComputeBuffer GetRepulsorsBuffer()
    {
        return repulsorsBuffer;
    }

    public ComputeBuffer GetPushersBuffer()
    {
        return pushersBuffer;
    }

    public int GetNumAttractors()
    {
        return numAttractors;
    }

    public int GetNumRepulsors()
    {
        return numRepulsors;
    }

    public int GetNumPushers()
    {
        return numPushers;
    }
}
