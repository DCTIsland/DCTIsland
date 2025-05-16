using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class VCamManager : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera WholeCamera;
    [SerializeField] CinemachineVirtualCamera FocusCamera;

    public void ToWholeCamera()
    {
        FocusCamera.GetComponent<FocusCamController>().allowMove = false;

        WholeCamera.Priority = 20;
        FocusCamera.Priority = 10;
    }

    public void ToFocusCamera(GameObject island)
    {
        Debug.Log($"focus on x: {island.transform.position.x}, y: {island.transform.position.y}, z: {island.transform.position.z}");

        FocusCamera.transform.position = island.transform.position + new Vector3(0, 0.8f, 1f);
        FocusCamera.transform.rotation = Quaternion.Euler(35, 180, 0);
        //FocusCamera.Follow = island.transform;
        //FocusCamera.LookAt = island.transform;

        WholeCamera.Priority = 10;
        FocusCamera.Priority = 20;
        
        FocusCamera.GetComponent<FocusCamController>().allowMove = true;
    }
}
