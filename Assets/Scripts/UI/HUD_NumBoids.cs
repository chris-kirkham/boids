using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD_NumBoids : MonoBehaviour
{
    public BoidSpawner boidSpawner;
    private Text numBoidsText;

    void Start()
    {
        numBoidsText = GetComponent<Text>();
        SetNumBoidsText(boidSpawner.GetBoidCount());
    }

    void Update()
    {
        SetNumBoidsText(boidSpawner.GetBoidCount());
    }

    void SetNumBoidsText(int numBoids)
    {
        numBoidsText.text = "Boids: " + numBoids.ToString();
    }
}
