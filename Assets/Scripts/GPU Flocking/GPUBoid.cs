using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Struct representing a boid; same as in the boid behaviour compute shader
/// </summary>
public struct GPUBoid
{
    public Vector3 position, velocity;
    //private Vector2 pad1; //pad to 32 bytes (4 bytes per float)

    public const int sizeOfGPUBoid = sizeof(float) * 6;

    public GPUBoid(Vector3 position, Vector3 velocity)
    {
        this.position = position;
        this.velocity = velocity;
        //pad1 = new Vector2();
    }
}
