using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUFlockSpawner : MonoBehaviour
{
    public float spawnAreaSize;

    public GPUBoid[] SpawnFlock(int flockSize)
    {
        if(flockSize <= 0)
        {
            Debug.LogError("Trying to spawn flock with size <= 0! Flock size must be > 0.");
            return new GPUBoid[0];
        }

        GPUBoid[] flock = new GPUBoid[flockSize];
        for(int i = 0; i < flockSize; i++)
        {
            flock[i] = new GPUBoid(GetBoidRandomSpawnPosition(), Vector3.zero);
        }

        return flock;
    }

    Vector3 GetBoidRandomSpawnPosition()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-spawnAreaSize, spawnAreaSize), Random.Range(0, spawnAreaSize * 2), Random.Range(-spawnAreaSize, spawnAreaSize));
        return transform.position + spawnPosition;
    }

    void OnDrawGizmos()
    {
        //draw small cube to help select spawner
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(transform.position, new Vector3(0.2f, 0.2f, 0.2f));

        //draw spawn area
        Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.2f); //translucent cyan
        Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y + spawnAreaSize, transform.position.z),
            new Vector3(spawnAreaSize * 2, spawnAreaSize * 2, spawnAreaSize * 2));
    }
}
