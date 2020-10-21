using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpatialHash : MonoBehaviour
{
    public float cellSizeX, cellSizeY, cellSizeZ;
    public int initNumCellsX, initNumCellsY, initNumCellsZ;

    public abstract void Include(GameObject obj);
    
    public abstract void Remove(GameObject obj);

    public abstract int GetIncludedObjsCount();
}
