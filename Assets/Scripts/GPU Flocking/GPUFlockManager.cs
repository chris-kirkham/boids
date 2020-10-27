using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GPUFlockSpawner))]
public class GPUFlockManager : MonoBehaviour
{
    [SerializeField] [Min(1)] private int flockSize = 1024;
    private GPUBoid[] flock;
    private ComputeBuffer flockBuffer;

    private GPUFlockSpawner spawner;

    void Start()
    {
        spawner = GetComponent<GPUFlockSpawner>();
        flock = spawner.SpawnFlock(flockSize);
        flockBuffer = new ComputeBuffer(flockSize, GPUBoid.sizeOfGPUBoid);
    }

    void Update()
    {
    }

    private void OnDestroy()
    {
        flockBuffer.Release();
    }

    public int GetFlockSize()
    {
        return flockSize;
    }

    public GPUBoid[] GetFlock()
    {
        return flock;
    }

    public ComputeBuffer GetFlockBuffer()
    {
        return flockBuffer;
    }

    public void SetFlockBuffer(ComputeBuffer flockBuffer)
    {
        this.flockBuffer = flockBuffer;
    }

    public void SetFlock(GPUBoid[] flock)
    {
        this.flock = flock;
    }

    public void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            foreach (GPUBoid boid in flock)
            {
                Gizmos.DrawSphere(boid.position, 1f);
            }
        }
    }
}
