# boids
#### An implementation of the Boids algorithm (Reynolds, C. W. (1987) Flocks, Herds, and Schools: A Distributed Behavioral Model) for Unity.
| <img src="README_1.gif"> | <img src="README_2.gif"> | <img src="README_3.gif"> |
|:----:|:----:|:----:|

## About
This is a Unity implementation of Craig Reynold's *Boids* algorithm (http://www.cs.toronto.edu/~dt/siggraph97-course/cwr87/ - please see the paper for a more thorough explanation). Essentially, the system is formed of several "boid" particles which react dynamically to each other and to the environment in a manner similar to a flock of birds or a school of fish.

## Controls
<p> WASD - move camera <br> 
Hold RMB + move mouse - rotate camera <br>
LMB - toggle mouse follow on/off <br>
Z - spawn boid <br>
X - delete boid </p>

## How it works
All individual boid behaviour is handled in *BoidBehaviour.cs*; 

### Reaction to other boids
Each boid attempts to:
* Stay a certain distance from other boids
* Match velocity with that of nearby boids
* Move towards the centre of nearby boids

In this implementation, all boids are the same (small) size and shape, so the maintenance of distance between boids is simply a case of, for each boid in the flock, finding the closest boids to that boid and averaging the difference between its position and the other boids' positions. To save performance, the boids react only to the closest up to *n* boids to it, even if there are more within its attention range.

### Obstacle avoidance
Unlike boids, obstacles can be any size and shape, so the method used for boid avoidance will not work. It would be easy to use an obstacle's bounding box to work out its size, but this would not allow the boids to closely avoid objects whose shape does not match its bounds - objects with holes in them, for example.

In this implementation, therefore, obstacle avoidance uses a ray/line casting method: rays are cast at increasingly wide angles from a boid's target direction until a vector is found (or until the number of rays cast reaches a predetermined maximum) which allows the boid to avoid the obstacle. If more than one avoid vector is found, the boid chooses the one which is closest to its target vector in order to minimise unnecessary directional change.

### Cursor follow
The boids may follow a 3D cursor controlled by the user's mouse input. In the future, I plan to implement path/curve following.


###### Christopher Kirkham, 2019
