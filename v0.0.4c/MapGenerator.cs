using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class MapGenerator : MonoBehaviour
{
    private GameObject[,,] blocks;
    private ChunkCollector chunks;
    
    [SerializeField] private Vector3Int mapSize = new Vector3Int(256, 256, 256);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(128, 0, 128);

    [SerializeField] private int seed=0;

    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private int generateDistance = 2;
    [SerializeField] private int renderDistance = 1;
    private Vector2Int playerPos = Vector2Int.zero;

    [SerializeField] private BlockMap blockMap;
    [SerializeField] private GameSettings gameSettings;

    // Start is called before the first frame update
    private void Awake()
    {
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];

        InitGame();
    }

    private void Update()
    {
        Vector2Int pos = new Vector2Int((int)transform.position.x / 16, (int)transform.position.z / 16);

        if (pos!=playerPos/16)
        {
            chunkManager.ChangeChunk(pos);

            UpdateTerrain();
        }

        playerPos = new Vector2Int((int)transform.position.x, (int)transform.position.z);
    }

    private void UpdateTerrain()
    {
        chunks = chunkManager.Chunks();

        foreach (var chunk in chunks.Chunks)
        {
            for(int y=0;y<mapSize.y;++y)
            {
                for(int x=0;x<16;++x)
                {
                    for(int z=0;z<16;++z)
                    {
                        if(blocks[chunk.Value.Position.x * 16 + x + mapOffset.x, y + mapOffset.y, chunk.Value.Position.y * 16 + z + mapOffset.z])
                            Destroy(blocks[chunk.Value.Position.x * 16 + x + mapOffset.x, y + mapOffset.y, chunk.Value.Position.y * 16 + z + mapOffset.z]);
                    }
                }
            }

            if (chunk.Value.State == ChunkState.Loaded)
            {
                string[,,] tmp = new string[16, mapSize.y, 16];
                Vector2Int chunkPos = chunk.Value.Position;

                for (int y = 0; y < mapSize.y; ++y)
                {
                    for (int x = 0; x < 16; ++x)
                    {
                        for (int z = 0; z < 16; ++z)
                        {
                            if (chunk.Value.Loader.Blocks[x, y, z] != null)
                            {
                                Vector3Int pos = new Vector3Int(chunkPos.x * 16 + x, y + mapOffset.y, chunkPos.y * 16 + z);

                                blocks[chunkPos.x * 16 + x + mapOffset.x, y + mapOffset.y, chunkPos.y * 16 + z + mapOffset.z] = Instantiate(blockMap.blockMap[chunk.Value.Loader.Blocks[x, y, z]].BlockPrefab, pos, Quaternion.identity);
                            }
                        }
                    }
                }
            }
        }
    }

    public void InitGame()
    {
        seed = (int)DateTime.Now.Ticks;

        seed = new Random((uint)seed).NextInt(-(int)Mathf.Pow(2, 7), (int)Mathf.Pow(2, 7) + 1);

        chunks = chunkManager.Chunks();

        foreach(var chunk in chunks.Chunks)
        {
            if (chunk.Value.State == ChunkState.Loaded)
            {
                string[,,] tmp = new string[16, mapSize.y, 16];
                Vector2Int chunkPos = chunk.Value.Position;

                for (int y = 0; y < mapSize.y; ++y)
                {
                    for (int x = 0; x < 16; ++x)
                    {
                        for (int z = 0; z < 16; ++z)
                        {
                            if (chunk.Value.Loader.Blocks[x, y, z] != null)
                            {
                                Vector3Int pos = new Vector3Int(chunkPos.x*16+x, y+mapOffset.y, chunkPos.y*16+z);

                                blocks[chunkPos.x*16+x+mapOffset.x, y+mapOffset.y, chunkPos.y*16+z+mapOffset.z]=Instantiate(blockMap.blockMap[chunk.Value.Loader.Blocks[x, y, z]].BlockPrefab, pos, Quaternion.identity);
                            }
                        }
                    }
                }
            }
        }

        for (int h = mapSize.y-mapOffset.y-1; h >= -mapOffset.y; --h)
        {
            if (Block(0, h, 0, "30"))
            {
                gameSettings.Spawn(h);

                break;
            }
        }
    }

    public void CleanMap()
    {
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];
        chunks = new ChunkCollector();
    }

    public GameObject[,,] Blocks()
    {
        return blocks;
    }

    public Vector3Int MapSize()
    {
        return mapSize;
    }

    public Vector3Int MapOffset()
    {
        return mapOffset;
    }

    public int Seed()
    {
        return seed;
    }

    public int GenerateDistance()
    {
        return generateDistance;
    }

    public int RenderDistance()
    {
        return renderDistance;
    }

    public bool Block(int x, int y, int z, string id)
    {
        if (chunkManager.Chunks().Chunks[new Vector2Int(x/16, z/16)].Loader.Blocks[x%16, y, z%16] == id)
            return true;

        return false;
    }
}
