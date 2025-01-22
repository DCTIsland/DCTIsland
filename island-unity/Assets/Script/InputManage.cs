using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class InputManage : MonoBehaviour
{
    public IslandManage islandManage;

    public void InputTest()
    {
        int rndID = Random.Range(0, 100);
        int rndType = Random.Range(0, 5);
        islandManage.thread_id = $"test{rndID}";
        islandManage.islandBase = (IslandManage.IslandType)rndType;
        islandManage.aiObjName = "";
        
        islandManage.LoadIsland();
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
