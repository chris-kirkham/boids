////BOID STEERING BEHAVIOURS, based on https://www.red3d.com/cwr/steer/gdc99/

float3 seek(float3 boidPos, float3 boidVel, float3 targetPos, float maxSpeed)
{
	float3 desiredVel = normalize(targetPos - boidPos) * maxSpeed;
	float3 steering = desiredVel - boidVel;
	
	return steering;
}

float3 avoid(float3 boidPos, float3 boidVel, float3 targetPos, float maxSpeed)
{
	float3 desiredVel = normalize(boidPos - targetPos) * maxSpeed;
	float3 steering = desiredVel - boidVel;
		
	return steering;
}

//simulates an "arrival" behaviour, in which the boid moves at full speed until it gets within a certain distance of the target (slowStartDist),
//at which point it starts to move slower the closer it gets to the target
float3 arrival(float3 boidPos, float3 boidVel, float3 targetPos, float maxSpeed, float slowStartDist, float deltaTime)
{
	float3 targetOffset = targetPos - boidPos;
	float dist = length(targetOffset);
	float speed = maxSpeed * saturate(dist / slowStartDist);
	float3 desiredVel = (speed) * targetOffset;
	float3 steering = desiredVel - boidVel;
	
	return steering;
}


float3 applyForce(float3 force, float3 mass)
{
	return force / mass;
}