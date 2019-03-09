using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//helper class for (visually) debugging the spatial hash algorithm
public class SpatialHashDebug : MonoBehaviour
{
    private SpatialHash hash;
    private float cellSizeX, cellSizeY, cellSizeZ;
    private Vector3 cellSize; //convenience variable used in drawing cell gizmos (cellsizeX, cellSizeY, cellSizeZ)

    public bool drawCellOutlines, drawCellCentres, highlightActiveCells;
    public bool logNumObjsInHash;

    void Start()
    {
        hash = GetComponent<SpatialHash>();
        if (hash == null) Debug.LogError("SpatialHashDebug cannot find SpatialHash script to debug! (GetComponent<SpatialHash> == null)");

        cellSizeX = hash.cellSizeX;
        cellSizeY = hash.cellSizeY;
        cellSizeZ = hash.cellSizeZ;

        cellSize = new Vector3(cellSizeX, cellSizeY, cellSizeZ);
    }

    void Update()
    {
        if (logNumObjsInHash) Debug.Log(hash.DEBUG_GetIncludedObjsCount());
    }

    void OnDrawGizmos()
    {
        if(drawCellOutlines)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f); //semi-transparent blue

            foreach (Vector3Int cell in hash.GetCells())
            {
                Vector3 centre = new Vector3((cell.x * cellSizeX) + (cellSizeX / 2),
                    (cell.y * cellSizeY) + (cellSizeY / 2),
                    (cell.z * cellSizeZ) + (cellSizeZ / 2));

                Gizmos.DrawWireCube(centre, cellSize);
            }
        }

        if(drawCellCentres)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.8f); 

            foreach (Vector3Int cell in hash.GetCells())
            {
                Vector3 centre = new Vector3((cell.x * cellSizeX) + (cellSizeX / 2),
                    (cell.y * cellSizeY) + (cellSizeY / 2),
                    (cell.z * cellSizeZ) + (cellSizeZ / 2));

                Gizmos.DrawSphere(centre, 0.25f);
            }
        }

        if (highlightActiveCells)
        {
            Gizmos.color = Color.cyan;

            foreach (Vector3Int cell in hash.GetNonEmptyCells())
            {
                Vector3 centre = new Vector3((cell.x * cellSizeX) + (cellSizeX / 2),
                    (cell.y * cellSizeY) + (cellSizeY / 2),
                    (cell.z * cellSizeZ) + (cellSizeZ / 2));

                Gizmos.DrawWireCube(centre, cellSize);
            }
        }
    }


}
