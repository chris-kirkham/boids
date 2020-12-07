using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD_NumBoids : MonoBehaviour
{
    public GPUFlockManager flockManager;
    private Text numBoidsText;

    void Start()
    {
        numBoidsText = GetComponent<Text>();
        SetNumBoidsText(flockManager.GetFlockSize());
    }

    void Update()
    {
    }

    void SetNumBoidsText(int numBoids)
    {
        numBoidsText.text = numBoids + " boids";
    }
}
