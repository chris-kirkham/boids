using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed;
    private Quaternion initialRotation;
    
    // Start is called before the first frame update
    void Start()
    {
        initialRotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (ControlInputs.Instance.useMouseLook) transform.localRotation = initialRotation * calcMouseLook();
        transform.position += calcMovement();
    }

    Vector3 calcMovement()
    {
        float moveHorizontal = ControlInputs.Instance.moveHorizontal;
        float moveVertical = ControlInputs.Instance.moveVertical;

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        movement = transform.TransformDirection(movement); //transform movement input so its direction is relative to the camera's rotation

        return movement * moveSpeed;
    }

    Quaternion calcMouseLook()
    {
        Quaternion xQ = Quaternion.AngleAxis(ControlInputs.Instance.rotationX, Vector3.up);
        Quaternion yQ = Quaternion.AngleAxis(ControlInputs.Instance.rotationY, Vector3.left);

        return xQ * yQ;
    }
}
