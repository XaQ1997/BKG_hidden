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
    
    [HideInInspector]
    public List<Liquid> liquids = new List<Liquid>();

    private Dictionary<int, string> blockIdToKeyMap = new Dictionary<int, string>();

    private void Awake()
    {
        // Mapowanie bloków na identyfikatory i ich prefabrykaty
        AddBlock("00", new Block(0x00, "Skała macierzysta", prefabs[0 * 16 + 0]));
        AddBlock("10", new Block(0x10, "Powietrze", prefabs[1 * 16 + 0]));

        AddBlock("20", new Block(0x20, "Kamień", prefabs[2 * 16 + 0]));

        AddBlock("20v0", new Block(0x20, "Kamień", stonePrefabs[0], 0));
        AddBlock("20v1", new Block(0x20, "Głęboki kamień", stonePrefabs[1], 1));
        AddBlock("20v2", new Block(0x20, "Mroczny kamień", stonePrefabs[2], 2));
        AddBlock("20v3", new Block(0x20, "Piekielny kamień", stonePrefabs[3], 3));
        AddBlock("20v4", new Block(0x20, "Niebiański kamień", stonePrefabs[4], 4));
        AddBlock("20v5", new Block(0x20, "Chmurzany kamień", stonePrefabs[5], 5, true));
        AddBlock("20v6", new Block(0x20, "Pusty kamień", stonePrefabs[6], 6, true));

        AddBlock("21", new Block(0x21, "Kruszony kamień", prefabs[2 * 16 + 1]));

        AddBlock("21v0", new Block(0x21, "Kruszony kamień", cobblestonePrefabs[0], 0));
        AddBlock("21v1", new Block(0x21, "Kruszony głęboki kamień", cobblestonePrefabs[1], 1));
        AddBlock("21v2", new Block(0x21, "Kruszony mroczny kamień", cobblestonePrefabs[2], 2));
        AddBlock("21v3", new Block(0x21, "Kruszony piekielny kamień", cobblestonePrefabs[3], 3));
        AddBlock("21v4", new Block(0x21, "Kruszony niebiański kamień", cobblestonePrefabs[4], 4));
        AddBlock("21v5", new Block(0x21, "Kruszony chmurzany kamień", cobblestonePrefabs[5], 5, true));
        AddBlock("21v6", new Block(0x21, "Kruszony pusty kamień", cobblestonePrefabs[6], 6, true));

        AddBlock("30", new Block(0x30, "Blok trawy", prefabs[3 * 16 + 0]));
        AddBlock("31", new Block(0x31, "Ziemia", prefabs[3 * 16 + 1]));

        AddBlock("40", new Block(0x40, "Blok cegły", prefabs[4 * 16 + 0]));
        AddBlock("41", new Block(0x41, "Glina", prefabs[4 * 16 + 1]));
        
        AddBlock("50", new Block(0x50, "Szkło", prefabs[5 * 16 + 0]));
        AddBlock("51", new Block(0x51, "Suchy piasek", prefabs[5 * 16 + 1], 0, false, true));
        AddBlock("52", new Block(0x52, "Mokry piasek", prefabs[5 * 16 + 2], 0, false, false, true));
        
        AddBlock("60", new Block(0x60, "Źródło wody", prefabs[6 * 16 + 0]));
        AddBlock("61", new Block(0x61, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("62", new Block(0x62, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("63", new Block(0x63, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("64", new Block(0x64, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("65", new Block(0x65, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("66", new Block(0x66, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("67", new Block(0x67, "Płynąca woda", prefabs[6 * 16 + 1]));
        AddBlock("68", new Block(0x68, "Spadająca woda", prefabs[6 * 16 + 1]));
        
        AddBlock("70", new Block(0x70, "Żwir", prefabs[7 * 16 + 0], 0, false, true));
        
        AddBlock("80", new Block(0x80, "Drewno", prefabs[8 * 16 + 0]));
        AddBlock("81", new Block(0x81, "Liść", prefabs[8 * 16 + 1]));
        
        AddLiquid(new Liquid(0x00, "Woda", blockMap["60"].BlockPrefab, blockMap["61"].BlockPrefab, 8));
    }

    private void AddBlock(string key, Block block)
    {
        blockMap[key] = block;
        blockIdToKeyMap[block.BlockId] = key;
    }
    
    private void AddLiquid(Liquid liquid)
    {
        liquids.Add(liquid);
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
    public bool IsPassable;
    public bool HasFullGravity;
    public bool HasHalfGravity;

    public Block(int id, string name, GameObject prefab, int variant=0, bool passable=false, bool fullGravity=false, bool halfGravity = false)
    {
        BlockId = id;
        VariantId = variant;
        BlockName = name;
        BlockPrefab = prefab;
        IsPassable = passable;
        HasFullGravity = fullGravity;
        HasHalfGravity = fullGravity || halfGravity;
    }
}

public struct Liquid
{
    public int LiquidId;
    public string LiquidName;
    public GameObject LiquidSourcePrefab;
    public GameObject LiquidPrefab;
    public int LiquidRange;
    
    public Liquid(int id, string name, GameObject source, GameObject prefab, int range)
    {
        LiquidId = id;
        LiquidName = name;
        LiquidSourcePrefab = source;
        LiquidPrefab = prefab;
        LiquidRange = range;
    }
}
