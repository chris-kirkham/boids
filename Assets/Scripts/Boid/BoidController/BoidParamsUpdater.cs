using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Updates the given boid params ScriptableObject(s) with user input 
/// </summary>
public class BoidParamsUpdater : MonoBehaviour
{
    public BoidBehaviourParams behaviourParams;

    private void Update()
    {
        behaviourParams.useCursorFollow = ControlInputs.Instance.useMouseFollow;
        behaviourParams.useBoundingCoordinates = ControlInputs.Instance.useBoundingCoordinates;
    }
}
