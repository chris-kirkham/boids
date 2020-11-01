using UnityEngine;

public struct GPUAffector
{
    public Vector3 position;
    public Vector3 direction; //pusher type only
    public float strength;
    public float radius; //sphere shape only
    public Vector3 aabbMin, aabbMax; //AABB shape only

    public uint type; //0 = attractor, 1 = repulsor, 2 = pusher;
    public uint shape; //0 = sphere, 1 = AABB

    //AABB
    public GPUAffector(Vector3 position, Vector3 direction, float strength, Vector3 aabbMin, Vector3 aabbMax, uint type)
    {
        this.position = position;
        this.direction = direction;
        this.strength = strength;
        this.aabbMin = aabbMin;
        this.aabbMax = aabbMax;
        this.type = type;

        radius = 0f;
        shape = 1; //assume AABB if giving AABB min and max
    }

    //Sphere
    public GPUAffector(Vector3 position, Vector3 direction, float strength, float radius, uint type)
    {
        this.position = position;
        this.direction = direction;
        this.radius = radius;
        this.strength = strength;
        this.type = type;

        aabbMin = Vector3.zero;
        aabbMax = Vector3.zero;
        shape = 0; //assume sphere if giving radius
    }

    public const int sizeofGPUAffector = sizeof(float) * 14 + sizeof(uint) * 2;
}