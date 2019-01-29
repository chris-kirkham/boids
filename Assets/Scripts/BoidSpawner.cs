using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Handles the spawning and destruction of boids around the attached GameObject
public class BoidSpawner : MonoBehaviour {

    public GameObject boid;
    public int initNumBoids; //initial number of boids to spawn
    public float spawnAreaSize;

    private Stack<GameObject> boids;
    private int boidCount; //current number of boids in the scene 

    private bool debug = false;

    // Use this for initialization
	void Awake ()
    {
        boids = new Stack<GameObject>();

        for (int i = 0; i < initNumBoids; i++)
        {
            SpawnBoid();
        }

        boidCount = initNumBoids;
	}

    void OnDrawGizmos()
    {
        //draw small cube to help select spawner
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(transform.position, new Vector3(0.2f, 0.2f, 0.2f));

        //draw spawn area
        Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.2f); //translucent cyan
        Gizmos.DrawCube(transform.position, new Vector3(spawnAreaSize * 2, spawnAreaSize * 2, spawnAreaSize * 2));
    }

    void Update()
    {
        if (ControlInputs.Instance.spawnNewBoid) 
        {
            SpawnBoid();
            boidCount++;
        }
        else if(ControlInputs.Instance.destroyBoid) //destroys last-created boid
        {
            Destroy(boids.Pop());
            boidCount--;
        }
    }

    //spawn a boid at a random point in a cube around the spawner object
    void SpawnBoid()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-spawnAreaSize, spawnAreaSize), Random.Range(-spawnAreaSize, spawnAreaSize), Random.Range(-spawnAreaSize, spawnAreaSize));
        Vector3 boidPosition = this.transform.position + spawnPosition;
        Quaternion boidRotation = new Quaternion();
        boids.Push(Instantiate(boid, boidPosition, boidRotation));
        if(debug) Debug.Log("boid spawned at " + boidPosition + "!");
    }

    public int GetBoidCount()
    {
        return boidCount;
    }
}
