using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LoadModelRemote : MonoBehaviour
{
    GameObject AIobj;
    public Material vertex;
    
    // Start is called before the first frame update
    void Start()
    {
        Addressables.LoadAssetAsync<GameObject>("Assets/Art/Model/cat.FBX").Completed += OnAssetLoad;
    }

    void OnAssetLoad(AsyncOperationHandle<GameObject> obj){
        AIobj = Instantiate(obj.Result, Vector3.zero, Quaternion.identity);
        AIobj.name = obj.Result.name;
        AIobj.GetComponent<MeshRenderer>().material = vertex;
        Debug.Log("generate obj " + obj.Result.name);
    }
}
