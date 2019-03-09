using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//based on https://unionassets.com/blog/spatial-hashing-295
public class SpatialHash : MonoBehaviour
{   
    //public float maxWidth, maxHeight;

    public float cellSizeX, cellSizeY, cellSizeZ;
    public int initNumCellsX, initNumCellsY, initNumCellsZ;

    public float updateInterval; //interval in seconds between hash updates. 0 = update every frame
    private float updateTime; //update interval counter

    //false = add to cells by transform.position (faster, good for very small objects), true = add to cells by AABB (slower, more accurately represents large objects)
    public bool useAABB; 

    private Dictionary<Vector3Int, List<GameObject>> cells; //<key, bucket> dictionary representing each spatial cell's key and its contents
    private float emptyCellTimeout = 5f; //time in seconds to wait before deleting an empty cell. Resets if an object enters the cell

    //objects to be put into cells by the spatial hash algorithm; each GameObject with a HashObject script (which points to the GameObject with this script) 
    //will add itself to includedObjs on Start() and remove itself on OnDestroy()
    //N.B. must initialise this in Awake() not Start() or there is no guarantee it will be initialised before any HashObjects try to add themselves to it
    //TODO: explain this better
    private HashSet<GameObject> includedObjs; //https://stackoverflow.com/questions/150750/hashset-vs-list-performance

    void Awake()
    {
        updateTime = updateInterval;

        includedObjs = new HashSet<GameObject>();

        //initialise dictionary with initial capacity = number of initial cells
        cells = new Dictionary<Vector3Int, List<GameObject>>(initNumCellsX * initNumCellsY * initNumCellsZ);

        //add keys for initial cells and initialise corresponding empty buckets 
        //N.B. goes from (-initNumCellsX/Y/Z / 2) to (+initNumCellsX/Y/Z / 2) so the center of the hash is at the position of the attached GameObject
        int halfCellsX = initNumCellsX / 2;
        int halfCellsY = initNumCellsY / 2;
        int halfCellsZ = initNumCellsZ / 2;

        for (int i = -halfCellsX; i < halfCellsX; ++i)
        {
            for (int j = -halfCellsY; j < halfCellsY; ++j)
            {
                for (int k = -halfCellsZ; k < halfCellsZ; ++k)
                {
                    cells.Add(Key(new Vector3(i * cellSizeX, j * cellSizeY, k * cellSizeZ)), new List<GameObject>());
                }
            }
        }

    }

    /*
    public SpatialHash(float cellSize, int numCellsX, int numCellsY, int numCellsZ) //constructor for cubic cells
    {
        cellSizeX = cellSize;
        cellSizeY = cellSize;
        cellSizeZ = cellSize;

        initNumCellsX = numCellsX;
        initNumCellsY = numCellsY;
        initNumCellsZ = numCellsZ;

        //initialise dictionary with initial capacity = number of initial cells
        buckets = new Dictionary<Vector3Int, List<GameObject>>(numCellsX * numCellsY * numCellsZ); 
        
        //add keys for initial cells and initialise corresponding empty buckets 
        for(int i = 0; i < numCellsX; ++i)
        {
            for(int j = 0; j < numCellsY; ++j)
            {
                for(int k = 0; k < numCellsZ; ++k)
                {
                    buckets.Add(Key(new Vector3(i * cellSizeX, j * cellSizeY, k * cellSizeZ)), new List<GameObject>());
                }
            }
        }
    }

    public SpatialHash(float cellSizeX, float cellSizeY, float cellSizeZ, int numCellsX, int numCellsY, int numCellsZ) //constructor for cuboid cells
    {
        this.cellSizeX = cellSizeX;
        this.cellSizeY = cellSizeY;
        this.cellSizeZ = cellSizeZ;

        initNumCellsX = numCellsX;
        initNumCellsY = numCellsY;
        initNumCellsZ = numCellsZ;

        //initialise dictionary with initial capacity = number of initial cells
        buckets = new Dictionary<Vector3Int, List<GameObject>>(numCellsX * numCellsY * numCellsZ);

        //add keys for initial cells and initialise corresponding empty buckets 
        for(int i = 0; i < numCellsX; ++i)
        {
            for(int j = 0; j < numCellsY; ++j)
            {
                for(int k = 0; k < numCellsZ; ++k)
                {
                    buckets.Add(Key(new Vector3(i * cellSizeX, j * cellSizeY, k * cellSizeZ)), new List<GameObject>());
                }
            }
        }
    }
    */


    void Update()
    {
        if(updateInterval > 0) //update interval set - update every updateInterval seconds
        {
            updateTime -= Time.deltaTime;

            if(updateTime <= 0)
            {
                updateTime = updateInterval;
                UpdateHash();
            }
        }
        else //update interval not set - update every frame
        {
            UpdateHash();
        }
            
    }

    private void UpdateHash()
    {
        ClearBuckets();

        
        if(!useAABB)
        {
            foreach (GameObject obj in includedObjs)
            {
                AddByPoint(obj);
            }
        }
        else
        {
            foreach (GameObject obj in includedObjs)
            {
                //AddByAABB
            }
        }
        
    }

    //inserts a GameObject into the correct bucket by its transform.position only (i.e. not taking into account its size, won't be added to more than one bucket);
    //this is faster than adding an object to the bucket(s) its AABB overlaps, but will of course cause inaccurate results for big objects.
    //Use only on very small objects (like boids)
    public void AddByPoint(GameObject o)
    {
        Vector3Int key = Key(o);
        if(cells.ContainsKey(key))
        {
            cells[key].Add(o);
        }
        else
        {
            //NB. dict[key] = value creates a new key if it doesn't already exist, dict.Add() creates a new key/value and throws an exception if it already exists
            //buckets[key] = new List<GameObject> { o };
            cells.Add(key, new List<GameObject> { o });
        }
    }

    //TODO
    //Inserts a GameObject into the bucket(s) which its AABB overlaps. Copies of objects whose AABB overlaps more than one cell will
    //be added to all overlapping cells. This allows big objects to be spatially represented more accurately, but means the dictionary may contain
    //duplicate objects - use GetByRadiusNoDups if you are doing spatial checking on a dictionary containing objects added by AABB and do not want duplicates
    /*
    public void AddByAABB(GameObject o)
    {

    }
    */

    //returns a list of all objects in buckets[key], or empty list if key isn't in the dictionary
    public List<GameObject> Get(Vector3Int key)
    {
        return cells.ContainsKey(key) ? cells[key] : new List<GameObject>();
    }
    
    //returns a list of all objects in the cell which contains pos, or empty list if pos is not a coordinate in an existing cell 
    public List<GameObject> Get(Vector3 pos)
    {
        Vector3Int key = Key(pos);
        return cells.ContainsKey(key) ? cells[key] : new List<GameObject>();
    }

    //returns a list of GameObjects <= r distance from pos. Does NOT remove duplicates (big objects added to more than one bucket by AddByAABB) 
    // - this is faster, but may produce bad results if you care about duplicates (e.g. if you are counting the number of objects within r distance)
    public List<GameObject> GetByRadius(Vector3 pos, float r)
    {
        List<GameObject> objsInRange = new List<GameObject>();

        Vector3Int posKey = Key(pos); //key of cell containing pos - used for checking later
        
        //objects which may be within search radius (from cells that are in search radius - will always check at least the cell containing pos)
        List<GameObject> objsToCheck = new List<GameObject>(Get(pos));

        /* add other cells within radius, if any, to cellsToCheck */
        //left/right/up/down/back/forward
        Vector3 left = new Vector3(pos.x - r, pos.y, pos.z);
        if (Key(left) != posKey) objsToCheck.AddRange(Get(left));

        Vector3 right = new Vector3(pos.x + r, pos.y, pos.z);
        if (Key(right) != posKey) objsToCheck.AddRange(Get(right));

        Vector3 up = new Vector3(pos.x, pos.y + r, pos.z);
        if (Key(up) != posKey) objsToCheck.AddRange(Get(up));

        Vector3 down = new Vector3(pos.x, pos.y - r, pos.z);
        if (Key(down) != posKey) objsToCheck.AddRange(Get(down));

        Vector3 forward = new Vector3(pos.x, pos.y, pos.z + r);
        if (Key(forward) != posKey) objsToCheck.AddRange(Get(forward));

        Vector3 back = new Vector3(pos.x, pos.y, pos.z - r);
        if (Key(back) != posKey) objsToCheck.AddRange(Get(back));

        //diagonals


        /* check distance on objects within cellsToCheck */
        float rSqr = r * r; //check sqr magnitude to avoid sqrt calls
        foreach(GameObject obj in objsToCheck)
        {
            if (Vector3.SqrMagnitude(pos - obj.transform.position) <= rSqr) objsInRange.Add(obj);
        }

        return objsInRange;
    }

    //returns a list of GameObjects <= r distance from pos. DOES remove duplicates (big objects added to more than one bucket by AddByAABB),
    //but is slower than GetByRadius (which doesn't remove duplicates)
    //TODO: use a HashSet then convert to list? https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1?redirectedfrom=MSDN&view=netframework-4.7.2
    /*
    public List<GameObject> GetByRadiusNoDups(Vector3 pos, float r)
    {
        List<GameObject> objects = new List<GameObject>();

        return objects;
    }
    */

    //returns a list of cells in the spatial hash, i.e. returns the keys. This is NOT the cells' positions in space, just their spatial keys
    //e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetCells()
    {
        return cells.Keys.ToList();
    }

    //returns a list of cells containing objects. This is NOT the cells' positions in space, just their spatial keys, e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetNonEmptyCells()
    {
        List<Vector3Int> nonEmptyCells = new List<Vector3Int>();

        foreach(KeyValuePair<Vector3Int, List<GameObject>> item in cells)
        {
            if (item.Value.Count() > 0) nonEmptyCells.Add(item.Key);
        }

        return nonEmptyCells;
    }

    //returns a list of the world positions of cells in the spatial hash
    public List<Vector3Int> GetCellPositions()
    {
        List<Vector3Int> cellPoss =  cells.Keys.ToList();

        foreach(Vector3Int cell in cellPoss)
        {

        }

        return cellPoss;
    }

    //returns a list of the world positions of cells in the spatial hash containing objects
    public List<Vector3Int> GetNonEmptyCellPositions()
    {
        List<Vector3Int> nonEmptyCells = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, List<GameObject>> item in cells)
        {
            if (item.Value.Count() > 0) nonEmptyCells.Add(item.Key);
        }

        return nonEmptyCells;
    }

    //Clears each bucket in the buckets dictionary. Does not delete the buckets themselves
    public void ClearBuckets()
    {
        List<Vector3Int> keys = cells.Keys.ToList();

        foreach(Vector3Int key in keys)
        {
            cells[key].Clear();
        }
    }

    //Adds an object to list of objects to hash
    public void Include(GameObject obj)
    {
        includedObjs.Add(obj);
    }

    //Removes an object from list of objects to hash (NOT from any cell in the hash - this is handled when hash is updated. Remove is called by a HashObject
    //when it is destroyed)
    public void Remove(GameObject obj)
    {
        includedObjs.Remove(obj);
    }

    //generate a key for the given GameObject (using its transform.position)
    private Vector3Int Key(GameObject obj)
    {
        return new Vector3Int(Mathf.FloorToInt(obj.transform.position.x / cellSizeX), 
            Mathf.FloorToInt(obj.transform.position.y / cellSizeY),
            Mathf.FloorToInt(obj.transform.position.z / cellSizeZ));
    }

    //generate a key for the given Vector3
    private Vector3Int Key(Vector3 pos)
    {
        return new Vector3Int(Mathf.FloorToInt(pos.x / cellSizeX), Mathf.FloorToInt(pos.y / cellSizeY), Mathf.FloorToInt(pos.z / cellSizeZ));
    }

    /*
    //generate a key for the given GameObject (using its transform.position)
    //see https://unionassets.com/blog/spatial-hashing-295 for hash function
    private int Key(GameObject o)
    {
        return ((Mathf.FloorToInt(o.transform.position.x / cellSizeX) * 73856093) ^
                (Mathf.FloorToInt(o.transform.position.y / cellSizeY) * 19349663) ^
                (Mathf.FloorToInt(o.transform.position.z / cellSizeZ) * 83492791));
    }

    //generate a key for the given Vector3
    //see https://unionassets.com/blog/spatial-hashing-295 for hash function
    private int Key(Vector3 pos)
    {
        
        return ((Mathf.FloorToInt(pos.x / cellSizeX) * 73856093) ^
                (Mathf.FloorToInt(pos.y / cellSizeY) * 19349663) ^
                (Mathf.FloorToInt(pos.z / cellSizeZ) * 83492791));
    }
    */

    /*----DEBUG/VISUALISATION FUNCTIONS - PASS DEBUG DATA TO SpatialHashDebug.cs ----*/
    public int DEBUG_GetIncludedObjsCount()
    {
        return includedObjs.Count();
    }
    
}
