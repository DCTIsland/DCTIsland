using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManage : MonoBehaviour
{
    public GenerateAIObj genai;

    public void InputTest()
    {
        int rndID = Random.Range(0, 100);
        int rndType = Random.Range(0, 5);
        genai.thread_id = $"test{rndID}";
        genai.islandBase = (GenerateAIObj.IslandType)rndType;
        genai.prompt = "";
        genai.islandObj = null;

        genai.LoadIsland();
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
