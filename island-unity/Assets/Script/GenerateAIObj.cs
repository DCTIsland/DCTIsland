using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class GenerateAIObj : MonoBehaviour
{
    string prompt = "";
    string textToMeshID = "nejnwmcwvhcax9";
    int steps = 64;
    int cfg = 15;
    string invoice = "IN010300192332";
    string modelName;
    int format = 0; //.FBX
    string directoryPath = "Assets/Shap-E/Models";
    bool postFlag;
    int postProgress;

    IEnumerator Post(string url, string bodyJsonString)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        postProgress = 1;
        postFlag = false;
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("There was an error in generating the model. \nPlease check your invoice/order number and try again or check the troubleshooting section in the documentation for more information." + "\nInfo: " + request.result + "\nError Code: " + request.responseCode);
        }
        else
        {
            if (request.downloadHandler.text == "Invalid Response")
                Debug.Log("Invalid Invoice/Order Number. Please check your invoice/order number and try again");

            else if (request.downloadHandler.text == "Limit Reached")
                Debug.Log("It seems that you may have reached the limit. To check your character usage, please click on the Status button. Please wait until the 1st of the next month to get a renewed character count. Thank you for using Shap-E for Unity.");
            else
            {
                byte[] modelData = Convert.FromBase64String(request.downloadHandler.text);
                File.WriteAllBytes($"Assets/Shap-E/Models/{modelName}.{format}", modelData);
                Debug.Log($"<color=green>Inference Successful: </color>Please find the model in the {directoryPath}");
                //Selection.activeObject = (UnityEngine.Object)AssetDatabase.LoadAssetAtPath($"Assets/Shap-E/Models/{modelName}.{format}", typeof(UnityEngine.Object));
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        this.StartCoroutine($"https://{textToMeshID}-5000.proxy.runpod.net/data", "{\"prompt\":\"" + $"{prompt}" + "\",\"steps\":\"" + $"{steps}" + "\",\"cfg\":\"" + $"{cfg}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"fileFormat\":\"" + $"{format}" + "\"}");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
