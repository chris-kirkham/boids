﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidMovement_NoRigidbody : BoidMovement
{
    private Vector3 lastPosition; //boid's last world position - used for GetVelocity()
    private Vector3 velocity = Vector3.zero; 

    private Vector3 smoothdampVelocity = Vector3.zero; //used for Smoothdamp when moving boid

    private void Awake()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        velocity = (transform.position - lastPosition) * Time.deltaTime;
        lastPosition = transform.position;
    
    }

    public override void MoveBoid(Vector3 vel)
    {
        transform.position = Vector3.SmoothDamp(transform.position, transform.position + vel, ref smoothdampVelocity, 0.2f, maxSpeed);
    }

    public override Vector3 GetVelocity()
    {
        return velocity;
    }
}
