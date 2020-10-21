using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script to attach to an object to be stored in a spatial hash.
//Adds this object to the attached SpatialHash on Start; removes it on destroy
public class HashObject : MonoBehaviour
{
    public SpatialHash hash; //hash in which to store this object

    void Start()
    {
        if (hash == null)
        {
            Debug.LogError("Trying to add this object to a null SpatialHash!");
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
