using UnityEngine;

public struct GPUPusher
{
    public Vector3 position, direction;
    public float radius, strength;

    public GPUPusher(Vector3 position, Vector3 direction, float radius, float strength)
    {
        this.position = position;
        this.direction = direction;
        this.radius = radius;
        this.strength = strength;
    }

    public const int sizeofGPUPusher = sizeof(float) * 8;
}
