using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Very simple script to hide the default cursor 
public class CursorManager : MonoBehaviour
{
    void Start()
    {
        Cursor.visible = false;
    }
}
