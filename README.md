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
All individual boid behaviour is handled in BoidBehaviour.cs; boids are spawned from the BoidController game object, which contains the scripts BoidSpawner.cs and BoidCollectiveController.cs. User input is handled in the singleton class ControlInputs.cs.

### Attention range
Each boid has a sphere around it which could be said to represent its range of attention - if another boid overlaps this sphere, it is added to a list of boids to react to on that frame. To save performance, the boids can be set to store and react to only the closest (up to) *n* boids to it, even if there are more within its attention range. 

### Adaptive attention range
A boid's attention sphere may be set to be of dynamic size; it will grow until it overlaps *n* other boids, then shrink (if necessary) to remain at the minimum size which still overlaps *n* boids. In practice, this allows boids to group together in sub-flocks of a set number of boids (which may not be realistic behaviour in terms of actual birds, but may be useful in certain situations).

### Reaction to other boids
Each boid attempts to:
* Stay a certain distance from other boids
* Match velocity with that of nearby boids
* Move towards the centre of nearby boids

In this implementation, all boids are the same (small) size and shape, so the maintenance of distance between boids is simply a case of, for each boid in the flock, averaging the difference between its position and that of the boids in its attention range. Matching velocities and centring the boid is also achieved through averaging nearby boids' velocity/position vectors.

### Obstacle avoidance
Because a boid may want to begin avoiding collisions from a great distance, seeming to plan ahead for obstacles rather than simply reacting when one is close, its attention sphere is not used to find potential collisions with obstacles. Rather, a ray is cast in the direction of a boid's target vector (which is either ahead of it in its current movement direction or the position of a follow target), and avoidance behaviour is undertaken if this ray hits an obstacle.

Unlike boids, obstacles can be any size and shape, so the method used for boid avoidance will not work. It would be easy to use an obstacle's bounding box to work out its size, but this would not allow the boids to closely avoid objects whose shape does not match its bounds - objects with holes in them, for example. In this implementation, therefore, the obstacle avoidance itself uses an incremental ray/line casting method: rays are cast at increasingly wide angles from a boid's target direction until a vector is found (or until the number of rays cast reaches a predetermined maximum) which allows the boid to avoid the obstacle. If more than one avoid vector is found, the boid chooses the one which is closest to its target vector in order to minimise unnecessary directional change.

### Cursor follow
The boids may follow a 3D target controlled by the user's mouse input, and will move around obstacles to reach this target. In the future, I plan to implement path/curve following.


###### Christopher Kirkham, 2019
