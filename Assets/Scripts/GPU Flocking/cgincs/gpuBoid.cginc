////Boid struct for flocking computeshader and boid (Graphics.DrawMeshInstancedIndirect) shader; same as GPUBoid.cs

struct Boid
{
	float3 position;
	float3 velocity;
}

Boid constructBoid(float3 position, float3 velocity)
{
	Boid boid;
	boid.position = position;
	boid.velocity = velocity;
	
	return boid;
}