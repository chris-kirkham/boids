using UnityEngine;

public struct GPUAttractorRepulsor
{
    public Vector3 position;
    public float radius, strength;

    public GPUAttractorRepulsor(Vector3 position, float radius, float strength)
    {
        this.position = position;
        this.radius = radius;
        this.strength = strength;
    }

    public const int sizeofGPUAttractorRepulsor = sizeof(float) * 5;
}