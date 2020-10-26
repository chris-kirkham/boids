using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Renders a flock of GPU boids using Graphics.DrawMeshInstancedIndirect
/// </summary>
/// REFERENCES:
/// https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html
/// https://medium.com/@bagoum/devlog-002-graphics-drawmeshinstancedindirect-a4024e05737f
/// https://forum.unity.com/threads/drawmeshinstancedindirect-example-comments-and-questions.446080/
/// https://github.com/noisecrime/Unity-InstancedIndirectExamples
[RequireComponent(typeof(GPUFlockManager))]
public class GPUFlockRenderer : MonoBehaviour
{
    public MeshFilter boidMesh;
    public Material boidMaterial;

    private GPUFlockManager flockManager;
    
    void Start()
    {
        flockManager = GetComponent<GPUFlockManager>();
    }

    void Update()
    {
        ComputeBuffer flock = flockManager.GetFlockBuffer();
        //Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, )
    }
}
