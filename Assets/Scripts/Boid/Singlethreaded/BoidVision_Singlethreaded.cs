using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidVision_Singlethreaded : BoidVision
{
    public SpatialHash_Singlethreaded hash;
    
    public List<GameObject> SeenBoids { get; private set; } = new List<GameObject>(SEEN_BOIDS_INIT_CAPACITY);

    public override void UpdateSeenBoids()
    {
        SeenBoids.Clear();

        //System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        List<GameObject> boids = new List<GameObject>();
        if(useFastHashCheck)
        {
            boids = hash.GetNRandom(transform.position, maxSeenBoidsToStore);
        } 
        else
        {
            //boids = hash.GetNByRadius(transform.position, overlapSphereRadius, maxSeenBoidsToStore);
            boids = hash.GetByRadius(transform.position, visionRadius);
        }

        foreach(GameObject boid in boids)
        {
            if (boid != this.gameObject && IsWithinVisionAngle(boid.transform.position))
            {
                SeenBoids.Add(boid);
            }
        }

        /*
        int n = (maxSeenBoidsToStore <= 0) ? boids.Count : Mathf.Min(boids.Count, maxSeenBoidsToStore);
        for (int i = 0; i < n; i++)
        {
            if (boids[i] != this.gameObject && IsWithinVisionAngle(boids[i].transform.position))
            {
                SeenBoids.Add(boids[i]);
            }
        }
        */

        //watch.Stop();
        //if(Random.Range(0f, 1f) >= 0.9f) Debug.Log("time to get seen boids (fast hash check = " + useFastHashCheck + "): " + watch.ElapsedMilliseconds + " ms");

        //ADAPTIVE OVERLAP SPHERE: if current pass didn't find enough boids, increase overlap sphere size; if it did, reduce it
        if (useAdaptiveVisionRadius)
        {
            if (SeenBoids.Count < maxSeenBoidsToStore && visionRadius < maxAdaptiveVisRadius)
            {
                visionRadius += adaptiveRadiusInc;
            }
            else if (visionRadius > minAdaptiveVisRadius)
            {
                visionRadius -= adaptiveRadiusInc;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawSphere(transform.position, visionRadius);
    }

    /*
    public void UpdateSeenObstacles()
    {
        SeenObstacles.Clear();

        Collider[] obstacles = Physics.OverlapSphere(rb.transform.position, OBSTACLE_CHECK_DISTANCE / 2, LAYER_OBSTACLE);
        foreach (Collider c in obstacles) SeenObstacles.Add(c.gameObject);
    }
    */
}
