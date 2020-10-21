using System.Collections.Generic;
using UnityEngine;

public abstract class BoidVision : MonoBehaviour
{
    /* Constants */
    protected const int SEEN_BOIDS_INIT_CAPACITY = 100;
    protected const int SEEN_OBSTACLES_INIT_CAPACITY = 100;
    protected const float BOID_SEEN_DOT_MIN = -0.5f;

    /* Boid overlap sphere params */
    public float visionRadius; //current overlap sphere radius; may be changed if using adaptive overlap sphere
    public bool useAdaptiveVisionRadius;
    public float minAdaptiveVisRadius, maxAdaptiveVisRadius;
    protected float adaptiveRadiusInc = 0.5f; //number to increment/decrement adaptive overlap sphere size by if using it

    //Can choose to store (and react to during behaviour calculation) a limited number of boids. 0 = store as many as boid sees
    public int maxSeenBoidsToStore = 5;

    /* use fast (but simple and inaccurate) spatial check - just return other boids from the hash this boid is in */
    public bool useFastHashCheck;

    /* Layer masks */
    protected const int LAYER_BOID = 1 << 9;
    protected const int LAYER_OBSTACLE = 1 << 10;

    public abstract void UpdateSeenBoids();

    protected bool IsWithinVisionAngle(Vector3 otherBoidPos)
    {
        return Vector3.Dot(transform.forward, (otherBoidPos - transform.position).normalized) > BOID_SEEN_DOT_MIN;
    }

}
