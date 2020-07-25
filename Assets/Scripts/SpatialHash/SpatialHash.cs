using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using UnityEngine;

//based on https://unionassets.com/blog/spatial-hashing-295
public class SpatialHash : MonoBehaviour
{
    //class to store list of objects in each cell, plus extra cell info
    private class Cell
    {
        private List<GameObject> objs;
        private const float emptyTimeout = 100f; //time in seconds to wait before deleting an empty cell. Resets if an object enters the cell
        private float timeout;

        public Cell()
        {
            objs = new List<GameObject>();
            timeout = emptyTimeout;
        }

        public Cell(GameObject obj)
        {
            objs = new List<GameObject> { obj };
            timeout = emptyTimeout;
        }

        public Cell(List<GameObject> objs)
        {
            this.objs = objs;
            timeout = emptyTimeout;
        }

        public List<GameObject> GetObjs()
        {
            return objs;
        }

        public void Add(GameObject obj)
        {
            objs.Add(obj);
        }

        public void Clear()
        {
            objs.Clear();
        }

        public void UpdateTimeout(float timePassed)
        {
            if (objs.Count > 0)
            {
                timeout = emptyTimeout;
            }
            else
            {
                timeout -= timePassed;
            }
        }

        public bool IsTimedOut()
        {
            return timeout <= 0;
        }

        public bool IsEmpty()
        {
            return objs.Count == 0;
        }

    }

    //public float maxWidth, maxHeight;

    public float cellSizeX, cellSizeY, cellSizeZ;
    public int initNumCellsX, initNumCellsY, initNumCellsZ;

    public float updateInterval; //interval in seconds between hash updates. 0 = update every frame
    private float updateTime; //update interval counter

    //false = add to cells by transform.position (faster, good for very small objects), true = add to cells by AABB (slower, more accurately represents large objects)
    public bool useAABB; 

    private Dictionary<Vector3Int, Cell> cells; //<key, bucket> dictionary representing each spatial cell's key and its contents
    
    //private Dictionary<Vector3Int, float> timeouts; //stores the timeout timers of each cell in the hash

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
        cells = new Dictionary<Vector3Int, Cell>(initNumCellsX * initNumCellsY * initNumCellsZ);

        //add keys for initial cells and initialise corresponding empty buckets 
        //N.B. goes from (-initNumCellsX/Y/Z / 2) to (+initNumCellsX/Y/Z / 2) so the center of the hash is at the position of the attached GameObject
        int halfCellsX = initNumCellsX / 2;
        int halfCellsY = initNumCellsY / 2;
        int halfCellsZ = initNumCellsZ / 2;

        for (int i = -halfCellsX; i < halfCellsX; ++i)
        {
            for (int j = 0; j < initNumCellsY; ++j)
            {
                for (int k = -halfCellsZ; k < halfCellsZ; ++k)
                {
                    Vector3Int key = Key(new Vector3(i * cellSizeX, j * cellSizeY, k * cellSizeZ));
                    cells.Add(key, new Cell()); //initialise empty cell
                }
            }
        }

    }

    void Update()
    {
        //delete timed-out cells
        List<Vector3Int> cellsToRemove = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, Cell> item in cells)
        {
            item.Value.UpdateTimeout(Time.deltaTime);
            if (item.Value.IsTimedOut()) cellsToRemove.Add(item.Key);
        }

        foreach (Vector3Int key in cellsToRemove)
        {
            cells.Remove(key);
        }
        
        //update cells at either set update interval or every frame
        if (updateInterval > 0) //update interval set - update every updateInterval seconds
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
        //Stopwatch watch = Stopwatch.StartNew();

        ClearCells();

        //watch.Stop();
        //UnityEngine.Debug.Log("time to clear cells: " + watch.ElapsedMilliseconds + " milliseconds");

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
    public void AddByPoint(GameObject obj)
    {
        Vector3Int key = Key(obj);
        if(cells.ContainsKey(key))
        {
            cells[key].Add(obj);
        }
        else
        {
            //NB. dict[key] = value creates a new key if it doesn't already exist, dict.Add() creates a new key/value and throws an exception if it already exists
            //buckets[key] = new List<GameObject> { obj };
            cells.Add(key, new Cell(obj));
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

    //returns a list of all objects in cells[key], or empty list if key isn't in the dictionary
    public List<GameObject> Get(Vector3Int key)
    {
        return cells.ContainsKey(key) ? cells[key].GetObjs() : new List<GameObject>();
    }
    
    //returns a list of all objects in the cell which contains pos, or empty list if pos is not a coordinate in an existing cell 
    public List<GameObject> Get(Vector3 pos)
    {
        return Get(Key(pos));
    }

    //returns a list of up to n objects in the given key's hash cell.
    //If number of objs in cell < n, returns all objects in the cell; returns an empty list if no objects in the cell or the cell does not exist 
    public List<GameObject> GetNRandom(Vector3Int key, int n)
    {
        List<GameObject> objsInCell = Get(key);
        
        if(objsInCell.Count == 0) return new List<GameObject>();

        List<GameObject> objs = new List<GameObject>(n);
        if (n < objsInCell.Count)
        {
            for (int i = 0; i < n; i++)
            {
                objs.Add(objsInCell[Random.Range(0, objsInCell.Count - 1)]);
            }
        }

        return objs;
    }
    
    //returns a list of n objects in the given position's hash cell, or an empty list if none in the cell or the cell does not exist 
    public List<GameObject> GetNRandom(Vector3 pos, int n)
    {
        return GetNRandom(Key(pos), n);
    }

    //returns a list of up to n GameObjects <= r distance from pos. Does NOT remove duplicates (big objects added to more than one bucket by AddByAABB) 
    // - this is faster, but may produce bad results if you care about duplicates (e.g. if you are counting the number of objects within r distance)
    public List<GameObject> GetNByRadius(Vector3 pos, float r, int maxToGet)
    {
        List<GameObject> objsInRange = new List<GameObject>();

        //objects which may be within search radius (from cells that are in search radius - will always check at least the cell containing pos)
        List<GameObject> objsToCheck = Get(pos);

        //If more objects in retrieved hash list(s) than are to check, get random indexes -
        //this stops all boids in this hash cell "seeing" the first n boids in the hash list and ignoring the rest.
        //It could produce duplicate seen boids, but that shouldn't visibly affect its behaviour unless it picks the same one loads of times
        float rSqr = r * r;
        if (maxToGet < objsToCheck.Count)
        {
            for (int i = 0; i < maxToGet; i++)
            {
                if (Vector3.SqrMagnitude(pos - objsToCheck[i].transform.position) <= rSqr) objsInRange.Add(objsToCheck[Random.Range(0, objsToCheck.Count - 1)]);
            }
        }
        else
        {
            for (int i = 0; i < objsToCheck.Count; i++)
            {
                if (Vector3.SqrMagnitude(pos - objsToCheck[i].transform.position) <= rSqr) objsInRange.Add(objsToCheck[i]);
            }
        }

        return objsInRange;
    }

    public List<GameObject> GetByRadius(Vector3 pos, float r)
    {
        List<GameObject> objsInRange = new List<GameObject>();

        /*
        //objects which may be within search radius from the cell containing pos and its neighbouring cells
        //this is SLOW!
        List<GameObject> objsToCheck = new List<GameObject>();
        Vector3Int key = Key(pos);
        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    objsToCheck.AddRange(Get(new Vector3Int(key.x + x, key.y + y, key.z + z)));
                }
            }
        }
        */

        //check distance on objects within cellsToCheck
        List<GameObject> objsToCheck = Get(Key(pos));
        float rSqr = r * r;
        for (int i = 0; i < objsToCheck.Count; i++)
        {
            if (Vector3.SqrMagnitude(pos - objsToCheck[i].transform.position) <= rSqr) objsInRange.Add(objsToCheck[i]);
        }

        return objsInRange;
    }

    //returns a list of GameObjects <= r distance from pos. DOES remove duplicates (big objects added to more than one bucket by AddByAABB),
    //but is slower than GetByRadius (which doesn't remove duplicates)
    //TODO: use a HashSet then convert to list? https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1?redirectedfrom=MSDN&view=netframework-4.7.2
    /*
    public List<GameObject> GetByRadiusNoDups(Vector3 pos, float r)
    {
        throw new NotImplementedException();
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

        foreach(KeyValuePair<Vector3Int, Cell> item in cells)
        {
            if (!item.Value.IsEmpty()) nonEmptyCells.Add(item.Key);
        }

        return nonEmptyCells;
    }

    //returns a list of cells not containing objects
    public List<Vector3Int> GetEmptyCells()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, Cell> item in cells)
        {
            if (item.Value.IsEmpty()) emptyCells.Add(item.Key);
        }

        return emptyCells;
    }


    //Clears each cell in the cells dictionary. Does not delete the cells themselves
    public void ClearCells()
    {
        foreach(KeyValuePair<Vector3Int, Cell> bucket in cells)
        {
            bucket.Value.Clear();
        }
    }

    //Adds an object to list of objects to hash
    public void Include(GameObject obj)
    {
        includedObjs.Add(obj);
    }

    //Removes an object from list of objects to hash and from its cell object list in the hash itself
    //N.B. objects with the HashObject script call this function when they are destroyed. It is necessary to 
    //remove the destroyed object from the hash immediately, even though it would be removed anyway on the next hash update,
    //because other scripts could try to get the now-destroyed object from the hash before it is updated
    public void Remove(GameObject obj)
    {
        includedObjs.Remove(obj);
        Vector3Int key = Key(obj.transform.position);
        if (cells.ContainsKey(key)) cells[key].GetObjs().Remove(obj);
    }

    //generate a key for the given GameObject (using its transform.position)
    private Vector3Int Key(GameObject obj)
    {
        return new Vector3Int(FastFloor(obj.transform.position.x / cellSizeX), 
            FastFloor(obj.transform.position.y / cellSizeY),
            FastFloor(obj.transform.position.z / cellSizeZ));
    }

    //generate a key for the given Vector3
    private Vector3Int Key(Vector3 pos)
    {
        return new Vector3Int(FastFloor(pos.x / cellSizeX), FastFloor(pos.y / cellSizeY), FastFloor(pos.z / cellSizeZ));
    }

    //from https://www.codeproject.com/Tips/700780/Fast-floor-ceiling-functions
    private int FastFloor(float f)
    {
        return (int)(f + 32768f) - 32768;
    }

    /*----DEBUG/VISUALISATION FUNCTIONS - PASS DEBUG DATA TO SpatialHashDebug.cs ----*/
    public int DEBUG_GetIncludedObjsCount()
    {
        return includedObjs.Count();
    }
    
}
