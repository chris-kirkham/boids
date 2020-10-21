using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BoidVisionDOTS : BoidVision
{
    /* Hash */
    public SpatialHashDOTS hash;

    /* Persistent vision list(s) */
    private NativeList<Boid_Blittable> seenBoids;

    /* Job variables */
    private JobHandle updateSeenBoidsJobHandle;

    private void Awake()
    {
        seenBoids = new NativeList<Boid_Blittable>(SEEN_BOIDS_INIT_CAPACITY, Allocator.Persistent);
        //StartCoroutine(UpdateSeenBoidsCoroutine());   
    }

    private void OnDestroy()
    {
        seenBoids.Dispose();
    }

    private IEnumerator UpdateSeenBoidsCoroutine()
    {
        while (true)
        {
            UpdateSeenBoids();
            
            if(!updateSeenBoidsJobHandle.IsCompleted)
            {
                yield return new WaitForEndOfFrame();
            }

            updateSeenBoidsJobHandle.Complete();
            yield return null;
        }
    }

    public override void UpdateSeenBoids()
    {
        NativeList<Boid_Blittable> hashBoids = hash.Get(transform.position);

        UpdateSeenBoidsJob updateSeenBoidsJob = new UpdateSeenBoidsJob()
        {
            seenBoids = this.seenBoids,
            hashBoids = hashBoids,
            sqrVisionRadius = visionRadius * visionRadius,
            thisBoidPos = transform.position
        };

        updateSeenBoidsJobHandle = updateSeenBoidsJob.Schedule();

        updateSeenBoidsJobHandle.Complete();
    }

    private void LateUpdate()
    {
        updateSeenBoidsJobHandle.Complete();
    }

    [BurstCompile]
    public struct UpdateSeenBoidsJob : IJob
    {
        public NativeList<Boid_Blittable> seenBoids;

        public NativeList<Boid_Blittable> hashBoids; //boids found by hashing this boid's position in the spatial hash (done on the main thread)
        public float sqrVisionRadius;

        public Vector3 thisBoidPos;

        public void Execute()
        {
            seenBoids.Clear();

            for(int i = 0; i < hashBoids.Length; i++)
            {
                if((thisBoidPos - (Vector3)hashBoids[i].position).sqrMagnitude < sqrVisionRadius)
                {
                    seenBoids.Add(hashBoids[i]);
                }
            }
        }
    }

    public NativeList<Boid_Blittable> GetSeenBoids()
    {
        return seenBoids;
    }
}
