using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script to attach to an object to be stored in a spatial hash.
//Adds this object to the attached SpatialHash on Start; removes it on destroy
public class HashObject : MonoBehaviour
{
    public string hashName; //name of spatial hash object to find
    private SpatialHash hash; //hash in which to store this object

    void Start()
    {

        hash = GameObject.Find(hashName).GetComponent<SpatialHash>();

        if (hash == null)
        {
            Debug.LogError("HashObject cannot find SpatialHash by name " + hashName + "!");
        }
        else
        {
            hash.Include(this.gameObject);
        }
    }

    void OnDestroy()
    {
        hash.Remove(this.gameObject);
    }
}
