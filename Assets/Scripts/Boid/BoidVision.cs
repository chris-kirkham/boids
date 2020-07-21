using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidVision : MonoBehaviour
{
    /* Constants */
    private const int SEEN_BOIDS_INIT_CAPACITY = 100;
    private const int SEEN_OBSTACLES_INIT_CAPACITY = 100;

    private const float OBSTACLE_CHECK_DISTANCE = 50.0f;

    /* Components */
    public string hashName; //name of spatial hash object to find
    private SpatialHash hash; //hash in which to store this object
    
    /* Persistent "seen x" lists  */
    public List<GameObject> SeenBoids { get; private set; } = new List<GameObject>(SEEN_BOIDS_INIT_CAPACITY);
    public List<GameObject> SeenObstacles { get; private set; } = new List<GameObject>(SEEN_OBSTACLES_INIT_CAPACITY);

    /* Boid overlap sphere params */
    public float overlapSphereRadius; //current overlap sphere radius; may be changed if using adaptive overlap sphere
    public bool useAdaptiveOverlapSphere;
    public float minAdaptiveOverlapRadius, maxAdaptiveOverlapRadius;
    private float adaptiveOverlapSphereInc = 0.5f; //number to increment/decrement adaptive overlap sphere size by if using it

    //Can choose to store (and react to during behaviour calculation) a limited number of boids. 0 = store as many as boid sees
    public int maxSeenBoidsToStore = 5;

    /* use fast (but simple and inaccurate) spatial check - just return other boids from the hash this boid is in */
    public bool useFastHashCheck;

    /* Layer masks */
    private const int LAYER_BOID = 1 << 9;
    private const int LAYER_OBSTACLE = 1 << 10;

    void Awake()
    {
        hash = GameObject.Find(hashName).GetComponent<SpatialHash>();

        if (hash == null)
        {
            Debug.LogError("BoidVision cannot find SpatialHash by name " + hashName + "!");
        }
    }

    public void UpdateSeenBoids()
    {
        SeenBoids.Clear();

        //System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        List<GameObject> boids = new List<GameObject>();
        if(useFastHashCheck)
        {
            boids = hash.Get(transform.position);
        } 
        else
        {
            boids = hash.GetByRadius(transform.position, overlapSphereRadius);
        }

        int n = (maxSeenBoidsToStore <= 0) ? boids.Count : Mathf.Min(boids.Count, maxSeenBoidsToStore);
        for (int i = 0; i < n; i++)
        {
            if (boids[i] != this.gameObject) SeenBoids.Add(boids[i]);
        }
        
        //watch.Stop();
        //if(Random.Range(0f, 1f) >= 0.9f) Debug.Log("time to get seen boids (fast hash check = " + useFastHashCheck + "): " + watch.ElapsedMilliseconds + " ms");

        //ADAPTIVE OVERLAP SPHERE: if current pass didn't find enough boids, increase overlap sphere size; if it did, reduce it
        if (useAdaptiveOverlapSphere)
        {
            if (SeenBoids.Count < maxSeenBoidsToStore && overlapSphereRadius < maxAdaptiveOverlapRadius)
            {
                overlapSphereRadius += adaptiveOverlapSphereInc;
            }
            else if (overlapSphereRadius > minAdaptiveOverlapRadius)
            {
                overlapSphereRadius -= adaptiveOverlapSphereInc;
            }
        }
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
