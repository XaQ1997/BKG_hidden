using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMap : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs = new GameObject[256];

    [HideInInspector]
    public Dictionary<int, Block> blockMap = new Dictionary<int, Block>();

    private void Awake()
    {
        blockMap[0x00] = new Block(0x00, "Ground Block", prefabs[0*16+0]);
        blockMap[0x10] = new Block(0x10, "Air", prefabs[1*16+0]);
        blockMap[0x20] = new Block(0x20, "Stone", prefabs[2*16+0]);
        blockMap[0x30] = new Block(0x30, "Grass Block", prefabs[3*16+0]);
        blockMap[0x31] = new Block(0x31, "Dirt", prefabs[3*16+1]);
    }
}

public struct Block
{
    public int BlockId;
    public string BlockName;
    public GameObject BlockPrefab;

    public Block(int id, string name, GameObject prefab)
    {
        BlockId = id;
        BlockName = name;
        BlockPrefab = prefab;
    }
}
