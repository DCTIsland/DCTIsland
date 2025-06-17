using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class Island : MonoBehaviour
{
    struct Obj
    {
        public GameObject objPrefab;
        public Quaternion rotation;
        public Vector3 position;
    }

    //island basic info
    [SerializeField]
    string id;
    public string thread_id;
    public string[] topics;
    public IslandType islandBase;
    public Vector2Int worldPos;
    string mascotTexName;
    string mascotTexUrl;
    Obj[] islandObj = new Obj[3];

    int[,] insideMap;
    GameObject[] mascot;
    Dictionary<IslandType, IslandData> islandDic = new Dictionary<IslandType, IslandData>();

    public Snapshot snapshot;

    void Awake()
    {
        InitInsideMap();
    }

    public void SetInfo(IslandType type, Vector2Int pos, IslandSet data)
    {
        id = data.key;
        thread_id = data.value.thread_id;
        topics = new string[3]{data.value.topic1, data.value.topic2, data.value.topic3};
        islandBase = type;
        worldPos = pos;
        mascotTexUrl = mascotTexUrl = TexUrl(data.value.thread_id);
    }

    public void SetupTool(IslandData[] datas, GameObject[] m, Snapshot s)
    {
        //obj data
        foreach (var data in datas)
        {
            islandDic.Add(data.islandType, data);
        }

        //mascot prefabs
        mascot = m;

        //snapshot tool
        snapshot = s;
    }

    public IEnumerator AddAllObj()
    {
        GameObject stage = LoadStage();
        yield return StartCoroutine(LoadMascot(mascotTexUrl, stage));

        RndObjToList();
        LoadObj();

        //snapshot
        snapshot.DoTakeSnapshot(thread_id);
    }

    void InitInsideMap()
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

    string TexUrl(string filename)
    {
        return $"https://firebasestorage.googleapis.com/v0/b/dctdb-8c8ad.firebasestorage.app/o/textures%2F{filename}.png?alt=media";
    }

    List<(int, int)> FindSpace(int[,] grid, int k)
    {
        List<(int, int)> emptySpaces = new List<(int, int)>();
        for (int i = 0; i < 8 - k; i++)
        {
            for (int j = 0; j < 8 - k; j++)
            {
                if (IsEmptyRegion(grid, i, j, k) == true)
                    emptySpaces.Add((i, j));
            }
        }

        return emptySpaces;
    }

    bool IsEmptyRegion(int[,] grid, int x, int y, int k)
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

    void RndObjToList()
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
                int rnd = Random.Range(0, 100);
                if (rnd >= 50)
                {
                    objN = rnd > 70 ? tmpObjList.Count - 2 : tmpObjList.Count - 1;
                }
                else
                {
                    objN = Random.Range(0, tmpObjList.Count - 2);
                }

                tmpObjList.Remove(tmpObjList[^2]);
                tmpObjList.Remove(tmpObjList[^1]);
            }
            else
            {
                objN = Random.Range(0, tmpObjList.Count);
            }

            AddObjList(ref current, objN, insideMap, tmpObjList);

            if (tmpObjList.Count == 0)
                break;
        }
    }

    void AddObjList(ref int current, int n, int[,] map, List<ObjectData> tmpObjList)
    {
        ObjectData[] objects = islandDic[islandBase].objects;
        List<(int, int)> spaceList = FindSpace(map, objects[n].sideLen);

        if (spaceList.Count != 0)
        {
            (int, int) coord = spaceList[Random.Range(0, spaceList.Count)];
            int rotY = Random.Range(0, 360);

            islandObj[current] = new Obj
            {
                objPrefab = objects[n].prefab,
                rotation = Quaternion.Euler(0, rotY, 0),
                position = CoordToPos(coord.Item1, coord.Item2, objects[n].sideLen)
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

    Vector3 CoordToPos(int x, int y, int side, float posY = 0.7f)
    {
        float posZ = x - 4 + side * 0.5f;
        float posX = y + (-2 * y + 4) - side * 0.5f;
        return new Vector3(posX, posY, posZ);
    }

    void LoadObj()
    {
        for (int i = 0; i < 3; i++)
        {
            if (islandObj[i].objPrefab != null)
            {
                GameObject obj = Instantiate(islandObj[i].objPrefab);
                obj.transform.SetParent(transform, false);
                obj.transform.SetLocalPositionAndRotation(islandObj[i].position, islandObj[i].rotation);
            }
        }
    }

    GameObject LoadStage()
    {
        //pos
        List<(int, int)> spaceList = FindSpace(insideMap, 2);
        (int, int) coord = spaceList[Random.Range(0, spaceList.Count)];
        FillRegion(insideMap, coord.Item1, coord.Item2, 2);
        Vector3 pos = CoordToPos(coord.Item1, coord.Item2, 2, 1);

        //load
        GameObject addStage = Instantiate(islandDic[islandBase].stagePrefab);
        addStage.transform.SetParent(transform, false);
        addStage.transform.localPosition = pos;
        return addStage;
    }

    IEnumerator LoadMascot(string url, GameObject stage)
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

            //load gameobject
            GameObject addMascot = Instantiate(mascot[Random.Range(0, mascot.Length)]);
            addMascot.GetComponent<MeshRenderer>().material = mascotMat;
            addMascot.transform.SetParent(stage.transform, false);
            Debug.Log("Load Mascot with AI Texture Successful");
        }
        else
        {
            Debug.LogError("Failed to load image: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }
}
