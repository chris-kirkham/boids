using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles actual movement of the boid (applying physics forces, limiting velocity etc.)
public class BoidMovement : MonoBehaviour
{
    private Rigidbody rb;
    public float velocityLimit;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void MoveBoid(Vector3 vel)
    {
        vel = LimitVelocity(vel, velocityLimit);
        rb.AddForce(vel);
        rb.velocity = LimitVelocity(rb.velocity, velocityLimit);
    }

    //Limit a vector's magnitude to a certain limit if it is over that limit
    Vector3 LimitVelocity(Vector3 velocity, float velocityLimit)
    {
        float velMagnitude = velocity.magnitude;

        if (velMagnitude > velocityLimit)
        {
            return (velocity / velMagnitude) * velocityLimit;
        }
        else
        {
            return velocity;
        }
    }
}
