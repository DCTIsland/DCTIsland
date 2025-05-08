using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

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
    [Header("Island assets:")]
    public IslandData[] islandDatas;
    public GameObject[] mascot;
    Dictionary<IslandType, IslandData> islandDic = new Dictionary<IslandType, IslandData>();

    Queue<IslandSet> IslandSetQueue = new Queue<IslandSet>();
    bool isProcess = false;

    int[][] map = new int[15][];
    Dictionary<IslandType, List<Vector2Int>> islandNext = new Dictionary<IslandType, List<Vector2Int>>();

    public Snapshot snapshot;

    void Awake()
    {
        InitMap();
        SetupIslandDic();
    }

    void InitMap()
    {
        int[] rowSizes = { 6, 9, 12, 15, 18, 19, 18, 19, 18, 19, 18, 15, 12, 9, 6 };
        for (int i = 0; i < 15; i++)
        {
            map[i] = new int[rowSizes[i]];
            Array.Fill(map[i], -1);
        }

        map[6][8] = (int)IslandType.concrete;
        map[6][9] = (int)IslandType.concrete;
        map[7][8] = (int)IslandType.desert;
        map[7][9] = (int)IslandType.concrete;
        map[7][10] = (int)IslandType.grass;
        map[8][8] = (int)IslandType.ice;
        map[8][9] = (int)IslandType.lava;

        islandNext.Add(IslandType.concrete, new List<Vector2Int>() { new(5, 8), new(5, 9), new(5, 10) });
        islandNext.Add(IslandType.desert, new List<Vector2Int>() { new(6, 7), new(7, 7), new(8, 7) });
        islandNext.Add(IslandType.grass, new List<Vector2Int>() { new(6, 10), new(7, 11), new(8, 10) });
        islandNext.Add(IslandType.ice, new List<Vector2Int>() { new(8, 7), new(9, 8), new(9, 9) });
        islandNext.Add(IslandType.lava, new List<Vector2Int>() { new(7, 10), new(7, 10), new(9, 9) });
    }

    void SetupIslandDic()
    {
        foreach (var data in islandDatas)
        {
            islandDic.Add(data.islandType, data);
        }
    }

    public void AddToQueue(string key, FirebaseDataThread value)
    {
        IslandSet islandSet = new() { key = key, value = value };
        IslandSetQueue.Enqueue(islandSet);
        ProcessQueue();
    }

    void ProcessQueue()
    {
        if (isProcess || IslandSetQueue.Count == 0)
            return;

        isProcess = true;
        IslandSet data = IslandSetQueue.Dequeue();

        StartCoroutine(LoadIsland(data));
    }

    public IEnumerator LoadIsland(IslandSet data)
    {
        //gen island base
        IslandType islandBase = EmotionToIslT(data.value.emotion);
        GameObject island = Instantiate(islandDic[islandBase].basePrefab, new Vector3(0, -100, 0), Quaternion.identity);
        island.transform.SetParent(gameObject.transform);
        island.name = data.key;

        //setup island
        var islandScript = island.transform.GetComponent<Island>();
        islandScript.SetInfo(data, islandBase);
        islandScript.SetupTool(islandDatas, mascot, snapshot);

        //after snapshot
        yield return islandScript.AddAllObj();

        //to pos
        Vector3 pos = RndIslandPos(islandBase);
        island.transform.localPosition = new Vector3(pos.x, -0.5f, pos.z);
        island.transform.DOLocalMoveY(pos.y, 1);

        //next process
        isProcess = false;
        ProcessQueue();
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

    //map record whole
    //next record next possible pos
    //if next empty, random choose from other type
    Vector3 RndIslandPos(IslandType islandBase)
    {
        IslandType baseIndex = islandBase;
        List<Vector2Int> nextList = islandNext[baseIndex];
        Vector2Int rndNext;

        //choose and check exist
        while (true)
        {
            if (nextList.Count != 0)
            {
                rndNext = nextList[UnityEngine.Random.Range(0, nextList.Count)];
                if (map[rndNext.x][rndNext.y] == -1)
                    break;
                else
                    nextList.Remove(rndNext);
            }
            else
            {
                if ((int)islandBase >= 4)
                    baseIndex = IslandType.concrete;
                else
                    baseIndex++;
            }
        }

        Debug.Log($"Island x: {rndNext.x}, y: {rndNext.y}");

        //to unity pos
        float x0;
        if (rndNext.x >= 4 && rndNext.x <= 10)
        {
            x0 = rndNext.x % 2 == 0 ? -0.43f : 0;
        }
        else if (rndNext.x < 4)
        {
            x0 = ((4 - rndNext.x) * 3 + 1) * -0.43f;
        }
        else
        {
            x0 = ((rndNext.x - 10) * 3 + 1) * -0.43f;
        }

        float z = rndNext.x * 0.75f;
        float x = x0 - rndNext.y * 0.86f;
        Vector3 pos = new(x, 0, z);

        Debug.Log($"Island unity x: {x}, z: {z}");

        //update map data
        nextList.Remove(rndNext);
        map[rndNext.x][rndNext.y] = (int)islandBase;
        UpdateNext(rndNext, islandBase);

        return pos;
    }

    void UpdateNext(Vector2Int newIPos, IslandType islandBase)
    {
        List<Vector2Int> neighbors = new();
        Vector2Int[] offsets = GetOffset(newIPos);

        //offset valid
        foreach (Vector2Int offset in offsets)
        {
            int x = newIPos.x + offset.x;
            int y = newIPos.y + offset.y;

            if (x > 0 && x < 15 && y > 0 && y < map[x].Length)
            {
                neighbors.Add(new(x, y));
            }
        }

        //add neighbor to next
        foreach (Vector2Int neighbor in neighbors)
        {
            if (map[neighbor.x][neighbor.y] == -1 &&
            !islandNext[islandBase].Contains(neighbor))
            {
                islandNext[islandBase].Add(neighbor);
            }
        }
    }

    Vector2Int[] GetOffset(Vector2Int newIPos)
    {
        Vector2Int[] offsets = new Vector2Int[6];

        if (newIPos.x <= 3)
            offsets = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, -2), new(-1, -1), new(1, 1), new(1, 2) };

        else if (newIPos.x == 4)       
            offsets = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, -2), new(-1, -1), new(1, 0), new(1, 1) };
        
        else if (newIPos.x == 5 || newIPos.x == 7 || newIPos.x == 9)
            offsets = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, -1), new(-1, 0), new(1, -1), new(1, 0) };
        
        else if (newIPos.x == 6 || newIPos.x == 8)
            offsets = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, 0), new(-1, 1), new(1, 0), new(1, 1) };
        
        else if (newIPos.x == 10)
            offsets = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, 0), new(-1, 1), new(1, -2), new(1, -1) };
        
        else
            offsets = new Vector2Int[] { new(0, -1), new(0, 1), new(-1, 1), new(-1, 2), new(1, -2), new(1, -1) };
        

        return offsets;
    }

}
