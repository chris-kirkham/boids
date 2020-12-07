using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Updates the given boid params ScriptableObject with user input 
/// </summary>
public class BoidParamsUpdater : MonoBehaviour
{
    public BoidBehaviourParams behaviourParams;

    private void Update()
    {
        behaviourParams.useCursorFollow = ControlInputs.Instance.useMouseFollow;
        behaviourParams.useBounds = ControlInputs.Instance.useBoundingCoordinates;
    }
}
