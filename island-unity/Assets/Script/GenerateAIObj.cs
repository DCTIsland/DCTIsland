using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

public class GenerateAIObj : MonoBehaviour
{
    public string prompt;
    int steps = 64;
    int cfg = 15;
    string invoice = "IN010300192332";
    string modelName;
    string format = "FBX";
    string directoryPath = "Assets/Resources/Models";

    void OverwriteCheck()
    {
        string filePath = Path.Combine(directoryPath, modelName);
        int suffixNumber = 1;

        if (modelName[modelName.Length - 2] == '_')
            while (File.Exists(filePath + "." + format))
            {
                modelName = modelName.Remove(modelName.Length - 1, 1) + suffixNumber;
                filePath = Path.Combine(directoryPath, modelName);
                suffixNumber++;
            }

        if (File.Exists(filePath + "." + format))
            modelName += "_1";

        if (modelName[modelName.Length - 2] == '_')
            while (File.Exists(filePath + "." + format))
            {
                modelName = modelName.Remove(modelName.Length - 1, 1) + suffixNumber;
                filePath = Path.Combine(directoryPath, modelName);
                suffixNumber++;
            }
    }

    IEnumerator PostShapE(string url, string bodyJsonString, Action<string> callback)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
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
                string filePath = Path.Combine(Application.persistentDataPath, $"{modelName}.{format}");

                File.WriteAllBytes(filePath, modelData);
                File.WriteAllBytes($"Assets/Resources/Models/{modelName}.{format}", modelData);
                Debug.Log($"<color=green>Inference Successful: </color>Please find the model in the {directoryPath}");
                AssetDatabase.Refresh();

                callback?.Invoke(modelName); 
            }
        }

        request.Dispose();
    }

    public void GenAIobj(Action<string> loadObj)
    {
        prompt = Regex.Replace(prompt, @"\\(?!n|"")", "");
        prompt = Regex.Replace(prompt, "(?<!n)\n", "\\n");
        prompt = Regex.Replace(prompt, "(?<!\\\\)\"", "\\\"");
        modelName = prompt.Replace(" ", "_");

        OverwriteCheck();
        Debug.Log("{\"prompt\":\"" + $"{prompt}" + "\",\"steps\":\"" + $"{steps}" + "\",\"cfg\":\"" + $"{cfg}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"fileFormat\":\"" + $"{format}" + "\"}");
        this.StartCoroutine(PostShapE($"https://nejnwmcwvhcax9-5000.proxy.runpod.net/data", "{\"prompt\":\"" + $"{prompt}" + "\",\"steps\":\"" + $"{steps}" + "\",\"cfg\":\"" + $"{cfg}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"fileFormat\":\"" + $"{format}" + "\"}", loadObj));
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
