using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using UnityEngine;
using Unity.Collections;

//based on https://unionassets.com/blog/spatial-hashing-295
public class SpatialHashDOTS : SpatialHash
{
    //class to store list of objects in each cell, plus extra cell info
    private struct BlittableBoidCell
    {
        //private NativeList<Vector3> positions;
        //private NativeList<Vector3> velocities;
        private NativeList<Boid_Blittable> boids;
        private const int INIT_SIZE = 1000;

        private const float emptyTimeout = 100f; //time in seconds to wait before deleting an empty cell. Resets if an object enters the cell
        private float timeout;

        public BlittableBoidCell(GameObject obj)
        {
            //positions = new NativeList<Vector3>(INIT_SIZE, Allocator.Persistent);
            //velocities = new NativeList<Vector3>(INIT_SIZE, Allocator.Persistent);
            //positions.Add(obj.transform.position);
            //velocities.Add(obj.GetVelocity());

            boids = new NativeList<Boid_Blittable>(INIT_SIZE, Allocator.Persistent);
            timeout = emptyTimeout;

            boids.Add(new Boid_Blittable(obj.transform.position, obj.GetComponent<Rigidbody>().velocity));
        }

        public BlittableBoidCell(List<GameObject> objs)
        {
            //positions = new NativeList<Vector3>(Math.Max(INIT_SIZE, objs.Count), Allocator.Persistent);
            //velocities = new NativeList<Vector3>(Math.Max(INIT_SIZE, objs.Count), Allocator.Persistent);
            boids = new NativeList<Boid_Blittable>(INIT_SIZE, Allocator.Persistent);
            
            timeout = emptyTimeout;

            for (int i = 0; i < objs.Count; i++) boids.Add(GetObjPosVel(objs[i]));
        }

        /*
        public NativeArray<Vector3> GetObjPositions()
        {
            return positions;
        }

        public NativeArray<Vector3> GetObjVelocities()
        {
            return positions;
        }
        */

        public NativeList<Boid_Blittable> GetBoids()
        {
            return boids;
        }

        public void Add(GameObject obj)
        {
            boids.Add(GetObjPosVel(obj));
        }

        public void Clear()
        {
            //positions.Clear();
            //velocities.Clear();
            boids.Clear();
        }

        //If this cell is empty, decrement its timeout counter; if not, reset its timeout
        public void UpdateTimeout(float timePassed)
        {
            if (boids.Length > 0 )
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
            return boids.Length == 0;
        }


        //utility
        private Boid_Blittable GetObjPosVel(GameObject obj)
        {
            return new Boid_Blittable(obj.transform.position, obj.GetComponent<Rigidbody>().velocity);
        }

    }

    public float updateInterval; //interval in seconds between hash updates. 0 = update every frame

    //false = add to cells by transform.position (faster, good for very small objects), true = add to cells by AABB (slower, more accurately represents large objects)
    public bool useAABB;

    //private NativeHashMap<Vector3Int, BlittableBoidCell> cells; //<key, bucket> dictionary representing each spatial cell's key and its contents
    private Dictionary<Vector3Int, BlittableBoidCell> cells;

    //objects to be put into cells by the spatial hash algorithm; each GameObject with a HashObject script (which points to the GameObject with this script) 
    //will add itself to includedObjs on Start() and remove itself on OnDestroy()
    //N.B. must initialise this in Awake() not Start() or there is no guarantee it will be initialised before any HashObjects try to add themselves to it
    private HashSet<GameObject> includedObjs; //https://stackoverflow.com/questions/150750/hashset-vs-list-performance

    void Awake()
    {
        includedObjs = new HashSet<GameObject>();

        //initialise dictionary with initial capacity = number of initial cells
        //cells = new NativeHashMap<Vector3Int, BlittableBoidCell>(initNumCellsX * initNumCellsY * initNumCellsZ, Allocator.Persistent);
        cells = new Dictionary<Vector3Int, BlittableBoidCell>(initNumCellsX * initNumCellsY * initNumCellsZ);

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
                    cells.Add(key, new BlittableBoidCell()); //initialise empty cell
                }
            }
        }

        StartCoroutine(UpdateHashCoroutine());
    }

    void Update()
    {
        /*
        //delete timed-out cells
        List<Vector3Int> cellsToRemove = new List<Vector3Int>();

        NativeKeyValueArrays<Vector3Int, BlittableBoidCell> cellsKV = cells.GetKeyValueArrays(Allocator.Temp);
        for(int i = 0; i < cellsKV.Length; i++)
        {
            item.Value.UpdateTimeout(Time.deltaTime);
            if (item.Value.IsTimedOut()) cellsToRemove.Add(item.Key);
        }

        foreach (Vector3Int key in cellsToRemove)
        {
            cells.Remove(key);
        }
        */
    }

    private IEnumerator UpdateHashCoroutine()
    {
        while(true)
        {
            UpdateHash();
            yield return new WaitForSecondsRealtime(updateInterval);
        }
    }

    private void UpdateHash()
    {
        //Stopwatch watch = Stopwatch.StartNew();

        ClearCells();

        //watch.Stop();
        //UnityEngine.Debug.Log("time to clear cells: " + watch.ElapsedTicks + " ticks");

        if (!useAABB)
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
        if (cells.ContainsKey(key))
        {
            cells[key].Add(obj);
        }
        else
        {
            //NB. dict[key] = value creates a new key if it doesn't already exist, dict.Add() creates a new key/value and throws an exception if it already exists
            //cells[key] = new List<GameObject> { obj };
            cells.Add(key, new BlittableBoidCell(obj));
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

    //Tries to get the list of boid positions/velocities at the given key.
    //sets posVels to the found pos/vel list and returns true if a cell exists at that key; sets it to an empty list and returns false otherwise.
    public bool TryGet(Vector3Int key, out NativeList<Boid_Blittable> posVels)
    {
        if(cells.TryGetValue(key, out BlittableBoidCell cell))
        {
            posVels = cell.GetBoids();
            return true;
        }
        else
        {
            posVels = new NativeList<Boid_Blittable>(0, Allocator.Temp);
            return false;
        }
    }

    //Tries to get the list of boid positions/velocities at the given position's key.
    //sets posVels to the found pos/vel list and returns true if a cell exists at that key; sets it to an empty list and returns false otherwise.
    public bool TryGet(Vector3 pos, out NativeList<Boid_Blittable> posVels)
    {
        return TryGet(Key(pos), out posVels);
    }

    //Tries to get the list of boid positions/velocities at the given key without returning true/false on success/failure.
    //Returns the cell's boid pos/vel list if found, or an empty list if there is no cell at the given key
    public NativeList<Boid_Blittable> Get(Vector3Int key)
    {
        return cells.ContainsKey(key) ? cells[key].GetBoids() : new NativeList<Boid_Blittable>(0, Allocator.Temp);
    }

    //Tries to get the list of boid positions/velocities at the given position's key without returning true/false on success/failure.
    //Returns the cell's boid pos/vel list if found, or an empty list if there is no cell at the given position's key
    public NativeList<Boid_Blittable> Get(Vector3 pos)
    {
        return Get(Key(pos));
    }

    //returns a list of GameObjects <= r distance from pos. Does NOT remove duplicates (big objects added to more than one bucket by AddByAABB) 
    // - this is faster, but may produce bad results if you care about duplicates (e.g. if you are counting the number of objects within r distance)
    public NativeList<Boid_Blittable> GetByRadius(Vector3 pos, float r)
    {
        NativeList<Boid_Blittable> objsInRange = new NativeList<Boid_Blittable>(100, Allocator.Temp);

        Vector3Int posKey = Key(pos); //key of cell containing pos - used for checking later

        //objects which may be within search radius (from cells that are in search radius - will always check at least the cell containing pos)
        NativeList<Boid_Blittable> objsToCheck = Get(pos);

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
        foreach (Boid_Blittable posVel in objsToCheck)
        {
            if (Vector3.SqrMagnitude(pos - (Vector3)posVel.position) <= rSqr) objsInRange.Add(posVel);
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

    /*
    //returns a list of keys in the spatial hash. This is not the cells' positions in world space, just their spatial keys
    //e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public NativeArray<Vector3Int> GetCellKeys()
    {
        return cells.GetKeyArray(Allocator.Temp);
    }

    //returns a list of cells containing objects. This is not the cells' positions in world space, just their spatial keys, e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetNonEmptyCellKeys()
    {
        List<Vector3Int> nonEmptyCells = new List<Vector3Int>();

        var cellsKV = cells.GetKeyValueArrays(Allocator.Temp);
        for(int i = 0; i < cellsKV.Length; i++)
        {
            if (!cellsKV.Values[i].IsEmpty()) nonEmptyCells.Add(cellsKV.Keys[i]);
        }

        return nonEmptyCells;
    }

    //returns a list of cells not containing objects
    public List<Vector3Int> GetEmptyCellKeys()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();

        var cellsKV = cells.GetKeyValueArrays(Allocator.Temp);
        for (int i = 0; i < cellsKV.Length; i++)
        {
            if (cellsKV.Values[i].IsEmpty()) emptyCells.Add(cellsKV.Keys[i]);
        }

        return emptyCells;
    }

    //Clears each cell. Does not delete the cells themselves
    public void ClearCells()
    {
        foreach(Vector3Int key in cells.GetKeyArray(Allocator.Temp))
        {
            cells[key].Clear();
        }
    }
    */

    //returns a list of cells in the spatial hash, i.e. returns the keys. This is NOT the cells' positions in space, just their spatial keys
    //e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetCells()
    {
        return cells.Keys.ToList();
    }

    //returns a list of cells containing objects. This is NOT the cells' positions in space, just their spatial keys, e.g (0, 0, 0), (0, 0, 1), (1, 0, 0) etc.
    public List<Vector3Int> GetNonEmptyCellKeys()
    {
        List<Vector3Int> nonEmptyCells = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, BlittableBoidCell> item in cells)
        {
            if (!item.Value.IsEmpty()) nonEmptyCells.Add(item.Key);
        }

        return nonEmptyCells;
    }

    //returns a list of cells not containing objects
    public List<Vector3Int> GetEmptyCellKeys()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, BlittableBoidCell> item in cells)
        {
            if (item.Value.IsEmpty()) emptyCells.Add(item.Key);
        }

        return emptyCells;
    }


    //Clears each cell in the cells dictionary. Does not delete the cells themselves
    public void ClearCells()
    {
        foreach (KeyValuePair<Vector3Int, BlittableBoidCell> cell in cells)
        {
            cell.Value.Clear();
        }
    }

    //Adds an object to list of objects to hash
    public override void Include(GameObject obj)
    {
        includedObjs.Add(obj);
    }

    //Removes an object from list of objects to hash and from its cell object list in the hash itself
    //N.B. objects with the HashObject script call this function when they are destroyed. It is necessary to 
    //remove the destroyed object from the hash immediately, even though it would be removed anyway on the next hash update,
    //because other scripts could try to get the now-destroyed object from the hash before it is updated
    public override void Remove(GameObject obj)
    {
        includedObjs.Remove(obj);
        Vector3Int key = Key(obj.transform.position);
        //if (cells.ContainsKey(key)) cells[key].GetObjsPosVels().Remove(obj);
        throw new NotImplementedException();
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
    public override int GetIncludedObjsCount()
    {
        return includedObjs.Count();
    }

}
