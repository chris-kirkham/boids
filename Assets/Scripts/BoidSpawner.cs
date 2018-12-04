using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script to spawn a number of boids at random points in a cube around the spawner object
public class BoidSpawner : MonoBehaviour {

    public GameObject boid;
    public int numBoids;
    public float spawnAreaSize;

	// Use this for initialization
	void Start () {
        numBoids = 100;
        spawnAreaSize = 25.0f;
  
        SpawnBoids(numBoids);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void SpawnBoids(int numBoids)
    {
        for(int i = 0; i < numBoids; i++)
        {
            Vector3 spawnPosition = new Vector3(Random.Range(-spawnAreaSize, spawnAreaSize), Random.Range(-spawnAreaSize, spawnAreaSize), Random.Range(-spawnAreaSize, spawnAreaSize));
            Vector3 boidPosition = this.transform.position + spawnPosition;
            Quaternion boidRotation = new Quaternion();
            Instantiate(boid, boidPosition, boidRotation);
        }
    }
}
