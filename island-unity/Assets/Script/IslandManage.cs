using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

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
    public string aiObjName;
    public Material vertex;
    public IslandType islandBase;
    int[] islandObj = new int[3];

    Dictionary<Vector2, IslandType> islandMap = new Dictionary<Vector2, IslandType>();
    Dictionary<IslandType, List<Vector2>> islandNext = new Dictionary<IslandType, List<Vector2>>();
    List<GameObject[]> objList = new List<GameObject[]>();

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

    Vector3 RndIslandPos()
    {
        List<Vector2> nextList = islandNext[islandBase];
        Vector2 rndNext;

        //choose and check exist
        while (true)
        {
            rndNext = nextList[Random.Range(0, nextList.Count)];
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

    void RandomObj(IslandType island)
    {
        int rnd, n;
        switch (island)
        {
            case IslandType.concrete:
                n = 7;
                for (int i = 0; i < 3; i++)
                {
                    rnd = Random.Range(0, n);
                    if (rnd == 6)
                    {
                        islandObj[i] = Random.Range(6, 8);
                        n = 6;
                    }
                    else
                        islandObj[i] = rnd;
                }
                break;
            case IslandType.desert:
                n = 6;
                for (int i = 0; i < 3; i++)
                {
                    rnd = Random.Range(0, n);
                    if (rnd == 5)
                    {
                        islandObj[i] = Random.Range(5, 7);
                        n = 5;
                    }
                    else
                        islandObj[i] = rnd;
                }
                break;
            case IslandType.grass:
                for (int i = 0; i < 3; i++)
                {
                    islandObj[i] = Random.Range(0, 7);
                }
                break;
            case IslandType.ice:
                n = 5;
                for (int i = 0; i < 3; i++)
                {
                    rnd = Random.Range(0, n);
                    if (rnd == 4)
                    {
                        islandObj[i] = Random.Range(4, 6);
                        n = 4;
                    }
                    else
                        islandObj[i] = rnd;
                }
                break;
            case IslandType.lava:
                for (int i = 0; i < 3; i++)
                {
                    islandObj[i] = Random.Range(0, 6);
                }
                break;
            default:
                break;
        }
    }

    Vector3 RandomObjPos()
    {
        float x = Random.Range(-3.0f, 3.0f);
        float z = Random.Range(-3.0f, 3.0f);
        Vector3 pos = new Vector3(x, 0.7f, z);
        return pos;
    }

    void LoadObj(GameObject island)
    {
        objList = new List<GameObject[]>() { concreteObjList, desertObjList, grassObjList, iceObjList, lavaObjList };
        GameObject[] l = objList[(int)islandBase];

        for (int i = 0; i < 3; i++)
        {
            GameObject obj = Instantiate(l[islandObj[i]]);
            obj.transform.SetParent(island.transform, false);
            obj.transform.localPosition = RandomObjPos();
        }
    }

    Vector3 RndAiObjPos()
    {
        float x = Random.Range(-3.0f, 3.0f);
        float z = Random.Range(-3.0f, 3.0f);
        Vector3 pos = new Vector3(x, 0, z);
        return pos;
    }

    void LoadAiObj(GameObject island)
    {
        GameObject aiobj = Resources.Load(Path.Combine("Models", aiObjName)) as GameObject;
        GameObject instobj = Instantiate(aiobj);
        instobj.GetComponent<MeshRenderer>().material = vertex;
        instobj.AddComponent<Rigidbody>();
        instobj.AddComponent<BoxCollider>();
        instobj.transform.SetParent(island.transform, false);
        instobj.transform.localPosition = RndAiObjPos();
        instobj.name = aiObjName;
    }

    public void LoadIsland()
    {
        //gen island base
        GameObject island = Instantiate(islandBaseList[(int)islandBase], RndIslandPos(), Quaternion.identity);
        island.transform.parent = gameObject.transform;
        island.name = thread_id;

        //LoadAiObj(island);
        RandomObj(islandBase);
        LoadObj(island);

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
