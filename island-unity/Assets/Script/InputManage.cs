using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class InputManage : MonoBehaviour
{
    public IslandManage islandManage;
    //public GenerateAIObj generateAIObj;

    public void InputTest()
    {
        int rndID = Random.Range(0, 100);
        int rndType = Random.Range(0, 5);
        islandManage.id = $"test{rndID}";
        islandManage.thread_id = $"test{rndID}";
        islandManage.islandBase = (IslandType)rndType;
        islandManage.mascotTexName = "";
        //generateAIObj.prompt = "mouse";
        
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
