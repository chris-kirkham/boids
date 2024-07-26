////Functions and parameters common to both batched and non-batched versions of the flocking compute shader

#define GROUP_SIZE 64

#define BOID_VISION_CONE_MIN_DOT -0.2f
#define TURNING_SPEED 5.0f
#define MAX_NEIGHBOUR_COUNT 5

/*
#include "../cgincs/gpuBoid.cginc"
#include "../cgincs/bccNoise4.cginc"
#include "../cgincs/boidSteering.cginc"
#include "../cgincs/affectors.cginc"
#include "UnityCG.cginc"
*/

#include "gpuBoid.cginc"
#include "bccNoise4.cginc"
#include "boidSteering.cginc"
#include "affectors.cginc"
#include "UnityCG.cginc"

/* Flock buffer */
RWStructuredBuffer<Boid> boids;
uint numBoids;

/* Boid positions/forward directions buffers, for passing to Graphics.DrawMeshInstancedIndirect */
RWStructuredBuffer<float4> boidPositions;
RWStructuredBuffer<float3> boidForwardDirs;

/* Boid affectors */
StructuredBuffer<Affector> affectors;
uint numAffectors;

/* Flock behaviour params, same for each boid */
CBUFFER_START(Params)
//boid movement
float moveSpeed;
float maxSpeed;
float mass;
float friction;
float deltaTime; //to scale boid movement by time since last update

//other boid reactions
float neighbourDist;
float avoidDist;
float avoidSpeed;

//cursor following
bool usingCursorFollow;
float cursorFollowSpeed;
float arrivalSlowStartDist;
float3 cursorPos;

//movement bounds
bool usingBounds;
float boundsSize;
float3 boundsCentre;
float boundsReturnSpeed;

//idle movement
bool usingIdleMvmt;
Texture2D idleNoiseTex;
float idleNoiseFrequency;
float idleOffset;
float idleMoveSpeed;
CBUFFER_END

float3 ReactToOtherBoids(uint id)
{
    Boid boid = boids[id];
    float3 pos = boid.position;
    float3 vel = boid.velocity;

    float3 avoidDir = float3(0, 0, 0);
    float3 centre = float3(0, 0, 0);
    float3 velocityMatch = float3(0, 0, 0);

    uint neighbourCount = 0;
    for (uint i = 0; i < numBoids; i++)
    {
        //don't count self as neighbour
        if (i == id)
        {
            continue;
        }
        
        Boid otherBoid = boids[i];

        float3 otherBoidPos = otherBoid.position;
        float3 otherBoidVel = otherBoid.velocity;
        float dist = distance(pos, otherBoidPos);

        //if (dist < neighbourDist && length(vel) > 0 && dot(normalize(vel), normalize(otherBoidPos - pos)) > BOID_VISION_CONE_MIN_DOT)
        if (dist < neighbourDist)
        {
            if (dist < avoidDist) avoidDir += (pos - otherBoidPos) * saturate(1.0f - (dist / avoidDist)); //scale avoid speed by distance (max ||boid pos - other boid pos||)
            centre += otherBoidPos - pos;
            velocityMatch += otherBoidVel;

            neighbourCount++;
            if (neighbourCount > MAX_NEIGHBOUR_COUNT)
            {
                break;
            }
        }
    }

    float avg = 1 / (float)(neighbourCount + 1); //if neighbourCount == 0, add 1 to avoid a divide-by-zero
    centre *= avg;
    velocityMatch *= avg;

    return (avoidDir * avoidSpeed) + centre + velocityMatch;
}

float3 MoveIdle(uint id)
{
    return Bcc4NoiseBaseDirectional3D((boids[id].position + idleOffset) * idleNoiseFrequency) * idleMoveSpeed * (int)usingIdleMvmt;
}

float3 FollowCursor(uint id)
{
    return arrival(boids[id].position, boids[id].velocity, cursorPos, cursorFollowSpeed, arrivalSlowStartDist) * (int)usingCursorFollow;
}

float3 ReturnToBounds(uint id)
{
    float3 position = boids[id].position;
    return normalize(boundsCentre - position) * boundsReturnSpeed * (int)(distance(boundsCentre, position) > boundsSize) * usingBounds;
}

float3 AffectorInfluence(uint id)
{
    float3 dir = float3(0, 0, 0);
    float3 pos = boids[id].position;
    float3 vel = boids[id].velocity;

    for (uint i = 0; i < numAffectors; i++)
    {
        Affector affector = affectors[i];
        bool boidInAffectorRange = false;

        switch (affector.shape)
        {
        case 0: //sphere
            if (isInSphere(pos, affector)) boidInAffectorRange = true;
            break;
        case 1: //AABB
            if (isInAABB(pos, affector)) boidInAffectorRange = true;
            break;
        default:
            break;
        }

        if (boidInAffectorRange)
        {
            switch (affector.type)
            {
            case 0: //attractor
                dir += arrival(pos, vel, affector.position, affector.strength, arrivalSlowStartDist);
                break;
            case 1: //repulsor
                dir += avoid(pos, vel, affector.position, affector.strength);
                break;
            case 2: //pusher
                dir += affector.direction * affector.strength;
                break;
            default:
                break;
            }
        }
    }

    return dir;
}
