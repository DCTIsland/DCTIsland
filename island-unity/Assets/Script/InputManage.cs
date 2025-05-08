using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class InputManage : MonoBehaviour
{
    public IslandManage islandManage;
    //public GenerateAIObj generateAIObj;

    int[] testTypeArr = new int[]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 2};
    int i = 0;

    public void InputTest()
    {
        int rndID = Random.Range(0, 100);
        string[] emotion = {"Happiness", "Sadness", "Anger", "Fear", "Disgust"};
        int rndType = Random.Range(0, emotion.Length);

        FirebaseDataThread data = new FirebaseDataThread{
            emotion = emotion[testTypeArr[i]],
            image_url = "",
            link = "",
            thread_id = $"test{rndID}",
            topic1 = "",
            topic2 = "",
            topic3 = "",
        };

        islandManage.AddToQueue($"test{rndID}", data);
        
        if(i < testTypeArr.Length - 1)
            i++;
    }
}
