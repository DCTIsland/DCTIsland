using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Networking;

public enum IslandType
{
    concrete, desert, grass, ice, lava
}

public class IslandSet
{
    public string key;
    public FirebaseDataThread value;
}

public class IslandManage : MonoBehaviour
{
    struct Obj
    {
        public GameObject objPrefab;
        public Quaternion rotation;
        public Vector3 position;
    }

    [Header("Island assets:")]
    public IslandData[] islandDatas;
    public GameObject[] mascot;

    [Header("Island set:")]
    //這坨晚點再改改
    public string id;
    public string thread_id;
    public IslandType islandBase;
    public string mascotTexName;
    public string mascotTexUrl;
    Obj[] islandObj = new Obj[3];
    public Material vertex; //unused


    Queue<IslandSet> IslandSetQueue = new Queue<IslandSet>();
    bool isProcess = false;

    Dictionary<Vector2, IslandType> islandMap = new Dictionary<Vector2, IslandType>();
    Dictionary<IslandType, List<Vector2>> islandNext = new Dictionary<IslandType, List<Vector2>>();
    Dictionary<IslandType, IslandData> islandDic = new Dictionary<IslandType, IslandData>();

    int[,] insideMap;
    public Snapshot snapshot;

    void initIslandMap()
    {
        islandMap.Add(new Vector2(0, 0), IslandType.concrete);
        islandMap.Add(new Vector2(1, -1), IslandType.concrete);
        islandMap.Add(new Vector2(-1, -1), IslandType.concrete);
        islandMap.Add(new Vector2(2, 0), IslandType.desert);
        islandMap.Add(new Vector2(-2, 0), IslandType.grass);
        islandMap.Add(new Vector2(1, 1), IslandType.ice);
        islandMap.Add(new Vector2(-1, 1), IslandType.lava);

        islandNext.Add(IslandType.concrete, new List<Vector2>() { new Vector2(2, -2), new Vector2(0, -2), new Vector2(-2, -2) });
        islandNext.Add(IslandType.desert, new List<Vector2>() { new Vector2(3, -1), new Vector2(4, 0), new Vector2(3, 1) });
        islandNext.Add(IslandType.grass, new List<Vector2>() { new Vector2(-3, -1), new Vector2(-4, 0), new Vector2(-3, 1) });
        islandNext.Add(IslandType.ice, new List<Vector2>() { new Vector2(3, 1), new Vector2(2, 2), new Vector2(0, 2) });
        islandNext.Add(IslandType.lava, new List<Vector2>() { new Vector2(-3, 1), new Vector2(-2, 2), new Vector2(0, 2) });
    }

    void initIslandDic()
    {
        foreach (var data in islandDatas)
        {
            islandDic.Add(data.islandType, data);
        }
    }

    void initInsideMap()
    {
        insideMap = new int[8, 8]
        {
            {1, 1, 1, 0, 0, 1, 1, 1},
            {1, 0, 0, 0, 0, 0, 0, 1},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 1},
            {1, 1, 1, 0, 0, 1, 1, 1}
        };
    }

    public void AddToQueue(string key, FirebaseDataThread value)
    {
        IslandSet islandSet = new IslandSet{key = key, value = value};
        IslandSetQueue.Enqueue(islandSet);
        ProcessQueue();
    }

    void ProcessQueue()
    {
        if(isProcess || IslandSetQueue.Count == 0)
            return;
        
        isProcess = true;
        IslandSet data = IslandSetQueue.Dequeue();

        id = data.key;
        thread_id = data.value.thread_id;
        islandBase = EmotionToIslT(data.value.emotion);
        mascotTexUrl = TexUrl(data.value.thread_id);
        
        StartCoroutine(LoadIsland());
    }

    IslandType EmotionToIslT(string emotion)
    {
        switch (emotion)
        {
            case "Happiness":
                return IslandType.grass;
            case "Sadness":
                return IslandType.ice;
            case "Anger":
                return IslandType.lava;
            case "Fear":
                return IslandType.desert;
            case "Disgust":
                return IslandType.concrete;
            default:
                Debug.Log("Unexpect Emotion");
                return IslandType.grass;
        }
    }

    string TexUrl(string filename)
    {
        return $"https://firebasestorage.googleapis.com/v0/b/dctdb-8c8ad.firebasestorage.app/o/textures%2F{filename}.png?alt=media";
    }

    Vector3 RndIslandPos()
    {
        List<Vector2> nextList = islandNext[islandBase];
        Vector2 rndNext;

        //choose and check exist
        while (true)
        {
            rndNext = nextList[UnityEngine.Random.Range(0, nextList.Count)];
            if (islandMap.ContainsKey(rndNext) == false)
            {
                break;
            }
            else
            {
                nextList.Remove(rndNext);
                islandNext[islandBase].Remove(rndNext);
            }
        }

        float x = rndNext.x / 2 == 0 ? rndNext.x * 0.86f : rndNext.x * 0.43f;
        float z = rndNext.y * 0.75f;
        Vector3 pos = new Vector3(x, 0, z);

        UpdIslandDic(rndNext);

        return pos;
    }

    void UpdIslandDic(Vector2 newIPos)
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

    List<(int, int)> FindSpace(int[,] grid, int k)
    {
        List<(int, int)> emptySpaces = new List<(int, int)>();
        for (int i = 0; i < 8 - k; i++)
        {
            for (int j = 0; j < 8 - k; j++)
            {
                if (isEmptyRegion(grid, i, j, k) == true)
                    emptySpaces.Add((i, j));
            }
        }

        return emptySpaces;
    }

    bool isEmptyRegion(int[,] grid, int x, int y, int k)
    {
        for (int i = x; i < x + k; i++)
        {
            for (int j = y; j < y + k; j++)
            {
                if (grid[i, j] == 1)
                    return false;
            }
        }
        return true;
    }

    void FillRegion(int[,] grid, int x, int y, int k)
    {
        Debug.Log($"Fill x: {x}, y: {y}");

        for (int i = x; i < x + k; i++)
        {
            for (int j = y; j < y + k; j++)
            {
                grid[i, j] = 1;
            }
        }
    }

    void RndObj()
    {
        List<ObjectData> tmpObjList = islandDic[islandBase].objects
            .Select(obj => new ObjectData { prefab = obj.prefab, sideLen = obj.sideLen })
            .ToList();

        bool isSpecialType = islandBase == IslandType.concrete || islandBase == IslandType.desert || islandBase == IslandType.ice;
        int current = 0;
        int objN;

        while (current < 3)
        {
            if (current == 0 && isSpecialType)
            {
                int rnd = UnityEngine.Random.Range(0, 100);
                if (rnd >= 50)
                {
                    objN = rnd > 70 ? tmpObjList.Count - 2 : tmpObjList.Count - 1;
                }
                else
                {
                    objN = UnityEngine.Random.Range(0, tmpObjList.Count - 2);
                }

                tmpObjList.Remove(tmpObjList[^2]);
                tmpObjList.Remove(tmpObjList[^1]);
            }
            else
            {
                objN = UnityEngine.Random.Range(0, tmpObjList.Count);
            }

            addObjList(ref current, objN, insideMap, tmpObjList);

            if (tmpObjList.Count == 0)
                break;
        }

        initInsideMap();
    }

    void addObjList(ref int current, int n, int[,] map, List<ObjectData> tmpObjList)
    {
        ObjectData[] objects = islandDic[islandBase].objects;
        List<(int, int)> spaceList = FindSpace(map, objects[n].sideLen);

        if (spaceList.Count != 0)
        {
            (int, int) coord = spaceList[UnityEngine.Random.Range(0, spaceList.Count)];
            islandObj[current] = new Obj
            {
                objPrefab = objects[n].prefab,
                rotation = Quaternion.identity,
                position = ObjPos(coord.Item1, coord.Item2, objects[n].sideLen)
            };

            FillRegion(map, coord.Item1, coord.Item2, objects[n].sideLen);
            current++;
        }
        else
        {
            if (n < tmpObjList.Count)
                tmpObjList.Remove(tmpObjList[n]);
        }
    }

    Vector3 ObjPos(int x, int y, int side, float posY = 0.7f)
    {
        float posZ = x - 4 + side * 0.5f;
        float posX = y + (-2 * y + 4) - side * 0.5f;
        return new Vector3(posX, posY, posZ);
    }

    void LoadObj(GameObject island)
    {
        for (int i = 0; i < 3; i++)
        {
            if (islandObj[i].objPrefab != null)
            {
                GameObject obj = Instantiate(islandObj[i].objPrefab);
                obj.transform.SetParent(island.transform, false);
                obj.transform.localPosition = islandObj[i].position;
                obj.transform.localRotation = islandObj[i].rotation;
            }
        }
    }

    Vector3 RndAiObjPos()
    {
        float x = UnityEngine.Random.Range(-1.0f, 1.0f);
        float z = UnityEngine.Random.Range(-1.0f, 1.0f);
        Vector3 pos = new Vector3(x, 3, z);
        return pos;
    }

    [System.Obsolete]
    void LoadAiObj(GameObject island, string objName)
    {
        GameObject aiobj = Resources.Load(Path.Combine("Models", objName)) as GameObject;
        GameObject instobj = Instantiate(aiobj);
        instobj.GetComponent<MeshRenderer>().material = vertex;
        instobj.AddComponent<Rigidbody>();
        instobj.AddComponent<BoxCollider>();
        instobj.transform.SetParent(island.transform, false);
        instobj.transform.localPosition = RndAiObjPos();
        instobj.name = objName;
        Debug.Log("Load AI Object Successful.");
    }

    Vector3 SetStagePos()
    {
        List<(int, int)> spaceList = FindSpace(insideMap, 2);
        (int, int) coord = spaceList[UnityEngine.Random.Range(0, spaceList.Count)];
        FillRegion(insideMap, coord.Item1, coord.Item2, 2);
        return ObjPos(coord.Item1, coord.Item2, 2, 1);
    }

    GameObject LoadStage(GameObject island, Vector3 stagePos)
    {
        GameObject addStage = Instantiate(islandDic[islandBase].stagePrefab);
        addStage.transform.SetParent(island.transform, false);
        addStage.transform.localPosition = stagePos;
        return addStage;
    }

    void LoadMascot(GameObject stage, Material mascotMat)
    {
        //material, load texture from resource
        // Material mascotMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));;
        // if (mascotTexName != null && mascotTexName != "")
        // {
        //     Texture texture = Resources.Load(Path.Combine("Textures", mascotTexName)) as Texture; 
        //     mascotMat.SetTexture("_BaseMap", texture);
        // }

        //mascot
        GameObject addMascot = Instantiate(mascot[UnityEngine.Random.Range(0, mascot.Length)]);
        addMascot.GetComponent<MeshRenderer>().material = mascotMat;
        addMascot.transform.SetParent(stage.transform, false);
        Debug.Log("Load Mascot with AI Texture Successful");
    }

    IEnumerator LoadImgFromUrl(string url, GameObject stage)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            //store png
            // string path = "Assets/Resources/Textures";
            // byte[] pngData = texture.EncodeToPNG();
            // File.WriteAllBytes($"{path}/{id}.png", pngData);
            // AssetDatabase.Refresh();

            Material mascotMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mascotMat.SetTexture("_BaseMap", texture);
            LoadMascot(stage, mascotMat);
        }
        else
        {
            Debug.LogError("Failed to load image: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    void IslandToPos(GameObject island)
    {
        Vector3 pos = RndIslandPos();
        island.transform.localPosition = new Vector3(pos.x, -0.5f, pos.z);
        island.transform.DOLocalMoveY(pos.y, 1);
    }

    public IEnumerator LoadIsland()
    {
        //gen island base
        GameObject island = Instantiate(islandDic[islandBase].basePrefab, new Vector3(0, -100, 0), Quaternion.identity);
        island.transform.SetParent(gameObject.transform);
        island.name = id;

        //ai obj
        // GenerateAIObj genai = gameObject.GetComponent<GenerateAIObj>();
        // genai.GenAIobj((aiObjName) => LoadAiObj(island, aiObjName));

        //mascot stage
        Vector3 stagePos = SetStagePos();
        GameObject stage = LoadStage(island, stagePos);

        //obj
        RndObj();
        LoadObj(island);
        Array.Clear(islandObj, 0, 3);

        //mascot texture
        yield return StartCoroutine(LoadImgFromUrl(mascotTexUrl, stage));

        //snapshot
        snapshot.DoTakeSnapshot(id, thread_id, () => IslandToPos(island));
        isProcess = false;
        ProcessQueue();
    }

    void OnEnable()
    {
        initIslandMap();
        initIslandDic();
        initInsideMap();
    }

}
