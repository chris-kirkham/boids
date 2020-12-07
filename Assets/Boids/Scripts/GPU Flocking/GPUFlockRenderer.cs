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
/// https://github.com/tiiago11/Unity-InstancedIndirectExamples/tree/master/Demos-DrawMeshInstancedIndirect/Assets/InstancedIndirectCompute
[RequireComponent(typeof(GPUFlockManager))]
public class GPUFlockRenderer : MonoBehaviour
{
    public Mesh boidMesh;
    public Material boidInstanceMaterial;

    private GPUFlockManager flockManager;
    private int flockSize;

    //"Buffer with arguments, bufferWithArgs, has to have five integer numbers at given argsOffset offset: index count per instance,
    //instance count, start index location, base vertex location, start instance location." - Sun Tzu
    private ComputeBuffer argsBuffer;
    private uint[] args;

    private ComputeBuffer boidPositions;
    private ComputeBuffer boidForwardDirs;

    private Bounds bounds;

    void Start()
    {
        flockManager = GetComponent<GPUFlockManager>();
        flockSize = flockManager.GetFlockSize();

        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        args = new uint[5] { 0, 0, 0, 0, 0 };

        boidPositions = new ComputeBuffer(flockSize, sizeof(float) * 4);
        boidForwardDirs = new ComputeBuffer(flockSize, sizeof(float) * 3);

        bounds = new Bounds(transform.position, Vector3.one * 100000f);
    }

    void Update()
    {
        boidInstanceMaterial.SetBuffer("boidPositions", boidPositions);
        boidInstanceMaterial.SetBuffer("boidForwardDirs", boidForwardDirs);

        //args
        uint numIndices = (boidMesh != null) ? (uint)boidMesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)flockManager.GetFlockSize();
        argsBuffer.SetData(args);

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidInstanceMaterial, bounds, argsBuffer);
    }

    private void OnDisable()
    {
        if(argsBuffer != null) argsBuffer.Release();
        if(boidPositions != null) boidPositions.Release();
        if(boidForwardDirs != null) boidForwardDirs.Release();
    }

    public ComputeBuffer GetBoidPositionsBuffer()
    {
        return boidPositions;
    }

    public void SetBoidPositionsBuffer(ComputeBuffer boidPositions)
    {
        this.boidPositions = boidPositions;
    }

    public ComputeBuffer GetBoidForwardDirsBuffer()
    {
        return boidForwardDirs;
    }
}
