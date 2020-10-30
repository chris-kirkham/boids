using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles converting attractors/repulsors/other affector GameObjects in the scene to buffers, and stores them for use by the flock compute shader
/// </summary>
public class GPUAffectorManager : MonoBehaviour
{
    private ComputeBuffer attractorsBuffer, repulsorsBuffer, pushersBuffer;

    // Start is called before the first frame update
    void Start()
    {
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
                    Debug.LogError("Tried to add Unknown affector type to affector buffers; did you add a new one and not update this????");
                    break;
            }
        }

        attractorsBuffer = new ComputeBuffer(attractors.Count, GPUAttractorRepulsor.sizeofGPUAttractorRepulsor);
        repulsorsBuffer = new ComputeBuffer(repulsors.Count, GPUAttractorRepulsor.sizeofGPUAttractorRepulsor);
        pushersBuffer = new ComputeBuffer(pushers.Count, GPUPusher.sizeofGPUPusher);

        attractorsBuffer.SetData(attractors);
        repulsorsBuffer.SetData(repulsors);
        pushersBuffer.SetData(pushers);
    }
}
