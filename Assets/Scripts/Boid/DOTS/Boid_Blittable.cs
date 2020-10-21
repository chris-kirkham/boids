using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Blittable representation of a boid. Used for DOTS/GPU
/// </summary>
public struct Boid_Blittable
{
    public float3 position, velocity;
    //public int boidID;

    public Boid_Blittable(float3 position, float3 velocity)
    {
        this.position = position;
        this.velocity = velocity;
    }
}
