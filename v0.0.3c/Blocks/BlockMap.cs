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
        blockMap[0x00] = new Block(0x00, "Ska³a macierzysta", prefabs[0*16+0]);
        blockMap[0x10] = new Block(0x10, "Powietrze", prefabs[1*16+0]);
        blockMap[0x20] = new Block(0x20, "Kamieñ", prefabs[2*16+0]);
        blockMap[0x21] = new Block(0x21, "Kruszony kamieñ", prefabs[2 * 16 + 1]);
        blockMap[0x30] = new Block(0x30, "Blok trawy", prefabs[3*16+0]);
        blockMap[0x31] = new Block(0x31, "Ziemia", prefabs[3*16+1]);
        blockMap[0x40] = new Block(0x40, "Blok ceg³y", prefabs[4 * 16 + 0]);
        blockMap[0x50] = new Block(0x50, "Szk³o", prefabs[5 * 16 + 0]);
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
