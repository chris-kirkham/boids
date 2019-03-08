using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoidVision : MonoBehaviour
{
    private Rigidbody rb;

    /* Boid overlap sphere params */
    public float overlapSphereRadius; //current overlap sphere radius; may be changed if using adaptive overlap sphere
    public bool useAdaptiveOverlapSphere;
    private float minAdaptiveOverlapRadius, maxAdaptiveOverlapRadius;
    private float adaptiveOverlapSphereInc = 0.5f; //number to increment/decrement adaptive overlap sphere size by if using it

    //Can choose to store (and react to during behaviour calculation) a limited number of boids. 0 = store as many as boid sees
    public int maxSeenBoidsToStore = 5;

    /* Obstacle overlap sphere params */
    private const float OBSTACLE_CHECK_DISTANCE = 50.0f; //distance from boid to cast ray to check if boid is heading towards an obstacle

    /* Layer masks */
    private const int LAYER_BOID = 1 << 9;
    private const int LAYER_OBSTACLE = 1 << 10;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public List<GameObject> GetSeenBoids()
    {
        List<GameObject> seenBoids = new List<GameObject>();

        Collider[] boids = Physics.OverlapSphere(rb.transform.position, overlapSphereRadius, LAYER_BOID);
        int n = (maxSeenBoidsToStore <= 0) ? boids.Length : Mathf.Min(boids.Length, maxSeenBoidsToStore);
        for (int i = 0; i < n; i++)
        {
            if (boids[i].gameObject != this.gameObject) seenBoids.Add(boids[i].gameObject);
        }

        //ADAPTIVE OVERLAP SPHERE: if current pass didn't find enough boids, increase overlap sphere size; if it did, reduce it
        if (useAdaptiveOverlapSphere)
        {
            if (seenBoids.Count < maxSeenBoidsToStore && overlapSphereRadius < maxAdaptiveOverlapRadius)
            {
                overlapSphereRadius += adaptiveOverlapSphereInc;
            }
            else if (overlapSphereRadius > minAdaptiveOverlapRadius)
            {
                overlapSphereRadius -= adaptiveOverlapSphereInc;
            }
        }

        return seenBoids;
    }

    public List<GameObject> GetSeenObstacles()
    {
        List<GameObject> seenObstacles = new List<GameObject>();

        Collider[] obstacles = Physics.OverlapSphere(rb.transform.position, OBSTACLE_CHECK_DISTANCE / 2, LAYER_OBSTACLE);
        foreach (Collider c in obstacles) seenObstacles.Add(c.gameObject);

        return seenObstacles;
    }
}
