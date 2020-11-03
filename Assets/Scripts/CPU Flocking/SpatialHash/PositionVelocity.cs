using UnityEngine;

//Small struct to hold a boid's position and velocity in the blittable cell struct.
//Not really necessary, but NativeArray<PositionVelocity> is nicer than NativeArray<ValueTuple<Vector3, Vector3>>
public struct PositionVelocity
{
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }

    public PositionVelocity(Vector3 position, Vector3 velocity)
    {
        Position = position;
        Velocity = velocity;
    }
}
