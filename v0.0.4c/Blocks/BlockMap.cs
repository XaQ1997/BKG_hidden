using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMap : MonoBehaviour
{
    [SerializeField] GameObject[] prefabs = new GameObject[256];
    [SerializeField] GameObject[] stonePrefabs = new GameObject[16];
    [SerializeField] GameObject[] cobblestonePrefabs = new GameObject[16];

    [HideInInspector]
    public Dictionary<string, Block> blockMap = new Dictionary<string, Block>();

    private Dictionary<int, string> blockIdToKeyMap = new Dictionary<int, string>();

    private void Awake()
    {
        // Mapowanie blok�w na identyfikatory i ich prefabrykaty
        AddBlock("00", new Block(0x00, "Ska�a macierzysta", prefabs[0 * 16 + 0]));
        AddBlock("10", new Block(0x10, "Powietrze", prefabs[1 * 16 + 0]));

        AddBlock("20", new Block(0x20, "Kamie�", prefabs[2 * 16 + 0]));

        AddBlock("20v0", new Block(0x20, "Kamie�", stonePrefabs[0], 0));
        AddBlock("20v1", new Block(0x20, "G�eboki kamie�", stonePrefabs[1], 1));
        AddBlock("20v2", new Block(0x20, "Mroczny kamie�", stonePrefabs[2], 2));
        AddBlock("20v3", new Block(0x20, "Piekielny kamie�", stonePrefabs[3], 3));
        AddBlock("20v4", new Block(0x20, "Niebia�ski kamie�", stonePrefabs[4], 4));

        AddBlock("21", new Block(0x21, "Kruszony kamie�", prefabs[2 * 16 + 1]));

        AddBlock("21v0", new Block(0x21, "Kruszony kamie�", cobblestonePrefabs[0], 0));
        AddBlock("21v1", new Block(0x21, "Kruszony g��boki kamie�", cobblestonePrefabs[1], 1));
        AddBlock("21v2", new Block(0x21, "Kruszony mroczny kamie�", cobblestonePrefabs[2], 2));
        AddBlock("21v3", new Block(0x21, "Kruszony piekielny kamie�", cobblestonePrefabs[3], 3));
        AddBlock("21v4", new Block(0x21, "Kruszony niebia�ski kamie�", cobblestonePrefabs[4], 4));

        AddBlock("30", new Block(0x30, "Blok trawy", prefabs[3 * 16 + 0]));
        AddBlock("31", new Block(0x31, "Ziemia", prefabs[3 * 16 + 1]));

        AddBlock("40", new Block(0x40, "Blok ceg�y", prefabs[4 * 16 + 0]));
        AddBlock("50", new Block(0x50, "Szk�o", prefabs[5 * 16 + 0]));
    }

    private void AddBlock(string key, Block block)
    {
        blockMap[key] = block;
        blockIdToKeyMap[block.BlockId] = key;
    }

    public GameObject[] Prefabs()
    {
        List<GameObject> result = new List<GameObject>();

        for (int i = 0; i < 256; ++i)
            result.Add(prefabs[i]);

        for (int i = 0; i < 16; ++i)
            result.Add(stonePrefabs[i]);

        for (int i = 0; i < 16; ++i)
            result.Add(cobblestonePrefabs[i]);

        return result.ToArray();
    }

    public string BlockId(int id)
    {
        if (blockIdToKeyMap.TryGetValue(id, out string key))
        {
            return key;
        }

        return null;
    }
}

public struct Block
{
    public int BlockId;
    public int VariantId;
    public string BlockName;
    public GameObject BlockPrefab;

    public Block(int id, string name, GameObject prefab, int variant=0)
    {
        BlockId = id;
        VariantId = variant;
        BlockName = name;
        BlockPrefab = prefab;
    }
}
