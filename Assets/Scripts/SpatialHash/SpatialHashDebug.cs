using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//helper class for visually debugging the spatial hash algorithm
public class SpatialHashDebug : MonoBehaviour
{
    private SpatialHash hash;
    private Material lineMaterial;
    private Vector3 cellSize;

    public bool drawCellOutlines, drawCellCentres, highlightActiveCells;
    public bool logNumObjsInHash;

    void Start()
    {
        hash = GetComponent<SpatialHash>();
        if (hash == null)
        {
            Debug.LogError("SpatialHashDebug cannot find SpatialHash script to debug! (GetComponent<SpatialHash> == null)");
            cellSize = Vector3.zero;
        }
        else
        {
            cellSize = new Vector3(hash.cellSizeX, hash.cellSizeY, hash.cellSizeZ);
        }

    }

    /*
    void OnRenderObject()
    {
        if (logNumObjsInHash) Debug.Log(hash.GetIncludedObjsCount());

        //if (drawCellOutlines && highlightActiveCells) //draw empty cells in blue, draw non-empty cells in cyan
        if (ControlInputs.Instance.drawCellOutlines && ControlInputs.Instance.highlightActiveCells)
        {
            foreach (Vector3Int cell in hash.GetEmptyCellKeys()) DrawWireCube(GetCellVertices(cell), Color.blue);
            foreach (Vector3Int cell in hash.GetNonEmptyCellKeys()) DrawWireCube(GetCellVertices(cell), Color.cyan);
        }
        //else if (drawCellOutlines) //draw all cells (empty or non-empty) in blue
        else if (ControlInputs.Instance.drawCellOutlines)
        {
            foreach (Vector3Int cell in hash.GetCells()) DrawWireCube(GetCellVertices(cell), Color.blue);
        }
        //else if (highlightActiveCells) //draw non-empty cells in cyan
        else if (ControlInputs.Instance.highlightActiveCells)
        {
            foreach (Vector3Int cell in hash.GetNonEmptyCellKeys()) DrawWireCube(GetCellVertices(cell), Color.cyan);
        }
    }
    */

    //draws wire cube using GL
    void DrawWireCube(List<Vector3> verts, Color colour)
    {
        CreateLineMaterial(colour);
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.Color(colour);

        //bottom horizontal edges
        GL.Begin(GL.LINE_STRIP);
        GL.Vertex(verts[0]);
        GL.Vertex(verts[1]);
        GL.Vertex(verts[2]);
        GL.Vertex(verts[3]);
        GL.Vertex(verts[0]);

        //vertical line from (0, 0, 0) to (0, 1, 0) (do it now we're on (0, 0, 0) so we don't have to repeat unecessarily)
        GL.Vertex(verts[4]);

        //top horizontal edges
        GL.Vertex(verts[5]);
        GL.Vertex(verts[6]);
        GL.Vertex(verts[7]);
        GL.Vertex(verts[4]);
        GL.End();

        //remaining vertical lines
        GL.Begin(GL.LINES);
        GL.Vertex(verts[1]);
        GL.Vertex(verts[5]);

        GL.Vertex(verts[2]);
        GL.Vertex(verts[6]);

        GL.Vertex(verts[3]);
        GL.Vertex(verts[7]);
        GL.End();

        GL.PopMatrix();
    }

    //converts a hash's key into eight vertices of its area in world space
    List<Vector3> GetCellVertices(Vector3Int cell)
    {
        List<Vector3> vertices = new List<Vector3>();

        float x = cell.x * cellSize.x;
        float y = cell.y * cellSize.y;
        float z = cell.z * cellSize.z;

        //add each vertex of cell bounds cube (comment coords are relative vertex positions)
        //lower vertices added counterclockwise, then upper vertices added counterclockwise
        vertices.Add(new Vector3(x, y, z)); //(0, 0, 0)
        vertices.Add(new Vector3(x + cellSize.x, y, z)); //(1, 0, 0)
        vertices.Add(new Vector3(x + cellSize.x, y, z + cellSize.z)); //(1, 0, 1)
        vertices.Add(new Vector3(x, y, z + cellSize.z)); //(0, 0, 1)
        vertices.Add(new Vector3(x, y + cellSize.y, z)); //(0, 1, 0)
        vertices.Add(new Vector3(x + cellSize.x, y + cellSize.y, z)); //(1, 1, 0)
        vertices.Add(new Vector3(x + cellSize.x, y + cellSize.y, z + cellSize.z)); //(1, 1, 1)
        vertices.Add(new Vector3(x, y + cellSize.y, z + cellSize.z)); //(0, 1, 1)

        return vertices;
    }

    //create material for GL drawing
    void CreateLineMaterial(Color colour)
    {
        //move this to Start? it's called every OnPostRender in the doc examples 
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.SetColor("_Color", colour); //set _Color property of Internal-Colored shader
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }

        if(colour != lineMaterial.GetColor("_Color"))
        {
            lineMaterial.SetColor("_Color", colour); 
        }
    }


}
