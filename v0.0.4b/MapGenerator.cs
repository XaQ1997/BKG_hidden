using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private Dictionary<Vector2, Chunk> chunks;
    private Dictionary<Vector2, bool> chunkActive;

    [SerializeField] private Vector3Int mapSize = new Vector3Int(64, 256, 64);

    private uint seed;

    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private int renderDistance = 2;

    // Start is called before the first frame update
    private void Awake()
    {
        var blockMap = this.gameObject.GetComponent<BlockMap>();

        chunks = new Dictionary<Vector2, Chunk>();

        InitGame(blockMap.blockMap);
    }

    public void InitGame(Dictionary<int, Block> blockMap)
    {
        var gameSettings = this.gameObject.GetComponent<GameSettings>();

        seed = (uint)DateTime.Now.Ticks;

        Vector2 vector = new Vector2(0, 0);

        chunks[vector] = new Chunk(vector, new int?[16, 256, 16]);

        chunkManager.GenerateChunk(seed, chunks[vector], null, null);

        for(int x=0;x<renderDistance;++x)
            for(int z=0;z<renderDistance;++z)
            {
                if((x!=0||z!=0)&&(Mathf.Pow(x, 2)+Mathf.Pow(z, 2)<=renderDistance))
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
        foreach(var pos in chunks.Keys)
            if (chunkActive[pos] == true)
                chunkManager.Load(chunks[pos]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CleanMap()
    {
        chunks = new Dictionary<Vector2, Chunk>();
        chunkManager.chunkLoaded = new Dictionary<Vector2, bool>();
    }

    public Dictionary<Vector2, Chunk> Chunks()
    {
        return chunks;
    }

    public Vector3Int MapSize()
    {
        return mapSize;
    }
}
