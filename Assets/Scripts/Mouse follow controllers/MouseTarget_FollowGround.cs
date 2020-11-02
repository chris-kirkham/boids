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

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (ControlInputs.Instance.useMouseFollow)
        {
            //raycast to try find ground
            Vector3 mousePosition = Input.mousePosition;
            if (Physics.Raycast(cam.ScreenPointToRay(mousePosition), out RaycastHit hit))
            {
                float distanceFromCamera = Vector3.Distance(cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f)), hit.point);
                mouseTarget.mouseTargetPosition = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceFromCamera)) + (hit.normal * distanceFromGround);
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
