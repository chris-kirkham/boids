using UnityEngine;

/// <summary>
/// Represents either an attractor (pulls boids in), a repulsor (pushes boids away), or a pusher (pushes boids in a certain direction)
/// affector in the scene. The scene object with this script is converted into a struct 
/// (GPUAffectorRepulsor) and placed in corresponding ComputeBuffers on start by the affector manager script.
/// </summary>
public class BoidAffector : MonoBehaviour
{
    public enum Type { Attractor, Repulsor, Pusher };
    public Type type = Type.Attractor;
    public float radius = 1f;
    public float strength = 1f;

    //visualises affector
    private void OnDrawGizmos()
    {
        if(type == Type.Attractor)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else if(type == Type.Repulsor)
        {
            Gizmos.color = new Color(1, 0, 1);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else //pusher
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawRay(transform.position, transform.forward * strength);
        }
    }
}
