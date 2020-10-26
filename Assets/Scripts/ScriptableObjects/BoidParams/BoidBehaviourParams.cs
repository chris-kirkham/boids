using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Boid Behaviour Params")]
public class BoidBehaviourParams : ScriptableObject
{
    [Header("Move speed parameters")]
    public float moveSpeed = 5f;

    [Header("Reaction to other boids")]
    public float neighbourDistance = 10f;
    public float avoidDistance = 1f;
    public float avoidSpeed = 1f;
    public int numClosestToCheck = 5;

    [Header("Cursor/goal following")]
    public bool useCursorFollow;
    public float cursorFollowSpeed = 1f;
    public bool useRandomGoal;
    public float goalFollowSpeed = 1f;

    [Header("Bounding coordinates")]
    public bool useBoundingCoordinates = true;
    public Vector3 boundsCentre = Vector3.zero;
    public float boundsSize; //size of cube representing boid bounding area (centre is at (0, 0, 0))
    public float boundsReturnSpeed = 1f;

    [Header("Obstacle avoidance")]
    public bool usePreemptiveObstacleAvoidance = true;
    public bool useObstacleRepulsion = true;
    public float obstacleAvoidDistance;
    public float obstacleAvoidSpeed = 5f;

    [Header("Idle behaviour")]
    public bool useIdleMvmt = true;
    public float idleNoiseFrequency = 0.01f;
    public float idleSpeed = 1f;
    public bool useTimeOffset = false;
}
