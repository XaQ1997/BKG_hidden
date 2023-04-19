using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private GameObject[,,] blocks;

    [SerializeField] private Vector3Int mapSize = new Vector3Int(32, 128, 32);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(16, 0, 16);

    // Start is called before the first frame update
    private void Awake()
    {
        var blockMap = this.gameObject.GetComponent<BlockMap>();

        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];

        InitGame(blockMap.blockMap);
    }

    public void InitGame(Dictionary<int, Block> blockMap)
    {
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        gameSettings.Spawn();

        for (int y = 0; y < mapSize.y; ++y)
        {
            for (int x = 0; x < mapSize.x; ++x)
            {
                for (int z = 0; z < mapSize.z; ++z)
                {
                    GameObject blockPrefab;

                    if (y == 0)
                        blockPrefab = blockMap[0x00].BlockPrefab;
                    else if (y < 60)
                        blockPrefab = blockMap[0x20].BlockPrefab;
                    else if (y < 64)
                        blockPrefab = blockMap[0x31].BlockPrefab;
                    else if (y == 64)
                        blockPrefab = blockMap[0x30].BlockPrefab;
                    else
                        blockPrefab = blockMap[0x10].BlockPrefab;

                    blocks[x, y, z] = Instantiate(blockPrefab, new Vector3Int(x - mapOffset.x, y - mapOffset.y, z - mapOffset.z), Quaternion.identity);
                }
            }
        }
    }

    public void CleanMap()
    {
        foreach (var block in blocks)
            Destroy(block);
    }    

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject[,,] Blocks()
    {
        return blocks;
    }

    public Vector3Int MapOffset()
    {
        return mapOffset;
    }

    public Vector3Int MapSize()
    {
        return mapSize;
    }
}
