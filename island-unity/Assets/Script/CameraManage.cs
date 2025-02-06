using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManage : MonoBehaviour
{
    public GameObject MainCamera;
    public GameObject RecCamera;

    void OnEnable()
    {
        MainCamera.GetComponent<Camera>().depth = 1;
        RecCamera.GetComponent<Camera>().depth = 0.1f;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
