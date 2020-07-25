using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidMovement_NoRigidbody : BoidMovement
{
    public float smoothTime = 2f;
    public float rollAmount = 10f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 lastPosition = Vector3.zero;
    private Vector3 smoothDampVelocity = Vector3.zero; //tracked by SmoothDamp when moving boid
    private Vector3 angularVelocity = Vector3.zero;
    private Vector3 lastRotation = Vector3.zero;

    private void Awake()
    {
        lastRotation = transform.eulerAngles;
        lastPosition = transform.position;
    }

    private void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        angularVelocity = (transform.eulerAngles - lastRotation) / Time.deltaTime;
        lastRotation = transform.eulerAngles;
    }

    public override void MoveBoid(Vector3 vel)
    {
        transform.position = Vector3.SmoothDamp(transform.position, transform.position + (vel.normalized * maxSpeed), ref smoothDampVelocity, smoothTime, maxSpeed);
        if (smoothDampVelocity != Vector3.zero) transform.forward = smoothDampVelocity;
        
        /*
        //apply roll based on local x velocity
        float localXVel = transform.InverseTransformDirection(velocity).x;
        Vector3 rollUp = Vector3.up + (transform.right * localXVel) * rollAmount;
        if(smoothDampVelocity != Vector3.zero) transform.rotation = Quaternion.LookRotation(smoothDampVelocity, rollUp);
        */
    }

    public override Vector3 GetVelocity()
    {
        return smoothDampVelocity;
    }
}
