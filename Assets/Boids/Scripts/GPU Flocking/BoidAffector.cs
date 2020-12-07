using UnityEngine;

/// <summary>
/// Represents either an attractor (pulls boids in), a repulsor (pushes boids away), or a pusher (pushes boids in a certain direction)
/// affector in the scene. The scene object with this script is converted into a struct 
/// (GPUAffector) and wrangled on start by the affector manager script.
/// </summary>
public class BoidAffector : MonoBehaviour
{
    public enum Type { Attractor, Repulsor, Pusher };
    public Type type = Type.Attractor;

    public enum Shape { Sphere, AABB };
    public Shape shape = Shape.Sphere;

    public float strength = 1f;

    //only for sphere
    public float radius = 1f;

    //only for AABB
    public Vector3 aabbMin, aabbMax;
    

    //visualises affector
    private void OnDrawGizmos()
    {
        if(type == Type.Attractor)
        {
            Gizmos.color = Color.green;
            
        }
        else if(type == Type.Repulsor)
        {
            Gizmos.color = new Color(1, 0, 1);
        }
        else //pusher
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * strength);
        }

        if (shape == Shape.Sphere)
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else //AABB
        {
            DrawAABB();
        }

        //draw centre sphere for an editor handle
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

    private void DrawAABB()
    {
        Vector3 zeroZeroZero = transform.position + aabbMin;
        Vector3 oneZeroZero = transform.position + new Vector3(aabbMax.x, aabbMin.y, aabbMin.z);
        Vector3 oneZeroOne = transform.position + new Vector3(aabbMax.x, aabbMin.y, aabbMax.z);
        Vector3 zeroZeroOne = transform.position + new Vector3(aabbMin.x, aabbMin.y, aabbMax.z);

        Vector3 zeroOneZero = transform.position + new Vector3(aabbMin.x, aabbMax.y, aabbMin.z);
        Vector3 oneOneZero = transform.position + new Vector3(aabbMax.x, aabbMax.y, aabbMin.z);
        Vector3 oneOneOne = transform.position + aabbMax;
        Vector3 zeroOneOne = transform.position + new Vector3(aabbMin.x, aabbMax.y, aabbMax.z);

        //base
        Gizmos.DrawLine(zeroZeroZero, oneZeroZero);
        Gizmos.DrawLine(oneZeroZero, oneZeroOne);
        Gizmos.DrawLine(oneZeroOne, zeroZeroOne);
        Gizmos.DrawLine(zeroZeroOne, zeroZeroZero);

        //top
        Gizmos.DrawLine(zeroOneZero, oneOneZero);
        Gizmos.DrawLine(oneOneZero, oneOneOne);
        Gizmos.DrawLine(oneOneOne, zeroOneOne);
        Gizmos.DrawLine(zeroOneOne, zeroOneZero);

        //vertical edges
        Gizmos.DrawLine(zeroZeroZero, zeroOneZero);
        Gizmos.DrawLine(oneZeroZero, oneOneZero);
        Gizmos.DrawLine(oneZeroOne, oneOneOne);
        Gizmos.DrawLine(zeroZeroOne, zeroOneOne);
    }

    private void DrawCuboid()
    {
        Vector3 zeroZeroZero = transform.TransformPoint(aabbMin);
        Vector3 oneZeroZero = transform.TransformPoint(new Vector3(aabbMax.x, aabbMin.y, aabbMin.z));
        Vector3 oneZeroOne = transform.TransformPoint(new Vector3(aabbMax.x, aabbMin.y, aabbMax.z));
        Vector3 zeroZeroOne = transform.TransformPoint(new Vector3(aabbMin.x, aabbMin.y, aabbMax.z));
        
        Vector3 zeroOneZero = transform.TransformPoint(new Vector3(aabbMin.x, aabbMax.y, aabbMin.z));
        Vector3 oneOneZero = transform.TransformPoint(new Vector3(aabbMax.x, aabbMax.y, aabbMin.z));
        Vector3 oneOneOne = transform.TransformPoint(aabbMax);
        Vector3 zeroOneOne = transform.TransformPoint(new Vector3(aabbMin.x, aabbMax.y, aabbMax.z));

        //base
        Gizmos.DrawLine(zeroZeroZero, oneZeroZero);
        Gizmos.DrawLine(oneZeroZero, oneZeroOne);
        Gizmos.DrawLine(oneZeroOne, zeroZeroOne);
        Gizmos.DrawLine(zeroZeroOne, zeroZeroZero);

        //top
        Gizmos.DrawLine(zeroOneZero, oneOneZero);
        Gizmos.DrawLine(oneOneZero, oneOneOne);
        Gizmos.DrawLine(oneOneOne, zeroOneOne);
        Gizmos.DrawLine(zeroOneOne, zeroOneZero);

        //vertical edges
        Gizmos.DrawLine(zeroZeroZero, zeroOneZero);
        Gizmos.DrawLine(oneZeroZero, oneOneZero);
        Gizmos.DrawLine(oneZeroOne, oneOneOne);
        Gizmos.DrawLine(zeroZeroOne, zeroOneOne);
    }
}
