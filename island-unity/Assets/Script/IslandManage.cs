using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandManage : MonoBehaviour
{
    public enum IslandType
    {
        concrete, desert, grass, ice, lava
    }

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

        // gen ai obj on island
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
