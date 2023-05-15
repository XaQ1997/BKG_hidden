using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private Dictionary<Vector2, Chunk> chunks;
    private Dictionary<Vector2, bool> chunkActive;

    private GameObject[,,] blocks;

    [SerializeField] private Vector3Int mapSize = new Vector3Int(64, 256, 64);
    [SerializeField] private Vector3Int mapOffset = new Vector3Int(32, 0, 32);

    private uint seed;

    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private int renderDistance = 2;

    [SerializeField] private BlockMap blockMap;

    // Start is called before the first frame update
    private void Awake()
    {
        chunks = new Dictionary<Vector2, Chunk>();
        chunkActive = new Dictionary<Vector2, bool>();
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];

        InitGame();
    }

    public void InitGame()
    {
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        seed = (uint)DateTime.Now.Ticks;

        Vector2 vector = new Vector2(0, 0);

        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
        chunkActive[vector]=true;

        chunkManager.GenerateChunk(seed, chunks[vector], null, null);

        for(int x=0;x<renderDistance;++x)
            for(int z=0;z<renderDistance;++z)
            {
                if((x!=0||z!=0)&&(Mathf.Pow(x, 2)+Mathf.Pow(z, 2)<=Mathf.Pow(renderDistance, 2)))
                {
                    vector = new Vector2(x, z);

                    chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                    chunkActive[vector] = true;

                    int verticalOffset = 0;

                    if (x <= z)
                        verticalOffset = chunks[new Vector2(x, z - 1)].verticalOffsets[0];
                    else
                        verticalOffset = chunks[new Vector2(x - 1, z)].verticalOffsets[0];

                    chunkManager.GenerateChunk(seed, chunks[vector], verticalOffset, 0);

                    vector = new Vector2(-x, z);

                    chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                    chunkActive[vector] = true;

                    if (x <= z)
                        verticalOffset = chunks[new Vector2(-x, z - 1)].verticalOffsets[1];
                    else
                        verticalOffset = chunks[new Vector2(1-x, z)].verticalOffsets[1];

                    chunkManager.GenerateChunk(seed, chunks[vector], verticalOffset, 1);

                    vector = new Vector2(-x, -z);

                    chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                    chunkActive[vector] = true;

                    if (x <= z)
                        verticalOffset = chunks[new Vector2(-x, 1-z)].verticalOffsets[2];
                    else
                        verticalOffset = chunks[new Vector2(1-x, -z)].verticalOffsets[2];

                    chunkManager.GenerateChunk(seed, chunks[vector], verticalOffset, 2);

                    vector = new Vector2(x, -z);

                    chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);
                    chunkActive[vector] = true;

                    if (x <= z)
                        verticalOffset = chunks[new Vector2(x, 1 - z)].verticalOffsets[3];
                    else
                        verticalOffset = chunks[new Vector2(x - 1, -z)].verticalOffsets[3];

                    chunkManager.GenerateChunk(seed, chunks[vector], verticalOffset, 3);
                }
            }

        Load();
    }

    public void Load()
    {
        foreach(var chunk in chunks.Keys)
        {
            if(chunkActive[chunk]==true&&chunkManager.chunkLoaded[chunk]==false)
            {
                chunkManager.chunkLoaded[chunk] = true;

                var chunkBlocks = chunks[chunk].Blocks;

                for (int y = 0; y < mapSize.y; ++y)
                    for (int x = 0; x < 16; ++x)
                        for (int z = 0; z < 16; ++z)
                            if (chunkBlocks[x, y, z] != null)
                                blocks[(int)chunk.x - mapOffset.x + x, y - mapOffset.y, (int)chunk.y - mapOffset.z + z] = Instantiate(blockMap.blockMap[(int)chunkBlocks[x, y, z]].BlockPrefab, new Vector3((int)chunk.x - mapOffset.x + x, y - mapOffset.y, (int)chunk.y - mapOffset.z + z), Quaternion.identity);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CleanMap()
    {
        chunks = new Dictionary<Vector2, Chunk>();

        chunkManager.chunkLoaded = new Dictionary<Vector2, bool>();
        chunkActive = new Dictionary<Vector2, bool>();
        blocks = new GameObject[mapSize.x, mapSize.y, mapSize.z];
    }

    public Dictionary<Vector2, Chunk> Chunks()
    {
        return chunks;
    }

    public GameObject[,,] Blocks()
    {
        return blocks;
    }

    public Vector3Int MapSize()
    {
        return mapSize;
    }
}
