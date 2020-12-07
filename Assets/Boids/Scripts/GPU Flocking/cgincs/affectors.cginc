////Affector structs for boid behaviour compute shader(s)

struct Affector
{
	float3 position;
	float3 direction; //pusher only
	float strength;
	float radius; //sphere only
	float3 aabbMin, aabbMax; //AABB only

	uint type; //0 = attractor, 1 = repulsor, 2 = pusher
	uint shape; //0 = sphere, 1 = AABB
};

bool isInSphere(float3 position, Affector sphereAffector)
{
	return distance(position, sphereAffector.position) <= sphereAffector.radius;
}

bool isInAABB(float3 position, Affector aabbAffector)
{
	float3 min = aabbAffector.aabbMin;
	float3 max = aabbAffector.aabbMax;

	return position.x >= min.x && position.y >= min.y && position.z >= min.z
		&& position.x <= max.x && position.y <= max.y && position.z <= max.z;
}