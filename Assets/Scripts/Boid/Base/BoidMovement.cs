using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoidMovement : MonoBehaviour
{
    public float maxSpeed;

    public abstract void MoveBoid(Vector3 vel);

    public abstract Vector3 GetVelocity();

}
