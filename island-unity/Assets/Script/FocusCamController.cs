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
    float mouseSensitivity = 3f;

    public bool inReturn = false;
    public Transform returnTarget;

    void Update()
    {
        if (!allowMove)
            return;

        if (inReturn)
        {
            ReturnToPos();
        }
        else
        {
            Move();
        }

    }

    void Move()
    {
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

    void ReturnToPos()
    {
        Vector3 targetPos = returnTarget.position + new Vector3(0, 0.8f, 1f);
        Quaternion targetRot = Quaternion.Euler(35, 180, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);


        if (Vector3.Distance(transform.position, targetPos) < 0.01f &&
            Quaternion.Angle(transform.rotation, targetRot) < 0.5f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
            yaw = 180f;
            inReturn = false;
        }
    }

    public void StartReturnPos(GameObject target)
    {
        returnTarget = target.transform;
        inReturn = true;
    }
}