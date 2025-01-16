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
    public enum IslandType
    {
        concrete, desert, grass, ice, lava
    }

    [Header("Assets:")]
    public Material vertex;
    int steps = 64;
    int cfg = 15;
    string invoice = "IN010300192332";
    string modelName;
    string format = "FBX";
    string directoryPath = "Assets/Resources/Models";

    [Header("Island base:")]
    public GameObject[] islandBaseList;
    [Header("Island assets:")]
    public GameObject[] concreteObjList;
    public GameObject[] desertObjList;
    public GameObject[] grassObjList;
    public GameObject[] iceObjList;
    public GameObject[] lavaObjList;

    [Header("Island set:")]
    public string thread_id;
    public string prompt;
    public IslandType islandBase;
    public int[] islandObj;

    Dictionary<Vector2, IslandType> islandMap = new Dictionary<Vector2, IslandType>(){};
    Dictionary<IslandType, List<Vector2>> islandNext = new Dictionary<IslandType, List<Vector2>>(){};

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

    IEnumerator PostShapE(string url, string bodyJsonString)
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

                // load
                // GameObject newobj = Resources.Load(Path.Combine("Models", modelName)) as GameObject;
                // GameObject gameObject = Instantiate(newobj, Vector3.zero, Quaternion.identity);
                // gameObject.name = modelName;
                // gameObject.GetComponent<MeshRenderer>().material = vertex;
                // gameObject.AddComponent<Rigidbody>();
                Debug.Log("generate obj " + $"{modelName}");
            }
        }

        request.Dispose();
    }

    void initIslandMap()
    {
        islandMap.Add(new Vector2(0, 0), IslandType.concrete);
        islandMap.Add(new Vector2(1, -1), IslandType.concrete);
        islandMap.Add(new Vector2(-1, -1), IslandType.concrete);
        islandMap.Add(new Vector2(2, 0), IslandType.desert);
        islandMap.Add(new Vector2(-2, 0), IslandType.grass);
        islandMap.Add(new Vector2(1, 1), IslandType.ice);
        islandMap.Add(new Vector2(-1, 1), IslandType.lava);

        islandNext.Add(IslandType.concrete, new List<Vector2>() { new Vector2(2, -2),new Vector2(0, -2), new Vector2(-2, -2) });
        islandNext.Add(IslandType.desert, new List<Vector2>() { new Vector2(3, -1), new Vector2(4, 0), new Vector2(3, 1) });
        islandNext.Add(IslandType.grass, new List<Vector2>() { new Vector2(-3, -1), new Vector2(-4, 0), new Vector2(-3, 1) });
        islandNext.Add(IslandType.ice, new List<Vector2>() { new Vector2(3, 1), new Vector2(2, 2), new Vector2(0, 2) });
        islandNext.Add(IslandType.lava, new List<Vector2>() { new Vector2(-3, 1), new Vector2(-2, 2), new Vector2(0, 2) });
    }

    void GenAIobj()
    {
        prompt = Regex.Replace(prompt, @"\\(?!n|"")", "");
        prompt = Regex.Replace(prompt, "(?<!n)\n", "\\n");
        prompt = Regex.Replace(prompt, "(?<!\\\\)\"", "\\\"");
        modelName = prompt.Replace(" ", "_");

        OverwriteCheck();
        Debug.Log("{\"prompt\":\"" + $"{prompt}" + "\",\"steps\":\"" + $"{steps}" + "\",\"cfg\":\"" + $"{cfg}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"fileFormat\":\"" + $"{format}" + "\"}");
        this.StartCoroutine(PostShapE($"https://nejnwmcwvhcax9-5000.proxy.runpod.net/data", "{\"prompt\":\"" + $"{prompt}" + "\",\"steps\":\"" + $"{steps}" + "\",\"cfg\":\"" + $"{cfg}" + "\",\"invoice\":\"" + $"{invoice}" + "\",\"fileFormat\":\"" + $"{format}" + "\"}"));
    }

    Vector3 rndIslandPos()
    {
        List<Vector2> nextList = islandNext[islandBase];
        Vector2 rndNext;

        //choose and check exist
        while(true){
            rndNext = nextList[UnityEngine.Random.Range(0, nextList.Count)];
            if(islandMap.ContainsKey(rndNext) == false){
                break;
            }
            else{
                nextList.Remove(rndNext);
                islandNext[islandBase].Remove(rndNext);
            }
        }

        float x = rndNext.x / 2 == 0 ? rndNext.x * 0.86f : rndNext.x * 0.43f;
        float z = rndNext.y * 0.75f;
        Vector3 pos = new Vector3(x, 0, z);

        updIslandDic(rndNext);

        return pos;
    }

    void updIslandDic(Vector2 newIPos)
    {
        islandNext[islandBase].Remove(newIPos);
        islandMap.Add(newIPos, islandBase);

        Vector3[] neighbors = {
            new Vector3(newIPos.x + 2, newIPos.y),
            new Vector3(newIPos.x - 2, newIPos.y),
            new Vector3(newIPos.x + 1, newIPos.y + 1),
            new Vector3(newIPos.x - 1, newIPos.y + 1),
            new Vector3(newIPos.x + 1, newIPos.y - 1),
            new Vector3(newIPos.x - 1, newIPos.y - 1),
        };

        foreach (Vector3 neighbor in neighbors)
        {
            bool inMap = islandMap.ContainsKey(neighbor);
            bool inNext = islandNext[islandBase].Contains(neighbor);
            if (inMap == false && inNext == false)
            {
                islandNext[islandBase].Add(neighbor);
            }
        }
    }

    Vector3 RandomObjPos()
    {
        Vector3 pos = new Vector3(0, 0, 0);
        return pos;
    }

    public void LoadIsland()
    {
        //gen island base
        GameObject island = Instantiate(islandBaseList[(int)islandBase], rndIslandPos(), Quaternion.identity);
        island.transform.parent = gameObject.transform;
        island.name = thread_id;

        //gen ai obj on island
        // GameObject aiobj = Resources.Load(Path.Combine("Models", modelName)) as GameObject;
        // Vector3 aiobjPos = RandomObjPos();
        // GameObject instAiobj = Instantiate(aiobj, aiobjPos, Quaternion.identity);
        // instAiobj.GetComponent<MeshRenderer>().material = vertex;
        // instAiobj.transform.parent = island.transform;
        // instAiobj.name = modelName;

        //gen normal obj
    }

    // Start is called before the first frame update
    void Start()
    {
        initIslandMap();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
