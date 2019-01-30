# boids
An implementation of the Boids algorithm (Reynolds, C. W. (1987) Flocks, Herds, and Schools: A Distributed Behavioral Model) for Unity.


## How it works

All individual boid behaviour is handled in *BoidBehaviour.cs*; 
### Reaction to other boids

### Obstacle avoidance
In this implementation, all boids are the same (small) size and shape, so the avoidance of other boids is simply a case of finding their world position(s) and 

Obstacle avoidance uses a combination of the

Rays are cast at increasingly wide angles from the boid's current facing until a vector is found (or until the number of rays cast reaches a predetermined maximum) which allows the boid to avoid the obstacle. If more than one avoid vector is found, the boid chooses the one which is closest to its current velocity vector in order to minimise directional change.
