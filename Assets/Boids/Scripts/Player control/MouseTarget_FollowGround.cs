using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the mouse target move along the ground (or a certain distance above it)
/// </summary>
[RequireComponent(typeof(Camera))]
public class MouseTarget_FollowGround : MonoBehaviour
{
    public MouseTargetPosition mouseTarget;
    public GameObject targetVisualiser; //object to use as 3D mouse cursor
    public float distanceFromGround = 0f;
    
    private Camera cam;

    //distance from camera to ground hit by camera ray. Stored as a member var so it can be used by the visualiser when the camera ray doesn't hit anything 
    private float groundDistanceFromCamera = 0f; 

    void Start()
    {
        cam = GetComponent<Camera>();
        targetVisualiser = Instantiate(targetVisualiser);
    }

    void Update()
    {
        if (ControlInputs.Instance.useMouseFollow)
        {
            //raycast to try find ground
            Vector3 mousePosition = Input.mousePosition;
            Ray camRay = cam.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(camRay, out RaycastHit hit))
            {
                groundDistanceFromCamera = Vector3.Distance(cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f)), hit.point);
                mouseTarget.mouseTargetPosition = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, groundDistanceFromCamera)) + (hit.normal * distanceFromGround);
                targetVisualiser.transform.position = hit.point - (camRay.direction * distanceFromGround);
            }
            else //if cursor doesn't hit anything, use the last valid ground distance from camera to position cursor
            {
                mouseTarget.mouseTargetPosition = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, groundDistanceFromCamera)) - (camRay.direction * distanceFromGround);
                targetVisualiser.transform.position = mouseTarget.mouseTargetPosition;
            }

            targetVisualiser.GetComponent<Renderer>().enabled = true;
        }
        else
        {
            targetVisualiser.GetComponent<Renderer>().enabled = false;
        }
    }
}
