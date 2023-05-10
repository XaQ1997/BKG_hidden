using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class MapGenerator : MonoBehaviour
{
    private GameObject[,,] blocks;

    [SerializeField] private Vector3Int mapSize = new Vector3Int(64, 256, 64);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(32, 0, 32);

    private uint seed;

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

        seed = (uint)DateTime.Now.Ticks;

        int[,] grounds = new int[mapSize.x, mapSize.z], verticalOffset = new int[mapSize.x, mapSize.z];

        for(int x=0;x<mapSize.x;++x)
            for(int z=0;z<mapSize.z;++z)
            {
                grounds[x, z] = new Random(257 * seed + (uint)(257 * x + z)).NextInt(0, 3);
                verticalOffset[x, z] = new Random(257 * seed + (uint)(257 * x + z)).NextInt(-5, 6);
            }

        for (int z = 0; z < mapSize.z; ++z)
            for (int x = 0; x < mapSize.x; ++x)
            {
                grounds[x, z] += new Random(509 * seed + (uint)(509 * z + Mathf.Pow(-1, x)*x)).NextInt(0, 3);
                verticalOffset[x, z] += new Random(509 * seed + (uint)(509 * z + Mathf.Pow(-1, x) * x)).NextInt(-5, 6);
            }

        for (int x = 0; x < mapSize.x; ++x)
            for (int z = 0; z < mapSize.z; ++z)
            {
                if (grounds[x, z] == 0)
                    grounds[x, z] = 1;

                if (Mathf.Abs(verticalOffset[x, z]) > 7)
                    verticalOffset[x, z] = (int)Mathf.Sign(verticalOffset[x, z]);
                else
                    verticalOffset[x, z] = 0;
            }

        for (int y = 0; y < mapSize.y; ++y)
            for (int x = 0; x < mapSize.x; ++x)
            {
                int offset = 0;

                if (x > 0)
                    offset = verticalOffset[x - 1, 0];

                for (int z = 0; z < mapSize.z; ++z)
                {
                    offset += verticalOffset[x, z];

                    if (y < grounds[x, z])
                        blocks[x, y, z] = Instantiate(blockMap[0x00].BlockPrefab, new Vector3(x - mapOffset.x, y - mapOffset.y, z - mapOffset.z), Quaternion.identity);
                    else if (y < grounds[x, z] + offset + 91)
                        blocks[x, y, z] = Instantiate(blockMap[0x20].BlockPrefab, new Vector3(x - mapOffset.x, y - mapOffset.y, z - mapOffset.z), Quaternion.identity);
                    else if (y < grounds[x, z] + offset + 94)
                        blocks[x, y, z] = Instantiate(blockMap[0x31].BlockPrefab, new Vector3(x - mapOffset.x, y - mapOffset.y, z - mapOffset.z), Quaternion.identity);
                    else if (y == grounds[x, z] + offset + 94)
                        blocks[x, y, z] = Instantiate(blockMap[0x30].BlockPrefab, new Vector3(x - mapOffset.x, y - mapOffset.y, z - mapOffset.z), Quaternion.identity);
                }
            }

        gameSettings.Spawn(blocks);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CleanMap()
    {
        for (int y = 0; y < mapSize.y; ++y)
        {
            for (int x = 0; x < mapSize.x; ++x)
            {
                for (int z = 0; z < mapSize.z; ++z)
                {
                    Destroy(blocks[x, y, z]);
                }
            }
        }
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
