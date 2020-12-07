using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles converting attractors/repulsors/other affector GameObjects in the scene to buffers, and stores them for use by the flock compute shader
/// </summary>
public class GPUAffectorManager : MonoBehaviour
{
    private BoidAffector[] affectors;
    private ComputeBuffer affectorsBuffer;
    private int numAffectors;

    void Start()
    {
        //Find affectors in scene and cache them to a list
        affectors = FindObjectsOfType<BoidAffector>();
        numAffectors = affectors.Length;
        if(numAffectors > 0)
        {
            affectorsBuffer = new ComputeBuffer(numAffectors, GPUAffector.sizeofGPUAffector);
            List<GPUAffector> affectorStructs = new List<GPUAffector>(numAffectors);
            foreach (BoidAffector affector in affectors)
            {
                switch(affector.shape)
                {
                    case BoidAffector.Shape.Sphere:
                        affectorStructs.Add(new GPUAffector(affector.transform.position, affector.transform.forward, affector.strength, affector.radius, (uint)affector.type));
                        break;
                    case BoidAffector.Shape.AABB:
                        affectorStructs.Add(new GPUAffector(affector.transform.position, affector.transform.forward, affector.strength, affector.aabbMin, affector.aabbMax, (uint)affector.type));
                        break;
                    default:
                        break;
                }
            }

            affectorsBuffer.SetData(affectorStructs);
        }
    }

    private void OnDisable()
    {
        if (affectorsBuffer != null) affectorsBuffer.Release();
    }

    public ComputeBuffer GetAffectorsBuffer()
    {
        return affectorsBuffer;
    }

    public int GetNumAffectors()
    {
        return numAffectors;
    }
}
