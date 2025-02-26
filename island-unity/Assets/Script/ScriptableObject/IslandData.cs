using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectData
{
    public GameObject prefab;
    public int sideLen;
}

[CreateAssetMenu(fileName = "IslandData", menuName = "ScriptableObject/IslandData", order = 0)]
public class IslandData : ScriptableObject
{
    public IslandType islandType;
    public GameObject basePrefab;
    public GameObject stagePrefab;
    public ObjectData[] objects;
}
