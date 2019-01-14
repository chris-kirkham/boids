using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool useMouseLook;

    public float moveSpeed;
    public float lookSpeedX, lookSpeedY;

    private float rotationX, rotationY;
    private Quaternion initialRotation;

    // Start is called before the first frame update
    void Start()
    {
        rotationX = 0.0f;
        rotationY = 0.0f;
        initialRotation = transform.localRotation;

    }

    // Update is called once per frame
    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Escape)) useMouseLook = !useMouseLook;
        useMouseLook = Input.GetKey(KeyCode.Mouse1);
        if (useMouseLook) transform.localRotation = initialRotation * calcMouseLook();
        transform.position += calcMovement();
    }

    Vector3 calcMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        movement = transform.TransformDirection(movement); //transform movement input so its direction is relative to the camera's rotation

        return movement * moveSpeed;

    }

    Quaternion calcMouseLook()
    {
        rotationX += Input.GetAxis("Mouse X") * lookSpeedX;
        rotationY += Input.GetAxis("Mouse Y") * lookSpeedY;

        Quaternion xQ = Quaternion.AngleAxis(rotationX, Vector3.up);
        Quaternion yQ = Quaternion.AngleAxis(rotationY, Vector3.left);

        return xQ * yQ;
    }
}
