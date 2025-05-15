using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusCamController : MonoBehaviour
{
    public bool allowMove = false;
    float horizontalInput;
    float verticalInput;
    float speed = 1f;

    void Update()
    {
        if(!allowMove)
            return;
        
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if(horizontalInput != 0 && verticalInput != 0)
        {
            horizontalInput *= 0.7f;
            verticalInput *= 0.7f;
        }

        Vector3 moveDirection = new Vector3(-horizontalInput, 0, -verticalInput);
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}
