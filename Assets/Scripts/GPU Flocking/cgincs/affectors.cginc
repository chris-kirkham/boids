////Affector structs for boid behaviour compute shader(s)

struct AttractorRepulsor
{
	float3 position;
	float radius, strength;
};

AttractorRepulsor constructAttractorRepulsor(float3 position, float radius, float strength)
{
	AttractorRepulsor ar;
	ar.position = position;
	ar.radius = radius;
	ar.strength = strength;
	
	return ar;
}

struct Pusher
{
	float3 position, direction;
	float radius, strength;
};

Pusher constructPusher(float3 position, float3 direction, float radius, float strength)
{
	Pusher p;
	p.position = position;
	p.direction = direction;
	p.radius = radius;
	p.strength = strength;
}
