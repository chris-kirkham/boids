////BOID STEERING BEHAVIOURS, based on https://www.red3d.com/cwr/steer/gdc99/

float3 seek(float3 boidPos, float3 boidVel, float3 targetPos, float maxSpeed)
{
	float3 desiredVel = normalize(targetPos - boidPos) * maxSpeed;
	return desiredVel - boidVel;
}

float3 avoid(float3 boidPos, float3 boidVel, float3 targetPos, float maxSpeed)
{
	float3 desiredVel = normalize(boidPos - targetPos) * maxSpeed;
	return desiredVel - boidVel;
}

float3 avoidDistanceBased(float3 boidPos, float3 boidVel, float3 targetPos, float avoidDist, float maxSpeed)
{
	float3 targetOffset = targetPos - boidPos;
	float targetOffsetMag = length(targetOffset);
	float speed = maxSpeed * saturate(1 - (targetOffsetMag / avoidDist));
	float3 desiredVel = speed * (targetOffset / targetOffsetMag);
	return desiredVel - boidVel;
}

//simulates an "arrival" behaviour, in which the boid moves at full speed until it gets within a certain distance of the target (slowStartDist),
//at which point it starts to move slower the closer it gets to the target
float3 arrival(float3 boidPos, float3 boidVel, float3 targetPos, float maxSpeed, float slowStartDist)
{
	float3 targetOffset = targetPos - boidPos;
	float distToTarget = length(targetOffset);
	float speed = maxSpeed * saturate(distToTarget / slowStartDist);
	float3 desiredVel = speed * (targetOffset / distToTarget);
	return desiredVel - boidVel;
}


float3 applyForce(float3 force, float3 mass)
{
	return force / mass;
}