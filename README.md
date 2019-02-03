# boids
#### An implementation of the Boids algorithm (Reynolds, C. W. (1987) Flocks, Herds, and Schools: A Distributed Behavioral Model) for Unity.
| <img src="README_1.gif"> | <img src="README_2.gif"> | <img src="README_3.gif"> |
|:----:|:----:|:----:|

## Controls
<p> WASD - move camera <br> 
Hold RMB + move mouse - rotate camera <br>
LMB - toggle mouse follow on/off <br>
Z - spawn boid <br>
X - delete boid </p>

## How it works


All individual boid behaviour is handled in *BoidBehaviour.cs*; 
### Reaction to other boids
In this implementation, all boids are the same (small) size and shape, so the avoidance of other boids is simply a case of, for each boid in the flock, finding the *n* closest boids to that boid and averaging the difference between its position and the other boids' positions.

### Obstacle avoidance
Unlike boids, obstacles can be any size and shape, so the method used for boid avoidance will not work.

In this implementation, obstacle avoidance uses a raycasting method:

Rays are cast at increasingly wide angles from a boid's current facing until a vector is found (or until the number of rays cast reaches a predetermined maximum) which allows the boid to avoid the obstacle. If more than one avoid vector is found, the boid chooses the one which is closest to its current velocity vector in order to minimise directional change.

This combination of methods, while 

### Cursor follow
The boids may follow a 3D cursor controlled by the user's mouse input. In the future, I plan to implement path/curve following.
