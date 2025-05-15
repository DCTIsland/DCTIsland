using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusCamController : MonoBehaviour
{
    public bool allowMove = false;
    float horizontalInput;
    float verticalInput;
    float speed = 1f;

    float yaw = 180f;
    public float mouseSensitivity = 3f;

    void Update()
    {
        if (!allowMove)
            return;

        //rotation
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            transform.rotation = Quaternion.Euler(35, yaw, 0);
        }

        //movement
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if (horizontalInput != 0 && verticalInput != 0)
        {
            horizontalInput *= 0.7f;
            verticalInput *= 0.7f;
        }

        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput);
        Vector3 moveDirection = Quaternion.Euler(0f, yaw, 0f) * inputDirection.normalized;
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}
