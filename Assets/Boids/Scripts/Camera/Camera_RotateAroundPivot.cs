using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_RotateAroundPivot : MonoBehaviour
{
    public GameObject pivotPoint;
    public float moveSpeed;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(pivotPoint.transform.position, transform.up, -ControlInputs.Instance.moveHorizontal * moveSpeed * Time.deltaTime);
        transform.RotateAround(pivotPoint.transform.position, transform.right, ControlInputs.Instance.moveVertical * moveSpeed * Time.deltaTime);
    }

}
