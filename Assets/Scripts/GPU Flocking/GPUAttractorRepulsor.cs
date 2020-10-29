using UnityEngine;

/// <summary>
/// Struct representing attractor/repulsor points for GPU flocking. Attractors and repulsors are differentiated in the compute shader by their respective buffers.
/// </summary>
public struct GPUAttractorRepulsor
{
    public Vector3 position;
    public float radius;
    public float strength;
    
    public GPUAttractorRepulsor(Vector3 position, float radius, float strength)
    {
        this.position = position;
        this.radius = radius;
        this.strength = strength;
    }
}
