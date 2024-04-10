using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMap : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs = new GameObject[256];

    [HideInInspector]
    public Dictionary<string, Block> blockMap = new Dictionary<string, Block>();

    private void Awake()
    {
        blockMap["00"] = new Block(0x00, "Ska�a macierzysta", prefabs[0*16+0]);
        blockMap["10"] = new Block(0x10, "Powietrze", prefabs[1*16+0]);

        blockMap["20"] = new Block(0x20, "Kamie�", prefabs[2 * 16 + 0]);
        blockMap["21"] = new Block(0x21, "Kruszony kamie�", prefabs[2 * 16 + 1]);
        blockMap["22"] = new Block(0x22, "G��boki kamie�", prefabs[2 * 16 + 2]);
        blockMap["23"] = new Block(0x23, "Mroczny kamie�", prefabs[2 * 16 + 3]);
        blockMap["24"] = new Block(0x24, "Kruszony g��boki kamie�", prefabs[2 * 16 + 4]);
        blockMap["25"] = new Block(0x25, "Kruszony mroczny kamie�", prefabs[2 * 16 + 5]);
        blockMap["26"] = new Block(0x26, "Piekielny kamie�", prefabs[2 * 16 + 6]);
        blockMap["27"] = new Block(0x27, "Niebia�ski kamie�", prefabs[2 * 16 + 7]);
        blockMap["28"] = new Block(0x28, "Kruszony piekielny kamie�", prefabs[2 * 16 + 8]);
        blockMap["29"] = new Block(0x29, "Kruszony niebia�ski kamie�", prefabs[2 * 16 + 9]);

        blockMap["30"] = new Block(0x30, "Blok trawy", prefabs[3*16+0]);
        blockMap["31"] = new Block(0x31, "Ziemia", prefabs[3*16+1]);

        blockMap["40"] = new Block(0x40, "Blok ceg�y", prefabs[4 * 16 + 0]);
        blockMap["50"] = new Block(0x50, "Szk�o", prefabs[5 * 16 + 0]);
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
